using Shared.GameFormats.RigidModel;
using SharpGLTF.Materials;
using System.Numerics;
using SharpGLTF.Schema2;
using Shared.GameFormats.Animation;
using MeshImportExport;

//https://github.com/ValveResourceFormat/ValveResourceFormat/blob/master/ValveResourceFormat/IO/GltfModelExporter.Material.cs material info

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
        // Load the files
        // ----------------------------
        var packFilePath = "..\\..\\..\\..\\..\\Data\\Karl_and_celestialgeneral.pack";
        var fileHelper = new FileHelper(packFilePath);
        using var fileStream = File.OpenRead(packFilePath);

        var meshPackFile = fileHelper.FindFile("emp_karl_franz.rigid_model_v2");
        var skeletonPackFile = fileHelper.FindFile("humanoid01.anim");
        var invMatrixPackFile = fileHelper.FindFile("humanoid01.bone_inv_trans_mats");

        var rmv = new ModelFactory().Load(meshPackFile.DataSource.ReadData());
        var animFile = AnimationFile.Create(skeletonPackFile);
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
        var lodLevel = rmv.ModelList.First();
        foreach (var rmvMesh in lodLevel)
        {
            var material = TextureHelper.BuildMaterial(fileHelper, rmvMesh);
            var mesh = model.CreateMesh(MeshExport.CreateMesh(rmvMesh, material));
            scene.CreateNode(rmvMesh.Material.ModelName).WithSkinnedMesh(mesh, bindings.ToArray());
        }

        //var model = scene.ToGltf2();
        model.SaveGLTF("mesh.gltf");
    }

   

    public static List<(Node node, Matrix4x4 invMatrix)> CreateSkeletonFromGameSkeleton(AnimationFile file, AnimInvMatrixFile invMatrixFile, Node orgRoot)
    {

        var invMatrixRootBase = invMatrixFile.MatrixList[0];
        var frameBase = file.AnimationParts[0].DynamicFrames[0];

        Dictionary<Node, BoneMetaData> boneMetaData = new();
        var output = new List<(Node node, Matrix4x4 invMatrix)>();
        var rootNode = (orgRoot.CreateNode(file.Bones[0].Name)
                   .WithLocalRotation(new Quaternion(frameBase.Quaternion[0].X, frameBase.Quaternion[0].Y, frameBase.Quaternion[0].Z, frameBase.Quaternion[0].W))
                   .WithLocalTranslation(new Vector3(frameBase.Transforms[0].X, frameBase.Transforms[0].Y, frameBase.Transforms[0].Z))
                   ,
                  Matrix4x4.Transpose(new Matrix4x4(invMatrixRootBase.M11, invMatrixRootBase.M12, invMatrixRootBase.M13, invMatrixRootBase.M14,
                   invMatrixRootBase.M21, invMatrixRootBase.M22, invMatrixRootBase.M23, invMatrixRootBase.M24,
                   invMatrixRootBase.M31, invMatrixRootBase.M32, invMatrixRootBase.M33, invMatrixRootBase.M34,
                   invMatrixRootBase.M41, invMatrixRootBase.M42, invMatrixRootBase.M43, invMatrixRootBase.M44)));

        boneMetaData[rootNode.Item1] = new BoneMetaData(file.Bones[0].Name, file.Bones[0].Id);
        output.Add(rootNode);
        var allBonesButFirst = file.Bones.Skip(1);
        foreach (var boneInfo in allBonesButFirst)
        {
            var parent = FindBoneInList(boneMetaData, boneInfo.ParentId, output.Select(x => x.node).ToList());
            if (parent == null)
            {

            }
            else
            {
                var frame = file.AnimationParts[0].DynamicFrames[0];

                var invMatrix = invMatrixFile.MatrixList[boneInfo.Id];

                var newNode = (parent.CreateNode(boneInfo.Name)
                    .WithLocalRotation(new Quaternion(frame.Quaternion[boneInfo.Id].X, frame.Quaternion[boneInfo.Id].Y, frame.Quaternion[boneInfo.Id].Z, frame.Quaternion[boneInfo.Id].W))
                    .WithLocalTranslation(new Vector3(frame.Transforms[boneInfo.Id].X, frame.Transforms[boneInfo.Id].Y, frame.Transforms[boneInfo.Id].Z))

                    ,
                   Matrix4x4.Transpose(new Matrix4x4(invMatrix.M11, invMatrix.M12, invMatrix.M13, invMatrix.M14,
                    invMatrix.M21, invMatrix.M22, invMatrix.M23, invMatrix.M24,
                    invMatrix.M31, invMatrix.M32, invMatrix.M33, invMatrix.M34,
                    invMatrix.M41, invMatrix.M42, invMatrix.M43, invMatrix.M44)));

                output.Add(newNode);
                boneMetaData[newNode.Item1] = new BoneMetaData(boneInfo.Name, boneInfo.Id);
            }
        }
        return output;
    }

    record BoneMetaData(string Name, int Id);


    static Node? FindBoneInList(Dictionary<Node, BoneMetaData> boneMetaData, int parentId, IEnumerable<Node> boneList)
    {
        foreach (var bone in boneList)
        {
            var metaData = boneMetaData[bone];
            var boneIndex = metaData.Id;
            if (boneIndex == parentId)
                return bone;
        }

        foreach (var bone in boneList)
        {
            var res = FindBoneInList(boneMetaData, parentId, bone.VisualChildren);
            if (res != null)
                return res;
        }

        return null;
    }
 

    //static void M2ain(string[] args)
    //{
    //    // Create a glTF model
    //    var model = new SceneBuilder();
    //
    //    // Create a material and load the image
    //    var material = new MaterialBuilder()
    //        .WithMetallicRoughnessShader()
    //        .WithChannelImage(KnownChannel.BaseColor, LoadImageAsMemoryImage("path_to_your_image.png"));
    //
    //    // Create a mesh with a single triangle
    //    var mesh = new MeshBuilder<VertexPositionNormal, VertexEmpty, VertexTexture1>("mesh")
    //        .UsePrimitive(material);
    //
    //    mesh.AddTriangle(new VertexPositionNormal(0, 0, 0), new VertexPositionNormal(0, 1, 0), new VertexPositionNormal(1, 0, 0));
    //
    //    // Add the mesh to the scene
    //    model.AddRigidMesh(mesh, Matrix4x4.Identity);
    //
    //    // Save the glTF model
    //    model.SaveGLTF("output_model.gltf");
    //}
    //
   

}
