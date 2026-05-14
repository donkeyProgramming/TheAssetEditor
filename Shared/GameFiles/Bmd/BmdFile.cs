using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;

namespace Shared.GameFormats.Bmd
{
    public class BmdFile
    {
        public FastBinHeader Header { get; set; } = new FastBinHeader();
        public List<BattlefieldBuilding> BattlefieldBuildings { get; set; } = new();
        public List<BattlefieldBuildingFar> BattlefieldBuildingFars { get; set; } = new();
        public List<CaptureLocation> CaptureLocations { get; set; } = new();
        public List<EFLine> EFLines { get; set; } = new();
        public List<GoOutline> GoOutlines { get; set; } = new();
        public List<NonTerrainOutline> NonTerrainOutlines { get; set; } = new();
        public List<ZonesTemplate> ZonesTemplates { get; set; } = new();
        public List<BmdInfo> BmdInfos { get; set; } = new();
        public List<BmdOutline> BmdOutlines { get; set; } = new();
        public List<TerrainOutline> TerrainOutlines { get; set; } = new();
        public List<LiteBuildingOutline> LiteBuildingOutlines { get; set; } = new();
        public List<CameraZone> CameraZones { get; set; } = new();
        public List<CivilianDeployment> CivilianDeployments { get; set; } = new();
        public List<CivilianShelter> CivilianShelters { get; set; } = new();
        public List<string> Props { get; set; } = new();
        public List<PropInfo> PropInfos { get; set; } = new();
        public List<VfxInfo> VfxInfos { get; set; } = new();
        public AiHints AiHints { get; set; } = new();
        public List<LightProbeInfo> LightProbes { get; set; } = new();
        public List<TerrainHoleTriangleInfo> TerrainHoles { get; set; } = new();
        public List<PointLightInfo> PointLights { get; set; } = new();
        public List<BuildingProjectileEmitter> BuildingProjectileEmitters { get; set; } = new();
        public PlayableArea PlayableArea { get; set; } = new();
        public List<PolyMeshInfo> PolyMeshes { get; set; } = new();
        public List<TerrainStencilBlendTriangle> TerrainStencilBlendTriangles { get; set; } = new();
        public List<SpotLightInfo> SpotLights { get; set; } = new();
        public List<SoundInfo> Sounds { get; set; } = new();
        public List<CscInfo> CscInfos { get; set; } = new();
        public List<Deployment> Deployments { get; set; } = new();
        public List<BmdCachedArea> BmdCachedAreas { get; set; } = new();
        public List<ToggleableBuildingSlot> ToggleableBuildingSlots { get; set; } = new();
        public List<TerraindDecal> TerraindDecals { get; set; } = new();
        public List<TreeListReference> TreeListReferences { get; set; } = new();
        public List<GrassListReference> GrassListReferences { get; set; } = new();
        public List<WaterOutline> WaterOutlines { get; set; } = new();

        //Pharaoh Exclusive classes (for Pharaoh's version of the version 25 BMD format)
        public CameraZoneNew CameraZoneNew { get; set; } = new();
        public MiscParams MiscParams { get; set; } = new();
    }

    public class FastBinHeader
    {
        public string FastBin0 { get; set; } = string.Empty;
        public ushort FastBinVersion { get; set; }
    }

    
    public struct CultureMask
    {
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
        
        // Properties
        public ushort PropertiesVersion { get; set; }
        public string PropertiesBuildingId { get; set; } = string.Empty;
        public float StartingDamageUnary { get; set; }
        public bool OnFire { get; set; }
        public bool StartDisabled { get; set; }
        public bool WeakPoint { get; set; }
        public bool AiBreachable { get; set; }
        public bool Indestructible { get; set; }
        public bool Dockable { get; set; }
        public bool Toggleable { get; set; }
        public bool Lite { get; set; }
        public bool CastShadows { get; set; }
        public bool KeyBuilding { get; set; }
        public bool KeyBuildingUseFort { get; set; }
        public bool IsPropInOutfield { get; set; }
        public bool SettlementLevelConfigurable { get; set; }
        public bool HideTooltip { get; set; }
        public bool IncludeInFog { get; set; }
        
