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
        public BitmapImage icon
        {
            get
            {
                if (permission.StartsWith("d"))
                {
                    return new BitmapImage(new Uri("pack://application:,,,/resource/Folder-icon.png"));
                }
                else
                {
                    return new BitmapImage(new Uri("pack://application:,,,/resource/document-icon.png"));
                }
            }
        }
        public string path { get; set; }
        public string fileName { get; set; }
        public string size { get; set; }
        public string updatedAt { get; set; }
        public string permission { get; set; }
        public string owner { get; set; }
    }
}
