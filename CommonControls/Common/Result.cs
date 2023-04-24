using FluentValidation.Results;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Common
{
    public class Result<T>
    {
        public ErrorList ErrorList { get; private set; }
        public bool Success { get; private set; }
        public T Item { get; private  set; }

        public static Result<T> FromError(string errorGroup, string description)
        {
            var item = new Result<T>()
            {
                Success = false,
                ErrorList = new ErrorList()
            };
            item.ErrorList.Error(errorGroup, description);
            return item;
        }

        public static Result<T> FromError(string errorGroup, ValidationResult result)
        {
            var item = new Result<T>()
            {
                Success = false,
                ErrorList = new ErrorList()
            };
            foreach (var error in result.Errors)
                item.ErrorList.Error(errorGroup, $"{error.PropertyName} - {error.ErrorMessage}");
            return item;
        }

        public static Result<T> FromError(ErrorList errorList)
        {
            var item = new Result<T>()
            {
                Success = false,
                ErrorList = new ErrorList()
            };
            foreach (var error in errorList.Errors)
                item.ErrorList.Error(error.ErrorType, error.Description);
            return item;
        }

        public static Result<T> FromOk(T obj)
        {
            var item = new Result<T>()
            {
                Success = true,
                Item = obj
            };
            return item;
        }
    }
}
