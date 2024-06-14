using System.Text;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using System.Numerics;
using System.Linq;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using Shared.GameFormats.Animation;
using System.Collections.ObjectModel;


//emp_karl_franz_hammer_2h_01.rigid_model_v2
//emp_karl_franz.rigid_model_v2.rigid_model_v2
//animations\battle\humanoid01\2handed_hammer\stand\hu1_2hh_stand_idle_01.anim
//animations\skeletons\humanoid01.anim
//animations\skeletons\humanoid01.bone_inv_trans_mats

//using VERTEX = SharpGLTF.Geometry.VertexBuilder<VertexPosition, VertexColor1, VertexJoints4>;
//using MESH = SharpGLTF.Geometry.MeshBuilder<SharpGLTF.Geometry.VertexTypes.VertexPosition, VertexColor1, VertexJoints4>;

internal class Program
{
    private static void Main(string[] args)
    {
        //SharpGLTF.Geometry.VertexTypes.VertexPositionNormalTangent
        ////SharpGLTF.Collections.
        //SharpGLTF.Geometry.VertexTypes.


        // Load the mesh
        // ----------------------------
        var packFilePath = "..\\..\\..\\..\\..\\Data\\Karl_and_celestialgeneral.pack";
        using var fileStream = File.OpenRead(packFilePath);
        using var reader = new BinaryReader(fileStream, Encoding.ASCII);

        var packfileContainer = PackFileSerializer.Load(packFilePath, reader, null, false, new CaPackDuplicatePackFileResolver());
        var packFile = packfileContainer.FileList
            .Where(x => x.Key.ToLower().Contains("emp_karl_franz.rigid_model_v2"))
            .Select(x => x.Value)
            .First();

        var factory = new ModelFactory();
        var rmv = factory.Load(packFile.DataSource.ReadData());
        var lodLevel = rmv.ModelList.First();

        // Load skeleton
        // ----------------------------
        var skeletonPackFile = packfileContainer.FileList
          .Where(x => x.Key.ToLower().Contains("humanoid01.anim"))
          .Select(x => x.Value)
          .First();

        var animFile = AnimationFile.Create(skeletonPackFile);

        // Load invMatrix
        // ----------------------------
        var invMatrixPackFile = packfileContainer.FileList
          .Where(x => x.Key.ToLower().Contains("humanoid01.bone_inv_trans_mats"))
          .Select(x => x.Value)
          .First();

        var invMatrixFile = AnimInvMatrixFile.Create(invMatrixPackFile.DataSource.ReadDataAsChunk());


        // Init gltf
        // ----------------------------
        // var scene = new SharpGLTF.Scenes.SceneBuilder();

        var model = ModelRoot.CreateModel();
        var scene = model.UseScene("default");

        Node bone = scene.CreateNode("Export root");
        var bindings = CreateSkeletonFromGameSkeleton(animFile, invMatrixFile, bone);


        //var bindings = new List<Node>();
        //for (var i = 0; i < 10; ++i)
        //{
        //    bone = bone.CreateNode("myBone" + i);
        //    bone.LocalTransform = Matrix4x4.CreateTranslation(0, 1, 0);
        //    bindings.Add(bone);
        //}

        //var skin = model.CreateSkin("TestSkin");
        //skin.Skeleton = bone;
        //scene.CreateNode("SkinNode").WithSkin(skin);

        // Construct the scene 
        // ----------------------------
        foreach (var exportMesh in lodLevel)
        {
            var mesh = model.CreateMesh(CreateMesh(exportMesh));
            //scene.CreateNode("Node").WithMesh(mesh);
            scene.CreateNode(exportMesh.Material.ModelName).WithSkinnedMesh(mesh, bindings.ToArray());
        }

        //var model = scene.ToGltf2();
        model.SaveGLTF("mesh.gltf");
    }

