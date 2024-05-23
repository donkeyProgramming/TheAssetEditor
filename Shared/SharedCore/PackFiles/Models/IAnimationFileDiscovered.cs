namespace SharedCore.PackFiles.Models
{
    public interface IAnimationFileDiscovered
    {
        void FileDiscovered(PackFile file, PackFileContainer container, string fullPath);

        public void LoadFromPackFileContainer(PackFileService pfs, PackFileContainer packFileContainer);
        public void UnloadAnimationFromContainer(PackFileService pfs, PackFileContainer packFileContainer);
    }
}
