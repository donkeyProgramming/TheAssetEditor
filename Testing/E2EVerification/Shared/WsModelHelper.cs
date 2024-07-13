using Shared.Core.PackFiles.Models;

namespace E2EVerification.Shared
{
    internal static class WsModelHelper
    {
        public static void AssertFile(PackFile wsModelFile)
        {
            Assert.That(wsModelFile, Is.Not.Null);
        }
    }
}
