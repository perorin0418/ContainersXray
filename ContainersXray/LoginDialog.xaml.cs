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

        private async void login_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    login.Content = "処理中";
                    mainWindow.hostName = hostName.Text;
                    mainWindow.userName = userName.Text;
                    mainWindow.password = password.Password;
                }));
                Boolean isConnected = mainWindow.connect();
                this.Dispatcher.Invoke((Action)(() =>
                {
                    if (isConnected)
                    {
                        listContainers();
                        containerList.SelectedIndex = 0;
                        containerList.Focus();
                    }
                    login.Content = "ログイン";
                }));
            });
        }

        private void listContainers()
        {
            Records.Clear();
            sw.Restart();
            string[] cmdretline = Regex.Split(mainWindow.client.CreateCommand("docker container ls").Execute(), "\n");
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            Console.WriteLine($"コマンド実行にかかった時間 {ts.Hours}時間 {ts.Minutes}分 {ts.Seconds}秒 {ts.Milliseconds}ミリ秒");
            foreach (var line in cmdretline)
            {
                if (line.StartsWith("CONTAINER") || string.IsNullOrEmpty(line))
                {
                    continue;
                }
                string[] cmdret = Regex.Split(line, "[ ]{1,}");
                Console.WriteLine(cmdret[0]);
                Console.WriteLine(cmdret[1]);
                Console.WriteLine(cmdret[cmdret.Length-1]);
                Records.Add(new ContainerRecord { containerName = cmdret[cmdret.Length - 1], image = cmdret[1], containerID = cmdret[0] });
            }
        }

        private void Password_PreviewKeyDown(object sender, KeyEventArgs e)
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
                mainWindow.containerName = ((ContainerRecord)containerList.SelectedItem).containerName;
                mainWindow.containerID = ((ContainerRecord)containerList.SelectedItem).containerID;
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
    }
}
