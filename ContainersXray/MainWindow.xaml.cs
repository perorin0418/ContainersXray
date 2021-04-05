using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ContainersXray
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public string hostName { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string containerID { get; set; }
        public string containerName { get; set; }
        private DirTreeRecord rootDir { get; set; }
        private DirTreeRecord currentDir { get; set; }
        public SshClient client { get; set; }
        public Boolean isConnected { get; set; }
        private ObservableCollection<ExplorerRecord> eRecords { get; set; }
        private ObservableCollection<DirTreeRecord> dRecords { get; set; }
        private Stopwatch sw = new System.Diagnostics.Stopwatch();

        public MainWindow()
        {
            InitializeComponent();
            this.eRecords = new ObservableCollection<ExplorerRecord>();
            this.dRecords = new ObservableCollection<DirTreeRecord>();
            fileList.DataContext = this.eRecords;
            dirTree.ItemsSource = this.dRecords;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if(!isConnected)
            {
                LoginDialog ld = new LoginDialog(this);
                ld.Owner = this;
                ld.ShowDialog();
                refreshDir(null, "/");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (client != null && client.IsConnected)
            {
                client.Disconnect();
                Console.WriteLine("Disconnected");
            }
        }

        public Boolean connect()
        {
            try
            {
                client = new SshClient(hostName, userName, password);
                client.Connect();
            }catch(Exception e)
            {
                return false;
            }

            Console.WriteLine($"Connect: {client.IsConnected}");
            return client.IsConnected;
        }

        private void refreshDir(DirTreeRecord parent, string dir)
        {
            refreshDir(parent, dir, true);
        }

        private void refreshDir(DirTreeRecord parent, string dir, Boolean addRec)
        {
            if (isConnected)
            {
                if(currentDir != null && string.IsNullOrEmpty(dir) && currentDir.getFullPath().Equals(parent.getDir(dir).getFullPath()))
                {
                    Console.WriteLine("skip");
                    return;
                }
                eRecords.Clear();
                sw.Restart();
                string[] cmdretline;
                if(parent == null)
                {
                    path.Text = dir;
                    cmdretline = Regex.Split(client.CreateCommand("docker container exec " + containerName + " ls -la " + dir).Execute(), "\n");
                }
                else
                {
                    path.Text = parent.getFullPath() + "/" + dir;
                    cmdretline = Regex.Split(client.CreateCommand("docker container exec " + containerName + " ls -la " + parent.getFullPath() + "/" + dir).Execute(), "\n");
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                Console.WriteLine($"コマンド実行にかかった時間 {ts.Hours}時間 {ts.Minutes}分 {ts.Seconds}秒 {ts.Milliseconds}ミリ秒");
                var dRecord = new DirTreeRecord();
                dRecord.name = dir;
                dRecord.parent = parent;
                foreach (var line in cmdretline)
                {
                    if (line.StartsWith("合計") || line.StartsWith("total") || string.IsNullOrEmpty(line))
                    {
                        continue;
                    }
                    string[] cmdret = Regex.Split(line, "[ ]{1,}");
                    var eRecord = new ExplorerRecord();
                    eRecord.permission = cmdret[0];
                    eRecord.owner = cmdret[2] + " : " + cmdret[3];
                    if (eRecord.permission.StartsWith("d"))
                    {
                        eRecord.size = "";
                    }
                    else
                    {
                        eRecord.size = cmdret[4];
                    }
                    eRecord.updatedAt = cmdret[5] + " " + cmdret[6] + " " + cmdret[7];
                    eRecord.fileName = cmdret[8];
                    Console.WriteLine(line);
                    if(!eRecord.fileName.Equals(".") && !eRecord.fileName.Equals(".."))
                    {
                        eRecords.Add(eRecord);
                        if (eRecord.permission.StartsWith("d"))
                        {
                            if (addRec)
                            {
                                if(currentDir == null)
                                {
                                    var dBuf = new DirTreeRecord { parent = dRecord, name = eRecord.fileName };
                                    dRecord.dirs.Add(dBuf);
                                }
                                else
                                {
                                    var dBuf = new DirTreeRecord { parent = currentDir.getDir(dir), name = eRecord.fileName };
                                    currentDir.getDir(dir).dirs.Add(dBuf);
                                }
                            }
                        }
                    }
                }
                if (addRec)
                {
                    if (currentDir == null)
                    {
                        rootDir = dRecord;
                        currentDir = dRecord;
                        dRecords.Add(rootDir);
                    }
                    else
                    {
                        currentDir = currentDir.getDir(dir);
                    }
                }
                else
                {
                    currentDir = parent;
                }
            }
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader columnHeader = sender as GridViewColumnHeader;
            this.SortListView(fileList, columnHeader.Tag as String);
        }

        private void SortListView(ListView listView, String tag)
        {
            if (listView.Items.Count < 2)
            {
                return;
            }

            ListSortDirection direction;
            SortDescription sortDescription;
            if (listView.Items.SortDescriptions.Count == 0)
            {
                direction = ListSortDirection.Descending;
                sortDescription = new SortDescription(tag, direction);
                listView.Items.SortDescriptions.Add(sortDescription);
                return;
            }

            if (listView.Items.SortDescriptions.Last().Direction == ListSortDirection.Ascending)
            {
                direction = ListSortDirection.Descending;
            }
            else
            {
                direction = ListSortDirection.Ascending;
            }

            listView.Items.SortDescriptions.Clear();
            sortDescription = new SortDescription(tag, direction);
            listView.Items.SortDescriptions.Add(sortDescription);
        }

        private void fileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExplorerRecord targetItem = (ExplorerRecord)fileList.SelectedItem;
            Console.WriteLine(targetItem.fileName);
            if (targetItem.permission.StartsWith("d"))
            {
                refreshDir(currentDir, targetItem.fileName);
            }
        }

        private void dirTree_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(((DirTreeRecord)dirTree.SelectedItem).getFullPath());
            refreshDir(((DirTreeRecord)dirTree.SelectedItem).parent, ((DirTreeRecord)dirTree.SelectedItem).name);
        }
    }
}
