using Shared.Core.Misc;

namespace Shared.Core.PackFiles.Models
{
    public class PackFile
    {
        public IDataSource DataSource { get; set; }

        public PackFile(string name, IDataSource dataSource)
        {
            Name = name;
            DataSource = dataSource;
        }


        public string Name { get; set; }

        public override string ToString() => Name;
        public string Extention { get => Path.GetExtension(Name); }


        public static PackFile CreateFromBytes(string fileName, byte[] bytes) => new(fileName, new MemorySource(bytes));
        public static PackFile CreateFromASCII(string fileName, string str) => new(fileName, new MemorySource(System.Text.Encoding.ASCII.GetBytes(str)));
        public static PackFile CreateFromFileSystem(string fileName, string fullPath) => new(fileName, new FileSystemSource(fullPath));
    }
}
