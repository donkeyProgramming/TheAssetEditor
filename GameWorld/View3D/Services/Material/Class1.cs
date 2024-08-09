using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Shared.Core.PackFiles;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Services.Material
{

    public class AeMaterial
    {
    }


    public class Shader
    { 
    
    }

    public class Generator
    { 
    }

    public class CharacterMaterialGenerator
    {
        string Generate(CharacterMaterial characterMaterial)
        {
            return characterMaterial.Value;


        }
    
    }

    public class UiProvider
    { 
        
    }

    public interface IMaterialConfiguration
    {
        UiProvider UiProvider { get;}
        Shader Shader { get; }
        Generator Generator { get; }
    }


    public class CharacterMaterialConfiguration : IMaterialConfiguration
    {
        public CharacterMaterialConfiguration(CharacterMaterial material)
        { 
        }

        public UiProvider UiProvider { get; }
        public Shader Shader { get; }
        public Generator Generator { get; }
    }


    public class CharacterMaterial : AeMaterial
    { 
        public string Value { get; set; }
    }





    public class AeMaterialService
    {
        PackFileService _pfs;

        AeMaterial Load(Rmv2MeshNode target, string wsModelName)
        {
            // Check if game is correct


            var wsModel = new WsModelFile(_pfs.FindFile(wsModelName));
            var lodIndex = target.LodIndex;
            var partIndex = target.OriginalPartIndex;

            var wsModelFileEntry = wsModel.MaterialList.FirstOrDefault(x => x.PartIndex == partIndex && x.LodIndex == lodIndex);
            if (wsModelFileEntry == null)
                return Default();
            var wsModelMaterial = new WsModelMaterialFile(_pfs.FindFile(wsModelFileEntry.MaterialPath));
            var shaderName = wsModelMaterial.ShaderPath;


            // weighted4_character
            // weighted4_character_emissive
            // weighted4_character_emissive_ghost
            // Iridecent
            // Disolve 


            throw new NotImplementedException();
        }


        UiProvider GetUiProvider(AeMaterial aeMaterial)
        {
            throw new NotImplementedException();
        }

        IShader GetShader(AeMaterial aeMaterial) 
        { 
            throw new NotImplementedException(); 
        }


        //-----------
        AeMaterial Default()
        { throw new NotImplementedException(); }


    }
}
