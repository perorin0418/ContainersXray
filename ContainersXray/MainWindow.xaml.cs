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
        public Boolean IsConnected { get; set; }
        private string CurrentPath { get; set; }
        private ObservableCollection<ExplorerRecord> ERecords { get; set; }
        private ObservableCollection<DirTreeRecord> DRecords { get; set; }
        
        public MainWindow()
        {
            InitializeComponent();
            this.ERecords = new ObservableCollection<ExplorerRecord>();
            this.DRecords = new ObservableCollection<DirTreeRecord>();
            FileList.DataContext = this.ERecords;
            DirTree.ItemsSource = this.DRecords;
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            if(!IsConnected)
            {
                LoginDialog ld = new LoginDialog(this);
                ld.Owner = this;
                ld.ShowDialog();
                if (IsConnected)
                {
                    SshTerminal.LsRoot();
                    CurrentPath = "/";
                    RefreshExplorer();
                    RefreshTree();
                }
                else
                {
                    this.Close();
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (IsConnected)
            {
                SshTerminal.Logout();
                Console.WriteLine("Disconnected");
            }
        }

        private void RefreshExplorer()
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbTempFile + ";connection=shared"))
            {
                Path.Text = CurrentPath;
                ERecords.Clear();
                var collection = db.GetCollection<FileEntry>("file_entry");
                string parentPath = CurrentPath == "/" ? null : CurrentPath;
                var feList = collection.Query()
                    .Where(rec => rec.ContainerID == SshTerminal.ContainerID && rec.ParentPath == parentPath)
                    .ToList();
                foreach (var fe in feList)
                {
                    var er = new ExplorerRecord();
                    er.Path = fe.Path;
                    er.FileName = fe.Name;
                    er.Size = fe.Size;
                    er.UpdatedAt = fe.UpdatedAt;
                    er.Permission = fe.Permission;
                    er.Owner = fe.Owner;
                    ERecords.Add(er);
                }
            }
        }

        private void RefreshTree()
        {
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbTempFile + ";connection=shared"))
            {
                DRecords.Clear();
                var collection = db.GetCollection<FileEntry>("file_entry");
                var feList = collection.Query()
                    .Where(rec => rec.ContainerID == SshTerminal.ContainerID
                        && rec.ParentPath == null
                        && rec.Permission.StartsWith("d"))
                    .ToList();
                foreach (var fe in feList)
                {
                    DRecords.Add(CreateTree(fe.Path));
                }
            }
        }

        private DirTreeRecord CreateTree(string path)
        {
            var dirTreeRecord = new DirTreeRecord();
            dirTreeRecord.Path = path;
            dirTreeRecord.Name = path.Split('/')[path.Split('/').Length - 1];
            using (LiteDatabase db = new LiteDatabase("Filename=" + SshTerminal.DbTempFile + ";connection=shared"))
            {
                var collection = db.GetCollection<FileEntry>("file_entry");
                var feList = collection.Query()
                    .Where(rec => rec.ContainerID == SshTerminal.ContainerID
                        && rec.ParentPath == path
                        && rec.Permission.StartsWith("d"))
                    .ToList();
                foreach (var fe in feList)
                {
                    dirTreeRecord.Dirs.Add(CreateTree(fe.Path));
                }
            }
            return dirTreeRecord;
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader columnHeader = sender as GridViewColumnHeader;
            this.SortListView(FileList, columnHeader.Tag as String);
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

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ExplorerRecord targetItem = (ExplorerRecord)FileList.SelectedItem;
            if (targetItem.Permission.StartsWith("d"))
            {
                SshTerminal.Ls(targetItem.Path);
                CurrentPath = targetItem.Path;
                RefreshExplorer();
                RefreshTree();
            }
        }

        private void DirTree_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DirTreeRecord targetItem = (DirTreeRecord)DirTree.SelectedItem;
            if(targetItem == null || CurrentPath == targetItem.Path)
            {
                return;
            }
            CurrentPath = targetItem.Path;
            SshTerminal.Ls(CurrentPath);
            RefreshExplorer();
            RefreshTree();
        }

        private void Up_MouseUp(object sender, MouseButtonEventArgs e)
        {
            CurrentPath = CurrentPath.Substring(0, CurrentPath.LastIndexOf('/'));
            if(CurrentPath == "")
            {
                CurrentPath = "/";
            }
            SshTerminal.Ls(CurrentPath);
            RefreshExplorer();
            RefreshTree();
        }

        private void Refresh_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SshTerminal.Ls(CurrentPath);
            RefreshExplorer();
            RefreshTree();
        }

        private void Outside_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Console.WriteLine(System.Environment.CurrentDirectory);
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = System.Environment.CurrentDirectory + @"\ContainersXray.exe";
            Process.Start(pInfo);
        }
    }
}
