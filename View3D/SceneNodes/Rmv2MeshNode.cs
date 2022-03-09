using CommonControls.Common;
using CommonControls.FileTypes.RigidModel;
using CommonControls.FileTypes.RigidModel.MaterialHeaders;
using CommonControls.FileTypes.RigidModel.Types;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Framework.WpfInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using View3D.Animation;
using View3D.Components.Gizmo;
using View3D.Components.Rendering;
using View3D.Rendering.Geometry;
using View3D.Rendering.RenderItems;
using View3D.Rendering.Shading;
using View3D.Services;
using View3D.Utility;

namespace View3D.SceneNodes
{
    public class Rmv2MeshNode : SceneNode, ITransformable, IEditableGeometry, ISelectable, IUpdateable, IDrawableItem
    {
        public IMaterial Material { get; set; }
        public MeshObject Geometry { get; set; }
        public RmvCommonHeader CommonHeader{ get; set; }

        Quaternion _orientation = Quaternion.Identity;
        Vector3 _position = Vector3.Zero;
        Vector3 _scale = Vector3.One;
        ResourceLibary _resourceLib;
        IComponentManager _componentManager;

        public string OriginalFilePath { get; set; }
        public int OriginalPartIndex { get; internal set; }
        public Vector3 Position { get { return _position; } set { _position = value; UpdateMatrix(); } }
        public Vector3 Scale { get { return _scale; } set { _scale = value; UpdateMatrix(); } }
        public Quaternion Orientation { get { return _orientation; } set { _orientation = value; UpdateMatrix(); } }
        public string AttachmentPointName { get; set; } = "";
        public SkeletonBoneAnimationResolver AttachmentBoneResolver { get; set; } = null;

        public bool DisplayBoundingBox { get; set; } = false;
        public bool DisplayPivotPoint { get; set; } = false;

        public override Matrix ModelMatrix { get => base.ModelMatrix; set => UpdateModelMatrix(value); }

        public PbrShader Effect { get; private set; }
        public int LodIndex { get; set; } = -1;
        

        bool _isSelectable = true;
        public bool IsSelectable { get => _isSelectable; set => SetAndNotifyWhenChanged(ref _isSelectable, value); }
        public bool ReduceMeshOnLodGeneration { get; set; } = true;
     

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

        public Rmv2MeshNode(RmvCommonHeader commonHeader, MeshObject meshObject, IMaterial material, AnimationPlayer animationPlayer, IComponentManager componentManager)
        {
            _componentManager = componentManager;
            CommonHeader = commonHeader;
            Material = material;
            AnimationPlayer = animationPlayer;
            Name = Material.ModelName;
            Geometry = meshObject;

            Position = Vector3.Zero;
            Scale = Vector3.One;
            Orientation = Quaternion.Identity;
        }

        public void Initialize(ResourceLibary resourceLib)
        {
            _resourceLib = resourceLib;
            if (_resourceLib != null)
                CreateShader();
        }

        void CreateShader()
        {
            if (_componentManager.GetComponent<RenderEngineComponent>().MainRenderFormat == Rendering.RenderFormats.MetalRoughness)
                Effect = new PbrShader_MetalRoughness(_resourceLib);
            else
                Effect = new PbrShader_SpecGloss(_resourceLib);


            Texture2D diffuse = LoadTexture(TexureType.Diffuse);
            Texture2D baseColour = LoadTexture(TexureType.BaseColour);
            Texture2D specTexture = LoadTexture(TexureType.Specular);
            Texture2D normalTexture = LoadTexture(TexureType.Normal);
            Texture2D glossTexture = LoadTexture(TexureType.Gloss);
            Texture2D materialTexture = LoadTexture(TexureType.MaterialMap);

            Effect.SetTexture(diffuse, TexureType.Diffuse);
            Effect.SetTexture(baseColour, TexureType.BaseColour);
            Effect.SetTexture(specTexture, TexureType.Specular);
            Effect.SetTexture(normalTexture, TexureType.Normal);
            Effect.SetTexture(glossTexture, TexureType.Gloss);
            Effect.SetTexture(materialTexture, TexureType.MaterialMap);
        }

