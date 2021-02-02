using Common;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
//using Pfim;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace View3D.Scene
{
    public enum ShaderTypes
    { 
        Line,
        Mesh,
        TexturePreview,
        Phazer
    }

    public class ResourceLibary : BaseComponent
    {
        ILogger _logger = Logging.Create<ResourceLibary>();

        Dictionary<string, Texture2D> _textureMap = new Dictionary<string, Texture2D>();
        Dictionary<ShaderTypes, Effect> _shaders = new Dictionary<ShaderTypes, Effect>();

        public ContentManager XnaContentManager { get { return Game.Content; } }

        public ResourceLibary(WpfGame game) : base(game)
        {
        }

        public Texture2D LoadTexture(string fileName, GraphicsDevice device)
        {
            if (_textureMap.ContainsKey(fileName))
                return _textureMap[fileName];

            var texture = LoadTextureAsTexture2d(fileName, device);
            if(texture != null)
                _textureMap[fileName] = texture;
            return texture;
        }

        public void SaveTexture(Texture2D texture, string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                texture.SaveAsPng(stream, texture.Width, texture.Height);
            }
        }

        Texture2D LoadTextureAsTexture2d(string fileName, GraphicsDevice device)
        {
            //try
            //{
            //    //r content = File.ReadAllBytes(@"C:\Users\ole_k\Desktop\New folder\rad_rustig.dds");
            //    var file = PackFileLoadHelper.FindFile(_loadedContent, fileName);
            //    if (file == null)
            //    {
            //        _logger.Here().Error($"Unable to find texture: {fileName}");
            //        return null;
            //    }
            //    
            //    var content = file.Data;
            //    using (MemoryStream stream = new MemoryStream(content))
            //    {
            //        var image = Pfim.Dds.Create(stream, new Pfim.PfimConfig(32768, Pfim.TargetFormat.Native, false));
            //        if (image as Pfim.Dxt1Dds != null)
            //        {
            //            var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt1);
            //            
            //            texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize);
            //            return texture;
            //        }
            //        else if (image as Pfim.Dxt5Dds != null)
            //        {
            //            var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt5);
            //            texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize);
            //            return texture;
            //        }
            //        else if (image as Pfim.Dxt3Dds != null)
            //        {
            //            var texture = new Texture2D(device, image.Width, image.Height, false, SurfaceFormat.Dxt3);
            //            texture.SetData(image.Data, 0, (int)image.Header.PitchOrLinearSize);
            //            return texture;
            //        }
            //        else
            //        {
            //            throw new Exception("Unknow texture format: " + image.ToString() + " Path = " + fileName);
            //        }
            //    }
            //}
            //catch (Exception e)
            //{
            //    _logger.Here().Error($"Error loading texture ({fileName}): {e}");
            //}
            return null;
        }

        public Effect LoadEffect(string fileName, ShaderTypes type)
        {
            if (_shaders.ContainsKey(type))
                return _shaders[type];
            var effect = XnaContentManager.Load<Effect>(fileName);
            _shaders[type] = effect;
            return effect;
        }

        public Effect GetEffect(ShaderTypes type)
        {
            if (_shaders.ContainsKey(type))
                return _shaders[type];
            throw new Exception($"Shader not found: ShaderTypes::{type}");
        }
    }
}
