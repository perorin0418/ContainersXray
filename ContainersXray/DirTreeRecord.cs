using System.Collections.ObjectModel;

namespace ContainersXray
{
    class DirTreeRecord
    {
        public DirTreeRecord parent { get; set; }
        public string name { get; set; }
        public ObservableCollection<DirTreeRecord> dirs { get; set; } = new ObservableCollection<DirTreeRecord>();
        public DirTreeRecord getDir(string name)
        {
            foreach(var d in dirs)
            {
                if (d.name.Equals(name))
                {
                    return d;
                }
            }
            return null;
        }
        public string getFullPath()
        {
            return getFullPath("");
        }

        private string getFullPath(string path)
        {
            if(parent == null)
            {
                return path;
            }
            else
            {
                return parent.getFullPath("/" + name + path);
            }
        }
    }
}
