using CommonControls.BaseDialogs;
using Shared.Core.Events;

namespace Editors.Reports.DeepSearch
{
    public class DeepSearchCommand : IUiCommand
    {
        private readonly DeepSearchReport _deepSearchReport;

        public DeepSearchCommand(DeepSearchReport deepSearchReport)
        {
            _deepSearchReport = deepSearchReport;
        }

        public void Execute()
        {
            var window = new TextInputWindow("Deep search - Output in console", "");
            if (window.ShowDialog() == true)
            {
                if (string.IsNullOrWhiteSpace(window.TextValue))
                {
                    System.Windows.MessageBox.Show("Invalid input");
                    return;
                }
                _deepSearchReport.DeepSearch(window.TextValue, false);
            }
        }
    }
}
