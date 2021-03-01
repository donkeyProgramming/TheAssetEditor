using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2ModelNode : GroupNode
    {
        // AnimationData

        public RmvRigidModel Model { get; set; }

        public void Update()
        {
            // Updathe the shader?
        }


        public void Render()
        {
            // Add to render qeueue.
        }

        public Rmv2ModelNode(RmvRigidModel model, GraphicsDevice device, ResourceLibary resourceLib, string name, AnimationPlayer animationPlayer)
        {
            Model = model;
            Name = name;

            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var node = new Rmv2MeshNode(model.MeshList[lodIndex][modelIndex], resourceLib, animationPlayer);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }

                lodNode.IsVisible = lodIndex == 0;
                AddObject(lodNode);
            }
        }

        public Rmv2ModelNode(string name)
        {
            Name = name;
        }

        public void SetModel(RmvRigidModel model, GraphicsDevice device, ResourceLibary resourceLibary, AnimationPlayer animationPlayer)
        {
            Model = model;
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = Children[lodIndex];

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var node = new Rmv2MeshNode(model.MeshList[lodIndex][modelIndex], resourceLibary, animationPlayer);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }
            }
        }

        public List<Rmv2LodNode> GetLodNodes()
        {
            return Children
                .Where(x => x is Rmv2LodNode)
                .Select(x => x as Rmv2LodNode)
                .ToList();
        }

        public List<Rmv2MeshNode> GetModelNodes()
        {
            return GetLodNodes()
                .SelectMany(x => x.Children)
                .Where(x => x is Rmv2MeshNode)
                .Select(x => x as Rmv2MeshNode
                )
                .ToList();
        }

        public override SceneNode Clone()
        {
            var newItem = new Rmv2ModelNode(Name + " - Clone")
            {
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                Name = Name + " - Clone",
                Model = Model,
            };
            return newItem;
        }
    }

 
}
