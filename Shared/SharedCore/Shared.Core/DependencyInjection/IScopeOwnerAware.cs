namespace Shared.Core.DependencyInjection
{
    public interface IScopeOwnerAware
    {
        void SetScopeOwner(Type ownerType);
    }
}
