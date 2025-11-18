using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;
using System;

namespace WorkStation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? connectionString = "Data Source=localhost;Initial Catalog=advsql-milestone-2;Integrated Security=True;TrustServerCertificate=True;";
        private SqlDataAdapter adapter = new SqlDataAdapter();
        private DataTable dataTable = new DataTable();
        private string[] parts = { "Housing", "Reflector", "Harness", "Bulb", "Lens", "Bezel" };
        SqlConnection connection = new SqlConnection();
        DateTime startedAt = DateTime.Now;
        DateTime finishedAt = DateTime.Now;


        public MainWindow()
        {
            InitializeComponent();
        }


        /*
         * FUNCTION : test_connection()
         * PURPOSE  : tests connection to servera nd database by the provided connection string in the APP config
         */
        private void test_connection()
        {
            try
            {
                if (easyConnect.IsChecked == false)
                {
                    connectionString = "Data Source=6.tcp.ngrok.io,16946;Initial Catalog=advsql-milestone-2;User ID=abdal;Password=abdal;Encrypt=False;TrustServerCertificate=True;";
                }
                connection = new SqlConnection(connectionString);
                connection.Open();
                //connection.Close();
                connectionButton.Visibility= Visibility.Hidden;
                onlineStatus.Visibility = Visibility.Visible;
                easyConnect.Visibility = Visibility.Hidden;
                StartStation.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private string read_quantity(string partName)
        {
            string query = "SELECT binCapacity FROM APP_PART WHERE Name = @desc";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@desc", partName);

            try
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        //MessageBox.Show($"Quantity: {reader["value"]}");
                        return reader["binCapacity"].ToString();
                    }
                    else
                    {
                        MessageBox.Show("No matching config found.");
                    }
                }
                return "null";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "null";
            }
        }

        private void callProcedure(string procName, int stationID, int partID, string partName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                using (SqlCommand command = new SqlCommand(procName, conn))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // parameters for the stored procedure
                    //command.Parameters.AddWithValue("@StationID", stationID);
                    //command.Parameters.AddWithValue("@PartID", partID);

                    conn.Open();
                    command.ExecuteNonQuery();

                    string quantity = read_quantity(partName);

                    MessageBox.Show($"Used a {partName} from the bin\n Parts left: {quantity}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void finalizeAssembly(int stationId, int workerId, DateTime startTime)
        {
            Random rand = new Random();

            int number = rand.Next(1, 101);
            char result;

            // 20% random chance of the part failing
            if (number >= 1 && number <= 21)
            {
                result = 'F';
            }
            else 
            {
                result = 'P';
            }

            try
            {

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    string sql = @"
                        INSERT INTO APP_ASSEMBLY (StationID, WorkerID, StartedAt, FinishedAt, Result)
                        VALUES (@StationID, @WorkerID, @StartedAt, @FinishedAt, @Result);
                    ";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@StationID", stationId);
                        cmd.Parameters.AddWithValue("@WorkerID", workerId);
                        cmd.Parameters.AddWithValue("@StartedAt", startTime);
                        cmd.Parameters.AddWithValue("@FinishedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Result", result); 
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            StartStation.Content = "Running...";
            finalizeAssembly(1, 10, DateTime.Now);
            await Task.Delay(2000);
            for (int i = 0; i < parts.Length; i++)
            {
                callProcedure("DecrementPartCount", i, i, parts[i]);

                await Task.Delay(5000);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            read_quantity("Lens");
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            test_connection();
        }
    }
}