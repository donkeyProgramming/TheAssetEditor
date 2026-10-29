using NUnit.Framework;

namespace Test.TestingUtility.TestUtility;

public class PathHelperTests
{
    [Test]
    public void FindsDataFromSolutionRootWhenCheckoutNameDiffersFromLegacyRootName()
    {
        var path = PathHelper.GetDataFolder("Data\\Rome_Man_And_Shield_Pack", "__nonexistent_checkout_name__");

        Assert.That(Directory.Exists(path), Is.True);
        Assert.That(Path.GetFileName(path), Is.EqualTo("Rome_Man_And_Shield_Pack"));
    }

    [Test]
    public void FindsDataFileUsingPlatformIndependentPathCombining()
    {
        var path = PathHelper.GetDataFile("Throt.pack", "TheAssetEditor", "Data");

        Assert.That(File.Exists(path), Is.True);
        Assert.That(Path.GetFileName(path), Is.EqualTo("Throt.pack"));
    }
}
