namespace AssetEditor.Services.Ipc
{
    public class IpcRequest
    {
        public string Action { get; set; }
        public string Path { get; set; }
        public bool? BringToFront { get; set; }
        public bool? OpenInExistingKitbashTab { get; set; }
        public string PackPathOnDisk { get; set; }
    }
}
