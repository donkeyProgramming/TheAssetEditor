namespace Shared.Core.ErrorHandling.Exceptions
{
    public interface IExceptionInformationProvider
    {
        public void HydrateExcetion(ExceptionInformation exceptionInformation);
    }


}
