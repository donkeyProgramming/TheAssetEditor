using GameWorld.Core.Animation;
using GameWorld.Core.Rendering.Geometry;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Rendering.Materials.Shaders;
using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.GameFormats.Animation;
using Shared.Core.PackFiles.Models;
using Shared.GameFormats.RigidModel;
using Shared.GameFormats.WsModel;

namespace GameWorld.Core.Vfx
{
    /// <summary>A model's LOD 0 meshes, each with its placement in the model, the box around them, and its skeleton's name.</summary>
    public record VfxModel(IReadOnlyList<(MeshObject Geometry, IShader Material, Matrix Local)> Meshes, BoundingBox Box, string Skeleton = "");

    /// <summary>
    /// The models continuous_rigid particles draw, loaded once per editor through the editor's own mesh loader, which returns
    /// null for a model not in the packs or that did not load (and says so itself). Owns the geometry it loaded; the
    /// materials' shaders belong to the app.
    /// </summary>
    public sealed class VfxMeshCache : IDisposable
    {
        static readonly VfxModel s_none = new([], new BoundingBox());
        static readonly Matrix[] s_bindPose = Enumerable.Repeat(Matrix.Identity, 256).ToArray();
        readonly Func<string, SceneNode?> _load;
        readonly Dictionary<string, VfxModel> _models = [];
        readonly Func<string, AnimationFile?>? _loadAnimation;
        readonly Action<string>? _log;
        readonly Dictionary<(string Skeleton, string Anim), (GameSkeleton Skeleton, AnimationClip Clip)?> _clips = [];

        /// <summary>loadAnimation: an AnimFile or a skeleton (animations/skeletons/name.anim) by path, null (said by itself) when
        /// missing; without it every model holds its bind pose.</summary>
        public VfxMeshCache(Func<string, SceneNode?> load, Func<string, AnimationFile?>? loadAnimation = null, Action<string>? log = null) =>
            (_load, _loadAnimation, _log) = (load, loadAnimation, log);

        static string Key(string path) => path.Replace('/', '\\').ToLowerInvariant();

        public VfxModel Get(string path)
        {
            var key = Key(path);
            if (_models.TryGetValue(key, out var known))
                return known;

            var root = _load(path);
            var models = root == null ? [] : root is Rmv2ModelNode single ? [single] : SceneNodeHelper.GetChildrenOfType<Rmv2ModelNode>(root);
            var meshes = models
                .SelectMany(model => model.GetLodNodes().OrderBy(lod => lod.LodValue).Take(1))
                .SelectMany(lod => SceneNodeHelper.GetChildrenOfType<Rmv2MeshNode>(lod))
                .Select(BindPose)
                .Select(mesh => (mesh.Geometry, (IShader)mesh.Material, Matrix.CreateTranslation(mesh.PivotPoint) * mesh.ModelMatrix))
                .ToList();
            return _models[key] = meshes.Count == 0 ? s_none
                : new VfxModel(meshes, meshes.Select(x => x.Geometry.BoundingBox).Aggregate(BoundingBox.CreateMerged),
                    meshes.Select(x => x.Geometry.SkeletonName).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "");
        }

        /// <summary>Poses a model's meshes for its next draw: anim sampled at age (seconds since the particle's birth) at speed,
        /// looping (VfxConventions.AnimShare); the bind pose when there is no anim or it, or the model's skeleton, does not load.</summary>
        public void Pose(VfxModel model, string? anim, float age, float speed)
        {
            var clip = anim == null ? null : Clip(model.Skeleton, anim);
            var transforms = s_bindPose;
            if (clip is var (skeleton, animation))
            {
                var frame = AnimationSampler.Sample(VfxConventions.AnimShare(age, speed, animation.PlayTimeInSec), skeleton, animation);
                transforms = s_bindPose.ToArray();
                for (var i = 0; i < Math.Min(frame.BoneTransforms.Count, transforms.Length); i++)
                    transforms[i] = frame.BoneTransforms[i].WorldTransform;
            }
            foreach (var (geometry, material, _) in model.Meshes)
                if ((material as CapabilityMaterial)?.TryGetCapability<AnimationCapability>() is { } animated)
                    (animated.AnimationTransforms, animated.ApplyAnimation) = (transforms, clip != null && geometry.VertexFormat != UiVertexFormat.Static);
        }

