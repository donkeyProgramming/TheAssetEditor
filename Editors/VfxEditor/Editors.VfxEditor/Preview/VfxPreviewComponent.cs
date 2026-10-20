using GameWorld.Core.Animation;
using GameWorld.Core.Components;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Services;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;

namespace Editors.VfxEditor.Preview
{
    /// <summary>Plays the open effect at the origin, its unsaved bytes standing in for its file. Lines lists
    /// what was not found or left out and why; a guide model keeps the effect's frame across Shows. Nothing throws.</summary>
    public sealed class VfxPreviewComponent : BaseComponent, IDisposable
    {
        const int Seed = 1;
        readonly IPackFileService _packFileService;
        readonly SceneManager _sceneManager;
        readonly ComplexMeshLoader _meshLoader;
        readonly VfxTextureCache _textures;
        readonly VfxMeshCache _meshes;
        readonly ResourceLibrary _resourceLibrary;
        readonly List<string> _lines = [];
        ParticleSimulator? _simulator;
        VfxEffectNode? _node;
        Func<float, Matrix> _effectWorldAt = _ => Matrix.Identity;
        string? _modelProblem;

        public VfxPreviewComponent(IPackFileService packFileService, SceneManager sceneManager, ComplexMeshLoader meshLoader, ResourceLibrary resourceLibrary)
        {
            _resourceLibrary = resourceLibrary;
            _packFileService = packFileService;
            _sceneManager = sceneManager;
            _meshLoader = meshLoader;
            _textures = new VfxTextureCache(ParticleRenderItem.PackTextureLoader(packFileService, _lines.Add));
            _meshes = new VfxMeshCache(LoadRigid, VfxMeshCache.PackAnimationLoader(packFileService, _lines.Add), _lines.Add);
        }

        public float Time { get; private set; }
        public bool IsPlaying { get; set; } = true;
        public bool Loop { get; set; } = true;
        public float LoopLength { get; set; } = 4;
        public float Speed { get; set; } = 1;
        /// <summary>Fly the effect like a projectile (ProjectileFlight), once per loop; read when Show builds the simulation.</summary>
        public bool Projectile { get; set; }
        public float ProjectileSpeed { get; set; } = 60;
        public int ProjectileAxis { get; set; }
        /// <summary>Draw from the projectile's frame: it and the guide model hold the centre and the trail streams back.</summary>
        public bool StaticView { get; set; }
        public IReadOnlyList<string> Lines => _lines;
        public SceneNode? Model { get; private set; }
        public event Action? TimeChanged;

        public void Show(string effectName, byte[] unsaved, int tier)
        {
            Clear();
            _lines.Clear();
            if (_modelProblem != null)
                _lines.Add(_modelProblem);
            var resolver = new VfxResolver(_packFileService, new Dictionary<string, byte[]> { [VfxResolver.Key(effectName)] = unsaved }, tier);
            var resolved = resolver.Resolve(effectName, new Random(Seed));
            _lines.AddRange(resolved.Problems);
            var settings = resolved.Emitters.Select(EmitterSettings.From).ToList();
            foreach (var emitter in settings)
            {
                _lines.AddRange(emitter.Skipped.Select(gap => $"{emitter.Name}: {gap}"));
                foreach (var model in emitter.Rigid.Select(x => x.ModelFile).Distinct().Where(x => _packFileService.FindFile(x) == null))
                    _lines.Add($"{emitter.Name}: rigid model {model} is not in the loaded packs");
                if (emitter.Texture is { } texture && _packFileService.FindFile(texture) == null)
                    _lines.Add($"{emitter.Name}: texture {texture} is not in the loaded packs");
                else if (emitter.Texture is { } found && _textures.Failed(found))
                    _lines.Add($"{emitter.Name}: texture {found} did not load, so it is drawn white");
            }
            if (settings.Count == 0)
                _lines.Add("no emitter is switched on at this tier");
            _effectWorldAt = Projectile
                ? ProjectileFlight.WorldAt(ProjectileFlight.Axes[ProjectileAxis].Axis, ProjectileSpeed, LoopLength)
                : _ => Matrix.Identity;
            _simulator = new ParticleSimulator(settings, Seed, _effectWorldAt, log: _lines.Add);
            _node = _sceneManager.RootNode.AddObject(new VfxEffectNode("VfxPreview", new ParticleRenderItem(_simulator, _textures, () => _resourceLibrary.GetStaticEffect(ShaderTypes.Particle), _meshes)));
            SetTime(Time);
        }

        public override void Update(GameTime gameTime)
        {
            if (_simulator == null || !IsPlaying)
                return;
            var next = Time + (float)gameTime.ElapsedGameTime.TotalSeconds * Speed;
            SetTime(Loop && next > LoopLength ? 0 : next);
        }

        public void SetTime(float time)
        {
            Time = Math.Max(0, time);
            try
            {
                _simulator?.AdvanceTo(Time);
            }
            catch (Exception e)
            {
                _lines.Add($"the preview stopped: {e.Message}");
                _simulator = null;
            }
            // The simulator runs whole steps; the model and the view follow its time, not the one asked for.
            var effectWorld = _effectWorldAt(_simulator?.Time ?? Time);
            var view = StaticView ? Matrix.Invert(effectWorld) : Matrix.Identity;
            if (_node != null)
                _node.ModelMatrix = view;
            if (Model != null)
                Model.ModelMatrix = effectWorld * view;
            TimeChanged?.Invoke();
        }

        /// <summary>Puts a model where the effect is, as a guide (a projectile's own mesh, say), in place of the last one.</summary>
        public void LoadModel(PackFile file)
        {
            ClearModel();
            try
            {
                Model = _sceneManager.RootNode.AddObject(_meshLoader.Load(file, new AnimationPlayer(), onlyLoadRootNode: true, onlyLoadFirstMesh: false));
            }
            catch (Exception e)
            {
                _modelProblem = $"guide model: {e.Message}";
                _lines.Add(_modelProblem);
            }
            SetTime(Time);
        }

        public void ClearModel()
        {
            _modelProblem = null;
            if (Model == null)
                return;
            _sceneManager.RootNode.RemoveObject(Model);   // frees its buffers (OnNodeRemoved)
            Model = null;
        }

        void Clear()
        {
            _simulator = null;
            if (_node == null)
                return;
            _node.Parent?.RemoveObject(_node);
            _node.RenderItem.Dispose();
            _node = null;
        }

        // A rigid particle's model, through the same loader as the guide model; null (and a line) when it is missing or broken.
        SceneNode? LoadRigid(string path)
        {
            if (_packFileService.FindFile(path) is not { } file)
                return null;
            foreach (var shader in VfxMeshCache.CustomShaders(_packFileService, path))
                _lines.Add($"rigid model {path} names {shader}, one of CA's own shaders the preview cannot run: drawn as a plain lit mesh");
            try
            {
                return _meshLoader.Load(file, new AnimationPlayer(), onlyLoadRootNode: true, onlyLoadFirstMesh: false);
            }
            catch (Exception e)
            {
                _lines.Add($"rigid model {path} did not load: {e.Message}");
                return null;
            }
        }

        /// <summary>Files changed in a pack: let go of the textures and models loaded from them, so the next draw loads the new ones.</summary>
        public void Forget(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                _textures.Forget(path);
                _meshes.Forget(path);
            }
        }

        public void Dispose()
        {
            Clear();
            ClearModel();
            _textures.Dispose();
            _meshes.Dispose();
        }
    }
}
