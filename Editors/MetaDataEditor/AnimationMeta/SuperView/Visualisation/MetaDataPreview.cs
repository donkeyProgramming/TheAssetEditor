using GameWorld.Core.SceneNodes;
using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Visualisation
{
    // Which of a tag's points a marker stands for: a splash attack has two, everything else one.
    public enum MetaDataMarkerPoint
    {
        Default,
        SplashStart,
        SplashEnd
    }

    // A point the viewport can pick, in world space.
    public readonly record struct MetaDataMarkerHitTarget(
        Vector3 Position,
        MetaDataMarkerPoint Point,
        float? HitTestRadius = null);

    // A tag drawn somewhere in the scene. ReferenceWorldTransform is the frame its values are stored in:
    // the bone it is on, or the model's root.
    public interface ISpatialMetaDataPreview
    {
        ParsedMetadataAttribute Source { get; }
        Vector3 FocusPosition { get; }
        Matrix ReferenceWorldTransform { get; }
        // Placed from the tag's values on every update, so a drag needs no rebuild until it ends.
        bool FollowsTagLive { get; }
    }

    // A tag whose marker the viewport can hover and click.
    public interface IMetaDataMarkerPreview : ISpatialMetaDataPreview
    {
        bool IsHitTestVisible { get; }
        float HitTestRadius { get; }
        IReadOnlyList<MetaDataMarkerHitTarget> HitTargets { get; }
    }

    static class MetaDataPreviewMath
    {
        // A node's world transform, from its own matrix up through its parents'.
        public static Matrix WorldOf(ISceneNode? node)
        {
            var world = Matrix.Identity;
            for (; node != null; node = node.Parent)
                world *= node.ModelMatrix;
            return world;
        }
    }
}
