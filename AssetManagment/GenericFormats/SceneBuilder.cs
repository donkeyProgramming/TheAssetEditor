//using System.Collections.Generic;
//using Matrix = Microsoft.Xna.Framework.Matrix;
//using Vector3 = Microsoft.Xna.Framework.Vector3;
//using Quaternion = Microsoft.Xna.Framework.Quaternion;
//using System;
//using AssetManagement.GenericFormats.Managed;
//using AssetManagement.GenericFormats.Unmanaged;
//using CommonControls.FileTypes.RigidModel;
//using System.Runtime.CompilerServices;
//using System.Linq;
//using CommonControls.FileTypes.RigidModel.Vertex;

//namespace AssetManagement.GenericFormats.Unmanaged
//{
//    public class SceneBuilder
//    {
//        static SceneContainer RmvToSceneContainer(RmvFile inFile)
//        {
//            var newScene = new SceneContainer();

//            if (!inFile.ModelList.Any())
//                return null;


//            foreach (var model in inFile.ModelList[0])
//            {
//                var outMesh = new PackedMesh();

//                for (var i = 0; i < model.Mesh.IndexList.Length; i += 3)
//                {
//                    // build 1 un-indexed triangle
//                    var c1 = model.Mesh.IndexList[i * 3 + 0];
//                    var c2 = model.Mesh.IndexList[i * 3 + 1];
//                    var c3 = model.Mesh.IndexList[i * 3 + 2];

//                    var v1 = ConvertToPackedVertex(model.Mesh.VertexList[c1]);
//                    var v2 = ConvertToPackedVertex(model.Mesh.VertexList[c2]);
//                    var v3 = ConvertToPackedVertex(model.Mesh.VertexList[c3]);

//                    outMesh.Vertices.Add(v1);
//                    outMesh.Vertices.Add(v2);
//                    outMesh.Vertices.Add(v3);                    
//                }

//            }

//            return newScene;
//        }

//        private void AddOneTriangle(ushort[] indices)
//        {

//        }

//        private static ExtPackedCommonVertex ConvertToPackedVertex(CommonVertex inVertex)
//        {
//            var outVertex = new ExtPackedCommonVertex();

//            outVertex.Position.x = inVertex.Position.X;
//            outVertex.Position.y = inVertex.Position.Y;
//            outVertex.Position.z = inVertex.Position.Z;
//            outVertex.Position.w = inVertex.Position.W;                     
            
//            outVertex.Uv.x = inVertex.Uv.X;
//            outVertex.Uv.y = inVertex.Uv.Y;

//            outVertex.Normal.x = inVertex.Normal.X;
//            outVertex.Normal.y = inVertex.Normal.Y;
//            outVertex.Normal.z = inVertex.Normal.Z;

//            return outVertex;
//        }

//    }
//}
