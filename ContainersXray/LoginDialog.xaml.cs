using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ContainersXray
{
    /// <summary>
    /// LoginDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class LoginDialog : Window
    {
        private MainWindow mainWindow;
        private ObservableCollection<ContainerRecord> Records { get; set; }
        private Stopwatch sw = new System.Diagnostics.Stopwatch();

        public LoginDialog(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.Records = new ObservableCollection<ContainerRecord>();
            containerList.DataContext = this.Records;
            containerList.SelectionMode = SelectionMode.Single;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            hostName.Focus();
        }

        private void login_Click(object sender, RoutedEventArgs e)
        {
            SshTerminal.hostName = hostName.Text;
            SshTerminal.loginUserName = loginUserName.Text;
            SshTerminal.loginPassword = loginPassword.Password;
            SshTerminal.execUserName = execUserName.Text;
            SshTerminal.execPassword = execPassword.Password;
            bool isConnected = SshTerminal.login();
            if (isConnected)
            {
                listContainers();
                containerList.SelectedIndex = 0;
                containerList.Focus();
            }
        }

        private void listContainers()
        {
            Records.Clear();
            string[] cmdretline = Regex.Split(SshTerminal.exec("docker container ls"), "\n");
            foreach (var line in cmdretline)
            {
                var l = Regex.Replace(line, "\r", "");
                if (l.StartsWith("CONTAINER") || string.IsNullOrEmpty(l))
                {
                    continue;
                }
                string[] cmdret = Regex.Split(l, "[ ]{1,}");
                Console.WriteLine(cmdret[0]);
                Console.WriteLine(cmdret[1]);
                Console.WriteLine(cmdret[cmdret.Length-1]);
                Records.Add(new ContainerRecord { containerName = cmdret[cmdret.Length - 1], image = cmdret[1], containerID = cmdret[0] });
            }
        }

        private void loginPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                loginPassword_LostFocus(null, null);
                login_Click(null, null);
            }
        }

        private void execPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                login_Click(null, null);
            }
        }

        private void ok_Click(object sender, RoutedEventArgs e)
        {
            if(containerList.SelectedIndex >= 0)
            {
                mainWindow.isConnected = true;
                SshTerminal.containerName = ((ContainerRecord)containerList.SelectedItem).containerName;
                SshTerminal.containerID = ((ContainerRecord)containerList.SelectedItem).containerID;
                SshTerminal.imageName = ((ContainerRecord)containerList.SelectedItem).image;
                SshTerminal.containerLogin();
                this.Close();
            }
        }

        private void containerList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ok_Click(null, null);
            }
        }

        private void loginPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            loginPassword.SelectAll();
        }

        private void execPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            execPassword.SelectAll();
        }

        private void loginPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(execPassword.Password))
            {
                execPassword.Password = loginPassword.Password;
            }
        }

        private void loginUserName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(execUserName.Text))
            {
                execUserName.Text = loginUserName.Text;
            }
        }
    }
}
