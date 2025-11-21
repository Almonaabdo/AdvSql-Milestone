/*
 * File : MainWindow.xaml.cs
 * Developers: Yafet Tekleab, Abdurrahman Almouna
 * Overview: UI for the database simulation based application, allows to select worker and start or stop assembly. logs most important logs. Connection to the database can be changed from App.config
 */

#nullable enable
using System;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;

namespace WorkStation
{
    public partial class MainWindow : Window
    {
        private sealed class Worker
        {
            public int WorkerId { get; set; }
            public int StationId { get; set; }
            public string Skill { get; set; } = "";

            public override string ToString() => $"Worker {WorkerId} ({Skill})";
        }


        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["advsql"]!.ConnectionString;

        private readonly string[] parts = { "Housing", "Reflector", "Harness", "Bulb", "Lens", "Bezel" };

        private const double BaseSec = 60.0;  // experienced baseline (seconds)
        private const double Jitter = 10.0;  // ±10%

        private readonly Random _rng = new Random();
        private bool _isRunning;
        private int _cycleNumber;            // counts assemblies this session


        public MainWindow()
        {
            InitializeComponent();
        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                double timeScale = await GetTimeScale();
                TimeScaleText.Text = timeScale.ToString(" 0.##", CultureInfo.InvariantCulture);
                await StationForThisInstance();
            }
            catch (Exception ex)
            {
                LogEvent("DB ERROR (auto-station on load): " + ex.Message);
            }
        }

        private async void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isRunning) return;

            if (WorkerBox.SelectedItem is not Worker)
            {
                MessageBox.Show("Please select a worker first.");
                return;
            }

            _isRunning = true;

            // UI → running
            AssembyStateText.Text = "Running...";
            Start.IsEnabled = false;
            StopBtn.IsEnabled = true;
            WorkerBox.IsEnabled = false;
            StationIdBox.IsEnabled = false;

            try
            {
                // continuous simulation until Stop is clicked
                while (_isRunning)
                {
                    await StartAssembly();   // one full cycle
                }
            }
            finally
            {
                _isRunning = false;

                // UI → back to idle
                AssembyStateText.Text = "Idle";
                Start.IsEnabled = true;
                StopBtn.IsEnabled = false;
                WorkerBox.IsEnabled = true;
                StationIdBox.IsEnabled = true;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopBtn.Content = "Stopping...";
            StopBtn.IsEnabled = false;
            _isRunning = false;
        }


        /*
         * Method : LogEvent()
         * Overview: Logs event to the UI with added timestaps for better details
         */
        private void LogEvent(string message)
        {
            string stamp = DateTime.Now.ToString("HH:mm:ss");
            LogList.Items.Insert(0, $"[{stamp}] {message}");
        }

        private static double SkillFactor(string skill) => skill switch
        {
            "Rookie" => 1.50,   // 50% longer
            "Super" => 0.85,   // 15% faster
            _ => 1.00    // Experienced/default
        };

        private double CalcSimulationCycle(string skill)
        {
            double jitter = (_rng.NextDouble() * 2 - 1) * (Jitter / 100.0);
            return BaseSec * SkillFactor(skill) * (1.0 + jitter);
        }

        private Worker CurrentWorker()
        {
            if (WorkerBox.SelectedItem is Worker w)
                return w;

            throw new InvalidOperationException("No worker selected.");
        }

        private int CurrentWorkerId() => CurrentWorker().WorkerId;

        private string CurrentSkill()
        {
            var skill = CurrentWorker().Skill;
            return string.IsNullOrWhiteSpace(skill) ? "Experienced" : skill;
        }


        private async Task<double> GetTimeScale()
        {
            const string sql = @"
            SELECT CAST(value AS float)
            FROM dbo.APP_CONFIG
            WHERE configType = 'System'
              AND configDescription = 'TimeScale';";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            await conn.OpenAsync();
            var o = await cmd.ExecuteScalarAsync();
            return (o == null || o == DBNull.Value) ? 1.0 : Convert.ToDouble(o);
        }

        private async Task<int> StationForThisInstance()
        {
            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand("dbo.usp_CreateStationWithBins", conn)
            { CommandType = CommandType.StoredProcedure };

            await conn.OpenAsync();
            using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
                throw new InvalidOperationException("No station returned.");

            int stationId = r.GetInt32(0);
            string code = r.GetString(1);

            StationIdBox.Text = stationId.ToString();
            LogEvent($"Auto-created {code} (ID {stationId}) and seeded bins.");

            await LoadWorkers();   // load workers into combo

            return stationId;
        }

        private async Task LoadWorkers()
        {
            var workers = new System.Collections.Generic.List<Worker>();

            const string sql = @"
            SELECT WorkerID, StationID, Skill
            FROM dbo.APP_WORKER
            ORDER BY WorkerID;";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);

            await conn.OpenAsync();
            using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                workers.Add(new Worker
                {
                    WorkerId = rdr.GetInt32(0),
                    StationId = rdr.IsDBNull(1) ? 0 : rdr.GetInt32(1),
                    Skill = rdr.IsDBNull(2) ? "" : rdr.GetString(2)
                });
            }

            WorkerBox.ItemsSource = workers;
        }


        private static async Task<int> CreateAssembly(
            SqlConnection conn,
            SqlTransaction tx,
            int stationId,
            int workerId)
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
                Result     = @res
            WHERE AssemblyID = @id;";

            using var conn = new SqlConnection(connectionString);
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@res", SqlDbType.Char, 1).Value = fail ? "F" : "P";
            cmd.Parameters.Add("@id", SqlDbType.Int).Value = assemblyId;

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


        /*
         * Method : StartAssembly()
         * Overview: generates stationID, assigns worker to station, and starts the assembly by getting the quantity first and then calls procedure to decrement parts by 1 and starts simulation
         */
        private async Task StartAssembly()
        {
            int stationId = int.TryParse(StationIdBox.Text, out var sid) ? sid : 1;
            int workerId = CurrentWorkerId();
            string skill = CurrentSkill();

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                _cycleNumber++;

                using (var command = new SqlCommand("DecrementPartCount", conn, tx))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    await command.ExecuteNonQueryAsync();
                }

                int assemblyId = await CreateAssembly(conn, tx, stationId, workerId);
                tx.Commit();

                double simulationCycle = CalcSimulationCycle(skill);
                double timeScale = await GetTimeScale();
                int waitMs = (int)Math.Round(
                    (simulationCycle / Math.Max(0.0001, timeScale)) * 1000.0
                );

                LogEvent(
                    $"Cycle {_cycleNumber}: START – Assembly #{assemblyId}, {CurrentWorker()}, " +
                    $"Duration ≈ {simulationCycle:0.##}s @ {timeScale:0.##}x");

                await Task.Delay(waitMs);

                double defect = skill switch
                {
                    "Rookie" => 0.0085,
                    "Super" => 0.0015,
                    _ => 0.0050
                };

                bool fail = _rng.NextDouble() < defect;

                await FinishAssembly(assemblyId, fail);
                LogEvent(
                    $"Cycle {_cycleNumber}: DONE – Assembly #{assemblyId} " +
                    $"{(fail ? "FAIL" : "GOOD")}");
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* ignore rollback failures */ }
                LogEvent("DB ERROR (Start): " + ex.Message);
            }
        }
    }
}
