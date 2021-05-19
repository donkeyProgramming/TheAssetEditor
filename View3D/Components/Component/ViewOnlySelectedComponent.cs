using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Text;
using View3D.Components.Component.Selection;
using View3D.SceneNodes;

namespace View3D.Components.Component
{
    public class ViewOnlySelectedComponent : BaseComponent
    {
        SceneManager _sceneManager;
        SelectionManager _selectionManager;

        Dictionary<ISceneNode, bool> _visMap;       

        public ViewOnlySelectedComponent(WpfGame game) : base(game)
        {
        }

        public override void Initialize()
        {
            _sceneManager = Game.GetComponent<SceneManager>();
            _selectionManager = Game.GetComponent<SelectionManager>();
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
