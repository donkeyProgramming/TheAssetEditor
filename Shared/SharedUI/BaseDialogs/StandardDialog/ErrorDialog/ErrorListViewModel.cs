using System.Collections.ObjectModel;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.StandardDialog.ErrorDialog
{
    public partial class ErrorListViewModel
    {
        public ObservableCollection<ErrorListDataItem> ErrorItems { get; set; } = new ObservableCollection<ErrorListDataItem>();
        public string WindowTitle { get; set; } = "Error";

    }
}
