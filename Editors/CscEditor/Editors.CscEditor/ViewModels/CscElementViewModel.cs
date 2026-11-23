using System;
using System.Collections.ObjectModel;
using Editors.CscEditor.Data;
using Microsoft.Xna.Framework;
using Shared.Core.Misc;

namespace Editors.CscEditor.ViewModels
{
    public enum ElementChange
    {
        /// <summary>Values changed - transforms/curves need re-applying.</summary>
        Data,
        /// <summary>The referenced asset changed - the 3D content must be rebuilt.</summary>
        Asset,
    }

    /// <summary>Tree item + details-panel adapter over a <see cref="CscElement"/>.</summary>
    public class CscElementViewModel : NotifyPropertyChangedImpl
    {
        public CscElement Element { get; }
        readonly Action<CscElementViewModel, ElementChange> _onModified;

        public ObservableCollection<CscElementViewModel> Children { get; } = [];

        public CscElementViewModel(CscElement element, Action<CscElementViewModel, ElementChange> onModified)
        {
            Element = element;
            _onModified = onModified;
        }

        bool _isSelected;
        public bool IsSelected { get => _isSelected; set => SetAndNotify(ref _isSelected, value); }

        bool _isExpanded = true;
        public bool IsExpanded { get => _isExpanded; set => SetAndNotify(ref _isExpanded, value); }

        public string DisplayName => Element.DisplayName;

        /// <summary>User-facing kind label. Differs from <see cref="CscElementKind"/>'s own member
        /// names for two kinds where the internal name (matching the ESF record grammar) reads as
        /// confusing/jargon-y in the tree: "RootRef" -> "Composite Scene" (it's a nested reference
        /// to another .csc, not a "root" of anything from the user's point of view) and "Locator"
        /// -> "Group" (matching the .csc format's own "element group" terminology - it's just an
        /// ELEMENT with no type record, i.e. an attach-tree grouping node).</summary>
        public string KindName => Element.Kind switch
        {
            CscElementKind.RootRef => "Composite Scene",
            CscElementKind.Locator => "Group",
            _ => Element.Kind.ToString(),
        };

        public int Id => Element.Id;
        public bool IsEditable => !Element.IsLegacyRaw && !Element.IsExternal;

        public string? ReadOnlyReason => Element.IsExternal
            ? "Part of a referenced scene (via Composite Scene reference) - display-only, edit the referenced .csc instead"
            : Element.IsLegacyRaw
                ? "Legacy record layout - preserved verbatim, read-only"
                : null;

        // ---- Kind flags for panel visibility ----
        public bool ShowAssetPath => Element.Kind is CscElementKind.Model or CscElementKind.VariantModel
            or CscElementKind.Vfx or CscElementKind.Sfx or CscElementKind.Animation
            or CscElementKind.Prefab or CscElementKind.RootRef or CscElementKind.SoundSphere;
        public bool IsSfx => Element.Kind == CscElementKind.Sfx;
        public bool SfxHasSecondEventPair => Element.SfxHasSecondEventPair;
        public string AssetPathLabel => Element.Kind switch
        {
            CscElementKind.Sfx => "Event 1 (start)",
            CscElementKind.Vfx => "Effect name",
            _ => "Asset",
        };
        public bool IsLight => Element.Kind is CscElementKind.PointLight or CscElementKind.SpotLight;
        public bool IsPointLight => Element.Kind == CscElementKind.PointLight;
        public bool IsSpotLight => Element.Kind == CscElementKind.SpotLight;
        public bool IsCamera => Element.Kind == CscElementKind.Camera;
        public bool HasWeight => Element.Weight != null;          // ELEMENT v5 has no weight
        public bool HasCameraFov => Element.CameraFov != null;
        public bool HasCameraRoll => Element.CameraRoll != null;  // CAMERA_ELEMENT v1/v2 have no roll
        public bool HasCameraNear => Element.CameraNear != null;
        public bool HasCameraFar => Element.CameraFar != null;
        public bool HasAttachParent => Element.Parent != null;

        // ---- Composite Scene (RootRef): the referenced .csc's own ROOT header info, shown
        // read-only - edit the referenced file directly instead (see CscScene's FocusPoint/
        // Radius/WeatherPath docs for what these fields mean). No setters at all (not merely
        // disabled bindings) so there is no path through which this VM could ever write back into
        // the referenced CscScene.
        CscScene? _subScene;

