using System;

namespace Shared.Core.ErrorHandling.Exceptions
{

    public interface IExceptionService
    {
        ExceptionInformation Create(Exception e);
        void CreateDialog(Exception e);
    }

    class ExceptionService : IExceptionService
    {
        private readonly IEnumerable<IExceptionInformationProvider> _informationProviders;
        private readonly ICustomExceptionWindowProvider _exceptionWindowProvider;

        public ExceptionService(IEnumerable<IExceptionInformationProvider> informationProviders, ICustomExceptionWindowProvider exceptionWindowProvider)
        {
            _informationProviders = informationProviders;
            _exceptionWindowProvider = exceptionWindowProvider;
        }

        public ExceptionInformation Create(Exception e)
        {

            var trace = new System.Diagnostics.StackTrace(e, true);

            var extendedException = new ExceptionInformation
            {
                ExceptionMessage = ExceptionHelper.GetErrorStringArray(e).ToArray(),
                StackTrace = e.StackTrace
            };

            foreach (var provider in _informationProviders)
                provider.HydrateExcetion(extendedException);

            return extendedException;
        }

        public void CreateDialog(Exception e)
        {
            var extendedException = Create(e);
            _exceptionWindowProvider.ShowDialog(extendedException);
        }
    }
}
