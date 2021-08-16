using Filetypes.RigidModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public RmvSubModel MeshModel { get; set; }

        // Remove mesh model!

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;
        ResourceLibary _resourceLib;

        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }
        public string AttachmentPointName { get; set; } = "";
        public SkeletonBoneAnimationResolver AttachmentBoneResolver { get; set; } = null;

        public bool DisplayBoundingBox { get; set; } = false;
        public bool DisplayPivotPoint { get; set; } = false;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }

        private void UpdateModelMatrix(Matrix value)
        {
            base.ModelMatrix = value;
            RenderMatrix = value;
        }

        void UpdateMatrix()
        {
            ModelMatrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Orientation) * Matrix.CreateTranslation(Position);
        }


        public AnimationPlayer AnimationPlayer;

        private Rmv2MeshNode()
        { }

        public Rmv2MeshNode(RmvSubModel rmvSubModel, IGeometryGraphicsContext context, ResourceLibary resourceLib, AnimationPlayer animationPlayer, IGeometry geometry = null)
        {
            MeshModel = rmvSubModel;
            _resourceLib = resourceLib;
            Geometry = geometry;
            if (Geometry == null)
                Geometry = new Rmv2Geometry(rmvSubModel, context);
            AnimationPlayer = animationPlayer;

            Name = rmvSubModel.Header.ModelName;
            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;

            if (resourceLib != null)
            {
                Effect = new PbrShader(resourceLib);
                Texture2D diffuse = LoadTexture(TexureType.Diffuse);
                if(diffuse == null)
                    diffuse = LoadTexture(TexureType.Diffuse_alternative);
                Texture2D specTexture = LoadTexture(TexureType.Specular);
                Texture2D normalTexture = LoadTexture(TexureType.Normal);
                Texture2D glossTexture = LoadTexture(TexureType.Gloss);

                (Effect as IShaderTextures).SetTexture(diffuse, TexureType.Diffuse);
                (Effect as IShaderTextures).SetTexture(specTexture, TexureType.Specular);
                (Effect as IShaderTextures).SetTexture(normalTexture, TexureType.Normal);
                (Effect as IShaderTextures).SetTexture(glossTexture, TexureType.Gloss);
            }

            Texture2D LoadTexture(TexureType type)
            {
                var texture = rmvSubModel.GetTexture(type);
                if (texture == null)
                    return null;
                return resourceLib.LoadTexture(texture.Value.Path);
            }
        }

        public IShader Effect { get; set; }
        public int LodIndex { get; set; } = -1;

        internal RmvSubModel CreateRmvSubModel()
        {
            var newSubModel = MeshModel.Clone();
            newSubModel.Mesh = (Geometry as Rmv2Geometry).CreateRmvMesh();
            return newSubModel;
        }

        public IGeometry Geometry { get; set; }

        bool _isSelectable = true;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }
        public bool ReduceMeshOnLodGeneration { get; set; } = true;

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


        public void UpdateTexture(string path, TexureType texureType)
        {
            var texture = _resourceLib.LoadTexture(path);
            (Effect as IShaderTextures).SetTexture(texture, texureType);

            for (int i = 0; i < MeshModel.Textures.Count; i++)
            {
                if (MeshModel.Textures[i].TexureType == texureType)
                {
                    var tex = MeshModel.Textures[i];
                    tex.Path = path;
                    MeshModel.Textures[i] = tex;
                    break;
                }
            }
        }

        public void UseTexture(TexureType texureType, bool value)
        {
            (Effect as IShaderTextures).UseTexture(value, texureType);
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
                    var frame = AnimationPlayer.GetCurrentAnimationFrame();
                    if (frame != null)
                    {
                        for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                        {
                            //var d = frame.BoneTransforms[i].WorldTransform;
                            //d.M44 = 1;
                            //frame.BoneTransforms[i].WorldTransform = d;
                            data[i] = frame.BoneTransforms[i].WorldTransform;
                        }
                    }
                }

                animationEffect.SetAnimationParameters(data, (Geometry as Rmv2Geometry).WeightCount);
                animationEffect.UseAnimation = AnimationPlayer.IsEnabled;
            }


            if(AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransform();

            if (Effect is IShaderTextures tetureEffect)
                tetureEffect.UseAlpha = MeshModel.AlphaSettings.Mode == AlphaMode.Alpha_Test;

            var pivotPos = new Vector3(MeshModel.Header.Transform.Pivot.X, MeshModel.Header.Transform.Pivot.Y, MeshModel.Header.Transform.Pivot.Z);
            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(pivotPos);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = modelWithOffset * parentWorld, Shader = Effect });

            if (DisplayPivotPoint)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new LocatorRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), pivotPos, 1));

            if (DisplayBoundingBox)
            {
                var bb = new BoundingBox(new Vector3(MeshModel.Header.BoundingBox.MinimumX, MeshModel.Header.BoundingBox.MinimumY, MeshModel.Header.BoundingBox.MinimumZ),
                    new Vector3(MeshModel.Header.BoundingBox.MaximumX, MeshModel.Header.BoundingBox.MaximumY, MeshModel.Header.BoundingBox.MaximumZ));
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), bb));
            }
        }

        public override ISceneNode Clone()
        {
            var node = CloneWithoutGeometry();
            node.Geometry = Geometry.Clone();
            return node;
        }

        public Rmv2MeshNode CloneWithoutGeometry()
        {
            var newItem = new Rmv2MeshNode()
            {
                Position = Position,
                Orientation = Orientation,
                Scale = Scale,
                Parent = Parent,
                SceneManager = SceneManager,
                IsEditable = IsEditable,
                IsVisible = IsVisible,
                IsSelectable = IsSelectable,
                LodIndex = LodIndex,
                Name = Name + " - Clone",
                AnimationPlayer = AnimationPlayer,
                MeshModel = MeshModel.Clone(),
                _resourceLib = _resourceLib,
                ReduceMeshOnLodGeneration = ReduceMeshOnLodGeneration
            };
            newItem.Effect = Effect.Clone();
            return newItem;
        }

        public void UpdatePivotPoint(Vector3 newPiv)
        {
            var header = MeshModel.Header;
            var transform = header.Transform;

            transform.Pivot = new Filetypes.RigidModel.Transforms.RmvVector3((float)newPiv.X, (float)newPiv.Y, (float)newPiv.Z);

            header.Transform = transform;
            MeshModel.Header = header;
        }

        public void RecomputeBoundingBox()
        {
            var header = MeshModel.Header;
            var bb = header.BoundingBox;

            var newBB = BoundingBox.CreateFromPoints(Geometry.GetVertexList());

            bb.MinimumX = newBB.Min.X;
            bb.MinimumY = newBB.Min.Y;
            bb.MinimumZ = newBB.Min.Z;

            bb.MaximumX = newBB.Max.X;
            bb.MaximumY = newBB.Max.Y;
            bb.MaximumZ = newBB.Max.Z;

            header.BoundingBox = bb;
            MeshModel.Header = header;
        }
    }


    public class Rmv2MeshNodeHelper
    {
        public static List<Rmv2MeshNode> GetAllVisibleMeshes(ISceneNode node)
        {
            return GetAllChildren(node);
        }

        static List<Rmv2MeshNode> GetAllChildren(ISceneNode parent)
        {
            var output = new List<Rmv2MeshNode>();
            var visibleChildren = parent.Children.Where(x => x.IsVisible);

            foreach (var child in visibleChildren)
            {
                if (child is Rmv2MeshNode mesh && mesh.LodIndex==0)
                {
                    output.Add(mesh);
                }
                else
                {
                    var result = GetAllChildren(child);
                    output.AddRange(result);
                }
            }

            return output;
        }
    }
}