        (GameSkeleton, AnimationClip)? Clip(string skeletonName, string anim)
        {
            var key = (skeletonName, Key(anim));
            if (_clips.TryGetValue(key, out var known))
                return known;
            _clips[key] = null;
            if (_loadAnimation == null || skeletonName.Length == 0)
                return null;
            if (_loadAnimation($"animations/skeletons/{skeletonName}.anim") is not { } skeletonFile || _loadAnimation(anim) is not { } animFile)
                return null;
            try
            {
                var skeleton = new GameSkeleton(skeletonFile, null!);
                return _clips[key] = (skeleton, new AnimationClip(animFile, skeleton));
            }
            catch (Exception e)
            {
                _log?.Invoke($"rigid model animation {anim} does not play on skeleton {skeletonName}: {e.Message}");
                return null;
            }
        }

        /// <summary>The custom shaders (VfxConventions.IsCustomShader) a wsmodel's materials name: the preview draws those meshes
        /// plain and lit. Empty for any other model, and for a wsmodel that does not read, whose load says so.</summary>
        public static IReadOnlyList<string> CustomShaders(IPackFileService packFileService, string path)
        {
            if (!path.EndsWith(".wsmodel", StringComparison.OrdinalIgnoreCase) || packFileService.FindFile(path) is not { } file)
                return [];
            try
            {
                return new WsModelFile(file).MaterialList
                    .Select(x => packFileService.FindFile(x.MaterialPath)).OfType<PackFile>()
                    .Select(x => new WsModelMaterialFile(x).ShaderPath)
                    .Where(VfxConventions.IsCustomShader).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch (Exception)
            {
                return [];
            }
        }

        /// <summary>Loads an AnimFile or a skeleton from the loaded packs; missing or unreadable, null and one log line.</summary>
        public static Func<string, AnimationFile?> PackAnimationLoader(IPackFileService packFileService, Action<string> log) => path =>
        {
            if (packFileService.FindFile(path) is not { } file)
            {
                log($"{path} is not in the loaded packs, so the rigid model holds its bind pose");
                return null;
            }
            try
            {
                return AnimationFile.Create(file);
            }
            catch (Exception e)
            {
                log($"{path} did not load, so the rigid model holds its bind pose: {e.Message}");
                return null;
            }
        };

        // A mesh node fills its material's bone transforms when it renders itself; drawn from here it never does, and a
        // material with none set throws. The model holds its bind pose.
        static Rmv2MeshNode BindPose(Rmv2MeshNode mesh)
        {
            if (mesh.Material.TryGetCapability<AnimationCapability>() is { } animation)
                (animation.AnimationTransforms, animation.AnimationWeightCount, animation.ApplyAnimation) = (s_bindPose, mesh.Geometry.WeightCount, false);
            return mesh;
        }

        /// <summary>A file changed in a pack: let go of what was loaded from it, so the next draw loads the new one. A wsmodel is
        /// built from geometry and material files this cache does not track, so a change to any model file lets go of every model.</summary>
        public void Forget(string path)
        {
            var key = Key(path);
            foreach (var clip in _clips.Keys.Where(x => x.Anim == key || Key($"animations/skeletons/{x.Skeleton}.anim") == key).ToList())
                _clips.Remove(clip);
            // Any model file reloads every model; track the files each wsmodel names if reloads get slow.
            var models = s_modelFiles.Any(key.EndsWith) ? _models.Keys.ToList() : [key];
            foreach (var model in models)
                if (_models.Remove(model, out var loaded))
                    foreach (var mesh in loaded.Meshes)
                        mesh.Geometry.Dispose();
        }

        static readonly string[] s_modelFiles = [".rigid_model_v2", ".wsmodel", ".material"];

        public void Dispose()
        {
            foreach (var mesh in _models.Values.SelectMany(x => x.Meshes))
                mesh.Geometry.Dispose();
            _models.Clear();
            _clips.Clear();
        }
    }
}
