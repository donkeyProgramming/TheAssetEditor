using CommonControls.FileTypes.PackFiles.Models;

namespace View3D.Services
{
    public interface IActiveFileResolver
    {
        public string ActiveFileName { get; set; }
        public PackFile Get();
    }

}
