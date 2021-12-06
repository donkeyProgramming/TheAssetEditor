using Common;
using CommonControls.Editors.VariantMeshDefinition;
using CommonControls.FileTypes.PackFiles.Models;
using CommonControls.FileTypes.RigidModel;
using CommonControls.Services;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using View3D.Animation;
using View3D.Rendering.Geometry;
using View3D.SceneNodes;
using View3D.Utility;
using static CommonControls.FileTypes.RigidModel.VariantMeshDefinition;

namespace View3D.Services
{
    public class SceneLoader
    {
        ILogger _logger = Logging.Create<SceneLoader>();
        PackFileService _packFileService;
        ResourceLibary _resourceLibary;
        IGeometryGraphicsContextFactory _geometryContextFactory;

        public SceneLoader(ResourceLibary resourceLibary, PackFileService pfs, IGeometryGraphicsContextFactory geometryContextFactory)
        {
            _packFileService = pfs;
            _resourceLibary = resourceLibary;
            _geometryContextFactory = geometryContextFactory;
        }

        public SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player)
        {
            return Load(file, parent, player, null);
        }

        SceneNode Load(PackFile file, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            if (file == null)
                throw new Exception("File is null in SceneLoader::Load");

            _logger.Here().Information($"Attempting to load file {file.Name}");

            switch (file.Extention)
            {
                case ".variantmeshdefinition":
                    LoadVariantMesh(file, ref parent, player, attachmentPointName);
                    break;

                case ".rigid_model_v2":
                    LoadRigidMesh(file, ref parent, player, attachmentPointName);
                    break;

                case ".wsmodel":
                    LoadWsModel(file, ref parent, player, attachmentPointName);
                    break;
                default:
                    throw new Exception("Unknown mesh extention");
            }

            return parent;
        }

        void Load(string path, SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var file = _packFileService.FindFile(path);
            if (file == null)
            {
                _logger.Here().Error($"File {path} not found");
                return;
            }

            Load(file, parent, player , attachmentPointName);
        }


        void LoadVariantMesh(PackFile file, ref SceneNode parent, AnimationPlayer player , string attachmentPointName)
        {
            var variantMeshElement = new VariantMeshNode(file.Name);
            if (parent == null)
                parent = variantMeshElement;
            else
                parent.AddObject(variantMeshElement);

            try
            {
                var meshFile = VariantMeshToXmlConverter.Load(file);
                LoadVariantMesh(meshFile, variantMeshElement, player, attachmentPointName);
            }
            catch (Exception e)
            {
                _logger.Here().Error("Failed to load file : " + file.Name);
                _logger.Here().Error("Error : " + e.ToString());
                throw e;
            }
        }

        void LoadVariantMesh(VariantMesh mesh, SceneNode root, AnimationPlayer player, string attachmentPointName)
        {
            if (mesh.ChildSlots.Count != 0)
                root = root.AddObject(new SlotsNode("Slots"));

            // Load model
            if (string.IsNullOrWhiteSpace(mesh.ModelReference) != true)
                 Load(mesh.ModelReference.ToLower(), root, player, attachmentPointName); 

            foreach (var slot in mesh.ChildSlots)
            {
                var slotNode = root.AddObject(new SlotNode(slot.Name + " " + slot.AttachmentPoint, slot.AttachmentPoint));

                foreach (var childMesh in slot.ChildMeshes)
                    LoadVariantMesh(childMesh, slotNode, player, attachmentPointName);

                foreach (var meshReference in slot.ChildReferences)
                    Load(meshReference.Reference.ToLower(), slotNode, player, slot.AttachmentPoint);

                for (int i = 0; i < slotNode.Children.Count(); i++)
                {
                    slotNode.Children[i].IsVisible = i == 0;
                    slotNode.Children[i].IsExpanded = false;
            
                    if (slotNode.Name.Contains("stump_"))
                    {
                        slotNode.IsVisible = false;
                        slotNode.IsExpanded = false;
                    }
                }
            }
        }

        Rmv2ModelNode LoadRigidMesh(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var rmvModel = ModelFactory.Create().Load(file.DataSource.ReadData());

            var modelNode = new Rmv2ModelNode(Path.GetFileName(file.Name));
            modelNode.CreateModelNodesFromFile(rmvModel, _resourceLibary, player, _geometryContextFactory);

            foreach (var mesh in modelNode.GetMeshNodes(0))
                mesh.AttachmentPointName = attachmentPointName;

            if (parent == null)
                parent = modelNode;
            else
                parent.AddObject(modelNode);

            return modelNode;
        }

        void LoadWsModel(PackFile file, ref SceneNode parent, AnimationPlayer player, string attachmentPointName)
        {
            var wsModelNode = new WsModelGroup("WsModel - " + file.Name);
            if (parent == null)
                parent = wsModelNode;
            else
                parent.AddObject(wsModelNode);

            var buffer = file.DataSource.ReadData();
            string xmlString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlString);
            
            var geometryNodes = doc.SelectNodes(@"/model/geometry");
            if (geometryNodes.Count != 0)
            {
                var geometryNode = geometryNodes.Item(0);
                var modelFile = _packFileService.FindFile(geometryNode.InnerText);
                var modelAsBase = wsModelNode as SceneNode;
                var loadedModelNode = LoadRigidMesh(modelFile, ref modelAsBase, player , attachmentPointName);

                // Materials
                var materialNodes = doc.SelectNodes(@"/model/materials/material");
                foreach (XmlNode materialNode in materialNodes)
                {
                    var materialFilePath = materialNode.InnerText;
                    var partIndex = materialNode.Attributes.GetNamedItem("part_index").InnerText;
                    var lodIndex = materialNode.Attributes.GetNamedItem("lod_index").InnerText;

                    var materialFile = _packFileService.FindFile(materialFilePath);
                    var materialConfig = new WsModelFile(materialFile, "");

                    var mesh = loadedModelNode.GetMeshNode(int.Parse(lodIndex), int.Parse(partIndex));
                    if (mesh == null)
                    {
                        _logger.Here().Error($"Trying to access mesh at index {partIndex} at lod {lodIndex}, which is not found ");
                    }
                    else
                    {
                        bool useAlpha = materialConfig.Alpha;
                        if (useAlpha)
                            mesh.Material.AlphaMode = AlphaMode.Alpha_Test;
                        else
                            mesh.Material.AlphaMode = AlphaMode.Opaque;

                        foreach (var newTexture in materialConfig.Textures)
                            mesh.UpdateTexture(newTexture.Value, newTexture.Key);
                    }
                }
            }
        }
    }
}
