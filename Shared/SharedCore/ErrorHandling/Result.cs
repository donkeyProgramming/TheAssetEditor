using FluentValidation.Results;

namespace Shared.Core.ErrorHandling
{
    public class Result<T>
    {
        public ErrorList LogItems { get; private set; }
        public bool IsSuccess { get; private set; }
        public bool Failed { get => !IsSuccess; }
        public T Item { get; private set; }

        public static Result<T> FromError(string errorGroup, string description)
        {
            var item = new Result<T>()
            {
                IsSuccess = false,
                LogItems = new ErrorList()
            };
            item.LogItems.Error(errorGroup, description);
            return item;
        }

        public static Result<T> FromError(string errorGroup, ValidationResult result)
        {
            var item = new Result<T>()
            {
                IsSuccess = false,
                LogItems = new ErrorList()
            };
            foreach (var error in result.Errors)
                item.LogItems.Error(errorGroup, $"{error.PropertyName} - {error.ErrorMessage}");
            return item;
        }

        public static Result<T> FromError(ErrorList errorList)
        {
            var item = new Result<T>()
            {
                IsSuccess = false,
                LogItems = new ErrorList()
            };
            foreach (var error in errorList.Errors)
                item.LogItems.Error(error.ErrorType, error.Description);
            return item;
        }

        public static Result<T> FromOk(T obj)
        {
            var item = new Result<T>()
            {
                IsSuccess = true,
                Item = obj
            };
            return item;
        }
    }
}
