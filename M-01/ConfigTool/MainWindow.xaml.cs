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
        private string sourceConnectionString = "Data Source=NULL;Initial Catalog=NULL;Integrated Security=True;TrustServerCertificate=True;";
        private string destConnectionString = "Data Source=NULL;Initial Catalog=NULL;Integrated Security=True;TrustServerCertificate=True;";

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

        private string _destDatabase;
        public string DestDatabase
        {
            get => _destDatabase;
            set
            {
                _destDatabase = value;
            }
        }

        private string _destTable;
        public string DestTable
        {
            get => _destTable;
            set
            {
                _destTable = value;
            }
        }
        private string _destServer;
        public string DestServer
        {
            get => _destServer;
            set
            {
                _destServer = value;
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
            DestDatabaseTextBox.TextChanged += DestDatabaseTextBox_TextChanged;
            DestTableTextBox.TextChanged += DestTableTextBox_TextChanged;
            SourceServerTextBox.TextChanged += SourceServerTextBox_TextChanged;
            DestServerTextBox.TextChanged += DestServerTextBox_TextChanged;
        }


        // start transfer UI button
        private void Btn_Start_Transfer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SourceServer) ||
                string.IsNullOrEmpty(SourceDatabase) ||
                string.IsNullOrEmpty(SourceTable) ||
                string.IsNullOrEmpty(DestServer) ||
                string.IsNullOrEmpty(DestDatabase) ||
                string.IsNullOrEmpty(DestTable))
            {
                MessageBox.Show("Please fill in all server, database, and table fields for Source and Dest.\nNote: Enter new table name in Destination if doesn't already exist!", "Error");
            }
            else
            {
                Perform_table_copy();
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
            if (string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(SourceServer))
            {
                MessageBox.Show("Source Database information is Empty.", "Error!");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////



        // DEST DATABASE
        private void DestDatabaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DestDatabase = DestDatabaseTextBox.Text;
            destConnectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + DestDatabase + ";Integrated Security=True;TrustServerCertificate=True;";
        }
        private void DestTableTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DestTable = DestTableTextBox.Text;
        }
        private void DestServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DestServer = DestServerTextBox.Text;
            destConnectionString = "Data Source=" + DestServer + ";Initial Catalog=" + DestDatabase + ";Integrated Security=True;TrustServerCertificate=True;";
        }

        private void Btn_Test_Dest_Db(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DestDatabase) || string.IsNullOrEmpty(DestServer))
            {
                MessageBox.Show("Dest Database information is Empty.", "Error!");
            }
            else
            {
                LoadDatabaseTables(1, destConnectionString);
            }
        }

        // function to copy data from source to dest and calls to create new table if didnt alr exist
        private void Perform_table_copy()
        {
            if (string.IsNullOrEmpty(SourceTable) || string.IsNullOrEmpty(DestTable))
            {
                MessageBox.Show("Source or destination table is empty.", "Error");
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (SqlConnection sourceConnection = new SqlConnection(sourceConnectionString))
                {
                    sourceConnection.Open();

                    string query = $"SELECT * FROM {SourceTable}";

                    using (SqlCommand commandSourceData = new SqlCommand(query, sourceConnection))
                    using (SqlDataReader reader = commandSourceData.ExecuteReader(CommandBehavior.SchemaOnly))
                    using (SqlConnection destinationConnection = new SqlConnection(destConnectionString))
                    {
                        destinationConnection.Open();

                        // check if dest table exists
                        bool destExists = false;
                        string checkQuery = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{DestTable}'";
                        using (SqlCommand checkCmd = new SqlCommand(checkQuery, destinationConnection))
                        {
                            destExists = (int)checkCmd.ExecuteScalar() > 0;
                        }

                        // create using copied source schema
                        if (!destExists)
                        {
                            DataTable schemaTable = reader.GetSchemaTable();
                            string createSql = createTable(DestTable, schemaTable);

                            using (SqlCommand createCmd = new SqlCommand(createSql, destinationConnection))
                            {
                                createCmd.ExecuteNonQuery();
                            }

                            MessageBox.Show($"Destination table '{DestTable}' did not exist and was created successfully.", "Info");
                        }

                        reader.Close();
                        commandSourceData.CommandText = query;
                        // copy data into new table
                        using (SqlDataReader dataReader = commandSourceData.ExecuteReader())
                        using (SqlBulkCopy bulkCopy = new SqlBulkCopy(destinationConnection))
                        {
                            bulkCopy.DestinationTableName = DestTable;

                            try
                            {
                                bulkCopy.WriteToServer(dataReader);
                                MessageBox.Show("Data copy completed successfully!", "Success");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error during bulk copy: {ex.Message}", "Error");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        // convert C# types to SQL
        private string GetSqlTypeFromType(Type type, int size)
        {
            if (type == typeof(string))
                return size > 0 && size < 4000 ? $"NVARCHAR({size})" : "NVARCHAR(MAX)";
            if (type == typeof(int))
                return "INT";
            if (type == typeof(long))
                return "BIGINT";
            if (type == typeof(short))
                return "SMALLINT";
            if (type == typeof(bool))
                return "BIT";
            if (type == typeof(DateTime))
                return "DATETIME";
            if (type == typeof(decimal))
                return "DECIMAL(18,2)";
            if (type == typeof(double))
                return "FLOAT";
            if (type == typeof(byte[]))
                return "VARBINARY(MAX)";

            return "NVARCHAR(MAX)";
        }

        // function to create table in dest database if table didnt already exist
        private string createTable(string tableName, DataTable schemaTable)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine($"CREATE TABLE [{tableName}] (");

            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                DataRow row = schemaTable.Rows[i];
                string columnName = row["ColumnName"].ToString();
                Type dataType = (Type)row["DataType"];
                int columnSize = row["ColumnSize"] != DBNull.Value ? Convert.ToInt32(row["ColumnSize"]) : 0;
                bool allowDBNull = row["AllowDBNull"] != DBNull.Value && (bool)row["AllowDBNull"];

                string sqlType = GetSqlTypeFromType(dataType, columnSize);
                string nullSpec = allowDBNull ? "NULL" : "NOT NULL";

                sql.Append($"[{columnName}] {sqlType} {nullSpec}");
                if (i < schemaTable.Rows.Count - 1)
                    sql.Append(",");
                sql.AppendLine();
            }

            sql.AppendLine(");");
            return sql.ToString();
        }

    }
}