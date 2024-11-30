using System.Diagnostics;

namespace Shared.Core.ErrorHandling
{
    [DebuggerDisplay("{ErrorType}:{ItemName}-{Description}")]
    public class ErrorListDataItem
    {
        public string ErrorType { get; set; }
        public string ItemName { get; set; }
        public string Description { get; set; }
        public bool IsError { get; set; } = false;

        public bool IsWarning { get; set; } = false;
    }

    public class ErrorList
    {
        public List<ErrorListDataItem> Errors { get; set; } = new List<ErrorListDataItem>();

        public bool HasData { get => Errors.Count != 0; }

        public ErrorListDataItem Error(string itemName, string description)
        {
            var item = new ErrorListDataItem() { ErrorType = "Error", ItemName = itemName, Description = description, IsError = true };
            Errors.Add(item);
            return item;
        }

        public ErrorListDataItem Warning(string itemName, string description)
        {
            var item = new ErrorListDataItem() { ErrorType = "Warning", ItemName = itemName, Description = description, IsWarning = true };
            Errors.Add(item);
            return item;
        }

        public ErrorListDataItem Ok(string itemName, string description)
        {
            var item = new ErrorListDataItem() { ErrorType = "Ok", ItemName = itemName, Description = description };
            Errors.Add(item);
            return item;
        }

        public void AddAllErrors(ErrorList instanceErrorList)
        {
            foreach (var error in instanceErrorList.Errors)
            {
                var item = new ErrorListDataItem() { ErrorType = error.ErrorType, ItemName = error.ItemName, Description = error.Description };
                Errors.Add(item);
            }
        }
    }
}
