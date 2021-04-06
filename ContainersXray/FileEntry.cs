namespace ContainersXray
{
    class FileEntry
    {
        public int Id { get; set; }
        public string ContainerID { get; set; }
        public string ParentPath { get; set; }
        public string Path { get; set; }
        public string Name { get; set; }
        public string Size { get; set; }
        public string UpdatedAt { get; set; }
        public string Permission { get; set; }
        public string Owner { get; set; }
    }
}
