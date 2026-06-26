using Shared.Core.ErrorHandling.Exceptions;

namespace Shared.Core.PackFiles.ErrorHandling
{
    class PackFileExceptionInformationProvider : IExceptionInformationProvider
    {
        private readonly IPackFileService _pfs;

        public PackFileExceptionInformationProvider(IPackFileService pfs)
        {
            ;
            _pfs = pfs;
        }

        public void HydrateExcetion(ExceptionInformation exceptionInformation)
        {
            var packfiles = _pfs.GetAllPackfileContainers();
            foreach (var db in packfiles)
            {
                var isMainEditable = _pfs.GetEditablePack() == db;
                var info = new ExceptionPackFileContainerInfo(isMainEditable, db.IsCaPackFile, db.Name, db.SystemFilePath);
                exceptionInformation.ActivePackFiles.Add(info);
            }

        }
    }
}
