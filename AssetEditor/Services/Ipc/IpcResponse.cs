namespace AssetEditor.Services.Ipc
{
    public class IpcResponse
    {
        public bool Ok { get; set; }
        public string Error { get; set; }
        public string NormalizedPath { get; set; }

        public static IpcResponse Success() => new() { Ok = true };

        public static IpcResponse Failure(string error, string normalizedPath = null) => new()
        {
            Ok = false,
            Error = error,
            NormalizedPath = normalizedPath
        };
    }
}
