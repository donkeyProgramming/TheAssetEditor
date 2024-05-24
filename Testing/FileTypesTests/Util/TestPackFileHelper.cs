using System.Text;
using Shared.Core.PackFiles.Models;

namespace FileTypesTests.Util
{
    public static class TestPackFileHelper
    {

        public static PackFile CreatePackFile(string name, string fileContent = "Jackob had a horse")
        {
            var source = new MemorySource(Encoding.UTF8.GetBytes(fileContent));
            return new PackFile(name, source);
        }
    }
}