        Texture2D LoadTexture(TexureType type)
        {
            var texture = Material.GetTexture(type);
            if (texture == null)
                return null;

            return _resourceLib.LoadTexture(texture.Value.Path);
        }

        public void Update(GameTime time) { }

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
            Material.SetTexture(texureType, path);

            var texture = LoadTexture(texureType);
            Effect.SetTexture(texture, texureType);
        }

        public void UseTexture(TexureType texureType, bool value)
        {
            Effect.UseTexture(value, texureType);
        }

        public void Render(RenderEngineComponent renderEngine, Matrix parentWorld)
        {
            if (Effect == null || renderEngine.MainRenderFormat != Effect.RenderFormat)
                CreateShader();

            Matrix[] data = new Matrix[256];
            for (int i = 0; i < 256; i++)
                data[i] = Matrix.Identity;

            if (AnimationPlayer != null)
            {
                var frame = AnimationPlayer.GetCurrentAnimationFrame();
                if (frame != null)
                {
                    for (int i = 0; i < frame.BoneTransforms.Count(); i++)
                        data[i] = frame.BoneTransforms[i].WorldTransform;
                }
            }

            Effect.SetAnimationParameters(data, Geometry.WeightCount);
            Effect.UseAnimation = AnimationPlayer.IsEnabled;
            
            if(AttachmentBoneResolver != null)
                parentWorld = parentWorld * AttachmentBoneResolver.GetWorldTransformIfAnimating();

            Effect.UseAlpha = Material.AlphaMode == AlphaMode.Transparent;

            var modelWithOffset = ModelMatrix * Matrix.CreateTranslation(Material.PivotPoint);
            RenderMatrix = modelWithOffset;

            renderEngine.AddRenderItem(RenderBuckedId.Normal, new GeoRenderItem() { Geometry = Geometry, ModelMatrix = modelWithOffset * parentWorld, Shader = Effect });

            if (DisplayPivotPoint)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new LocatorRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Material.PivotPoint, 1));

            if (DisplayBoundingBox)
                renderEngine.AddRenderItem(RenderBuckedId.Normal, new BoundingBoxRenderItem(_resourceLib.GetStaticEffect(ShaderTypes.Line), Geometry.BoundingBox));
        }

        public override ISceneNode CreateCopyInstance() => new Rmv2MeshNode();

        public override void CopyInto(ISceneNode target)
        {
            var typedTarget = target as Rmv2MeshNode;
            typedTarget.Material = Material.Clone();
            typedTarget.CommonHeader = CommonHeader;
            typedTarget.Position = Position;
            typedTarget.Orientation = Orientation;
            typedTarget.Scale = Scale;
            typedTarget.LodIndex = LodIndex;
            typedTarget.ReduceMeshOnLodGeneration = ReduceMeshOnLodGeneration;
            typedTarget.AnimationPlayer = AnimationPlayer;
            typedTarget.CommonHeader = CommonHeader;
            typedTarget.Material = Material.Clone();
            typedTarget.Geometry = Geometry.Clone();
            typedTarget._resourceLib = _resourceLib;
            typedTarget._componentManager = _componentManager;
            typedTarget.Effect = Effect.Clone() as PbrShader_MetalRoughness;
            typedTarget.Geometry = Geometry.Clone();
            typedTarget.OriginalFilePath = OriginalFilePath;
            typedTarget.OriginalPartIndex = OriginalPartIndex;
            base.CopyInto(target);
        }

        public void UpdatePivotPoint(Vector3 newPiv)
        {
            Material.PivotPoint = newPiv;
        }

        public void RecomputeBoundingBox()
        {
            Geometry.BuildBoundingBox();
        }

        public Dictionary<TexureType, string> GetTextures()
        {
            var enumCollection = Enum.GetValues(typeof(TexureType));
            Dictionary<TexureType, string> output = new Dictionary<TexureType, string>();

            foreach (var enumValue in enumCollection)
            {
                var texture = Material.GetTexture((TexureType)enumValue);
                if (texture != null && texture.HasValue)
                {
                    if (string.IsNullOrWhiteSpace(texture.Value.Path) == false)
                        output[(TexureType)enumValue] = texture.Value.Path;
                }
            }

            return output;
        }
    }



}
