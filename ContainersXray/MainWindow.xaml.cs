using LiteDB;
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
        public Boolean isConnected { get; set; }
        private string currentPath { get; set; }
        private ObservableCollection<ExplorerRecord> eRecords { get; set; }
        private ObservableCollection<DirTreeRecord> dRecords { get; set; }
        
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
                if (isConnected)
                {
                    SshTerminal.lsRoot();
                    currentPath = "/";
                    refreshExplorer();
                    refreshTree();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (isConnected)
            {
                SshTerminal.logout();
                Console.WriteLine("Disconnected");
            }
        }

        private void refreshExplorer()
        {
            using (LiteDatabase db = new LiteDatabase(@"Filename=ContainersXray.db;connection=shared"))
            {
                path.Text = currentPath;
                eRecords.Clear();
                var collection = db.GetCollection<FileEntry>("file_entry");
                string parentPath = currentPath == "/" ? null : currentPath;
                var feList = collection.Query()
                    .Where(rec => rec.containerId == SshTerminal.containerID && rec.parentPath == parentPath)
                    .ToList();
                foreach (var fe in feList)
                {
                    var er = new ExplorerRecord();
                    er.path = fe.path;
                    er.fileName = fe.name;
                    er.size = fe.size;
                    er.updatedAt = fe.updatedAt;
                    er.permission = fe.permission;
                    er.owner = fe.owner;
                    eRecords.Add(er);
                }
            }
        }

        private void refreshTree()
        {
            using (LiteDatabase db = new LiteDatabase(@"Filename=ContainersXray.db;connection=shared"))
            {
                dRecords.Clear();
                var collection = db.GetCollection<FileEntry>("file_entry");
                var feList = collection.Query()
                    .Where(rec => rec.containerId == SshTerminal.containerID
                        && rec.parentPath == null
                        && rec.permission.StartsWith("d"))
                    .ToList();
                foreach (var fe in feList)
                {
                    dRecords.Add(createTree(fe.path));
                }
            }
        }

        private DirTreeRecord createTree(string path)
        {
            var dirTreeRecord = new DirTreeRecord();
            dirTreeRecord.path = path;
            dirTreeRecord.name = path.Split('/')[path.Split('/').Length - 1];
            using (LiteDatabase db = new LiteDatabase(@"Filename=ContainersXray.db;connection=shared"))
            {
                var collection = db.GetCollection<FileEntry>("file_entry");
                var feList = collection.Query()
                    .Where(rec => rec.containerId == SshTerminal.containerID
                        && rec.parentPath == path
                        && rec.permission.StartsWith("d"))
                    .ToList();
                foreach (var fe in feList)
                {
                    dirTreeRecord.dirs.Add(createTree(fe.path));
                }
            }
            return dirTreeRecord;
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
            if (targetItem.permission.StartsWith("d"))
            {
                SshTerminal.ls(targetItem.path);
                currentPath = targetItem.path;
                refreshExplorer();
                refreshTree();
            }
        }

        private void dirTree_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DirTreeRecord targetItem = (DirTreeRecord)dirTree.SelectedItem;
            if(targetItem == null || currentPath == targetItem.path)
            {
                return;
            }
            currentPath = targetItem.path;
            SshTerminal.ls(currentPath);
            refreshExplorer();
            refreshTree();
        }

        private void up_MouseUp(object sender, MouseButtonEventArgs e)
        {
            currentPath = currentPath.Substring(0, currentPath.LastIndexOf('/'));
            if(currentPath == "")
            {
                currentPath = "/";
            }
            SshTerminal.ls(currentPath);
            refreshExplorer();
            refreshTree();
        }

        private void refresh_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SshTerminal.ls(currentPath);
            refreshExplorer();
            refreshTree();
        }

        private void outside_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(System.Environment.CurrentDirectory);
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = System.Environment.CurrentDirectory + @"\ContainersXray.exe";
            Process.Start(pInfo);
        }
    }
}
