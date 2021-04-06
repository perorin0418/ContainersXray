using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContainersXray
{
    class FileEntry
    {
        public int id { get; set; }
        public string containerId { get; set; }
        public string parentPath { get; set; }
        public string path { get; set; }
        public string name { get; set; }
        public string size { get; set; }
        public string updatedAt { get; set; }
        public string permission { get; set; }
        public string owner { get; set; }
    }
}
