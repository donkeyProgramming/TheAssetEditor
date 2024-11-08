namespace Shared.Core.ErrorHandling.Exceptions
{
    public interface ICustomExceptionWindowProvider
    {
        void ShowDialog(ExceptionInformation extendedException);
    }
}
