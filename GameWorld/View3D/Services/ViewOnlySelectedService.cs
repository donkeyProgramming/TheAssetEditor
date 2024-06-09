using System.Collections.Generic;
using View3D.Components.Component;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Services
{
    public class ViewOnlySelectedService
    {
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        Dictionary<ISceneNode, bool> _visMap;

        public ViewOnlySelectedService(SceneManager sceneManager, SelectionManager selectionManager)
        {
            _sceneManager = sceneManager;
            _selectionManager = selectionManager;
        }

        public void Toggle()
        {
            if (_visMap == null)
            {
                // Hide
                var selectedObjects = _selectionManager.GetState().SelectedObjects();
                if (selectedObjects.Count == 0)
                    return;

                // Build selection map
                _visMap = new Dictionary<ISceneNode, bool>();
                var allSceneObjects = _sceneManager.GetEnumeratorConditional(x => x is Rmv2MeshNode);
                foreach (var sceneObject in allSceneObjects)
                {
                    _visMap.Add(sceneObject, sceneObject.IsVisible);
                    if (!selectedObjects.Contains(sceneObject as ISelectable))
                        sceneObject.IsVisible = false;
                }
            }
            else
            {
                // Show
                var allSceneObjects = _sceneManager.GetEnumeratorConditional(x => true);
                foreach (var sceneObject in allSceneObjects)
                {
                    if (_visMap.TryGetValue(sceneObject, out var oldVisState))
                        sceneObject.IsVisible = oldVisState;
                }

                _visMap = null;
            }
        }
    }
}
