using System.Windows.Forms;

namespace Shared.Core.Services
{
    public interface IStandardDialogProvider
    {
        DialogResult ShowDialogBox(string message, string heading);
    }

    public class StandardDialogProvider : IStandardDialogProvider
    {
        public DialogResult ShowDialogBox(string message, string heading)
        {
            return MessageBox.Show("You are trying to load a pack file before loading CA packfile. Most editors EXPECT the CA packfiles to be loaded and will cause issues if they are not.\nFile not loaded!", "Error");
        }
    }
}
