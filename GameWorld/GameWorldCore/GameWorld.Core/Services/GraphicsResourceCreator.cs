using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using Shared.Core.DependencyInjection;
using Shared.Core.ErrorHandling;

namespace GameWorld.Core.Services
{
    public record GraphicsResourceRecord(int ResourceId, string ScopeOwner, string ResourceType, string SourceMember, string SourceFile, int SourceLine);

    public interface IGraphicsResourceCreator
    {
        string ScopeOwner { get; }
        IReadOnlyList<GraphicsResourceRecord> Records { get; }

        void RemoveTracking(object resource);
        T? DisposeTracked<T>(T? resource) where T : class, IDisposable;

        T Track<T>(T resource,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0) where T : class;

        Texture2D CreateTexture2D(int width, int height,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        Texture2D CreateTexture2D(int width, int height, bool mipMap, SurfaceFormat format,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        Texture2D CreateTextureFromStream(Stream stream,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        SpriteBatch CreateSpriteBatch(
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        DepthStencilState CreateDepthStencilState(DepthStencilState state,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        RasterizerState CreateRasterizerState(RasterizerState state,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        BasicEffect CreateBasicEffect(
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        DynamicVertexBuffer CreateDynamicVertexBuffer(VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        VertexBuffer CreateVertexBuffer(VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);

        IndexBuffer CreateIndexBuffer(Type indexElementType, int indexCount, BufferUsage usage,
            [CallerMemberName] string sourceMember = "",
            [CallerFilePath] string sourceFile = "",
            [CallerLineNumber] int sourceLine = 0);
    }

    public class GraphicsResourceCreator : IGraphicsResourceCreator, IScopeOwnerAware, IDisposable
    {
        private record TrackedResource(object Resource, GraphicsResourceRecord Record);

        private readonly Func<GraphicsDevice> _graphicsDeviceFactory;
        private readonly ILogger _logger = Logging.Create<GraphicsResourceCreator>();
        private readonly List<TrackedResource> _trackedResources = [];
        private bool _isDisposed;

        public string ScopeOwner { get; private set; } = "UnknownScopeOwner";
        public IReadOnlyList<GraphicsResourceRecord> Records => _trackedResources.Select(x => x.Record).ToList();

        private GraphicsDevice GraphicsDevice => _graphicsDeviceFactory() ?? throw new InvalidOperationException("GraphicsDevice is not available for the current scope.");

        public GraphicsResourceCreator(Func<GraphicsDevice> graphicsDeviceFactory)
        {
            _graphicsDeviceFactory = graphicsDeviceFactory;
        }

        public void SetScopeOwner(Type ownerType)
        {
            ScopeOwner = ownerType.Name;
        }

        public T Track<T>(T resource, string sourceMember = "", string sourceFile = "", int sourceLine = 0) where T : class
        {
            if (resource == null)
                return null;

            var resourceId = RuntimeHelpers.GetHashCode(resource);

            var record = new GraphicsResourceRecord(
                resourceId,
                ScopeOwner,
                resource.GetType().Name,
                sourceMember,
                Path.GetFileName(sourceFile),
                sourceLine);

            _trackedResources.Add(new TrackedResource(
                resource,
                record));

            _logger.Here().Information(
                "Graphics resource created: Id={ResourceId}, Type={ResourceType}, ScopeOwner={ScopeOwner}, Source={SourceFile}:{SourceLine}::{SourceMember}",
                resourceId,
                record.ResourceType,
                record.ScopeOwner,
                record.SourceFile,
                record.SourceLine,
                record.SourceMember);

            return resource;
        }

        public void RemoveTracking(object resource)
        {
            if (resource == null)
                return;

            var trackedMatches = _trackedResources.Where(x => ReferenceEquals(x.Resource, resource)).ToList();
            if (trackedMatches.Count == 0)
                return;

            foreach (var match in trackedMatches)
            {
                _logger.Here().Information(
                    "Graphics resource deleted: Id={ResourceId}, Type={ResourceType}, ScopeOwner={ScopeOwner}, Source={SourceFile}:{SourceLine}::{SourceMember}",
                    match.Record.ResourceId,
                    match.Record.ResourceType,
                    match.Record.ScopeOwner,
                    match.Record.SourceFile,
                    match.Record.SourceLine,
                    match.Record.SourceMember);
            }

            _trackedResources.RemoveAll(x => ReferenceEquals(x.Resource, resource));
        }

        public T? DisposeTracked<T>(T? resource) where T : class, IDisposable
        {
            if (resource == null)
                return null;

            try
            {
                resource.Dispose();
            }
            catch
            {
                // Best effort cleanup: some resources may have already been disposed elsewhere.
            }
            finally
            {
                RemoveTracking(resource);
            }

            return null;
        }

        public Texture2D CreateTexture2D(int width, int height, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new Texture2D(GraphicsDevice, width, height), sourceMember, sourceFile, sourceLine);

        public Texture2D CreateTexture2D(int width, int height, bool mipMap, SurfaceFormat format, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new Texture2D(GraphicsDevice, width, height, mipMap, format), sourceMember, sourceFile, sourceLine);

        public Texture2D CreateTextureFromStream(Stream stream, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(Texture2D.FromStream(GraphicsDevice, stream), sourceMember, sourceFile, sourceLine);

        public RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new RenderTarget2D(GraphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat), sourceMember, sourceFile, sourceLine);

        public RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new RenderTarget2D(GraphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage), sourceMember, sourceFile, sourceLine);

        public RenderTarget2D CreateRenderTarget2D(int width, int height, bool mipMap, SurfaceFormat preferredFormat, DepthFormat preferredDepthFormat, int preferredMultiSampleCount, RenderTargetUsage usage, bool shared, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new RenderTarget2D(GraphicsDevice, width, height, mipMap, preferredFormat, preferredDepthFormat, preferredMultiSampleCount, usage, shared), sourceMember, sourceFile, sourceLine);

        public SpriteBatch CreateSpriteBatch(string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new SpriteBatch(GraphicsDevice), sourceMember, sourceFile, sourceLine);

        public DepthStencilState CreateDepthStencilState(DepthStencilState state, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(state, sourceMember, sourceFile, sourceLine);

        public RasterizerState CreateRasterizerState(RasterizerState state, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(state, sourceMember, sourceFile, sourceLine);

        public BasicEffect CreateBasicEffect(string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new BasicEffect(GraphicsDevice), sourceMember, sourceFile, sourceLine);

        public DynamicVertexBuffer CreateDynamicVertexBuffer(VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new DynamicVertexBuffer(GraphicsDevice, vertexDeclaration, vertexCount, usage), sourceMember, sourceFile, sourceLine);

        public VertexBuffer CreateVertexBuffer(VertexDeclaration vertexDeclaration, int vertexCount, BufferUsage usage, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new VertexBuffer(GraphicsDevice, vertexDeclaration, vertexCount, usage), sourceMember, sourceFile, sourceLine);

        public IndexBuffer CreateIndexBuffer(Type indexElementType, int indexCount, BufferUsage usage, string sourceMember = "", string sourceFile = "", int sourceLine = 0)
            => Track(new IndexBuffer(GraphicsDevice, indexElementType, indexCount, usage), sourceMember, sourceFile, sourceLine);

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;

#if DEBUG
            var leakedResources = _trackedResources.Select(x => x.Record).ToList();
#endif

            var trackedDisposables = new List<IDisposable>();
            var uniqueDisposables = new HashSet<IDisposable>(ReferenceEqualityComparer.Instance);
            foreach (var item in _trackedResources)
            {
                if (item.Resource is IDisposable disposable && uniqueDisposables.Add(disposable))
                {
                    trackedDisposables.Add(disposable);
                }
            }

            for (var i = trackedDisposables.Count - 1; i >= 0; i--)
                DisposeTracked(trackedDisposables[i]);

            _trackedResources.Clear();

#if DEBUG
           //if (leakedResources.Count > 0)
           //{
           //    var leakedResourceMessage = BuildLeakedResourceMessage(leakedResources);
           //    _logger.Here().Error(leakedResourceMessage);
           //    throw new InvalidOperationException(leakedResourceMessage);
           //}
#endif
        }

#if DEBUG
        private string BuildLeakedResourceMessage(IReadOnlyList<GraphicsResourceRecord> leakedResources)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Graphics resources were still alive when scope '{ScopeOwner}' was destroyed.");
            builder.AppendLine($"Tracked resources still alive: {leakedResources.Count}");

            foreach (var record in leakedResources)
                builder.AppendLine($"- id={record.ResourceId} | {record.ResourceType} | source={record.SourceFile}:{record.SourceLine}::{record.SourceMember}");

            return builder.ToString().TrimEnd();
        }
#endif
    }
}
