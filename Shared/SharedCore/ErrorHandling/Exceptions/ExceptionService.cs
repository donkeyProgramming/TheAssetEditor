namespace Shared.Core.ErrorHandling.Exceptions
{
    public interface IExceptionService
    {
        ExceptionInformation Create(Exception e);
        void CreateDialog(Exception e, string userMessage = "");
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
            var exceptionList = UnrollException(e);
            var extendedException = new ExceptionInformation
            {
                ExceptionInfo = exceptionList.ToArray(),
            };

            foreach (var provider in _informationProviders)
                provider.HydrateExcetion(extendedException);

            return extendedException;
        }

        public void CreateDialog(Exception e, string userMessage = "")
        {
            var extendedException = Create(e);
            extendedException.UserMessage = userMessage;
            _exceptionWindowProvider.ShowDialog(extendedException);
        }

        static List<ExceptionInstance> UnrollException(Exception e)
        {
            var output = new List<ExceptionInstance>();

            var innerE = e;
            while (innerE != null)
            {
                var splitTrace = new string[0];
                if (innerE.StackTrace != null)
                    splitTrace = innerE.StackTrace.Split("\n");

                output.Add(new ExceptionInstance(innerE.Message, splitTrace));
                innerE = innerE.InnerException;
            }

            return output;
        }
    }
}