    static MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4> CreateMesh(RmvModel rmvMesh)
    {
        var mesh = new MeshBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4>(rmvMesh.Material.ModelName);
        mesh.VertexPreprocessor.SetValidationPreprocessors();
        var prim = mesh.UsePrimitive(MaterialBuilder.CreateDefault());

        var vertexList = new List<VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4>>();
        foreach (var vertex in rmvMesh.Mesh.VertexList)
        {
            var glTfvertex = new VertexBuilder<VertexPositionNormal, VertexTexture1, VertexJoints4>();
            glTfvertex.Geometry.Position = new Vector3(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
            glTfvertex.Geometry.Normal = new Vector3(vertex.Normal.X, vertex.Normal.Y, vertex.Normal.Z);
            glTfvertex.Material.TexCoord = new Vector2(0, 0);
            glTfvertex.Skinning.Weights = new Vector4(0, 1, 0, 0);
            glTfvertex.Skinning.Joints = new Vector4(0, 1, 0, 0);
            vertexList.Add(glTfvertex);
        }

        var triangleCount = rmvMesh.Mesh.IndexList.Length;
        for (var i = 0; i < triangleCount; i += 3)
        {
            var i0 = rmvMesh.Mesh.IndexList[i + 0];
            var i1 = rmvMesh.Mesh.IndexList[i + 1];
            var i2 = rmvMesh.Mesh.IndexList[i + 2];

            prim.AddTriangle(vertexList[i0], vertexList[i1], vertexList[i2]);
        }

        return mesh;
       
    }

    public static List<(Node node, Matrix4x4 invMatrix)> CreateSkeletonFromGameSkeleton(AnimationFile file, AnimInvMatrixFile invMatrixFile, Node orgRoot)
    {

        var invMatrixRootBase = invMatrixFile.MatrixList[0];
        var frameBase = file.AnimationParts[0].DynamicFrames[0];


        var output = new List<(Node node, Matrix4x4 invMatrix)>();
        var rootNode = (orgRoot.CreateNode("Bone_0")
                   .WithLocalRotation(new Quaternion(frameBase.Quaternion[0].X, frameBase.Quaternion[0].Y, frameBase.Quaternion[0].Z, frameBase.Quaternion[0].W))
                   .WithLocalTranslation(new Vector3(frameBase.Transforms[0].X, frameBase.Transforms[0].Y, frameBase.Transforms[0].Z))

                   ,
                  Matrix4x4.Transpose(new Matrix4x4(invMatrixRootBase.M11, invMatrixRootBase.M12, invMatrixRootBase.M13, invMatrixRootBase.M14,
                   invMatrixRootBase.M21, invMatrixRootBase.M22, invMatrixRootBase.M23, invMatrixRootBase.M24,
                   invMatrixRootBase.M31, invMatrixRootBase.M32, invMatrixRootBase.M33, invMatrixRootBase.M34,
                   invMatrixRootBase.M41, invMatrixRootBase.M42, invMatrixRootBase.M43, invMatrixRootBase.M44)));
        output.Add(rootNode);
        var allBonesButFirst = file.Bones.Skip(1);
        foreach (var boneInfo in allBonesButFirst)
        {
            var parent = FindBoneInList(boneInfo.ParentId, output.Select(x=>x.node).ToList());
            if (parent == null)
            {

            }
            else
            {
                var frame = file.AnimationParts[0].DynamicFrames[0];

                var invMatrix = invMatrixFile.MatrixList[boneInfo.Id];

                var newNode = (parent.CreateNode("Bone_" + boneInfo.Id)
                    .WithLocalRotation(new Quaternion(frame.Quaternion[boneInfo.Id].X, frame.Quaternion[boneInfo.Id].Y, frame.Quaternion[boneInfo.Id].Z, frame.Quaternion[boneInfo.Id].W))
                    .WithLocalTranslation(new Vector3(frame.Transforms[boneInfo.Id].X, frame.Transforms[boneInfo.Id].Y, frame.Transforms[boneInfo.Id].Z))

                    ,
                   Matrix4x4.Transpose( new Matrix4x4(invMatrix.M11, invMatrix.M12, invMatrix.M13, invMatrix.M14, 
                    invMatrix.M21, invMatrix.M22, invMatrix.M23, invMatrix.M24,
                    invMatrix.M31, invMatrix.M32, invMatrix.M33, invMatrix.M34,
                    invMatrix.M41, invMatrix.M42, invMatrix.M43, invMatrix.M44)));

                output.Add(newNode);
            }
        }
        return output;
    }



    static Node? FindBoneInList(int parentId, IEnumerable<Node> boneList)
    {
        foreach (var bone in boneList)
        {
            var boneIndex = int.Parse(bone.Name.Replace("Bone_", ""));
            if (boneIndex == parentId)
                return bone;
        }

        foreach (var bone in boneList)
        {
            var res = FindBoneInList(parentId, bone.VisualChildren);
            if (res != null)
                return res;
        }

        return null;
    }

}