        /// <summary>Set by the owning view model once the referenced scene has (or hasn't) loaded
        /// - null while unset, on load failure, or for any non-RootRef element.</summary>
        public void SetSubScene(CscScene? subScene)
        {
            _subScene = subScene;
            NotifyPropertyChanged(nameof(HasSubScene));
            NotifyPropertyChanged(nameof(SubSceneDuration));
            NotifyPropertyChanged(nameof(SubSceneFocusPointX));
            NotifyPropertyChanged(nameof(SubSceneFocusPointY));
            NotifyPropertyChanged(nameof(SubSceneFocusPointZ));
            NotifyPropertyChanged(nameof(SubSceneRadius));
            NotifyPropertyChanged(nameof(SubSceneHasWeatherPath));
            NotifyPropertyChanged(nameof(SubSceneWeatherPath));
        }

        public bool HasSubScene => Element.Kind == CscElementKind.RootRef && _subScene != null;
        public float SubSceneDuration => _subScene?.Duration ?? 0f;
        public float SubSceneFocusPointX => _subScene?.FocusPoint.X ?? 0f;
        public float SubSceneFocusPointY => _subScene?.FocusPoint.Y ?? 0f;
        public float SubSceneFocusPointZ => _subScene?.FocusPoint.Z ?? 0f;
        public float SubSceneRadius => _subScene?.Radius ?? 0f;
        public bool SubSceneHasWeatherPath => _subScene?.HasWeatherPath ?? false;
        public string SubSceneWeatherPath => _subScene?.WeatherPath ?? "";

        // ---- ANIMATION_SPLICE_ELEMENT: a sibling record alongside ANIMATION_ELEMENT ----
        public bool HasSplice => Element.SpliceRecord != null;

        public int SpliceBoneId
        {
            get => Element.SpliceBoneId;
            set { Element.SpliceBoneId = value; NotifyPropertyChanged(); Modified(); }
        }

        public int SpliceDepthA
        {
            get => Element.SpliceDepthA;
            set { Element.SpliceDepthA = value; NotifyPropertyChanged(); Modified(); }
        }

        public int SpliceDepthB
        {
            get => Element.SpliceDepthB;
            set { Element.SpliceDepthB = value; NotifyPropertyChanged(); Modified(); }
        }

        public string SpliceRemainingFieldsDisplay => Element.SpliceRemainingFieldsDisplay;

        // ---- ANIMATION_NODE_TRANSFORM_ELEMENT: this element's own kind ----
        public bool IsAnimationNodeTransform => Element.Kind == CscElementKind.AnimationNodeTransform;

        public int NodeTransformBoneId
        {
            get => Element.NodeTransformBoneId;
            set { Element.NodeTransformBoneId = value; NotifyPropertyChanged(); Modified(); }
        }

        public int NodeTransformSecondValue
        {
            get => Element.NodeTransformSecondValue;
            set { Element.NodeTransformSecondValue = value; NotifyPropertyChanged(); Modified(); }
        }

        void Modified(ElementChange change = ElementChange.Data) => _onModified(this, change);

        // ---- Asset ----
        public string AssetPath
        {
            get => Element.AssetPath;
            set { Element.AssetPath = value; NotifyPropertyChanged(); NotifyPropertyChanged(nameof(DisplayName)); Modified(ElementChange.Asset); }
        }

        public string SfxStopEvent
        {
            get => Element.SfxStopEvent;
            set { Element.SfxStopEvent = value; NotifyPropertyChanged(); Modified(); }
        }

        public string SfxEvent2Start
        {
            get => Element.SfxEvent2Start;
            set { Element.SfxEvent2Start = value; NotifyPropertyChanged(); Modified(); }
        }

        public string SfxEvent2Stop
        {
            get => Element.SfxEvent2Stop;
            set { Element.SfxEvent2Stop = value; NotifyPropertyChanged(); Modified(); }
        }

        // ---- Timing ----
        public float Begin
        {
            get => Element.Begin;
            set { Element.Begin = value; NotifyPropertyChanged(); Modified(); }
        }

        public float End
        {
            get => Element.End;
            set { Element.End = value; NotifyPropertyChanged(); Modified(); }
        }

