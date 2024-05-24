namespace Shared.Core.DependencyInjection
{
    public interface IScopeHelper
    {
        void ResolveGlobalServices(IServiceProvider serviceProvider);
    }

    public interface IScopeHelper<T> : IScopeHelper
    {
    }
}
