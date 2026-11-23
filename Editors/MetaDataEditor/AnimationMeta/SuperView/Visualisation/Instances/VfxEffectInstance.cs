using Editors.Shared.Core.Common;
using GameWorld.Core.Animation;
using GameWorld.Core.SceneNodes;
using GameWorld.Core.Vfx;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Definitions;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Visualisation.Instances
{
    // An EFFECT tag: its locator on the tag's bone, and its particles on the main animation's clock from
    // the tag's Start Time, the way AnimatedPropInstance runs props. A persistent meta's effect (loop > 0, the
    // clip's length) keeps its own clock instead, which runs on across the clip's loops.
    // The locator is drawn in the tag's own frame and placed from the tag's Position and Orientation on every
    // update, so it follows a drag in the viewport; the particles keep the place they were built with.
    public class VfxEffectInstance : IMetaDataInstance, IMetaDataMarkerPreview
    {
        private readonly ILogger _logger = Logging.Create<VfxEffectInstance>();
        private readonly SceneNode _locator;
        private readonly Func<float, Matrix> _boneAt;
        private readonly float _startTime;
        private readonly VfxEffectNode? _particles;
        private readonly float _loop;
        private readonly IEffectMeta? _effect;
        private float? _last;
        private float _clock;
        private float _currentTime;
        private bool _hasError;

        // Null, so the playback controls leave this alone; Update drives it.
        public AnimationPlayer Player => null;
        public ParticleSimulator? Simulator { get; }
        public ParsedMetadataAttribute Source => _effect as ParsedMetadataAttribute;

        public VfxEffectInstance(SceneNode locator, Func<float, Matrix> boneAt, float startTime, VfxEffectNode? particles = null, ParticleSimulator? simulator = null, float loop = 0, IEffectMeta? effect = null)
        {
            _locator = locator;
            _boneAt = boneAt;
            _startTime = startTime;
            _particles = particles;
            Simulator = simulator;
            _loop = loop;
            _effect = effect;
        }

        // The tag's position and orientation, in its bone's frame.
        Matrix Local => _effect == null ? Matrix.Identity : Matrix.CreateFromQuaternion(new Quaternion(_effect.Orientation)) * Matrix.CreateTranslation(_effect.Position);

        public bool IsHitTestVisible => _effect != null && _locator.IsVisible && _locator.Parent != null;
        public float HitTestRadius => 0.3f;
        public bool FollowsTagLive => true;
        // The bone at the time last shown, inside the node the locator is drawn under.
        public Matrix ReferenceWorldTransform => _boneAt(_currentTime) * MetaDataPreviewMath.WorldOf(_locator.Parent);
        public Vector3 FocusPosition => (Local * ReferenceWorldTransform).Translation;
        public IReadOnlyList<MetaDataMarkerHitTarget> HitTargets => _effect == null ? [] : [new(FocusPosition, MetaDataMarkerPoint.Default)];

        public void Update(float currentTime)
        {
            if (_hasError)
                return;

            try
            {
                _currentTime = currentTime;
                _locator.ModelMatrix = Local * _boneAt(currentTime);
                Simulator?.AdvanceTo(EffectTime(currentTime));
            }
            catch (Exception e)
            {
                // Nothing throws into the viewport: one log line, and this effect stops updating.
                _logger.Here().Error($"{_locator.Name} stopped updating - {e.Message}");
                _hasError = true;
            }
        }

        // A persistent effect never runs backwards: a jump back, a loop or a scrub, counts as running on to the clip's end
        // and round again.
        float EffectTime(float currentTime)
        {
            if (_loop <= 0)
                return currentTime - _startTime;
            _clock = _last is { } last ? _clock + (currentTime >= last ? currentTime - last : currentTime + _loop - last) : currentTime - _startTime;
            _last = currentTime;
            return _clock;
        }

        public void CleanUp()
        {
            _locator.Parent.RemoveObject(_locator);
            if (_particles == null)
                return;
            _particles.Parent.RemoveObject(_particles);
            _particles.RenderItem.Dispose();
        }
    }
}
