using Filetypes.RigidModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using static CommonControls.ErrorListDialog.ErrorListViewModel;

namespace View3D.Utility
{
    public class ModelCombiner
    {
        public bool CanCombine(List<Rmv2MeshNode> items, out ErrorList errors)
        {
            errors = new ErrorList();
            foreach (var outerLoopItem in items)
            {
                foreach (var innerLoopItem in items)
                {
                    if (outerLoopItem == innerLoopItem)
                        continue;

                    // Textures
                    if (!ValidateTextures(outerLoopItem.MeshModel, innerLoopItem.MeshModel, out string textureErrorMsg))
                    {
                        //errors.Add("Texture -> " + textureErrorMsg);
                        errors.Error("Texture", textureErrorMsg);
                    }

                    // Vertex type
                    if (outerLoopItem.MeshModel.Header.VertextType != innerLoopItem.MeshModel.Header.VertextType)
                    {
                        errors.Error("VertexType", $"{outerLoopItem.MeshModel.Header.ModelName} has a different vertex type then {innerLoopItem.MeshModel.Header.ModelName}");
                    }

                    // Alpha mode
                    if (outerLoopItem.Geometry.Alpha != innerLoopItem.Geometry.Alpha)
                    {
                        errors.Error("AlphaSettings mode", $"{outerLoopItem.MeshModel.Header.ModelName} has a different AlphaSettings mode then {innerLoopItem.MeshModel.Header.ModelName}");
                    }
                }
            }

            return errors.Errors.Count == 0;
        }

        private bool ValidateTextures(RmvSubModel item0, RmvSubModel item1, out string textureErrorMsg)
        {
            if (item0.Textures.Count != item1.Textures.Count)
            {
                textureErrorMsg = $"{item0.Header.ModelName} has a different number of textures then {item1.Header.ModelName}";
                return false;
            }

            foreach (var texture in item0.Textures)
            {
                var res = item1.Textures.Count(x => x.Path == texture.Path);
                if (res != 1)
                {
                    textureErrorMsg = $"{item1.Header.ModelName} does not contain texture {texture.Path}";
                    return false;
                }
            }

            textureErrorMsg = null;
            return true;
        }

  
    }
}
