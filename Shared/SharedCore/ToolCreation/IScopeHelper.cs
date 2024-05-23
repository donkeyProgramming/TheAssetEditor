namespace SharedCore.ToolCreation
{
    public interface IScopeHelper
    {
        void ResolveGlobalServices(IServiceProvider serviceProvider);
    }

    public interface IScopeHelper<T> : IScopeHelper
    {
    }
}
