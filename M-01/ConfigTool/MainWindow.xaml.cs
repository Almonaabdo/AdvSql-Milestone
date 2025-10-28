using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SqlConnection = Microsoft.Data.SqlClient.SqlConnection;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;
using Microsoft.Data.SqlClient;

namespace ConfigTool
{
    public partial class MainWindow : Window
    {
        private string sourceConnectionString = "Data Source=104.198.160.37;Initial Catalog=advsql-milestone;User ID=sqlserver;Password=1Milestone@;TrustServerCertificate=True;";
    

        // Binding properties to the front-end
        private string _sourceDatabase;
        public string SourceDatabase
        {
            get => _sourceDatabase;
            set
            {
                _sourceDatabase = value;
            }
        }

        private string _sourceTable;
        public string SourceTable
        {
            get => _sourceTable;
            set
            {
                _sourceTable = value;
            }
        }

        private string _sourceServer;
        public string SourceServer
        {
            get => _sourceServer;
            set
            {
                _sourceServer = value;
            }
        }


        public MainWindow()
        {
            InitializeComponent();

            // bind textInput change to methods
            SourceDatabaseTextBox.TextChanged += SourceDatabaseTextBox_TextChanged;
            SourceTableTextBox.TextChanged += SourceTableTextBox_TextChanged;
            SourceServerTextBox.TextChanged += SourceServerTextBox_TextChanged;

            LoadTable("temp table name", sourceConnectionString);
        }


        // start transfer UI button
        private void Btn_Start_Transfer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SourceServer) ||
                string.IsNullOrEmpty(SourceDatabase) ||
                string.IsNullOrEmpty(SourceTable))
            {
                MessageBox.Show("Please fill in all server, database, and table fields for Source and Dest.\nNote: Enter new table name in Destination if doesn't already exist!", "Error");
            }
            else
            {
                Console.WriteLine("temp");
            }
        }


        private void LoadDatabaseTables(int grid, string connectionString)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get all table names in the database
                    string query = @"SELECT TABLE_SCHEMA + '.' + TABLE_NAME AS TableName
                             FROM INFORMATION_SCHEMA.TABLES
                             WHERE TABLE_TYPE = 'BASE TABLE'";

                    Microsoft.Data.SqlClient.SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Bind to DataGrid
                    if (grid == 0)
                    {
                        sourceGrid.ItemsSource = dt.DefaultView;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadTable(string tableName, string connectionString)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();

                    // Get all table names in the database
                    string query = @"SELECT * FROM APP_CONFIG;";

                    Microsoft.Data.SqlClient.SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Bind to DataGrid
                    sourceGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // SOURCE DATABASE
        private void SourceDatabaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceDatabase = SourceDatabaseTextBox.Text;
            sourceConnectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";Integrated Security=True;TrustServerCertificate=True;";
        }
        private void SourceTableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceTable = SourceTableTextBox.Text;
        }
        private void SourceServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceServer = SourceServerTextBox.Text;
            sourceConnectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";Integrated Security=True;TrustServerCertificate=True;";
        }

        private void Btn_Test_Source_Db(object sender, RoutedEventArgs e)
        {
            //if (string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(SourceServer))
            //{
            //    MessageBox.Show("Source Database information is Empty.", "Error!");
            //}
            //else
            //{
                LoadTable("temp table name", sourceConnectionString);
            //}
        }
    }
}