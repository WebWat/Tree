namespace Tree.Models
{
    public class FilesInfo
    {
        public string CurrentPath { get; set; } = default!;
        public IEnumerable<File>? Files { get; set; }
    }
}
