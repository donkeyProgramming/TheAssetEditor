// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FluentValidation.Results;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

namespace CommonControls.Common
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
