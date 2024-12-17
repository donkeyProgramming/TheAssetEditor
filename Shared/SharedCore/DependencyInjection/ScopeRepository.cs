using Microsoft.Extensions.DependencyInjection;
using Shared.Core.ToolCreation;

namespace Shared.Core.DependencyInjection
{
    public interface IScopeRepository
    {
        List<IEditorInterface> GetEditorHandles();
        IServiceScope CreateScope(IEditorInterface owner);
        IEditorInterface CreateScope(Type owner);
        void RemoveScope(IEditorInterface owner);

        T GetRequiredServiceRootScope<T>();
        T GetRequiredService<T>(IEditorInterface editorHandle);
   
    }


    public class ScopeRepository : IScopeRepository
    {
        private readonly Dictionary<IEditorInterface, IServiceScope> _scopes = [];
        private readonly IServiceProvider _rootProvider;

        public List<IEditorInterface> GetEditorHandles() => _scopes.Select(x=>x.Key).ToList();

        public ScopeRepository(IServiceProvider rootProvider)
        {
            _rootProvider = rootProvider;
        }

        public IServiceScope CreateScope(IEditorInterface owner)
        { 
            var scope = _rootProvider.CreateScope();
            Add(owner, scope);
            return scope;
        }

        public IEditorInterface CreateScope(Type editorType)
        {
            var scope = _rootProvider.CreateScope();
            var instance = scope.ServiceProvider.GetRequiredService(editorType) as IEditorInterface;
            if (instance == null)
                throw new Exception($"Type '{editorType}' is not a IEditorViewModel");
            
            Add(instance, scope);
            return instance;
        }

        void Add(IEditorInterface owner, IServiceScope scope)
        {
            if (_scopes.ContainsKey(owner))
                throw new ArgumentException("Owner already added!");

            _scopes.Add(owner, scope);
        }

        public void RemoveScope(IEditorInterface owner)
        {
            _scopes[owner].Dispose();
            _scopes.Remove(owner);
        }


        public T GetRequiredService<T>(IEditorInterface editorHandle) where T : notnull
        { 
            var found = _scopes.TryGetValue(editorHandle, out var result);
            if (found == false)
                throw new Exception($"Unable to scope '{editorHandle.DisplayName}|{editorHandle.GetType()}' when looking for {typeof(T)}");
            
            var instance = result.ServiceProvider.GetRequiredService<T>();
            return instance;
        }

        public T GetRequiredServiceRootScope<T>() where T : notnull
        {
            var instance = _rootProvider.GetRequiredService<T>();
            return instance;
        }

    }
}
