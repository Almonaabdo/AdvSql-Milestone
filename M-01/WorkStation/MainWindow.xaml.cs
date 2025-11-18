using Microsoft.Data.SqlClient;
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
        SqlConnection connection = new SqlConnection();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void read_quantity(string partName)
        {
            string query = "SELECT value FROM APP_CONFIG WHERE configDescription = @desc";

            SqlCommand command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@desc", partName);

            try
            {
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        MessageBox.Show($"Quantity: {reader["value"]}");
                    }
                    else
                    {
                        MessageBox.Show("No matching config found.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
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