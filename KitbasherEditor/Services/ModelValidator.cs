using Filetypes.RigidModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using View3D.SceneNodes;

namespace KitbasherEditor.Services
{
    public class ModelValidator
    {
        public bool CanCombine(List<Rmv2MeshNode> items, out List<string> errors)
        {
            errors = new List<string>();
            foreach (var outerLoopItem in items)
            {
                foreach (var innerLoopItem in items)
                {
                    if (outerLoopItem == innerLoopItem)
                        continue;

                    // Textures
                    if (!ValidateTextures(outerLoopItem.MeshModel, innerLoopItem.MeshModel, out string textureErrorMsg))
                    {
                        errors.Add("Texture -> " + textureErrorMsg);
                    }

                    // Vertex type
                    if (outerLoopItem.MeshModel.Header.VertextType != innerLoopItem.MeshModel.Header.VertextType)
                    {
                        errors.Add($"Vertext type -> {outerLoopItem.MeshModel.Header.ModelName} has a different vertex type then {innerLoopItem.MeshModel.Header.ModelName}");
                    }

                    // Alpha mode
                    if (outerLoopItem.MeshModel.Mesh.AlphaSettings.Mode != innerLoopItem.MeshModel.Mesh.AlphaSettings.Mode)
                    {
                        errors.Add($"AlphaSettings Mode -> {outerLoopItem.MeshModel.Header.ModelName} has a different AlphaSettings mode then {innerLoopItem.MeshModel.Header.ModelName}");
                    }

                  
                    // Skeleton stuff
                    //if(ValidateSkeletonInfo(outerLoopItem.Geometry, ))
                }



                //var skellyName = outerLoopItem.MeshModel.ParentSkeletonName;
                //var indexList = outerLoopItem.Geometry.GetUniqeBlendIndices();


                //var newSkeletonFile = _animLookUp.GetSkeletonFileFromName(modelNode.Model.Header.SkeletonName);
                //config.ParnetModelSkeletonName = modelNode.Model.Header.SkeletonName;
                //config.ParentModelBones = AnimatedBone.CreateFromSkeleton(newSkeletonFile);
            }

            
            return errors.Count == 0;
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
