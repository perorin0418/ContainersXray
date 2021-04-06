using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace ContainersXray
{
    class DirTreeRecord
    {
        public BitmapImage Icon
        {
            get
            {
                return new BitmapImage(new Uri("pack://application:,,,/resource/Folder-icon.png"));
            }
        }
        public string Name { get; set; }
        public string Path { get; set; }
        public ObservableCollection<DirTreeRecord> Dirs { get; set; } = new ObservableCollection<DirTreeRecord>();
    }
}
