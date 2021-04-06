using System;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;

namespace ContainersXray
{
    class DirTreeRecord
    {
        public BitmapImage icon
        {
            get
            {
                return new BitmapImage(new Uri("pack://application:,,,/resource/Folder-icon.png"));
            }
        }
        public string name { get; set; }
        public string path { get; set; }
        public ObservableCollection<DirTreeRecord> dirs { get; set; } = new ObservableCollection<DirTreeRecord>();
    }
}
