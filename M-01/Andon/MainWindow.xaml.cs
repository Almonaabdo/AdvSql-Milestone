using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Media;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;
using System.Windows.Threading;

namespace Andon
{
    public partial class MainWindow : Window
    {
        private string? connectionString = "Data Source=localhost;Initial Catalog=advsql-milestone-2;Integrated Security=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        private SqlDataAdapter adapter = new SqlDataAdapter();
        private DataTable dataTable = new DataTable();
        private DispatcherTimer refreshTimer;
        Dictionary<string, int> partCounts = new Dictionary<string, int>
        {
            { "Bezel", 0 },
            { "Bulb", 0 },
            { "Harness", 0 },
            { "Housing", 0 },
            { "Lens", 0 },
            { "Reflector", 0 }
        };


        public MainWindow()
        {
            InitializeComponent();
            LoadTable("APP_PART");

            // bg timer
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(2);
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }


        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadTable("APP_PART");
        }

        /*
        * Name: void LoadTable()
        * Called when the connection string is valid and the main window is in control 
        * it takes a table name and builds a querry to return table data and bind it to the front end
        */
        private void LoadTable(string tableName)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // get table data
                    string query = @"SELECT * FROM " + tableName + ";";

                    adapter = new SqlDataAdapter(query, conn);
                    adapter.Fill(dataTable);

                    // loop thru rows in the table fetched from the db
                    foreach (DataRow row in dataTable.Rows)
                    {
                        string partName = row["name"].ToString();
                        if (partCounts.ContainsKey(partName))
                        {
                            partCounts[partName] = Convert.ToInt32(row["binCapacity"]);
                        }
                    }

                    // update parts progress bar value
                    Harness.Value = partCounts["Harness"];
                    Reflector.Value = partCounts["Reflector"];
                    Housing.Value = partCounts["Housing"];
                    Lens.Value = partCounts["Lens"];
                    Bulb.Value = partCounts["Bulb"];
                    Bezel.Value = partCounts["Bezel"];

                    // update runner signal
                    indHarness.Fill = partCounts["Harness"] <= 5 ? Brushes.Red : partCounts["Harness"] <= 10 ? Brushes.Orange : Brushes.Green;
                    indReflector.Fill = partCounts["Reflector"] <= 5 ? Brushes.Red : partCounts["Reflector"] <= 10 ? Brushes.Orange : Brushes.Green;
                    indHousing.Fill = partCounts["Housing"] <= 5 ? Brushes.Red : partCounts["Housing"] <= 10 ? Brushes.Orange : Brushes.Green;
                    indLens.Fill = partCounts["Lens"] <= 5 ? Brushes.Red : partCounts["Lens"] <= 10 ? Brushes.Orange : Brushes.Green;
                    indBulb.Fill = partCounts["Bulb"] <= 5 ? Brushes.Red : partCounts["Bulb"] <= 10 ? Brushes.Orange : Brushes.Green;
                    indBezel.Fill = partCounts["Bezel"] <= 5 ? Brushes.Red : partCounts["Bezel"] <= 10 ? Brushes.Orange : Brushes.Green;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

    }
}