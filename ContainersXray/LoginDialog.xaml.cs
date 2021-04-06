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
        private MainWindow MainWindow;
        private ObservableCollection<ContainerRecord> Records { get; set; }

        public LoginDialog(MainWindow mainWindow)
        {
            InitializeComponent();
            this.MainWindow = mainWindow;
            this.Records = new ObservableCollection<ContainerRecord>();
            ContainerList.DataContext = this.Records;
            ContainerList.SelectionMode = SelectionMode.Single;
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            HostName.Focus();
            foreach(var host in SshTerminal.GetHostList())
            {
                HostName.Items.Add(host);
            }            
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            SshTerminal.HostName = HostName.Text;
            SshTerminal.LoginUserName = LoginUserName.Text;
            SshTerminal.LoginPassword = LoginPassword.Password;
            SshTerminal.ExecUserName = ExecUserName.Text;
            SshTerminal.ExecPassword = ExecPassword.Password;
            bool isConnected = SshTerminal.Login();
            if (isConnected)
            {
                ListContainers();
                ContainerList.SelectedIndex = 0;
                ContainerList.Focus();
                if ((bool)UserPasswdSave.IsChecked)
                {
                    SshTerminal.SaveHost(SshTerminal.HostName);
                    SshTerminal.SaveUserPasswd(SshTerminal.HostName, SshTerminal.LoginUserName, SshTerminal.LoginPassword);
                    SshTerminal.SaveUserPasswd(SshTerminal.HostName, SshTerminal.ExecUserName, SshTerminal.ExecPassword);
                }
            }
        }

        private void ListContainers()
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
                Records.Add(new ContainerRecord { ContainerName = cmdret[cmdret.Length - 1], Image = cmdret[1], ContainerID = cmdret[0] });
            }
        }

        private void LoginPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginPassword_LostFocus(null, null);
                Login_Click(null, null);
            }
        }

        private void ExecPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Login_Click(null, null);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if(ContainerList.SelectedIndex >= 0)
            {
                MainWindow.IsConnected = true;
                SshTerminal.ContainerName = ((ContainerRecord)ContainerList.SelectedItem).ContainerName;
                SshTerminal.ContainerID = ((ContainerRecord)ContainerList.SelectedItem).ContainerID;
                SshTerminal.ImageName = ((ContainerRecord)ContainerList.SelectedItem).Image;
                SshTerminal.ContainerLogin();
                this.Close();
            }
        }

        private void ContainerList_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Ok_Click(null, null);
            }
        }

        private void LoginPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            LoginPassword.SelectAll();
        }

        private void ExecPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            ExecPassword.SelectAll();
        }

        private void LoginPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ExecPassword.Password))
            {
                ExecPassword.Password = LoginPassword.Password;
            }
        }

        private void LoginUserName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ExecUserName.Text))
            {
                ExecUserName.Text = LoginUserName.Text;
            }
            if (!string.IsNullOrEmpty(LoginUserName.Text))
            {
                LoginPassword.Password = SshTerminal.GetPasswd(HostName.Text, LoginUserName.Text);
            }
        }

        private void HostName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(HostName.Text))
            {
                foreach(var usr in SshTerminal.GetUserList(HostName.Text))
                {
                    LoginUserName.Items.Add(usr);
                    ExecUserName.Items.Add(usr);
                }
            }
        }

        private void ExecUserName_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ExecUserName.Text))
            {
                ExecPassword.Password = SshTerminal.GetPasswd(HostName.Text, ExecUserName.Text);
            }
        }
    }
}