        public string TimingMode
        {
            get => Element.TimingMode;
            set { Element.TimingMode = value; NotifyPropertyChanged(); Modified(); }
        }

        public string AnchorMode
        {
            get => Element.AnchorMode;
            set { Element.AnchorMode = value; NotifyPropertyChanged(); Modified(); }
        }

        public int AttachBoneIndex
        {
            get => Element.AttachBoneIndex;
            set { Element.AttachBoneIndex = value; NotifyPropertyChanged(); Modified(); }
        }

        // ---- ELEMENT's trailing Bool (v7/v100 only) - meaning unconfirmed, shown for every kind ----
        public bool HasElementTrailingBool => Element.HasElementTrailingBool;

        public bool ElementTrailingBool
        {
            get => Element.ElementTrailingBool;
            set { Element.ElementTrailingBool = value; NotifyPropertyChanged(); Modified(); }
        }

        // ---- ELEMENT_PERIOD: (flag, speed multiplier, time offset) - in-game confirmed ----
        public bool HasPeriod => Element.PeriodRecord != null;

        public bool PeriodFlag
        {
            get => Element.PeriodFlag;
            set { Element.PeriodFlag = value; NotifyPropertyChanged(); Modified(); }
        }

        public float PeriodSpeedMultiplier
        {
            get => Element.PeriodSpeedMultiplier;
            set { Element.PeriodSpeedMultiplier = value; NotifyPropertyChanged(); Modified(); }
        }

        public float PeriodTimeOffset
        {
            get => Element.PeriodTimeOffset;
            set { Element.PeriodTimeOffset = value; NotifyPropertyChanged(); Modified(); }
        }

        // ---- Static channel transform (header when un-animated; shifts the whole curve when animated) ----
        float ChannelStatic(CscChannelGroup? group, int index) =>
            group != null && index < group.Channels.Count ? group.Channels[index].StaticValue : 0;

        void SetChannelStatic(CscChannelGroup? group, int index, float value)
        {
            if (group == null || index >= group.Channels.Count)
                return;
            group.Channels[index].SetStatic(value);
            Modified();
        }

        public float PositionX { get => ChannelStatic(Element.Position, 0); set { SetChannelStatic(Element.Position, 0, value); NotifyPropertyChanged(); } }
        public float PositionY { get => ChannelStatic(Element.Position, 1); set { SetChannelStatic(Element.Position, 1, value); NotifyPropertyChanged(); } }
        public float PositionZ { get => ChannelStatic(Element.Position, 2); set { SetChannelStatic(Element.Position, 2, value); NotifyPropertyChanged(); } }

        public float RotationXDegrees { get => MathHelper.ToDegrees(ChannelStatic(Element.Rotation, 0)); set { SetChannelStatic(Element.Rotation, 0, MathHelper.ToRadians(value)); NotifyPropertyChanged(); } }
        public float RotationYDegrees { get => MathHelper.ToDegrees(ChannelStatic(Element.Rotation, 1)); set { SetChannelStatic(Element.Rotation, 1, MathHelper.ToRadians(value)); NotifyPropertyChanged(); } }
        public float RotationZDegrees { get => MathHelper.ToDegrees(ChannelStatic(Element.Rotation, 2)); set { SetChannelStatic(Element.Rotation, 2, MathHelper.ToRadians(value)); NotifyPropertyChanged(); } }

        public float ScaleValue { get => ChannelStatic(Element.Scale, 0); set { SetChannelStatic(Element.Scale, 0, value); NotifyPropertyChanged(); } }
        public float WeightValue { get => ChannelStatic(Element.Weight, 0); set { SetChannelStatic(Element.Weight, 0, value); NotifyPropertyChanged(); } }

        // ---- Base placement (ELEMENT's trailing Coord3d pair) ----
        public float BasePositionX { get => Element.BasePosition.X; set { Element.BasePosition = new Vector3(value, Element.BasePosition.Y, Element.BasePosition.Z); NotifyPropertyChanged(); Modified(); } }
        public float BasePositionY { get => Element.BasePosition.Y; set { Element.BasePosition = new Vector3(Element.BasePosition.X, value, Element.BasePosition.Z); NotifyPropertyChanged(); Modified(); } }
        public float BasePositionZ { get => Element.BasePosition.Z; set { Element.BasePosition = new Vector3(Element.BasePosition.X, Element.BasePosition.Y, value); NotifyPropertyChanged(); Modified(); } }

