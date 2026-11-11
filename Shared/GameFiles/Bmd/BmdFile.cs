using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Bmd
{
    public class BmdFile
    {
        public FastBinHeader Header { get; set; } = new FastBinHeader();

        /// <summary>
        /// Per-section header version numbers (e.g. "BattlefieldBuilding", "Prop", "SpotLight" -
        /// see the keys <see cref="BmdParser"/> writes into this dictionary). These are read from
        /// the stream purely to advance position - nothing in the parser branches on them - but
        /// <see cref="BmdWriter"/> needs the original value back to reproduce the file byte-for-byte,
        /// so they're captured here rather than discarded. Missing key = version 0 (used when a
        /// section has no separate version field at all, e.g. GoOutline).
        /// </summary>
        public Dictionary<string, ushort> SectionVersions { get; set; } = [];
        /// <summary>FASTBIN &lt; 22 only.</summary>
        public List<LegacyDeploymentArea> LegacyDeploymentAreas { get; set; } = [];
        public List<BattlefieldBuilding> BattlefieldBuildings { get; set; } = [];
        public List<BattlefieldBuilding> BattlefieldBuildingFars { get; set; } = [];
        public List<CaptureLocationList> CaptureLocationLists { get; set; } = [];
        public List<EFLine> EFLines { get; set; } = [];
        public List<GoOutline> GoOutlines { get; set; } = [];
        public List<NonTerrainOutline> NonTerrainOutlines { get; set; } = [];
        public List<ZonesTemplate> ZonesTemplates { get; set; } = [];
        public List<BmdInfo> BmdInfos { get; set; } = [];
        public List<BmdOutline> BmdOutlines { get; set; } = [];
        public List<TerrainOutline> TerrainOutlines { get; set; } = [];
        public List<LiteBuildingOutline> LiteBuildingOutlines { get; set; } = [];
        public List<CameraZone> CameraZones { get; set; } = [];
        public List<CivilianDeployment> CivilianDeployments { get; set; } = [];
        public List<CivilianShelter> CivilianShelters { get; set; } = [];
        public List<string> Props { get; set; } = [];
        public List<PropInfo> PropInfos { get; set; } = [];
        public List<VfxInfo> VfxInfos { get; set; } = [];
        public AiHints AiHints { get; set; } = new();
        public List<LightProbeInfo> LightProbes { get; set; } = [];
        public List<TerrainHoleTriangleInfo> TerrainHoles { get; set; } = [];
        public List<PointLightInfo> PointLights { get; set; } = [];
        public List<BuildingProjectileEmitter> BuildingProjectileEmitters { get; set; } = [];
        public PlayableArea PlayableArea { get; set; } = new();
        public List<PolyMeshInfo> PolyMeshes { get; set; } = [];
        public List<TerrainStencilBlendTriangle> TerrainStencilBlendTriangles { get; set; } = [];
        public List<SpotLightInfo> SpotLights { get; set; } = [];
        public List<SoundInfo> Sounds { get; set; } = [];
        public List<CscInfo> CscInfos { get; set; } = [];
        public List<Deployment> Deployments { get; set; } = [];
        public List<BmdCachedArea> BmdCachedAreas { get; set; } = [];
        public List<ToggleableBuildingSlot> ToggleableBuildingSlots { get; set; } = [];
        public List<TerraindDecal> TerraindDecals { get; set; } = [];
        public List<TreeListReference> TreeListReferences { get; set; } = [];
        public List<GrassListReference> GrassListReferences { get; set; } = [];
        public List<WaterOutline> WaterOutlines { get; set; } = [];

        //Pharaoh Exclusive classes (for Pharaoh's version of the version 25 BMD format)
        public CameraZoneNew CameraZoneNew { get; set; } = new();
        public MiscParams MiscParams { get; set; } = new();
    }

    /// <summary>
    /// One item of the kit's BATTLE_DEPLOYMENT_AREA_MANAGER, the deployment format FASTBIN &lt; 22 used
    /// before <see cref="Deployment"/> replaced it. Keyed by a BATTLE_DEPLOYMENT_AREA_TYPE (two ints).
    /// </summary>
    public class LegacyDeploymentArea
    {
        public int TypeX { get; set; }
        public int TypeY { get; set; }
        public List<LegacyDeploymentAreaCollection> Collections { get; set; } = [];
    }

    public class LegacyDeploymentAreaCollection
    {
        public List<LegacyDeploymentZone> Alliance0 { get; set; } = [];
        public List<LegacyDeploymentZone> Alliance1 { get; set; } = [];
    }

    /// <summary>Unversioned; gates on the manager version (<c>SectionVersions["LegacyPreV22"]</c>).</summary>
    public class LegacyDeploymentZone
    {
        public List<LegacyDeploymentBoundary> Boundaries { get; set; } = [];
        public string Category { get; set; } = string.Empty;
        public float Orientation { get; set; }
        public bool SnapFacing { get; set; }
        /// <summary>Manager version 2 and up.</summary>
        public uint Id { get; set; }
    }

    public class LegacyDeploymentBoundary
    {
        public string Type { get; set; } = string.Empty;
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class FastBinHeader
    {
        public string FastBin0 { get; set; } = string.Empty;
        public ushort FastBinVersion { get; set; }
    }

    
    public struct CultureMask
    {
        /// <summary>
        /// The exact 8 bytes this was decoded from, when read by <see cref="BmdParser.ReadCultureMask"/>.
        /// Several bit positions within these bytes aren't decoded into any named field below (real
        /// vanilla files set them as part of an all-<c>0xFF</c> "no restriction" pattern), so
        /// <see cref="BmdWriter"/> writes these raw bytes back directly when present instead of
        /// reconstructing from the named bools alone, which would silently zero those bits. Null for
        /// a mask built by other code (e.g. freshly constructed, not parsed) - <see cref="BmdWriter"/>
        /// falls back to bit-reconstruction in that case.
        /// </summary>
        public byte[]? RawBytes { get; set; }

        // First byte
        public bool CultMaskBase { get; set; }
        public bool CultMaskBst { get; set; }
        public bool CultMaskBrt { get; set; }

        // Second byte
        public bool CultMaskChs { get; set; }
        public bool CultMaskDwf { get; set; }
        public bool CultMaskEmp { get; set; }
        public bool CultMaskGrn { get; set; }
        public bool CultMaskVmp { get; set; }
        public bool CultMaskWef { get; set; }

        // Third byte
        public bool CultMaskDef { get; set; }
        public bool CultMaskHef { get; set; }
        public bool CultMaskLzd { get; set; }
        public bool CultMaskSkv { get; set; }
        public bool CultMaskTmb { get; set; }
        public bool CultMaskRogue { get; set; }
        public bool CultMaskKsl { get; set; }

        // Fourth byte
        public bool CultMaskOgr { get; set; }
        public bool CultMaskCst { get; set; }
        public bool CultMaskKho { get; set; }
        public bool CultMaskTze { get; set; }
        public bool CultMaskNur { get; set; }
        public bool CultMaskSla { get; set; }
        public bool CultMaskDae { get; set; }

        // Fifth byte
        public bool CultMaskCth { get; set; }
        public bool CultMaskNor { get; set; }
        public bool CultMaskChd { get; set; }
        public bool CultMaskNag { get; set; }
    }

    public class BmdComponentFlags
    {
        public ushort FlagVersion { get; set; }
        public bool AllowInOutfield { get; set; }
        public bool ClampToSurface { get; set; }   //Flag version 2 only
        public bool ClampToWaterSurface { get; set; }
        public bool SeasonSpring { get; set; }
        public bool SeasonSummer { get; set; }
        public bool SeasonAutumn { get; set; }
        public bool SeasonWinter { get; set; }
        public bool VisibleInTactical { get; set; }
        public bool OnlyVisibleInTactical { get; set; }
    }

    public class BattlefieldBuilding
    {
        public ushort Version { get; set; }
        public string BuildingId { get; set; } = string.Empty;
        public int ParentId { get; set; }
        public string BuildingKey { get; set; } = string.Empty;
        public string PositionType { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        public BuildingProperties Properties { get; set; } = new();
        public string HeightMode { get; set; } = string.Empty;
        public long Uid { get; set; }
    }

    /// <summary>
    /// A building's settings (Terry's ECBuilding/ECWall). Battlefield buildings carry one, and a prefab
    /// instance carries one per building of its child prefab whose settings it overrides, keyed by
    /// <see cref="BuildingId"/> (Terry's &lt;override name=...&gt;).
    /// </summary>
    public class BuildingProperties
    {
        /// <summary>0 for the unversioned block of battlefield buildings older than version 4.</summary>
        public ushort Version { get; set; }
        public string BuildingId { get; set; } = string.Empty;
        public float StartingDamageUnary { get; set; }
        public bool OnFire { get; set; }
        public bool StartDisabled { get; set; }
        public bool WeakPoint { get; set; }
        public bool AiBreachable { get; set; }
        public bool Indestructible { get; set; }
        public bool Dockable { get; set; }
        public bool Toggleable { get; set; }
        public bool Lite { get; set; }
        /// <summary>Versions 3-7 only.</summary>
        public bool ClampToSurface { get; set; }
        public bool CastShadows { get; set; }
        public bool KeyBuilding { get; set; }
        public bool KeyBuildingUseFort { get; set; }
        public bool IsPropInOutfield { get; set; }
        public bool SettlementLevelConfigurable { get; set; }
        public bool HideTooltip { get; set; }
        public bool IncludeInFog { get; set; }
        public float BlendEdges { get; set; }
        public bool ReceiveDecals { get; set; }
    }

    /// <summary>
    /// One CAPTURE_LOCATION_LIST of the kit's CAPTURE_LOCATION_SET: the set is a vector of these lists,
    /// and each list is a vector of capture locations.
    /// </summary>
    public class CaptureLocationList
    {
        public List<CaptureLocation> Locations { get; set; } = [];
    }

    /// <summary>
    /// A capture point (the kit's CAPTURE_LOCATION). Its records carry no version of their own: every
    /// gated field follows the CAPTURE_LOCATION_SET version (<c>SectionVersions["CaptureLocation"]</c>).
    /// </summary>
    public class CaptureLocation
    {
        public RmvVector2 Location { get; set; }
        public float Radius { get; set; }
        public uint ValidForMinNumPlayers { get; set; }
        public uint ValidForMaxNumPlayers { get; set; }
        public string CapturePointType { get; set; } = string.Empty;
        /// <summary>List version 3 and up.</summary>
        public string RestoreType { get; set; } = string.Empty;
        public List<RmvVector2> LocationPoints { get; set; } = [];
        /// <summary>List version 1 only (building links replace it from 2).</summary>
        public List<uint> BuildingIndices { get; set; } = [];
        public string DatabaseKey { get; set; } = string.Empty;
        public RmvVector2 FlagFacing { get; set; }
        /// <summary>List version 4 and up.</summary>
        public bool DestroyBuildingOnCapture { get; set; }
        /// <summary>List version 6 and up.</summary>
        public bool DisableBuildingAbilitiesWhenNoOriginalOwner { get; set; }
        /// <summary>List version 5 and up.</summary>
        public bool AbilitiesAffectGlobally { get; set; }
        /// <summary>List version 2 and up.</summary>
        public List<BuildingLink> BuildingLinks { get; set; } = [];
        /// <summary>List version 7 and up.</summary>
        public List<uint> ToggleSlotsLinks { get; set; } = [];
        /// <summary>List version 8 and up.</summary>
        public List<uint> AiHintsLinks { get; set; } = [];
        /// <summary>List version 9 and up.</summary>
        public string ScriptId { get; set; } = string.Empty;
        /// <summary>List version 10 and up.</summary>
        public bool IsTimeBased { get; set; }
        /// <summary>List version 12 and up.</summary>
        public string InitialOwnerType { get; set; } = string.Empty;
    }

    /// <summary>A reference from a capture location or toggleable slot to a building, possibly one inside a prefab.</summary>
    public class BuildingLink
    {
        public ushort Version { get; set; }
        public int BuildingIndex { get; set; }
        public int PrefabIndex { get; set; }
        public string PrefabBuildingKey { get; set; } = string.Empty;
        /// <summary>Version 2 and up.</summary>
        public ulong Uid { get; set; }
        /// <summary>Version 3 and up.</summary>
        public ulong PrefabUid { get; set; }
    }

    /// <summary>An entity-formation line (the kit's LINE). Unversioned.</summary>
    public class EFLine
    {
        public string Label { get; set; } = string.Empty;
        public RmvVector3 End1 { get; set; }
        public RmvVector3 End2 { get; set; }
        public RmvVector3 Direction { get; set; }
        public string Purpose { get; set; } = string.Empty;
    }

    public class GoOutline
    {
        public List<RmvVector2> VertexList { get; set; } = [];
    }

    public class NonTerrainOutline
    {
        public List<RmvVector2> VertexList { get; set; } = [];
    }

    public class ZonesTemplate
    {
        public List<RmvVector2> Outline { get; set; } = [];
        public string ZoneName { get; set; } = string.Empty;
        public string EntityFormationTemplateName { get; set; } = string.Empty;
        public List<ZoneLine> Lines { get; set; } = [];
        public Matrix Transform { get; set; } = Matrix.Identity; //4x4
    }

    /// <summary>A line inside a zones template. Unversioned.</summary>
    public class ZoneLine
    {
        public string Label { get; set; } = string.Empty;
        public RmvVector3 Start { get; set; }
        public RmvVector3 End { get; set; }
        public string Purpose { get; set; } = string.Empty;
        public float Orientation { get; set; }
    }
    
    public class BmdInfo
    {
        public ushort Version { get; set; }
        public string BmdString { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        /// <summary>Terry's &lt;property_overrides&gt;: settings for buildings inside the referenced prefab.</summary>
        public List<BuildingProperties> PropertyOverrides { get; set; } = [];
        public CultureMask CampaignTypeMask { get; set; }
        public string RegionString { get; set; } = string.Empty;
        /// <summary>Versions 5-7 only.</summary>
        public bool ClampToSurface { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public byte[] Uid { get; set; } = new byte[8];
    }

    public class BmdOutline
    {
        public ushort Version { get; set; }
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class TerrainOutline
    {
        public List<RmvVector2> VertexList { get; set; } = [];
    }

    public class LiteBuildingOutline
    {
        public List<RmvVector2> VertexList { get; set; } = [];
    }

    public class CameraZone
    {
        public ushort Version { get; set; }
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class CivilianDeployment
    {
        public ushort Version { get; set; }
        public string CivilianType { get; set; } = string.Empty;
        public string CivilianAmount { get; set; } = string.Empty;
        public List<RmvVector2> Outline { get; set; } = [];
    }

    public class CivilianShelter
    {
        public ushort Version { get; set; }
        public RmvVector2 Position { get; set; }
        public RmvVector2 Entrance { get; set; }
        public int CivilianDeploymentLink { get; set; }
    }

    public class PropInfo
    {
        public ushort PropInfoVersion { get; set; }
        public string Rmv2Path { get; set; } = string.Empty;

        /// <summary>
        /// The prop's original index into the file's shared string table (only set for
        /// <see cref="PropInfoVersion"/> &gt; 12, which reference <see cref="Rmv2Path"/> by index
        /// rather than storing it inline). The table can contain duplicate paths in real files, so
        /// <see cref="BmdWriter"/> prefers reusing this exact index over re-deriving one from
        /// <see cref="Rmv2Path"/> alone, to avoid silently repointing a prop at a different (though
        /// string-identical) table slot on an unedited save.
        /// </summary>
        public int? PropIndex { get; set; }
        public Matrix Transform { get; set; } = Matrix.Identity;
        public bool IsDecal { get; set; }
        public bool LogicalDecal { get; set; }
        public bool IsFauna { get; set; }
        public bool VisibleInsideSnowRegion { get; set; }
        public bool VisibleOutsideSnowRegion { get; set; }
        public bool VisibleInsideDestructionRegion { get; set; }
        public bool VisibleOutsideDestructionRegion { get; set; }
        public bool Animated { get; set; }
        /// <summary>Versions 1-2 only.</summary>
        public float DecalParallax { get; set; }
        /// <summary>Versions 1-2 only.</summary>
        public RmvVector3 DecalScale { get; set; }
        /// <summary>Versions 1-2 only.</summary>
        public bool DecalTiled { get; set; }
        public float DecalParallaxScale { get; set; }
        public float DecalTiling { get; set; }
        public bool DecalOverrideGbufferNormal { get; set; }
        /// <summary>
        /// Version 3 props in vanilla store junk (e.g. 0x8D) in this bool, apparently uninitialised memory
        /// in the exporter. The raw byte is kept so an unedited save reproduces the file exactly.
        /// </summary>
        public byte? DecalOverrideGbufferNormalRawByte { get; set; }

        public BmdComponentFlags Flags { get; set; } = new();

        public bool VisibleInShroud { get; set; }
        public bool ApplyToTerrain { get; set; }
        public bool ApplyToPropsOrReceiveDecal { get; set; }
        public bool RenderAboveSnow { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public byte[] CultureMask { get; set; } = new byte[8];
        public bool CastsShadow { get; set; }
        public bool NoCulling { get; set; }
        public bool HasHeightPatch { get; set; }
        public bool ApplyHeightPatch { get; set; }
        public bool IncludeInFog { get; set; }
        public bool VisibleWithoutShroud { get; set; }
        /// <summary>Versions 21-22 only (the flags carry these from 23 on).</summary>
        public bool VisibleInTacticalView { get; set; }
        /// <summary>Version 22 only.</summary>
        public bool VisibleInTacticalViewOnly { get; set; }
        /// <summary>Version 9 only.</summary>
        public bool DecalApplyToRigids { get; set; }
        public bool UseDynamicShadows { get; set; }
        public bool UsesTerrainVertexOffset { get; set; }
        public float BlendEdges { get; set; }
        public float DecalFadeStart { get; set; }
        public float DecalAngleThreshold { get; set; }
        public bool DecalMirrorX { get; set; }
        public bool DecalMirrorZ { get; set; }
        public int DecalLayer { get; set; }
        public bool DecalTilingAlpha { get; set; }

        // The undecoded tail of a FASTBIN < 11 prop (the kit can't decode that layout)
        public byte[] EarlyVersionUnknownBytes { get; set; } = new byte[23];
        public bool EarlyVersionUnknownBool { get; set; }
    }

        /// <summary>Version 1 only.</summary>
        public float Scale { get; set; } = 1;
    public class VfxInfo
    {
        public ushort VfxInfoVersion { get; set; }
        public string VfxString { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        public float EmissionRate { get; set; }
        public string InstanceName { get; set; } = string.Empty;
        
        public BmdComponentFlags Flags { get; set; } = new();

        public string HeightMode { get; set; } = string.Empty;
        public byte[] CultureMask { get; set; } = new byte[8];
        public bool Autoplay { get; set; }
        public bool VisibleInShroud { get; set; }
        public int ParentId { get; set; }
        public bool VisibleWithoutShroud { get; set; }
        public bool NoCulling { get; set; }

        // Early version 4 specific fields
        public byte[] EarlyVersionUnknownBytes { get; set; } = new byte[12];
    }

    public class AiHints
    {
        public List<Separator> Separators { get; set; } = [];
        public List<DirectedPoint> DirectedPoints { get; set; } = [];
        public List<HintPolyLine> PolyLines { get; set; } = [];
        public List<HintPolyLineList> PolyLinesList { get; set; } = [];
    }

    public class Separator
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty;
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class DirectedPoint
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty;
        public RmvVector2 Position { get; set; }
        public RmvVector2 Direction { get; set; }
    }

    public class HintPolyLine
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., "AIH_AMBUSH_ASSAULT_AREA"
        public string ScriptId { get; set; } = string.Empty;
        public bool OnlyVanguard { get; set; }
        public bool OnlyDeployWhenClear { get; set; }
        public bool SpawnVfx { get; set; }
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class HintPolyLineList
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty;
        /// <summary>Version 2 and up.</summary>
        public uint District { get; set; }
        public List<Polygon> PolygonList { get; set; } = [];
    }

    public class Polygon
    {
        public List<RmvVector2> Points { get; set; } = [];
    }

    public class LightProbeInfo
    {
        public ushort Version { get; set; }
        public RmvVector3 Position { get; set; }
        public float OuterRadius { get; set; }
        public float InnerRadius { get; set; }
        public bool IsCylinder { get; set; }
        public bool Primary { get; set; }
        public string HeightMode { get; set; } = string.Empty;
    }

    public class TerrainHoleTriangleInfo
    {
        public ushort TerrainHoleVersion { get; set; }
        public RmvVector3 FirstVert { get; set; }
        public RmvVector3 SecondVert { get; set; }
        public RmvVector3 ThirdVert { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public BmdComponentFlags Flags { get; set; } = new();
    }

    public class PointLightInfo
    {
        public ushort PointLightInfoVersion { get; set; }
        public RmvVector3 Position { get; set; }
        public float Radius { get; set; }
        public float Red { get; set; }
        public float Green { get; set; }
        public float Blue { get; set; }
        public float ColorScale { get; set; }
        public byte AnimationTypeEnum { get; set; }
        public float AnimationSpeedScale1 { get; set; }
        public float AnimationSpeedScale2 { get; set; }
        public float ColorMin { get; set; }
        public float RandomOffset { get; set; }
        public string FalloffType { get; set; } = string.Empty;
        public byte LFRelative { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public bool LightProbeOnly { get; set; }
        public ulong PdlcMask { get; set; }
        public BmdComponentFlags Flags { get; set; } = new();
    }

    public class BuildingProjectileEmitter
    {
        public ushort BuildingProjectileEmitterVersion { get; set; }
        public RmvVector3 Location { get; set; }
        public float[] Rotation { get; set; } = [0, 0, 0];
        public uint BuildingIndex { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public string SpecializedBuildingProjectileEmitterKey { get; set; } = string.Empty;
    }

    public class PlayableArea
    {
        public ushort PlayableAreaVersion { get; set; }
        public bool HasBeenSet { get; set; }
        public float[] BoundingBox { get; set; } = [0, 0, 0, 0];
        public ushort FlagVersion { get; set; }
        public bool ValidNorth { get; set; }
        public bool ValidSouth { get; set; }
        public bool ValidEast { get; set; }
        public bool ValidWest { get; set; }
    }

    public class PolyMeshInfo
    {
        public ushort PolyMeshVersion { get; set; }
        public RmvVector3[] VertexList { get; set; } = [];
        public ushort[] TriangleList { get; set; } = [];
        public string MaterialString { get; set; } = string.Empty;
        public string HeightMode { get; set; } = string.Empty;
        public BmdComponentFlags Flags { get; set; } = new();
        public Matrix Transform { get; set; } = Matrix.Identity;
        public bool VisibleInsideSnowRegion { get; set; }
        public bool VisibleOutsideSnowRegion { get; set; }
        public bool VisibleInsideDestructionRegion { get; set; }
        public bool VisibleOutsideDestructionRegion { get; set; }
        public bool VisibleInShroud { get; set; }
        public bool VisibleWithoutShroud { get; set; }
    }

    public class TerrainStencilBlendTriangle
    {
        public ushort Version { get; set; }
        public RmvVector3 Position0 { get; set; }
        public RmvVector3 Position1 { get; set; }
        public RmvVector3 Position2 { get; set; }
        /// <summary>Version 2 and up.</summary>
        public string HeightMode { get; set; } = string.Empty;
    }

    public class SpotLightInfo
    {
        public ushort Version { get; set; }
        public RmvVector3 Position { get; set; }
        public float QuartX { get; set; }
        public float QuartY { get; set; }
        public float QuartZ { get; set; }
        public float QuartW { get; set; }
        public float Length { get; set; }
        public float InnerAngleRadians { get; set; }
        public float OuterAngleRadians { get; set; }
        public float IntensityRed { get; set; }
        public float IntensityGreen { get; set; }
        public float IntensityBlue { get; set; }
        public float Falloff { get; set; }
        public string Gobo { get; set; } = string.Empty;
        public bool Volumetric { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public ulong PdlcMask { get; set; }
        public BmdComponentFlags Flags { get; set; } = new();
    }

    public class RiverNode
    {
        public ushort Version { get; set; }
        public RmvVector3 Position { get; set; }
        public float Width { get; set; }
        public float FlowSpeed { get; set; }
    }

    public class SoundInfo
    {
        public ushort Version { get; set; }
        public string SoundString { get; set; } = string.Empty;
        public string TypeString { get; set; } = string.Empty;
        public RmvVector3[] CoordList { get; set; } = [];
        public float InnerRadius { get; set; }
        public float OuterRadius { get; set; }
        public (RmvVector3 Min, RmvVector3 Max) InnerCubeBoundingBox { get; set; }
        public (RmvVector3 Min, RmvVector3 Max) OuterCubeBoundingBox { get; set; }
        public RiverNode[] RiverNodeList { get; set; } = [];
        public byte ClampToSurface { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public ulong CampaignTypeMask { get; set; }
        public CultureMask CultureMask { get; set; }
        public RmvVector3 DirectionVector { get; set; }
        public RmvVector3 UpVector { get; set; }
        public string Scope { get; set; } = string.Empty;
    }

    public class CscInfo
    {
        public ushort Version { get; set; }
        public string SceneFile { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        public string HeightMode { get; set; } = string.Empty;
        public ulong PdlcMask { get; set; }
        public bool Autoplay { get; set; }
        public bool VisibleInShroud { get; set; }
        public bool NoCulling { get; set; }
        public string ScriptId { get; set; } = string.Empty;
        public string ParentScriptId { get; set; } = string.Empty;
        public bool VisibleWithoutShroud { get; set; }
        public bool VisibleInTacticalView { get; set; }
        public bool VisibleInTacticalViewOnly { get; set; }
        public bool HoldFirst { get; set; }
        public bool HoldLast { get; set; }
    }

    public class Deployment
    {
        public ushort Version { get; set; }
        public string Category { get; set; } = string.Empty;
        public List<DeploymentZone> DeploymentZones { get; set; } = [];
    }

    public class DeploymentZone
    {
        public ushort Version { get; set; }
        public List<DeploymentZoneRegion> DeploymentZoneRegions { get; set; } = [];
    }

    public class DeploymentZoneRegion
    {
        public ushort Version { get; set; }
        public List<Boundary> Boundaries { get; set; } = [];
        public float Orientation { get; set; }
        public byte SnapFacing { get; set; }
        public uint Id { get; set; }
    }

    public class Boundary
    {
        public ushort Version { get; set; }
        public string BoundaryType { get; set; } = string.Empty;
        public List<RmvVector2> PointList { get; set; } = [];
    }

    /// <summary>The kit's BMD_CATCHMENT_AREA.</summary>
    public class BmdCachedArea
    {
        public ushort Version { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>min_x, min_y, max_x, max_y.</summary>
        public float[] Aabb { get; set; } = [0, 0, 0, 0];
        /// <summary>Version 6 and up.</summary>
        public string BattleType { get; set; } = string.Empty;
        /// <summary>Version 6 and up.</summary>
        public string DefendingFactionRestriction { get; set; } = string.Empty;
        /// <summary>Version 5 and up.</summary>
        public ValidLocationFlags ValidLocationFlags { get; set; } = new();
    }

    public class ValidLocationFlags
    {
        public ushort Version { get; set; }
        public bool ValidNorth { get; set; }
        public bool ValidSouth { get; set; }
        public bool ValidEast { get; set; }
        public bool ValidWest { get; set; }
    }

    /// <summary>
    /// The kit's TOGGLEABLE_BUILDINGS_SLOT. Its records carry no version of their own: the gates follow
    /// the list version (<c>SectionVersions["ToggleableBuildingSlot"]</c>).
    /// </summary>
    public class ToggleableBuildingSlot
    {
        /// <summary>List version 2 and up.</summary>
        public string ToggleableSlotType { get; set; } = string.Empty;
        public List<BuildingLink> BuildingLinks { get; set; } = [];
        /// <summary>List version 3 and up.</summary>
        public List<CscInfo> CompositeScenes { get; set; } = [];
        /// <summary>List version 3 and up.</summary>
        public List<List<RmvVector2>> Outlines { get; set; } = [];
        /// <summary>List version 4 and up.</summary>
        public string ScriptId { get; set; } = string.Empty;
        /// <summary>List version 5 and up.</summary>
        public string MapBarrierRecordKey { get; set; } = string.Empty;
        /// <summary>List version 6 only.</summary>
        public bool MeleeAttackAllowed { get; set; }
        /// <summary>List version 7 and up.</summary>
        public bool GroundMeleeAttackAllowed { get; set; }
    }

    public class TerraindDecal
    {
        public ushort Version { get; set; }
        public ushort SplineVersion { get; set; }
        public RmvVector2 VertexSpacing { get; set; }
        public float UvScale { get; set; }
        public List<TerrainDecalSpline> Splines { get; set; } = [];
        public string Material { get; set; } = string.Empty;
        /// <summary>Version 2 and up (4x3).</summary>
        public Matrix Transform { get; set; } = Matrix.Identity;
    }

    public class TerrainDecalSpline
    {
        public ushort Version { get; set; }
        public float StartWidth { get; set; }
        public float EndWidth { get; set; }
        public RmvVector3 Cp0 { get; set; }
        public RmvVector3 Cp1 { get; set; }
        public RmvVector3 Cp2 { get; set; }
        public RmvVector3 Cp3 { get; set; }
    }

    public class TreeListReference
    {
        public ushort Version { get; set; }
        public string File { get; set; } = string.Empty;
        public string Climate { get; set; } = string.Empty;
    }

    public class GrassListReference
    {
        public ushort Version { get; set; }
        public string File { get; set; } = string.Empty;
        public string Climate { get; set; } = string.Empty;
    }

    public class WaterOutline
    {
        public List<RmvVector2> VertexList { get; set; } = [];
    }



    //Pharaoh Exclusive classes (for Pharaoh's version of the version 25 BMD format)
    //This is version hell! They didn't keep the versions consistent!
    public class CameraZoneNew
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }
    public class MiscParams
    {
        public ushort Version { get; set; }
        public string WaterPlaneMaterial { get; set; } = string.Empty;
        public float NormalTiling { get; set; }
        public float NormalStrengthScale { get; set; }
        public float NormalTimeScale { get; set; }
        public float DepthDistortionCoef { get; set; }
        public float CausticsScale { get; set; }
        public float CausticsMinDepthCoef { get; set; }
        public float CausticsMaxDepthCoef { get; set; }
        public float WaterOpacity { get; set; }
        public float WaterSpeed { get; set; }
        public float WaterDirection { get; set; }
        public bool ShoreWaves { get; set; }
        public RmvVector2 CausticsUvScale { get; set; }
        public RmvVector3 WaterColor { get; set; }
        public RmvVector3 WaterSpecular { get; set; }
    }
}
