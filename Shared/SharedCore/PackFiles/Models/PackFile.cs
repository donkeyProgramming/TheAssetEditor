using Shared.Core.Misc;

namespace Shared.Core.PackFiles.Models
{
    public class PackFile : NotifyPropertyChangedImpl
    {
        public IDataSource DataSource { get; set; }

        public PackFile(string name, IDataSource dataSource)
        {
            Name = name;
            DataSource = dataSource;
        }

        string _name;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        public override string ToString() { return Name; }

        public string Extention { get => Path.GetExtension(Name); }


        public static PackFile CreateFromBytes(string fileName, byte[] bytes) => new(fileName, new MemorySource(bytes));
        public static PackFile CreateFromASCII(string fileName, string str) => new(fileName, new MemorySource(System.Text.Encoding.ASCII.GetBytes(str)));
        public static PackFile CreateFromFileSystem(string fileName, string fullPath) => new(fileName, new FileSystemSource(fullPath));
    }
}
