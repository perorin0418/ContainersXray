using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ContainersXray
{
    class ExplorerRecord
    {
        public BitmapImage Icon
        {
            get
            {
                if (Permission.StartsWith("d"))
                {
                    return new BitmapImage(new Uri("pack://application:,,,/resource/Folder-icon.png"));
                }
                else
                {
                    return new BitmapImage(new Uri("pack://application:,,,/resource/document-icon.png"));
                }
            }
        }
        public string Path { get; set; }
        public string FileName { get; set; }
        public string Size { get; set; }
        public string UpdatedAt { get; set; }
        public string Permission { get; set; }
        public string Owner { get; set; }
    }
}
