namespace Tree.Models
{
    public class FilesInfo
    {
        public string CurrentPath { get; set; }
        public IEnumerable<File> Files { get; set; }
    }
}