        public string HeightMode { get; set; } = string.Empty;
        public long Uid { get; set; }
    }

    public class BattlefieldBuildingFar
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
        
    }

    public class CaptureLocation
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
        public ushort Zero { get; set; }
        public float Something1 { get; set; }
        public float Something2 { get; set; }
        public int Something3 { get; set; }
        public int Something4 { get; set; }
        public int Something5 { get; set; }
        public string Str { get; set; } = string.Empty;
        public string Str2 { get; set; } = string.Empty;
        public float[] Coords { get; set; } = Array.Empty<float>();
        public string Str3 { get; set; } = string.Empty;
        public float Something6 { get; set; }
        public float Something7 { get; set; }
        public byte[] Bools { get; set; } = new byte[7];
        public ushort Something8 { get; set; }
        public float Something9 { get; set; }
        public float Something10 { get; set; }
    }

    public class EFLine
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class GoOutline
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class NonTerrainOutline
    {
        public List<RmvVector2> VertexList { get; set; } = new();
    }

    public class ZonesTemplate
    {
        public ushort Version { get; set; }
        public List<RmvVector2> Outline { get; set; } = new();
        public string ZoneName { get; set; } = string.Empty;
        public string EntityFormationTemplateName { get; set; } = string.Empty;
        public uint LinesLength { get; set; }
        public byte[] LinesData { get; set; } = new byte[0]; // Raw data since Lines structure is unknown
        public float[] TransformMatrix { get; set; } = new float[16]; // 4x4 transform matrix
    }
    
    public class BmdInfo
    {
        public ushort Version { get; set; }
        public string BmdString { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        public byte[] SeasonsMaybe { get; set; } = new byte[4]; //this has to correspond to <property_overrides/>
        public CultureMask CultureMask { get; set; } //"campaign_type_mask"?
        public string RegionString { get; set; } = string.Empty;
        public string HeightMode { get; set; } = string.Empty;
        public byte[] Uid { get; set; } = new byte[8];
    }

    public class BmdOutline
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class TerrainOutline
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class LiteBuildingOutline
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class CameraZone
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class CivilianDeployment
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class CivilianShelter
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class PropInfo
    {
        public ushort PropInfoVersion { get; set; }
        public string Rmv2Path { get; set; } = string.Empty;
        public Matrix Transform { get; set; } = Matrix.Identity;
        public bool IsDecal { get; set; }
        public bool LogicalDecal { get; set; }
        public bool IsFauna { get; set; }
        public bool VisibleInsideSnowRegion { get; set; }
        public bool VisibleOutsideSnowRegion { get; set; }
        public bool VisibleInsideDestructionRegion { get; set; }
        public bool VisibleOutsideDestructionRegion { get; set; }
        public bool Animated { get; set; }
        public float DecalParallaxScale { get; set; }
        public float DecalTiling { get; set; }
        public bool DecalOverrideGbufferNormal { get; set; }
        
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
        public bool SomeWeirdThing { get; set; } // for Version 21/22
        public bool SomeWeirdThing2 { get; set; } // for Version 22
        public bool UseDynamicShadows { get; set; }
        public bool UsesTerrainVertexOffset { get; set; }
        
        // Early version 4 specific fields
        public byte[] EarlyVersionUnknownBytes { get; set; } = new byte[23];
        public bool EarlyVersionUnknownBool { get; set; }
    }

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
        public bool VisibleInShroudOnly { get; set; }
    }

    public class AiHints
    {
        public List<Separator> Separators { get; set; } = new();
        public List<DirectedPoint> DirectedPoints { get; set; } = new();
        public List<HintPolyLine> PolyLines { get; set; } = new();
        public List<HintPolyLineList> PolyLinesList { get; set; } = new();
    }

    public class Separator
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class DirectedPoint
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class HintPolyLine
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., "AIH_AMBUSH_ASSAULT_AREA"
        public string ScriptId { get; set; } = string.Empty;
        public bool OnlyVanguard { get; set; }
        public bool OnlyDeployWhenClear { get; set; }
        public bool SpawnVfx { get; set; }
        public List<RmvVector2> Points { get; set; } = new();
    }

    public class HintPolyLineList
    {
        public ushort Version { get; set; }
        public string Type { get; set; } = string.Empty;
        public uint District { get; set; }
        public List<Polygon> PolygonList { get; set; } = new();
    }

    public class Polygon
    {
        public List<RmvVector2> Points { get; set; } = new();
    }

    public class LightProbeInfo
    {
        public ushort Version { get; set; }
        public RmvVector3 Position { get; set; }
        public float OuterRadius { get; set; }
        public float InnerRadius { get; set; }
        public byte SomeZero { get; set; }
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
        public byte[] Booleans { get; set; } = new byte[10];
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
        public byte[] MoreData2 { get; set; } = new byte[10];
    }

    public class BuildingProjectileEmitter
    {
        public ushort BuildingProjectileEmitterVersion { get; set; }
        public RmvVector3 Location { get; set; }
        public float[] Rotation { get; set; } = new float[3];
        public uint BuildingIndex { get; set; }
        public string HeightMode { get; set; } = string.Empty;
        public string SpecializedBuildingProjectileEmitterKey { get; set; } = string.Empty;
    }

    public class PlayableArea
    {
        public ushort PlayableAreaVersion { get; set; }
        public bool HasBeenSet { get; set; }
        public float[] BoundingBox { get; set; } = new float[4];
        public ushort FlagVersion { get; set; }
        public bool Flag1 { get; set; }
        public bool Flag2 { get; set; }
        public bool Flag3 { get; set; }
        public bool Flag4 { get; set; }
    }

    public class PolyMeshInfo
    {
        public ushort PolyMeshVersion { get; set; }
        public RmvVector3[] VertexList { get; set; } = Array.Empty<RmvVector3>();
        public ushort[] TriangleList { get; set; } = Array.Empty<ushort>();
        public string MaterialString { get; set; } = string.Empty;
        public string HeightMode { get; set; } = string.Empty;
        public byte[] MoreData { get; set; } = new byte[8];
        public bool VisibleInTactical { get; set; }
        public bool OnlyVisibleInTactical { get; set; }
        public Matrix Transform { get; set; } = Matrix.Identity;
        public byte[] Booleans { get; set; } = new byte[4];
        public bool VisibleInShroud { get; set; }
        public byte[] MoreBooleans { get; set; } = new byte[1];
    }

    public class TerrainStencilBlendTriangle
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
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
        public byte[] MoreData { get; set; } = new byte[18];
    }

    public class SoundInfo
    {
        public ushort Version { get; set; }
        public string SoundString { get; set; } = string.Empty;
        public string TypeString { get; set; } = string.Empty;
        public RmvVector3[] CoordList { get; set; } = Array.Empty<RmvVector3>();
        public float InnerRadius { get; set; }
        public float OuterRadius { get; set; }
        public (RmvVector3 Min, RmvVector3 Max) InnerCubeBoundingBox { get; set; }
        public (RmvVector3 Min, RmvVector3 Max) OuterCubeBoundingBox { get; set; }
        public uint RiverNodesLength { get; set; }
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
        public List<DeploymentZone> DeploymentZones { get; set; } = new();
    }

    public class DeploymentZone
    {
        public ushort Version { get; set; }
        public List<DeploymentZoneRegion> DeploymentZoneRegions { get; set; } = new();
    }

    public class DeploymentZoneRegion
    {
        public ushort Version { get; set; }
        public List<Boundary> Boundaries { get; set; } = new();
        public float Orientation { get; set; }
        public byte SnapFacing { get; set; }
        public uint Id { get; set; }
    }

    public class Boundary
    {
        public ushort Version { get; set; }
        public string BoundaryType { get; set; } = string.Empty;
        public List<RmvVector2> PointList { get; set; } = new();
    }

    public class BmdCachedArea
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class ToggleableBuildingSlot
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class TerraindDecal
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class TreeListReference
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class GrassListReference
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
    }

    public class WaterOutline
    {
        //TODO: Not properly implemented
        public ushort Version { get; set; }
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
