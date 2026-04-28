namespace Shared.Ui.Common
{
    public static class WpfHelpers
    {

        public static string DeduplicateUnderscores(string wtfWpf) => wtfWpf.Replace("__", "_");
        public static string DuplicateUnderscores(string wtfWpf) => wtfWpf.Replace("_", "__");
    }
}
