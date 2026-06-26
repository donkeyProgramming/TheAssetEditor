using System.Collections;
using System.Reflection;
using System.Text;
using CommunityToolkit.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Core.ToolCreation
{
    public interface IScopeRepository
    {
        List<IEditorInterface> GetEditorHandles();
        IServiceScope CreateScope(IEditorInterface owner);
        IEditorInterface CreateScope(Type owner);
        void RemoveScope(IEditorInterface owner);

        T GetRequiredServiceRootScope<T>() where T : notnull;
        T GetRequiredService<T>(IEditorInterface editorHandle) where T : notnull;

        void Print();
        IEditorInterface? GetEditorFromToken(ScopeToken token);
    }

    public class ScopeToken
    { 
        private static int _scopeCounter = 0;

        public string MetaData { get; set; } = string.Empty;
        public int ScopeId { get; private set; }

        public ScopeToken()
        {
            ScopeId = _scopeCounter++;
        }
    }

    public class ScopeRepository : IScopeRepository
    {
        private record ScopeInfo(IServiceScope Scope, ScopeToken token);
        private readonly ILogger _logger = Logging.Create<ScopeRepository>();

        private readonly Dictionary<IEditorInterface, ScopeInfo> _scopes = [];
        private readonly IServiceProvider _rootProvider;

        public List<IEditorInterface> GetEditorHandles() => _scopes.Select(x=>x.Key).ToList();

        public ScopeRepository(IServiceProvider rootProvider)
        {
            _rootProvider = rootProvider;
        }

        public IServiceScope CreateScope(IEditorInterface owner)
        { 
            var scope = _rootProvider.CreateScope();
            foreach (var ownerAware in scope.ServiceProvider.GetServices<IScopeOwnerAware>())
                ownerAware.SetScopeOwner(owner.GetType());

            Add(owner, scope);
            return scope;
        }

        public IEditorInterface CreateScope(Type editorType)
        {
            var scope = _rootProvider.CreateScope();
            foreach (var ownerAware in scope.ServiceProvider.GetServices<IScopeOwnerAware>())
                ownerAware.SetScopeOwner(editorType);

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

            var scopeToken = scope.ServiceProvider.GetRequiredService<ScopeToken>();
            scopeToken.MetaData = $"Scope for {owner.DisplayName} of type {owner.GetType()}";

            _logger.Here().Information($"Adding scope for {owner.DisplayName} of type {owner.GetType()}");
            _scopes.Add(owner, new ScopeInfo(scope, scopeToken));
        }

        public void RemoveScope(IEditorInterface owner)
        {
           // WeakReference weak;
            {
                _logger.Here().Information($"Removing scope for {owner.DisplayName} of type {owner.GetType()}");
                var scope = _scopes[owner].Scope;

                //weak = new WeakReference(owner);

                _scopes.Remove(owner);
                scope.Dispose();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            //Console.WriteLine(weak.IsAlive);
        }

        public T GetRequiredService<T>(IEditorInterface editorHandle) where T : notnull
        { 
            var found = _scopes.TryGetValue(editorHandle, out var result);
            if (found == false || result == null)
                throw new Exception($"Unable to scope '{editorHandle.DisplayName}|{editorHandle.GetType()}' when looking for {typeof(T)}");
            
            var instance = result.Scope.ServiceProvider.GetRequiredService<T>();
            return instance;
        }

        public IEditorInterface? GetEditorFromToken(ScopeToken token)
        {
            foreach (var item in _scopes)
            {
                if (item.Value.token == token)
                    return item.Key;
            }

            return null;
        }

        public T GetRequiredServiceRootScope<T>() where T : notnull
        {
            var instance = _rootProvider.GetRequiredService<T>();
            return instance;
        }

        public void Print()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"ScopeTable info - Num Scopes:{_scopes.Count}");
            GenerateDebugString(_rootProvider, "RootScope", builder);
            foreach (var scope in _scopes)
                GenerateDebugString(scope.Value.Scope.ServiceProvider, $"{scope.Key.GetType()}-'{scope.Key.DisplayName}-[{scope.Value.token.ScopeId}]'", builder);

            _logger.Here().Information(builder.ToString()); 
        }

        void GenerateDebugString(IServiceProvider provider, string scopeName, StringBuilder stringBuilder)
        {
            try
            {
                stringBuilder.AppendLine($"ScopeTable for {scopeName} - ");

                // Get resolved services
                var serviceProviderType = provider.GetType();
                var resolvedServicesProperty = serviceProviderType.GetProperty("ResolvedServices", BindingFlags.NonPublic | BindingFlags.Instance);

                Guard.IsNotNull(resolvedServicesProperty, "Failed get property ResolvedServices");
                var resolved = resolvedServicesProperty.GetValue(provider);
                Guard.IsNotNull(resolved, "Failed to get value from provider");
                var resolveList = CastToObjectDictionary(resolved);


                stringBuilder.AppendLine($"\tResolvedServices[{resolveList.Count}]");
                foreach (var service in resolveList)
                    stringBuilder.AppendLine($"\t\t{ service.Value.GetType().FullName}");

                var disposablesServicesProperty = serviceProviderType.GetProperty("Disposables", BindingFlags.NonPublic | BindingFlags.Instance);
                Guard.IsNotNull(disposablesServicesProperty, "Unable to find Disposables property on ServiceProvider");
                var disposables = disposablesServicesProperty.GetValue(provider) as IList;
                Guard.IsNotNull(disposables, "Unable to cast Disposables to IList");

                stringBuilder.AppendLine($"\tDisposables[{disposables.Count}]");
                foreach (var service in disposables)
                    stringBuilder.AppendLine($"\t\t{service.GetType().FullName}");

                stringBuilder.AppendLine();
            }
            catch (Exception ex)
            {
                stringBuilder.AppendLine($"Failed to generate ScopeTable for {scopeName} due to {ex.Message}");
            }
        }

        static Dictionary<object, object> CastToObjectDictionary(object input)
        {
            // Check if the input is an IDictionary
             if (input is not IDictionary dictionary)
            {
                throw new ArgumentException("Input object is not a dictionary.", nameof(input));
            }

            var objectDictionary = new Dictionary<object, object>();

            // Use reflection to iterate over the dictionary entries
            foreach (var key in dictionary.Keys)
            {
                var value = dictionary[key];
                objectDictionary[key] = value!;
            }

            return objectDictionary;
        }

    }
}
