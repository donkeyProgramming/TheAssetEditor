using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using View3D.Rendering;
using View3D.Rendering.Geometry;

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

        public Rmv2ModelNode(RmvRigidModel model, GraphicsDevice device, string name)
        {
            Name = name;

            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = new Rmv2Geometry(model.MeshList[lodIndex][modelIndex], device);
                    var node = RenderItemHelper.CreateRenderItem(geometry, new Vector3(0, 0, 0), new Vector3(1.0f), model.MeshList[lodIndex][modelIndex].Header.ModelName, device);
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

            for (int lodIndex = 0; lodIndex < 4; lodIndex++)
            {
                var lodNode = new Rmv2LodNode("Lod " + lodIndex, lodIndex);
                lodNode.IsVisible = lodIndex == 0;
                AddObject(lodNode);
            }
        }

        public void AddModel(RmvRigidModel model, GraphicsDevice device)
        {
            for (int lodIndex = 0; lodIndex < model.Header.LodCount; lodIndex++)
            {
                var lodNode = Children[lodIndex];

                for (int modelIndex = 0; modelIndex < model.LodHeaders[lodIndex].MeshCount; modelIndex++)
                {
                    var geometry = new Rmv2Geometry(model.MeshList[lodIndex][modelIndex], device);
                    var node = RenderItemHelper.CreateRenderItem(geometry, new Vector3(0, 0, 0), new Vector3(1.0f), model.MeshList[lodIndex][modelIndex].Header.ModelName, device);
                    node.LodIndex = lodIndex;
                    lodNode.AddObject(node);
                }
            }
        }
    }

 
}
