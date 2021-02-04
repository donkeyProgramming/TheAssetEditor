using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Rendering;

namespace View3D.Components.Component.Selection
{
    public static class PickingUtil
    {
        public static RenderItem SelectObject(Ray ray, SceneManager scene)
        {
            RenderItem bestItem = null;
            float bestDistance = float.MaxValue;

            foreach (var item in scene.RenderItems)
            {
                var distance = item.Geometry.IntersectObject(ray, item.ModelMatrix);
                if (distance != null)
                {
                    if (distance < bestDistance)
                    {
                        bestDistance = distance.Value;
                        bestItem = item;
                    }
                }
            }

            return bestItem;
            
        }

       //bool SelectFaces(Ray ray, ISelectionState currentState)
       //{
       //    if (currentState.Mode == GeometrySelectionMode.Face)
       //    {
       //        var faceState = currentState as FaceSelectionState;
       //
       //        if (faceState.RenderObject.Geometry.IntersectFace(ray, faceState.RenderObject.ModelMatrix, out var selectedFace) != null)
       //        {
       //            _logger.Here().Information($"Selected face {selectedFace} in {faceState.RenderObject.Name}");
       //
       //            FaceSelectionCommand faceSelectionCommand = new FaceSelectionCommand(_selectionManager)
       //            {
       //                IsModification = _keyboard.IsKeyDown(Keys.LeftShift),
       //                SelectedFaces = new List<int>() { selectedFace.Value }
       //            };
       //            _commandManager.ExecuteCommand(faceSelectionCommand);
       //            return true;
       //        }
       //    }
       //
       //    return false;
       //}
    }
}
