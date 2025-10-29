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
    
        private string connectionString;


        public MainWindow()
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                var connectingModal = new ConnectingModal
                {
                    Owner = this // makes it a child of MainWindow
                };

                connectingModal.ShowDialog();
                connectionString = connectingModal.ConnectionStatus;
                if (connectionString != null)
                {
                    LoadTable("APP_CONFIG", connectionString);
                }
            };          
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
                    string query = @"SELECT * FROM " + tableName + ";";

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

    }
}