using System.Collections.Generic;
using System.Linq;
using View3D.SceneNodes;
using static CommonControls.BaseDialogs.ErrorListDialog.ErrorListViewModel;

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

                    var model0Name = outerLoopItem.Name;
                    var model1Name = innerLoopItem.Name;

                    // Textures
                    if (!ValidateTextures(outerLoopItem, model0Name, innerLoopItem, model1Name, out string textureErrorMsg))
                        errors.Error("Texture", textureErrorMsg);

                    // Vertex type
                    if (outerLoopItem.Geometry.VertexFormat != innerLoopItem.Geometry.VertexFormat)
                        errors.Error("VertexType", $"{model0Name} has a different vertex type then {model1Name}");

                    // Alpha mode
                    if (outerLoopItem.Material.AlphaMode != innerLoopItem.Material.AlphaMode)
                        errors.Error("AlphaSettings mode", $"{model0Name} has a different AlphaSettings mode then {model1Name}");
                }
            }

            return errors.Errors.Count == 0;
        }

        private bool ValidateTextures(Rmv2MeshNode item0, string item0Name, Rmv2MeshNode item1, string item1Name, out string textureErrorMsg)
        {
            var textureList0 = item0.GetTextures();
            var textureList1 = item1.GetTextures();
            if (textureList0.Count != textureList1.Count())
            {
                textureErrorMsg = $"{item0Name} has a different number of textures then {item1Name}";
                return false;
            }

            foreach (var item in textureList0)
            {
                if (textureList1.ContainsKey(item.Key) && textureList1[item.Key] == textureList0[item.Key])
                    continue;

                textureErrorMsg = $"{item1Name} does not contain texture {item.Key}";
                return false;
            }

            textureErrorMsg = null;
            return true;
        }






    }
}
