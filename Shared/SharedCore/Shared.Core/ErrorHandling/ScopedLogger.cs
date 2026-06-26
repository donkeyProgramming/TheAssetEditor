using Shared.Core.ToolCreation;

namespace Shared.Core.ErrorHandling
{
    public interface IScopedLogger
    {
        ILogger ForContext<T>();
    }

    public class ScopedLogger : IScopedLogger
    {
        public readonly ILogger _scopedBase;

        public ScopedLogger(ScopeToken scopeToken)
        {
            _scopedBase = Log.ForContext("ScopeId", scopeToken.ScopeId);
        }

        public ILogger ForContext<T>() => _scopedBase.ForContext<T>();
    }
}
