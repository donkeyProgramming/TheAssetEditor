using System.Windows;
using Shared.Core.ErrorHandling.Exceptions;

namespace Shared.Ui.Common.Exceptions
{
    internal class CustomExceptionWindowProvider : ICustomExceptionWindowProvider
    {
        public void ShowDialog(ExceptionInformation extendedException)
        {
            var errorWindow = new CustomExceptionWindow(extendedException);
            if (Application.Current.MainWindow != null)
            {
                if (errorWindow != Application.Current.MainWindow)
                {
                    errorWindow.Owner = Application.Current.MainWindow;
                }
            }
            errorWindow.ShowDialog();
        }
    }
}
