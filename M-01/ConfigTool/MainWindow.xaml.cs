/*
 * File : MainWindow.xaml.cs
 * Developers: Abdurrahman Almouna, Yafet Tekleab
 * Overview: Main window of the applications. Allows user to configure the system's settings by altering the config table hosted the SQL server database.
 */



using Microsoft.Data.SqlClient;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SqlDataAdapter = Microsoft.Data.SqlClient.SqlDataAdapter;

namespace ConfigTool
{
    public partial class MainWindow : Window
    {

        private string? connectionString;
        private SqlDataAdapter adapter = new SqlDataAdapter();
        private DataTable dataTable = new DataTable();
        public MainWindow()
        {
            InitializeComponent();
            // load the connecting to server pop up window
            Loaded += (s, e) =>
            {
                // make the modal a child of MainWindow
                var connectingModal = new ConnectingModal
                {
                    Owner = this
                };

                connectingModal.ShowDialog();
                connectionString = connectingModal.ConnectionStatus;
                if (connectionString != null)
                {
                    LoadTable("APP_CONFIG");
                }
            };          
        }

        /*
        * Name: void LoadTable()
        * Called when the connection string is valid and the main window is in control 
        * it takes a table name and builds a querry to return table data and bind it to the front end datagrid
        */
        private void LoadTable(string tableName)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                SqlConnection conn = new SqlConnection(connectionString);

                conn.Open();

                // get table data
                string query = @"SELECT * FROM " + tableName + ";";

                adapter = new SqlDataAdapter(query, conn);
                adapter.Fill(dataTable);

                // bind data to UI grid
                configGrid.ItemsSource = dataTable.DefaultView;

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

        /*
        * Name: void ApplyButton_Click()
        * Called when the Apply button is clicked on the UI
        * It refers to the update SQL command and invokes the adapter update method sp that edited field on the UI are committed to the database
        */
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // check if grid has validation errors
                var errors = (from c in
                  (from object i in configGrid.ItemsSource
                   select configGrid.ItemContainerGenerator.ContainerFromItem(i))
                   where c != null
                   select Validation.GetHasError(c))
                .FirstOrDefault(x => x);

                if (errors)
                {
                    MessageBox.Show("One of the values is invalid", "Warning");
                }
                else
                {
                    adapter.UpdateCommand = new SqlCommandBuilder(adapter).GetUpdateCommand();
                    adapter.Update(dataTable);
                    MessageBox.Show("Changes have been applied", "Success");
                }     
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Warning");
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}