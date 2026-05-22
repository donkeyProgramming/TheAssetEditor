namespace Shared.UiTest;

internal class SmokeTests
{
    [Test]
    public void SharedUiAssemblyLoads()
    {
        var assembly = typeof(Shared.Ui.BaseDialogs.PackFileTree.TreeNode).Assembly;
        Assert.That(assembly.GetName().Name, Is.EqualTo("Shared.Ui"));
    }
}