        public float BaseRotationXDegrees { get => MathHelper.ToDegrees(Element.BaseRotation.X); set { Element.BaseRotation = new Vector3(MathHelper.ToRadians(value), Element.BaseRotation.Y, Element.BaseRotation.Z); NotifyPropertyChanged(); Modified(); } }
        public float BaseRotationYDegrees { get => MathHelper.ToDegrees(Element.BaseRotation.Y); set { Element.BaseRotation = new Vector3(Element.BaseRotation.X, MathHelper.ToRadians(value), Element.BaseRotation.Z); NotifyPropertyChanged(); Modified(); } }
        public float BaseRotationZDegrees { get => MathHelper.ToDegrees(Element.BaseRotation.Z); set { Element.BaseRotation = new Vector3(Element.BaseRotation.X, Element.BaseRotation.Y, MathHelper.ToRadians(value)); NotifyPropertyChanged(); Modified(); } }

        // ---- Lights ----
        float TypeChannelStatic(int group, int channel = 0) => Element.TypeChannel(group, channel)?.StaticValue ?? 0;

        void SetTypeChannelStatic(int group, float value, int channel = 0)
        {
            var ch = Element.TypeChannel(group, channel);
            if (ch == null)
                return;
            ch.SetStatic(value);
            Modified();
        }

        public float LightColourR { get => TypeChannelStatic(0, 0); set { SetTypeChannelStatic(0, value, 0); NotifyPropertyChanged(); } }
        public float LightColourG { get => TypeChannelStatic(0, 1); set { SetTypeChannelStatic(0, value, 1); NotifyPropertyChanged(); } }
        public float LightColourB { get => TypeChannelStatic(0, 2); set { SetTypeChannelStatic(0, value, 2); NotifyPropertyChanged(); } }
        public float LightIntensity { get => TypeChannelStatic(1); set { SetTypeChannelStatic(1, value); NotifyPropertyChanged(); } }

        /// <summary>Range for point lights, length for spot lights - both live in type group 2.</summary>
        public float LightRange { get => TypeChannelStatic(2); set { SetTypeChannelStatic(2, value); NotifyPropertyChanged(); } }

        public float SpotInnerAngleDegrees { get => MathHelper.ToDegrees(TypeChannelStatic(3)); set { SetTypeChannelStatic(3, MathHelper.ToRadians(value)); NotifyPropertyChanged(); } }
        public float SpotOuterAngleDegrees { get => MathHelper.ToDegrees(TypeChannelStatic(4)); set { SetTypeChannelStatic(4, MathHelper.ToRadians(value)); NotifyPropertyChanged(); } }

        // ---- Camera (fov already in degrees on the wire - in-game confirmed). Near/far are the
        // real render clip planes. Routed through Element's own group-index lookup
        // (CscElement.CameraGroupIndex), which is the same for every CAMERA_ELEMENT version. ----
        public float CameraFov { get => Element.CameraFov?.StaticValue ?? 0; set { Element.CameraFov?.SetStatic(value); Modified(); NotifyPropertyChanged(); } }
        public float CameraRollDegrees { get => MathHelper.ToDegrees(Element.CameraRoll?.StaticValue ?? 0); set { Element.CameraRoll?.SetStatic(MathHelper.ToRadians(value)); Modified(); NotifyPropertyChanged(); } }
        public float CameraNear { get => Element.CameraNear?.StaticValue ?? 0; set { Element.CameraNear?.SetStatic(value); Modified(); NotifyPropertyChanged(); } }
        public float CameraFar { get => Element.CameraFar?.StaticValue ?? 0; set { Element.CameraFar?.SetStatic(value); Modified(); NotifyPropertyChanged(); } }
        public bool HasCameraUnknownFlag => Element.HasCameraUnknownFlag;
        public bool CameraUnknownFlag { get => Element.CameraUnknownFlag; set { Element.CameraUnknownFlag = value; Modified(); NotifyPropertyChanged(); } }

        /// <summary>Re-raises every binding after the domain changed underneath (gizmo drags, curve edits).</summary>
        public void RefreshFromDomain()
        {
            NotifyPropertyChanged(string.Empty);
        }
    }
}
