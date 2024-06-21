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
        var bindings = SkeletonExporter.CreateSkeletonFromGameSkeleton(animFile, invMatrixFile, bone);

  
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
}
