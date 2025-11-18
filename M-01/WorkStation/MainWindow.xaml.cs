#nullable enable
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using Microsoft.Data.SqlClient;

namespace WorkStation
{
    public partial class MainWindow : Window
    {
        private string CurrentSkill() => (SkillBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Experienced";
        private string connectionString = "Data Source=localhost;Initial Catalog=advsql-milestone-2;Integrated Security=True;TrustServerCertificate=True;";


        private const double BaseSec = 60.0;  // experienced baseline
        private const double Jitter = 10.0;  // ±10%

        public MainWindow()
        {
            InitializeComponent();
        }

        private static double SkillFactor(string skill) => skill switch
        {
            "Rookie" => 1.50,   // 50% longer
            "Super" => 0.85,   // 15% faster
            _ => 1.00    // Experienced/default
        };

        private async Task<double> GetTimeScale()
        {
            const string sql = @"SELECT CAST(value AS float) FROM dbo.APP_CONFIG
                         WHERE configType='System' AND configDescription='TimeScale'";
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            var o = await cmd.ExecuteScalarAsync();
            return (o == null || o == DBNull.Value) ? 1.0 : Convert.ToDouble(o);
        }

        // Compute a single simulated cycle time (seconds)
        private double calcSimulationCycle(string skill)
        {
            double jitter = (new Random().NextDouble() * 2 - 1) * (Jitter / 100.0);
            return BaseSec * SkillFactor(skill) * (1.0 + jitter);
        }

        private static async Task<int> CreateAssembly(SqlConnection conn, SqlTransaction tx, int stationId, int workerId)
        {
            const string sql = @"
        INSERT INTO dbo.APP_ASSEMBLY (StationID, WorkerID)
        OUTPUT INSERTED.AssemblyID
        VALUES (@StationID, @WorkerID);";

            using var cmd = new SqlCommand(sql, conn, tx);
            cmd.Parameters.Add("@StationID", SqlDbType.Int).Value = stationId;
            cmd.Parameters.Add("@WorkerID", SqlDbType.Int).Value = workerId;

            var obj = await cmd.ExecuteScalarAsync();
            return (obj is int id) ? id : Convert.ToInt32(obj);
        }

        private async Task FinishAssembly(int assemblyId, bool fail)
        {
            const string sql = @"
        UPDATE dbo.APP_ASSEMBLY
        SET FinishedAt = SYSUTCDATETIME(),
            Result     = @res            -- 'P' pass, 'F' fail
        WHERE AssemblyID = @id;";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@res", SqlDbType.Char, 1).Value = fail ? "F" : "P";
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = assemblyId;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


        private async Task StartAssembly()
        {
            int stationId = int.TryParse(StationIdBox.Text, out var sid) ? sid : 1;

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            using var tx = conn.BeginTransaction();

            try
            {
                //  decrement parts (proc returns single cell 'OK' or 'OUT_OF_STOCK')
                using (var cmd = new SqlCommand("dbo.DecrementPartCount", conn, tx))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    var obj = await cmd.ExecuteScalarAsync();
                    var status = (obj == null || obj == DBNull.Value) ? "ERR" : obj.ToString();
                }

                //  create assembly in the SAME transaction
                int workerId = 0; // place holder for now
                int assemblyId = await CreateAssembly(conn, tx, stationId, workerId);

                tx.Commit();

                double simulationCyle = calcSimulationCycle(CurrentSkill());
                double timeScale = await GetTimeScale();
                int waitMs = (int)Math.Round((simulationCyle / Math.Max(0.0001, timeScale)) * 1000.0);


                LogList.Items.Insert(0, $"Start → OK, Assembly #{assemblyId}, Cycle={simulationCyle:0.##}s (TS={timeScale:0.##}x)");

                await Task.Delay(waitMs);


                // Decide pass/fail (hardcode defect rates)
                double defect = CurrentSkill() switch
                {
                    "Rookie" => 0.0085,
                    "Super" => 0.0015,
                    _ => 0.0050
                };
                bool fail = new Random().NextDouble() < defect;

                await FinishAssembly(assemblyId, fail);
                LogList.Items.Insert(0, $"Finish → Assembly #{assemblyId} {(fail ? "FAIL" : "GOOD")}");

            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* ignore */ }
                LogList.Items.Insert(0, "DB ERROR (Start): " + ex.Message);
            }
        }



        // for this instance, call proc to create station, seed bins 
        private async Task<int> StationForThisInstance()
        {

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.usp_CreateStationWithBins", conn)
            { CommandType = CommandType.StoredProcedure };

            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync()) throw new InvalidOperationException("No station returned.");

            int stationId = r.GetInt32(0);
            string code = r.GetString(1);

            StationIdBox.Text = stationId.ToString();
            LogList.Items.Insert(0, $"Auto-created {code} (ID {stationId}) and seeded bins.");
            return stationId;
        }


        // upon window starting call method
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await StationForThisInstance();
            }
            catch (Exception ex)
            {
                LogList.Items.Insert(0, "DB ERROR (auto-station on load): " + ex.Message);
            }
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            try { await StartAssembly(); }
            catch (Exception ex)
            {
                LogList.Items.Insert(0, "DB ERROR (Start): " + ex.Message);
            }
        }

    }
}
