using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
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
            Name = name;

            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var node = new MeshNode(model.MeshList[lodIndex][modelIndex], device, resourceLib, animationPlayer);
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

        public void AddModel(RmvRigidModel model, GraphicsDevice device, ResourceLibary resourceLibary, AnimationPlayer animationPlayer)
        {
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = Children[lodIndex];

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var node = new MeshNode(model.MeshList[lodIndex][modelIndex], device, resourceLibary, animationPlayer);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }
            }
        }
    }

 
}
