using System;
using System.Collections.Generic;
using System.Linq;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using Serilog;
using Shared.Core.PackFiles;
using Shared.GameFormats.RigidModel.MaterialHeaders;

namespace Editors.BmdEditor.Services
{
    /// <summary>
    /// Loads each prop model once and shares its LOD 0 geometry between every prop that uses it, so
    /// a BMD with 200 copies of a fence builds one set of vertex and index buffers, not 200.
    /// Scene nodes count their uses (see <see cref="SharedPropMeshNode"/>): a model's GPU buffers
    /// are freed, and the model forgotten, when the last node using it leaves the scene.
    /// </summary>
    public class BmdPropMeshCache(IPackFileService packFileService, Rmv2ModelNodeLoader rmv2ModelNodeLoader, IModelAssetResolver modelAssetResolver)
    {
        private readonly ILogger _logger = Log.ForContext<BmdPropMeshCache>();
        private readonly Dictionary<string, PropModel> _models = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>A loaded model: its LOD 0 meshes, or why it could not be loaded.</summary>
        public sealed class PropModel
        {
            internal PropModel(string path, IReadOnlyList<MeshPart> parts, string? failureReason)
            {
                Path = path;
                Parts = parts;
                FailureReason = failureReason;
            }

            public string Path { get; }
            public IReadOnlyList<MeshPart> Parts { get; }
            public string? FailureReason { get; }
            internal int UseCount { get; set; }
        }

        public sealed record MeshPart(MeshObject Geometry, IRmvMaterial RmvMaterial, CapabilityMaterial Material, string Name);

        /// <summary>The model at <paramref name="path"/>, loaded on first request. A model that is
        /// missing or fails to load is remembered too, so its other copies don't retry.</summary>
        public PropModel Get(string path)
        {
            if (_models.TryGetValue(path, out var model))
                return model;

            model = Load(path);
            _models[path] = model;
            return model;
        }

        /// <summary>New scene nodes for one copy of <paramref name="model"/>. They share the cached
        /// geometry, each with its own copy of the material.</summary>
        public List<SharedPropMeshNode> CreateInstance(PropModel model) =>
            model.Parts.Select(part => new SharedPropMeshNode(this, model, part)).ToList();

        internal void AddUse(PropModel model)
        {
            model.UseCount++;
            foreach (var part in model.Parts)
                part.Geometry.EnsureGraphicsResourcesCreated();
        }

        internal void RemoveUse(PropModel model)
        {
            model.UseCount--;
            if (model.UseCount > 0)
                return;

            foreach (var part in model.Parts)
                part.Geometry.RemoveGraphicsCardResources();
            if (_models.TryGetValue(model.Path, out var cached) && cached == model)
                _models.Remove(model.Path);
        }

        /// <summary>Forgets models no scene node uses, freeing geometry that was built but never
        /// added to the scene. Models that failed to load are kept unless
        /// <paramref name="includeFailures"/> is set, so an Add or Delete rebuild doesn't retry
        /// them all; a reload from the packs passes true to try them again.</summary>
        public void RemoveUnused(bool includeFailures)
        {
            foreach (var model in _models.Values.Where(m => m.UseCount <= 0 && (includeFailures || m.FailureReason == null)).ToList())
            {
                foreach (var part in model.Parts)
                    part.Geometry.RemoveGraphicsCardResources();
                _models.Remove(model.Path);
            }
        }

        private PropModel Load(string path)
        {
            var modelFile = packFileService.FindFile(path);
            if (modelFile == null)
            {
                _logger.Warning($"Prop model file not found: {path}");
                return new PropModel(path, [], "File not found");
            }

            try
            {
                var resolvedAsset = modelAssetResolver.Resolve(modelFile);
                var modelFullPath = packFileService.GetFullPath(resolvedAsset.GeometryFile);

                // Only LOD 0 is drawn, so only LOD 0 is built.
                var lodNode = rmv2ModelNodeLoader.CreateModelNodesFromAsset(resolvedAsset, modelFullPath, onlyLoadRootNode: true).FirstOrDefault();
                if (lodNode == null)
                    return new PropModel(path, [], "No LOD nodes found");

                var parts = lodNode.Children.OfType<Rmv2MeshNode>()
                    .Select(mesh => new MeshPart(mesh.Geometry, mesh.RmvMaterial, mesh.Material, mesh.Name))
                    .ToList();
                return new PropModel(path, parts, parts.Count == 0 ? "No meshes found" : null);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading prop '{path}': {ex.Message}");
                return new PropModel(path, [], ex.Message);
            }
        }
    }

    /// <summary>A prop mesh drawing geometry shared through <see cref="BmdPropMeshCache"/>. Where a
    /// plain <see cref="Rmv2MeshNode"/> frees its own buffers when it leaves the scene, this one
    /// only gives up its use of the shared ones, which are freed when the last user leaves.</summary>
    public class SharedPropMeshNode : Rmv2MeshNode
    {
        private readonly BmdPropMeshCache _cache;
        private readonly BmdPropMeshCache.PropModel _model;
        private bool _inUse;

        internal SharedPropMeshNode(BmdPropMeshCache cache, BmdPropMeshCache.PropModel model, BmdPropMeshCache.MeshPart part)
            : base(part.Geometry, part.RmvMaterial, part.Material.Clone(), null!)
        {
            _cache = cache;
            _model = model;
            Name = part.Name;
        }

        // AddObject calls OnNodeAdded each time the node, or any group above it, is added to a
        // parent, so the use is only counted once.
        public override void OnNodeAdded()
        {
            if (_inUse)
                return;
            _inUse = true;
            _cache.AddUse(_model);
        }

        public override void OnNodeRemoved()
        {
            if (!_inUse)
                return;
            _inUse = false;
            _cache.RemoveUse(_model);
        }
    }
}
