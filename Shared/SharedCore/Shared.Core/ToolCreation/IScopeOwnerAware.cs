namespace Shared.Core.ToolCreation
{
    public interface IScopeOwnerAware
    {
        void SetScopeOwner(Type ownerType);
    }
}
