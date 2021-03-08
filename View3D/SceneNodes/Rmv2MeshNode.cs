using Filetypes.RigidModel;
using Filetypes.RigidModel.Vertex;
using MeshDecimator;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public RmvSubModel MeshModel { get; set; }

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }


        public AnimationPlayer AnimationPlayer;

        private Rmv2MeshNode()
        { }


        public Rmv2MeshNode(RmvSubModel rmvSubModel, ResourceLibary resourceLib, AnimationPlayer animationPlayer, IGeometry geometry = null)
        {
           // var orgSub = rmvSubModel.Clone();
           //
           //
           //
           // var quality = 0.5f;
           // // ObjMesh sourceObjMesh = new ObjMesh();
           // // sourceObjMesh.ReadFile(sourcePath);
           // var sourceVertices = rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.Math.Vector3d(x.Postition.X, x.Postition.Y, x.Postition.Z)).ToArray();
           //
           //
           // // var sourceTexCoords3D = sourceObjMesh.TexCoords3D;
           // var sourceSubMeshIndices = rmvSubModel.Mesh.IndexList.Select(x => (int)x).ToArray();
           //
           // var sourceMesh = new Mesh(sourceVertices, sourceSubMeshIndices);
           // sourceMesh.Normals = rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.Math.Vector3(x.Normal.X, x.Normal.Y, x.Normal.Z)).ToArray();
           // sourceMesh.Tangents = rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.Math.Vector4(x.Tangent.X, x.Tangent.Y, x.Tangent.Z, x.Tangent.W)).ToArray();
           // sourceMesh.SetUVs(0, rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.Math.Vector2(x.Uv.X, x.Uv.Y)).ToArray());
           //
           //
           // if (rmvSubModel.Header.VertextType == VertexFormat.Cinematic)
           // {
           //     sourceMesh.BoneWeights = rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.BoneWeight(
           //         x.BoneIndex[0], x.BoneIndex[1], x.BoneIndex[2], x.BoneIndex[3],
           //         x.BoneWeight[0], x.BoneWeight[1], x.BoneWeight[2], x.BoneWeight[3])).ToArray();
           // }
           // else if (rmvSubModel.Header.VertextType == VertexFormat.Weighted)
           // {
           //     sourceMesh.BoneWeights = rmvSubModel.Mesh.VertexList.Select(x => new MeshDecimator.BoneWeight(
           //         x.BoneIndex[0], x.BoneIndex[1], 0, 0,
           //         x.BoneWeight[0], x.BoneWeight[1], 0,0)).ToArray();
           // }
           //
           // //if (sourceTexCoords2D != null)
           // //{
           //
           // //}
           // //else if (sourceTexCoords3D != null)
           // //{
           // //    sourceMesh.SetUVs(0, sourceTexCoords3D);
           // //}
           //
           // int currentTriangleCount = sourceSubMeshIndices.Length / 3;
           //
           //
           // int targetTriangleCount = (int)Math.Ceiling(currentTriangleCount * quality);
           // Console.WriteLine("Input: {0} vertices, {1} triangles (target {2})",
           //     sourceVertices.Length, currentTriangleCount, targetTriangleCount);
           //
           // var stopwatch = new System.Diagnostics.Stopwatch();
           // stopwatch.Reset();
           // stopwatch.Start();
           //
           // var algorithm = MeshDecimation.CreateAlgorithm(Algorithm.Default);
           // algorithm.Verbose = true;
           // Mesh destMesh = MeshDecimation.DecimateMesh(algorithm, sourceMesh, targetTriangleCount);
           // stopwatch.Stop();
           //
           // var destVertices = destMesh.Vertices;
           // var destNormals = destMesh.Normals;
           // var destIndices = destMesh.GetSubMeshIndices();
           //
           // CinematicVertex[] outputVerts = new CinematicVertex[destVertices.Length];
           //
           //
           //
           // for (int i = 0; i < outputVerts.Length; i++)
           // {
           //     var pos = destMesh.Vertices[i];
           //     var norm = destMesh.Normals[i];
           //     var tangents = destMesh.Tangents[i];
           //     var uv = destMesh.UV1[i];
           //
           //     Vector3 normal = new Vector3(norm.x, norm.y, norm.z);
           //     Vector3 tangent = new Vector3(tangents.x, tangents.y, tangents.z);
           //     var binormal = Vector3.Normalize(Vector3.Cross(normal, tangent));// * sign
           //
           //
           //     var vert = new CinematicVertex();
           //     vert.Postition = new Filetypes.RigidModel.Transforms.RmvVector4((float)pos.x, (float)pos.y, (float)pos.z);
           //     vert.Normal = new Filetypes.RigidModel.Transforms.RmvVector4((float)norm.x, (float)norm.y, (float)norm.z);
           //     vert.Tangent = new Filetypes.RigidModel.Transforms.RmvVector4((float)tangents.x, (float)tangents.y, (float)tangents.z, tangents.w);
           //     vert.BiNormal = new Filetypes.RigidModel.Transforms.RmvVector4((float)binormal.X, (float)binormal.Y, (float)binormal.Z, 1);
           //
           //     
           //
           //     vert.Uv = new Filetypes.RigidModel.Transforms.RmvVector2()
           //     {
           //         X = uv.x, Y = uv.y
           //     };
           //
           //     if (rmvSubModel.Header.VertextType == VertexFormat.Cinematic)
           //     {
           //         var boneInfo = destMesh.BoneWeights[i];
           //         vert.BoneIndex = new byte[] { (byte)boneInfo.boneIndex0, (byte)boneInfo.boneIndex1, (byte)boneInfo.boneIndex2, (byte)boneInfo.boneIndex3 };
           //         vert.BoneWeight = new float[] { boneInfo.boneWeight0, boneInfo.boneWeight1, boneInfo.boneWeight2, boneInfo.boneWeight3 };
           //     }
           //     else if (rmvSubModel.Header.VertextType == VertexFormat.Weighted)
           //     {
           //         var boneInfo = destMesh.BoneWeights[i];
           //         vert.BoneIndex = new byte[] { (byte)boneInfo.boneIndex0, (byte)boneInfo.boneIndex1 };
           //         vert.BoneWeight = new float[] { boneInfo.boneWeight0, boneInfo.boneWeight1 };
           //     }
           //
           //     outputVerts[i] = vert;
           // }
           //
           // rmvSubModel.Mesh.IndexList = destIndices[0].Select(x => (ushort)x).ToArray();
           // rmvSubModel.Mesh.VertexList = outputVerts;




            MeshModel = rmvSubModel;
            Geometry = geometry;
            if (Geometry == null)
                Geometry = new Rmv2Geometry(rmvSubModel, resourceLib.GraphicsDevice);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            Effect = new PbrShader(resourceLib);
            var diffuse = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Diffuse).Path);
            var specTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Specular).Path);
            var normalTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Normal).Path);
            var glossTexture = resourceLib.LoadTexture(rmvSubModel.GetTexture(TexureType.Gloss).Path);
            //
            (Effect as IShaderTextures).SetTexture(diffuse, TexureType.Diffuse);
            (Effect as IShaderTextures).SetTexture(specTexture, TexureType.Specular);
            (Effect as IShaderTextures).SetTexture(normalTexture, TexureType.Normal);
            (Effect as IShaderTextures).SetTexture(glossTexture, TexureType.Gloss);
        }


        public IShader Effect { get; set; }
        public int LodIndex { get; set; } = -1;
        public IGeometry Geometry { get; set; }
        public bool IsSelectable { get; set; } = true;

        public void Update(GameTime time)
        {

        }

        public Rmv2ModelNode GetParentModel()
        {
            var parent = Parent;
            while (parent != null)
            {
                if (parent is Rmv2ModelNode modelNode)
                    return modelNode;
                parent = parent.Parent;
            }

            return null;
        }

        public Vector3 GetObjectCenter()
        {
            return MathUtil.GetCenter(Geometry.BoundingBox) + Position;
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (Effect is IShaderAnimation animationEffect)
            {
                Matrix[] data = new Matrix[256];
                for (int i = 0; i < 256; i++)
                    data[i] = Matrix.Identity;

                if (AnimationPlayer != null)
                {
                    var frame = AnimationPlayer.GetCurrentFrame();
                    if (frame != null)
                    {
                        for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                            data[i] = frame.BoneTransforms[i].WorldTransform;
                    }
                }

                animationEffect.SetAnimationParameters(data, 4);
                animationEffect.UseAnimation = AnimationPlayer.IsEnabled;
            }

            if (Effect is IShaderTextures tetureEffect)
            {
                tetureEffect.UseAlpha = false;
            }

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = ModelMatrix, Shader = Effect });
        }

        public override ISceneNode Clone()
        {
            var newItem = new Rmv2MeshNode()
            {
                Geometry = Geometry.Clone(),
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                Parent = Parent,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                IsSelectable= IsSelectable,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
                AnimationPlayer = AnimationPlayer,
                MeshModel = MeshModel
            };
            newItem.Effect = Effect.Clone();
            return newItem;
        }
    }
}
