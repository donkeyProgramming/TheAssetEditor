using GameWorld.Core.Utility;
using GameWorld.Core.WpfWindow.Events;
using Microsoft.Xna.Framework.Graphics;
using Shared.Core.Events;
using Shared.Core.Events.Global;
using Shared.Core.Services;

namespace GameWorld.Core.Services
{
    public interface IScopedResourceLibrary
    {
        Texture2D? ForceLoadImage(string imagePath, out ImageInformation imageInformation);
        Effect GetStaticEffect(ShaderTypes type);
        Texture2D? LoadTexture(string fileName, bool forceRefreshTexture = false, bool fromFile = false);
    }

    public class ScopedResourceLibrary : IScopedResourceLibrary, IDisposable
    {     
        private readonly ResourceLibrary _resourceLibrary;
        private readonly IEventHub _eventHub;
        private readonly IStandardDialogs _standardDialogs;
        private readonly IGraphicsResourceCreator _graphicsResourceCreator;

        private readonly Dictionary<string, Texture2D?> _cachedTextures = [];
        private bool _isDisposed = false;

        public ScopedResourceLibrary(ResourceLibrary resourceLibrary, IEventHub eventHub, IStandardDialogs standardDialogs, IGraphicsResourceCreator graphicsResourceCreator)
        {
            _resourceLibrary = resourceLibrary;
            _eventHub = eventHub;
            _standardDialogs = standardDialogs;
            _graphicsResourceCreator = graphicsResourceCreator;
            _eventHub.Register<GraphicDeviceDisposedEvent>(this, x=> ClearTextureCache());
            _eventHub.Register<PackFileContainerManipulationEvent>(this, x => ClearTextureCache());
        }

        public Texture2D? ForceLoadImage(string imagePath, out ImageInformation imageInformation) => _resourceLibrary.ForceLoadImage(imagePath, out imageInformation, _graphicsResourceCreator);

        public Texture2D? LoadTexture(string fileName, bool forceRefreshTexture = false, bool fromFile = false)
        {
            var isFound = _cachedTextures.ContainsKey(fileName);
            if (forceRefreshTexture || isFound == false)
            {
                Texture2D? textureLoadResult = null;
                try
                {
                    textureLoadResult = _resourceLibrary.LoadTexture(fileName, forceRefreshTexture, fromFile, _graphicsResourceCreator);
                }
                catch (Exception ex)
                { 
                    _standardDialogs.ShowExceptionWindow(ex, $"Failed to load texture {fileName}. ForceRefreshTexture={forceRefreshTexture} FromFile={fromFile}");
                }

                _cachedTextures[fileName] = textureLoadResult;
            }

            var value = _cachedTextures[fileName];
            return value;
        }

        public Effect GetStaticEffect(ShaderTypes type) => _resourceLibrary.GetStaticEffect(type);

        public void Dispose()
        {
            if (_isDisposed)
                return;
            _isDisposed = true;
            ClearTextureCache();
            _eventHub.UnRegister(this);
        }

        void ClearTextureCache()
        {
            foreach (var item in _cachedTextures)
                _graphicsResourceCreator.DisposeTracked(item.Value);
            _cachedTextures.Clear();
        }
    }
}
