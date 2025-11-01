/*
 * File : ConnectingModal.xaml.cs
 * Developers: Abdurrahman Almouna, Yafet Tekleab
 * Overview: This modal shows when the app first loads and allows user to connect to a server
 * Refrence : https://learn.microsoft.com/en-us/dotnet/desktop/wpf/windows/dialog-boxes-overview
 */

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace ConfigTool
{
    public partial class ConnectingModal : Window
    {

        private string? connectionString;
        public string ConnectionStatus { get; private set; }

        private string _sourceServer;
        public string SourceServer
        {
            get => _sourceServer;
            set
            {
                _sourceServer = value;
            }
        }
        private string _sourceDatabase;
        public string SourceDatabase
        {
            get => _sourceDatabase;
            set
            {
                _sourceDatabase = value;
            }
        }

        private string _sourceLogin;
        public string SourceLogin
        {
            get => _sourceLogin;
            set
            {
                _sourceLogin = value;
            }
        }


        private string _sourcePassword;
        public string SourcePassword
        {
            get => _sourcePassword;
            set
            {
                _sourcePassword = value;
            }
        }

        private string trustedCertificateValue = "True";



        public ConnectingModal()
        {
            InitializeComponent();
            if (DesignerProperties.GetIsInDesignMode(this)) return; 

            // Disable the x close button
            this.Closing += Modal_Closing;

            // bind textInput change to methods
            SourceServerTextBox.TextChanged += SourceServerTextBox_TextChanged;
            SourceDatabaseTextBox.TextChanged += SourceDatabaseTextBox_TextChanged;
            SourceLoginTextBox.TextChanged += SourceLoginTextBox_TextChanged;
            SourcePasswordTextBox.PasswordChanged += SourcePasswordTextBox_TextChanged;
            trustCertificateCheckbox.Checked += TrustCertificate_Checked;
            trustCertificateCheckbox.Unchecked += TrustCertificate_Unchecked;
            trustedCertificateValue = trustCertificateCheckbox.IsChecked == true ? "True" : "False";
        }



        private bool _allowClose = false;


        /*
        * Name: ConnectButton_Click()
        * tests connecting to a server with the provided user information using ADO.NET SqlConnection libraries
        */
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SourceServer) || string.IsNullOrEmpty(SourceDatabase) || string.IsNullOrEmpty(SourceLogin) || string.IsNullOrEmpty(SourcePassword))
            {
                MessageBox.Show("Please fill in all fields", "Error");
                return;
            }
            using (var connection = new SqlConnection(connectionString))
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    connection.Open();
                    _allowClose = true;
                    ConnectionStatus = connectionString;
                    Mouse.OverrideCursor = null;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message  , "Warning");
                    Mouse.OverrideCursor = null;
                }
            }

        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }


        /*
        * Name: Modal_Closing()
        * Overriden version of the Modal_Closing method to prevent user from accessing MainWindow without properlly connecting to a server first 
        */
        private void Modal_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {  
            if (!_allowClose)
            {
                e.Cancel = true;
            }
        }
        
        private void SourceServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceServer = SourceServerTextBox.Text;
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }

        private void SourceDatabaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceDatabase = SourceDatabaseTextBox.Text;
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }
        private void SourceLoginTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SourceLogin = SourceLoginTextBox.Text;
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }
        private void SourcePasswordTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            SourcePassword = SourcePasswordTextBox.Password;
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }
        private void TrustCertificate_Checked(object sender, RoutedEventArgs e)
        {
            trustedCertificateValue = "True";
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }

        private void TrustCertificate_Unchecked(object sender, RoutedEventArgs e)
        {
            trustedCertificateValue = "False";
            connectionString = "Data Source=" + SourceServer + ";Initial Catalog=" + SourceDatabase + ";User ID=" + SourceLogin + ";Password=" + SourcePassword + ";TrustServerCertificate=" + trustedCertificateValue + ";";
        }
    }
}