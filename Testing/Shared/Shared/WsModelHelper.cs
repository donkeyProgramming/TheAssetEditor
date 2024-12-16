using Shared.Core.PackFiles.Models;

namespace Test.TestingUtility.Shared
{
    public static class WsModelHelper
    {
        public static void AssertFile(PackFile wsModelFile)
        {
            Assert.That(wsModelFile, Is.Not.Null);
        }
    }
}
