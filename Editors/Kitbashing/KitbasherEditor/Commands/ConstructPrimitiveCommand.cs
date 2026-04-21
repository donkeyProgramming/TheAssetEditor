using GameWorld.Core.Animation;
using GameWorld.Core.Commands;
using GameWorld.Core.Components;
using GameWorld.Core.Components.Selection;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Editors.KitbasherEditor.Commands
{
    internal enum PrimitiveType
    {
        Box,
        Plane,
        Sphere
    }

    internal class ConstructPrimitiveCommand : ICommand
    {
        private readonly SceneManager _sceneManager;
        private readonly SelectionManager _selectionManager;
        private readonly CapabilityMaterialFactory _capabilityMaterialFactory;
        private readonly PrimitiveConstructor _primitiveConstructor;

        private PrimitiveType _primitiveType = PrimitiveType.Box;
        private ISelectionState? _oldSelectionState;
        private Rmv2MeshNode? _createdMeshNode;

        public string HintText => "Construct primitive";
        public bool IsMutation => true;

        public ConstructPrimitiveCommand(SceneManager sceneManager, SelectionManager selectionManager, CapabilityMaterialFactory capabilityMaterialFactory, PrimitiveConstructor primitiveConstructor)
        {
            _sceneManager = sceneManager;
            _selectionManager = selectionManager;
            _capabilityMaterialFactory = capabilityMaterialFactory;
            _primitiveConstructor = primitiveConstructor;
        }

        public void Configure(PrimitiveType primitiveType)
        {
            _primitiveType = primitiveType;
        }

        public void Execute()
        {
            _oldSelectionState = _selectionManager.GetStateCopy();

            var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
            var lod0Node = rootNode.GetLodNodes().First();
            var templateMesh = rootNode.GetMeshesInLod(0, false).FirstOrDefault();

            var template = BuildMaterialTemplate(templateMesh, rootNode);

            _createdMeshNode = _primitiveType switch
            {
                PrimitiveType.Box => ExecuteCreateBox(lod0Node, template),
                PrimitiveType.Plane => ExecuteCreatePlane(lod0Node, template),
                PrimitiveType.Sphere => ExecuteCreateSphere(lod0Node, template),
                _ => throw new NotImplementedException($"Unknown primitive {_primitiveType}")
            };

            var newState = _selectionManager.CreateSelectionSate(GeometrySelectionMode.Object, null) as ObjectSelectionState;
            newState?.ModifySelectionSingleObject(_createdMeshNode, false);
            _selectionManager.SetState(newState!);
        }

        public void Undo()
        {
            if (_createdMeshNode?.Parent != null)
                _createdMeshNode.Parent.RemoveObject(_createdMeshNode);

            if (_oldSelectionState != null)
                _selectionManager.SetState(_oldSelectionState);
        }

        private Rmv2MeshNode ExecuteCreateBox(SceneNode lod0Node, (IRmvMaterial RmvMaterial, CapabilityMaterial CapabilityMaterial, AnimationPlayer? AnimationPlayer, UiVertexFormat VertexFormat, string SkeletonName) template)
        {
            var geometry = _primitiveConstructor.CreateBox(template.VertexFormat, template.SkeletonName, 10);
            return CreateAndInsertNode(lod0Node, geometry, template, "construct box");
        }

        private Rmv2MeshNode ExecuteCreatePlane(SceneNode lod0Node, (IRmvMaterial RmvMaterial, CapabilityMaterial CapabilityMaterial, AnimationPlayer? AnimationPlayer, UiVertexFormat VertexFormat, string SkeletonName) template)
        {
            var geometry = _primitiveConstructor.CreatePlane(template.VertexFormat, template.SkeletonName, 10);
            return CreateAndInsertNode(lod0Node, geometry, template, "construct plane");
        }

        private Rmv2MeshNode ExecuteCreateSphere(SceneNode lod0Node, (IRmvMaterial RmvMaterial, CapabilityMaterial CapabilityMaterial, AnimationPlayer? AnimationPlayer, UiVertexFormat VertexFormat, string SkeletonName) template)
        {
            var geometry = _primitiveConstructor.CreateSphere(template.VertexFormat, template.SkeletonName, 10);
            return CreateAndInsertNode(lod0Node, geometry, template, "construct sphere");
        }

        private static Rmv2MeshNode CreateAndInsertNode(SceneNode lod0Node, MeshObject geometry, (IRmvMaterial RmvMaterial, CapabilityMaterial CapabilityMaterial, AnimationPlayer? AnimationPlayer, UiVertexFormat VertexFormat, string SkeletonName) template, string name)
        {
            var newNode = new Rmv2MeshNode(geometry, template.RmvMaterial.Clone(), template.CapabilityMaterial.Clone(), template.AnimationPlayer)
            {
                Name = name
            };

            lod0Node.AddObject(newNode);
            return newNode;
        }

        private (IRmvMaterial RmvMaterial, CapabilityMaterial CapabilityMaterial, AnimationPlayer? AnimationPlayer, UiVertexFormat VertexFormat, string SkeletonName) BuildMaterialTemplate(Rmv2MeshNode? templateMesh, MainEditableNode rootNode)
        {
            if (templateMesh != null)
            {
                var rmvMaterial = templateMesh.RmvMaterial.Clone();
                rmvMaterial.ModelName = "construct_primitive";
                rmvMaterial.PivotPoint = Vector3.Zero;

                var vertexFormat = templateMesh.Geometry.VertexFormat;
                rmvMaterial.UpdateInternalState(vertexFormat);

                foreach (var texture in rmvMaterial.GetAllTextures().ToList())
                    rmvMaterial.SetTexture(texture.TexureType, string.Empty);

                var capabilityMaterial = _capabilityMaterialFactory.Create(rmvMaterial, null);

                return (rmvMaterial, capabilityMaterial, templateMesh.AnimationPlayer, vertexFormat, templateMesh.Geometry.SkeletonName);
            }

            var fallbackMaterial = MaterialFactory.Create().CreateMaterial(ModelMaterialEnum.weighted);
            fallbackMaterial.ModelName = "construct_primitive";
            fallbackMaterial.PivotPoint = Vector3.Zero;
            fallbackMaterial.UpdateInternalState(UiVertexFormat.Weighted);

            var shader = _capabilityMaterialFactory.Create(fallbackMaterial, null);
            var skeletonName = rootNode.SkeletonNode?.Skeleton?.SkeletonName ?? string.Empty;
            return (fallbackMaterial, shader, null, UiVertexFormat.Weighted, skeletonName);
        }
    }
}