using System;
using System.IO;
using System.Text;
using Serilog;
using Shared.ByteParsing;
using Shared.Core.Misc;
using Shared.Core.ErrorHandling;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;
using Shared.GameFormats.Bmd;

namespace Shared.GameFormats.Bmd
{
    public class BmdParser
    {
        private readonly BinaryReader _reader;
        private readonly Stream _stream;
        private readonly ushort _fastBinVersion;
        private readonly ILogger _logger = Logging.Create<BmdParser>();

        public BmdParser(Stream stream)
        {
            _stream = stream;
            _reader = new BinaryReader(stream, Encoding.UTF8);
            
            // Read header
            _fastBinVersion = ReadFastBinHeader();
        }

        private ushort ReadFastBinHeader()
        {
            var fastBin0Bytes = _reader.ReadBytes(8);
            var fastBin0 = Encoding.UTF8.GetString(fastBin0Bytes);
            var fastBinVersion = _reader.ReadUInt16();
            
            // Validate the FASTBIN0 magic number
            if (fastBin0 != "FASTBIN0")
            {
                throw new InvalidOperationException($"Invalid file format. Expected 'FASTBIN0' but got '{fastBin0}'. This is not a valid BMD file.");
            }
            
            return fastBinVersion;
        }

        public BmdFile Parse()
        {
            var bmdFile = new BmdFile
            {
                Header = new FastBinHeader
                {
                    FastBin0 = "FASTBIN0",
                    FastBinVersion = _fastBinVersion
                }
            };

            _logger.Here().Information($"BMD Parser - FastBinVersion: {_fastBinVersion}, FastBin0: {bmdFile.Header.FastBin0}");
            _logger.Here().Information($"BMD Parser - Initial stream position: {_stream.Position}, Length: {_stream.Length}");

            try
            {
                if (_fastBinVersion < 8)
                {
                    //In a super early version of FastBin, there was stuff up here
                    //(and it was eventually moved down?)

                    //Props (the modern version of props is way down the page)
                    //no version
                    var early_props_length = _reader.ReadUInt32();
                    _logger.Here().Information($"BMD Parser - Early props length: {early_props_length}");
                    
                    // Read early props entries
                    for (int i = 0; i < early_props_length; i++)
                    {
                        if (_stream.Position >= _stream.Length) break;
                        
                        var earlyProp = new PropInfo();
                        
                        earlyProp.Rmv2Path = ReadString();
                        earlyProp.PropInfoVersion = _reader.ReadUInt16(); //seems to be the version
                        earlyProp.Transform = ReadRowMajorMatrix(true);

                        //23bytes of bytes I can't make heads or tails of
                        earlyProp.EarlyVersionUnknownBytes = _reader.ReadBytes(23);

                        if (earlyProp.PropInfoVersion > 1)
                            earlyProp.EarlyVersionUnknownBool = _reader.ReadByte() != 0;
                        if (earlyProp.PropInfoVersion > 2) //this being the seasons is a guess
                        {
                            earlyProp.Flags.SeasonSpring = _reader.ReadByte() != 0;
                            earlyProp.Flags.SeasonSummer = _reader.ReadByte() != 0;
                            earlyProp.Flags.SeasonAutumn = _reader.ReadByte() != 0;
                            earlyProp.Flags.SeasonWinter = _reader.ReadByte() != 0;
                        }
                        
                        bmdFile.PropInfos.Add(earlyProp);
                    }

                    //One of these is likely the early version of vfx, and the others... who knows
                    var otherthing1_length = _reader.ReadUInt32();
                    var otherthing2_length = _reader.ReadUInt32();
                    var otherthing3_length = _reader.ReadUInt32();
                    var otherthing4_length = _reader.ReadUInt32();
                }
                
                if (_fastBinVersion < 22)
                {
                    // Some unknown thing that was removed for some ungodly reason
                    var some_version = _reader.ReadUInt16();
                    var some_length = _reader.ReadUInt32();
                    _logger.Here().Information($"BMD Parser - Some version: {some_version}, Some length: {some_length}");
                }

                // BattlefieldBuilding
                var battlefieldBuildingVersion = _reader.ReadUInt16();
                ReadCollection("BattlefieldBuilding", bmdFile.BattlefieldBuildings, ReadBattlefieldBuilding, battlefieldBuildingVersion);

                // BattlefieldBuildingFar
                var battlefieldBuildingFarVersion = _reader.ReadUInt16();
                ReadCollection("BattlefieldBuildingFar", bmdFile.BattlefieldBuildingFars, ReadBattlefieldBuildingFar, battlefieldBuildingFarVersion);

                // CaptureLocation
                var captureLocationVersion = _reader.ReadUInt16();
                ReadCollection("CaptureLocation", bmdFile.CaptureLocations, ReadCaptureLocation, captureLocationVersion);

                // EFLine
                // no version
                ReadCollection("EFLine", bmdFile.EFLines, ReadEFLine);

                // GoOutline
                // no version
                ReadCollection("GoOutline", bmdFile.GoOutlines, ReadGoOutline);

                // NonTerrainOutline
                // no version
                ReadCollection("NonTerrainOutline", bmdFile.NonTerrainOutlines, ReadNonTerrainOutline);

                // ZonesTemplate
                var zonesTemplateVersion = _reader.ReadUInt16();
                ReadCollection("ZonesTemplate", bmdFile.ZonesTemplates, ReadZonesTemplate, zonesTemplateVersion);

                // Bmd
                var bmdVersion = _reader.ReadUInt16();
                ReadCollection("BmdInfo", bmdFile.BmdInfos, ReadBmdInfo, bmdVersion);

                // BmdOutlines
                var bmdOutlineVersion = _reader.ReadUInt16();
                ReadCollection("BmdOutline", bmdFile.BmdOutlines, ReadBmdOutline, bmdOutlineVersion);

                // TerrainOutlines
                // no version
                ReadCollection("TerrainOutline", bmdFile.TerrainOutlines, ReadTerrainOutline);

                // LiteBuildingOutlines
                // no version
                ReadCollection("LiteBuildingOutline", bmdFile.LiteBuildingOutlines, ReadLiteBuildingOutline);

                if (_fastBinVersion > 4)
                {
                    // CameraZones
                    var cameraZoneVersion = _reader.ReadUInt16();
                    ReadCollection("CameraZone", bmdFile.CameraZones, ReadCameraZone, cameraZoneVersion);
                }
                if (_fastBinVersion > 7)
                {
                    // CivilianDeployments
                    // no version
                    ReadCollection("CivilianDeployment", bmdFile.CivilianDeployments, ReadCivilianDeployment);

                    // CivilianShelters
                    // no version
                    ReadCollection("CivilianShelter", bmdFile.CivilianShelters, ReadCivilianShelter);

                    // Prop
                    var propVersion = _reader.ReadUInt16();
                    ReadPropInfos(propVersion, bmdFile);

                    // Vfx
                    var vfxVersion = _reader.ReadUInt16();
                    ReadCollection("VfxInfo", bmdFile.VfxInfos, ReadVfxInfo, vfxVersion);

                    // AiHints
                    var aiHintsVersion = _reader.ReadUInt16();
                    _logger.Here().Information($"BMD Parser - AiHints version: {aiHintsVersion}");
                    bmdFile.AiHints = ReadAiHints();

                    // LightProbe
                    var lightProbeVersion = _reader.ReadUInt16();
                    ReadCollection("LightProbe", bmdFile.LightProbes, ReadLightProbeInfo, lightProbeVersion);

                    // TerrainHole
                    var terrainHoleVersion = _reader.ReadUInt16();
                    ReadCollection("TerrainHole", bmdFile.TerrainHoles, ReadTerrainHoleInfo, terrainHoleVersion);

                    // PointLight
                    var pointLightVersion = _reader.ReadUInt16();
                    ReadCollection("PointLight", bmdFile.PointLights, ReadPointLightInfo, pointLightVersion);

                    // BuildingProjectileEmitters
                    var buildingProjectileEmitterVersion = _reader.ReadUInt16();
                    ReadCollection("BuildingProjectileEmitter", bmdFile.BuildingProjectileEmitters, ReadBuildingProjectileEmitter, buildingProjectileEmitterVersion);

                    // PlayableArea
                    _logger.Here().Information($"BMD Parser - PlayableArea");
                    bmdFile.PlayableArea = ReadPlayableArea();

                    // PolyMesh
                    var polyMeshVersion = _reader.ReadUInt16();
                    ReadCollection("PolyMesh", bmdFile.PolyMeshes, ReadPolyMeshInfo, polyMeshVersion);

                    // TerrainStencilBlendTriangles
                    var terrainStencilBlendTriangleVersion = _reader.ReadUInt16();
                    ReadCollection("TerrainStencilBlendTriangle", bmdFile.TerrainStencilBlendTriangles, ReadTerrainStencilBlendTriangle, terrainStencilBlendTriangleVersion);

                    // SpotLight
                    var spotLightVersion = _reader.ReadUInt16();
                    ReadCollection("SpotLight", bmdFile.SpotLights, ReadSpotLightInfo, spotLightVersion);

                    // Sound
                    var soundVersion = _reader.ReadUInt16();
                    ReadCollection("Sound", bmdFile.Sounds, ReadSoundInfo, soundVersion);

                    // CSC (Composite Scene Container)
                    var cscVersion = _reader.ReadUInt16();
                    ReadCollection("CSC", bmdFile.CscInfos, ReadCscInfo, cscVersion);
                }
                if (_fastBinVersion > 21)
                {
                    // Deployment
                    var deploymentVersion = _reader.ReadUInt16();
                    ReadCollection("Deployment", bmdFile.Deployments, ReadDeployment, deploymentVersion);

                    // BmdCachedAreas
                    var bmdCachedAreaVersion = _reader.ReadUInt16();
                    ReadCollection("BmdCachedArea", bmdFile.BmdCachedAreas, ReadBmdCachedArea, bmdCachedAreaVersion);
                }
                if (_fastBinVersion > 23)
                {
                    // ToggleableBuildingSlots
                    var toggleableBuildingSlotVersion = _reader.ReadUInt16();
                    ReadCollection("ToggleableBuildingSlot", bmdFile.ToggleableBuildingSlots, ReadToggleableBuildingSlot, toggleableBuildingSlotVersion);
                }
                if (_fastBinVersion > 24)
                {
                    // TerraindDecals
                    var terraindDecalVersion = _reader.ReadUInt16();
                    ReadCollection("TerraindDecal", bmdFile.TerraindDecals, ReadTerraindDecal, terraindDecalVersion);

                    // TreeListReferences
                    var treeListReferenceVersion = _reader.ReadUInt16();
                    ReadCollection("TreeListReference", bmdFile.TreeListReferences, ReadTreeListReference, treeListReferenceVersion);

                    // GrassListReferences
                    var grassListReferenceVersion = _reader.ReadUInt16();
                    ReadCollection("GrassListReference", bmdFile.GrassListReferences, ReadGrassListReference, grassListReferenceVersion);
                }
                if (_fastBinVersion > 26)
                {
                    // WaterOutlines
                    // no version
                    ReadCollection("WaterOutline", bmdFile.WaterOutlines, ReadWaterOutline);
                }

                return bmdFile;
            }
            catch (EndOfStreamException ex)
            {
                _logger.Here().Error($"BMD Parser - EndOfStreamException caught: {ex.Message}");
                _logger.Here().Error($"BMD Parser - Stream position: {_stream.Position}, Length: {_stream.Length}");
                _logger.Here().Warning($"BMD Parser - Returning partially parsed BMD file with {bmdFile.BattlefieldBuildings.Count} battlefield buildings");
                return bmdFile;
            }
            catch (Exception ex)
            {
                _logger.Here().Error($"BMD Parser - Unexpected exception: {ex.Message}");
                _logger.Here().Error($"BMD Parser - Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public static BmdFile Parse(byte[] data)
        {
            using var stream = new MemoryStream(data);
            var parser = new BmdParser(stream);
            return parser.Parse();
        }

        private void ReadCollection<T>(string collectionName, List<T> collection, Func<T> readFunc, ushort? version = null)
        {
            var count = _reader.ReadUInt32();
            var versionText = version.HasValue ? $" version: {version.Value}," : "";
            _logger.Here().Information($"BMD Parser - {collectionName}{versionText} count: {count}");
            
            for (int i = 0; i < count; i++)
            {
                if (_stream.Position >= _stream.Length) break;
                collection.Add(readFunc());
            }
        }

        private BattlefieldBuilding ReadBattlefieldBuilding()
        {
            var building = new BattlefieldBuilding();
            building.Version = _reader.ReadUInt16();

            building.BuildingId = ReadString();
            if (building.Version > 8)
                building.ParentId = _reader.ReadInt32();
            else
                building.ParentId = _reader.ReadInt16();
            building.BuildingKey = ReadString();
            building.PositionType = ReadString();

            // Transform matrix (3x4 matrix stored in row-major order)
            building.Transform = ReadRowMajorMatrix(false);
            
            // Properties
            building.PropertiesVersion = _reader.ReadUInt16();
            building.PropertiesBuildingId = ReadString();
            building.StartingDamageUnary = _reader.ReadSingle();
            building.OnFire = _reader.ReadByte() != 0;
            building.StartDisabled = _reader.ReadByte() != 0;
            building.WeakPoint = _reader.ReadByte() != 0;
            building.AiBreachable = _reader.ReadByte() != 0;
            building.Indestructible = _reader.ReadByte() != 0;
            building.Dockable = _reader.ReadByte() != 0;
            building.Toggleable = _reader.ReadByte() != 0;
            building.Lite = _reader.ReadByte() != 0;
            building.CastShadows = _reader.ReadByte() != 0;
            building.KeyBuilding = _reader.ReadByte() != 0;
            if (building.Version > 8)
            {
                building.KeyBuildingUseFort = _reader.ReadByte() != 0;
                building.IsPropInOutfield = _reader.ReadByte() != 0;
                building.SettlementLevelConfigurable = _reader.ReadByte() != 0;
                building.HideTooltip = _reader.ReadByte() != 0;
                building.IncludeInFog = _reader.ReadByte() != 0;
            }
            
            building.HeightMode = ReadString();

            if (building.Version > 8)
                building.Uid = _reader.ReadInt64();
            
            return building;
        }

        private BattlefieldBuildingFar ReadBattlefieldBuildingFar()
        {
            throw new NotImplementedException("BmdOutline parsing not implemented yet");
        }

        private CaptureLocation ReadCaptureLocation()
        {
            var location = new CaptureLocation();
            location.Version = _reader.ReadUInt16();
            location.Zero = _reader.ReadUInt16();
            location.Something1 = _reader.ReadSingle();
            location.Something2 = _reader.ReadSingle();
            location.Something3 = _reader.ReadInt32();
            location.Something4 = _reader.ReadInt32();
            location.Something5 = _reader.ReadInt32();
            location.Str = ReadString();
            location.Str2 = ReadString();
            
            var coordCount = _reader.ReadUInt32();
            location.Coords = new float[coordCount * 2];
            for (int i = 0; i < location.Coords.Length; i++)
            {
                location.Coords[i] = _reader.ReadSingle();
            }
            
            location.Str3 = ReadString();
            location.Something6 = _reader.ReadSingle();
            location.Something7 = _reader.ReadSingle();
            location.Bools = _reader.ReadBytes(7);
            location.Something8 = _reader.ReadUInt16();
            location.Something9 = _reader.ReadSingle();
            location.Something10 = _reader.ReadSingle();
            return location;
        }

        private EFLine ReadEFLine()
        {
            throw new NotImplementedException("BmdOutline parsing not implemented yet");
        }

        private GoOutline ReadGoOutline()
        {
            throw new NotImplementedException("BmdOutline parsing not implemented yet");
        }

        private NonTerrainOutline ReadNonTerrainOutline()
        {
            var outline = new NonTerrainOutline();
            
            // Read VertexListLength (hidden=true, so we just read it but don't store it)
            var vertexListLength = _reader.ReadUInt32();
            
            // Read VertexList array
            outline.VertexList = new List<RmvVector2>();
            for (int i = 0; i < vertexListLength; i++)
            {
                var x = _reader.ReadSingle();
                var y = _reader.ReadSingle();
                outline.VertexList.Add(new RmvVector2(x, y));
            }
            
            return outline;
        }

        private ZonesTemplate ReadZonesTemplate()
        {
            var template = new ZonesTemplate();
            template.Version = _reader.ReadUInt16();
            
            var outlineLength = _reader.ReadUInt32();
            for (uint i = 0; i < outlineLength; i++)
            {
                var coord = new RmvVector2
                {
                    X = _reader.ReadSingle(),
                    Y = _reader.ReadSingle()
                };
                template.Outline.Add(coord);
            }
            
            template.ZoneName = ReadString();
            template.EntityFormationTemplateName = ReadString();
            
            template.LinesLength = _reader.ReadUInt32();
            //template.LinesData = blah blah; //skip the actual Lines data since structure is unknown
            
            for (int i = 0; i < 16; i++)
                template.TransformMatrix[i] = _reader.ReadSingle();
            
            return template;
        }

        private BmdInfo ReadBmdInfo()
        {
            var bmd = new BmdInfo();
            bmd.Version = _reader.ReadUInt16();
            
            bmd.BmdString = ReadString();
            bmd.Transform = ReadRowMajorMatrix(true);
            bmd.SeasonsMaybe = _reader.ReadBytes(4);
            bmd.CultureMask = ReadCultureMask();
            bmd.RegionString = ReadString();
            bmd.HeightMode = ReadString();
            if (bmd.Version > 8)
                bmd.Uid = _reader.ReadBytes(8);
            return bmd;
        }

        private BmdOutline ReadBmdOutline()
        {
            throw new NotImplementedException("BmdOutline parsing not implemented yet");
        }

        private TerrainOutline ReadTerrainOutline()
        {
            throw new NotImplementedException("TerrainOutline parsing not implemented yet");
        }

        private LiteBuildingOutline ReadLiteBuildingOutline()
        {
            throw new NotImplementedException("LiteBuildingOutline parsing not implemented yet");
        }

        private CameraZone ReadCameraZone()
        {
            throw new NotImplementedException("CameraZone parsing not implemented yet");
        }

        private CivilianDeployment ReadCivilianDeployment()
        {
            throw new NotImplementedException("CivilianDeployment parsing not implemented yet");
        }

        private CivilianShelter ReadCivilianShelter()
        {
            throw new NotImplementedException("CivilianShelter parsing not implemented yet");
        }


        private void ReadPropInfos(ushort propVersion, BmdFile bmdFile)
        {
            _logger.Here().Information($"BMD Parser - Prop version: {propVersion}");
            var propsList = new List<string>();
            
            if (propVersion > 1)
            {
                var propCount = _reader.ReadUInt32();
                 _logger.Here().Information($"BMD Parser - PropString count: {propCount}");
                
                for (int i = 0; i < propCount; i++)
                {
                    if (_stream.Position >= _stream.Length) break;
                    propsList.Add(ReadString());
                }
            }

            // PropInfo
            var propInfoCount = _reader.ReadUInt32();
            _logger.Here().Information($"BMD Parser - PropInfo count: {propInfoCount}");
            
            for (int i = 0; i < propInfoCount; i++)
            {
                if (_stream.Position >= _stream.Length) break;
                bmdFile.PropInfos.Add(ReadPropInfo(propsList));
            }
        }

        private PropInfo ReadPropInfo(List<string> propsList)
        {
            var prop = new PropInfo();
            prop.PropInfoVersion = _reader.ReadUInt16();
            
            if (prop.PropInfoVersion <= 12)
                prop.Rmv2Path = ReadString(); //Read string directly for old versions
            else
            {
                var propIndex = _reader.ReadUInt32();
                prop.Rmv2Path = propsList[(int)propIndex]; // Map the PropIndex to the actual RMV2 path from the props list
            }
            
            // Read transform matrix (3x4 matrix stored in row-major order)
            prop.Transform = ReadRowMajorMatrix(false);
            prop.IsDecal = _reader.ReadByte() != 0;
            prop.LogicalDecal = _reader.ReadByte() != 0;
            prop.IsFauna = _reader.ReadByte() != 0;
            prop.VisibleInsideSnowRegion = _reader.ReadByte() != 0;
            prop.VisibleOutsideSnowRegion = _reader.ReadByte() != 0;
            prop.VisibleInsideDestructionRegion = _reader.ReadByte() != 0;
            prop.VisibleOutsideDestructionRegion = _reader.ReadByte() != 0;
            prop.Animated = _reader.ReadByte() != 0;
            prop.DecalParallaxScale = _reader.ReadSingle();
            prop.DecalTiling = _reader.ReadSingle();
            prop.DecalOverrideGbufferNormal = _reader.ReadByte() != 0;

            prop.Flags = ReadBmdComponentFlags();
            
            prop.VisibleInShroud = _reader.ReadByte() != 0;
            prop.ApplyToTerrain = _reader.ReadByte() != 0;
            prop.ApplyToPropsOrReceiveDecal = _reader.ReadByte() != 0;
            prop.RenderAboveSnow = _reader.ReadByte() != 0;
            
            if (prop.PropInfoVersion > 7)
                prop.HeightMode = ReadString();
            
            if (prop.PropInfoVersion > 14)
                prop.CultureMask = _reader.ReadBytes(8);
            else if (prop.PropInfoVersion > 10)
            {
                prop.CultureMask = new byte[8];
                var oldMask = _reader.ReadBytes(4);
                Array.Copy(oldMask, prop.CultureMask, 4);
            }
            else
                prop.CultureMask = new byte[8]; //no culture mask in the binary

             //there's an extra byte in here just for version 9, then it's gone in version 10
             //  (representing it with "CastsShadow")

            if (prop.PropInfoVersion > 11 || prop.PropInfoVersion == 9)
                prop.CastsShadow = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 13)
                prop.NoCulling = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 15)
                prop.HasHeightPatch = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 16)
                prop.ApplyHeightPatch = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 18)
                prop.IncludeInFog = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 19)
                prop.VisibleWithoutShroud = _reader.ReadByte() != 0;

            //Not quite sure what's happening with version 21/22
            if (prop.PropInfoVersion == 21)
                prop.SomeWeirdThing = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion == 22)
            {
                prop.SomeWeirdThing = _reader.ReadByte() != 0;
                prop.SomeWeirdThing2 = _reader.ReadByte() != 0;
            }

            if (prop.PropInfoVersion > 23)
                prop.UseDynamicShadows = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 24)
                prop.UsesTerrainVertexOffset = _reader.ReadByte() != 0;
            
            return prop;
        }

        private VfxInfo ReadVfxInfo()
        {
            var vfx = new VfxInfo();
            vfx.VfxInfoVersion = _reader.ReadUInt16();
            vfx.VfxString = ReadString();
            
            // Read transform matrix (3x4 matrix stored in row-major order)
            vfx.Transform = ReadRowMajorMatrix(false);
            
            vfx.EmissionRate = _reader.ReadSingle();
            vfx.InstanceName = ReadString();
            vfx.Flags = ReadBmdComponentFlags();
            
            if (vfx.VfxInfoVersion > 2)
                vfx.HeightMode = ReadString();
            if (vfx.VfxInfoVersion > 3)
            {
                if (vfx.VfxInfoVersion > 5)
                    vfx.CultureMask = _reader.ReadBytes(8);
                else
                {
                    vfx.CultureMask = new byte[8];
                    var oldMask = _reader.ReadBytes(4);
                    Array.Copy(oldMask, vfx.CultureMask, 4);
                }
            }
            if (vfx.VfxInfoVersion > 4)
            {
                vfx.Autoplay = _reader.ReadByte() != 0;
                vfx.VisibleInShroud = _reader.ReadByte() != 0;
            }
            if (vfx.VfxInfoVersion > 7)
            {
                vfx.ParentId = _reader.ReadInt32();
            }
            if (vfx.VfxInfoVersion > 9)
            {
                vfx.VisibleInShroudOnly = _reader.ReadByte() != 0;
            }
            
            return vfx;
        }

        private AiHints ReadAiHints()
        {
            var aiHints = new AiHints();
            
            // Read Separators
            var separatorsVersion = _reader.ReadUInt16();
            var separatorsCount = _reader.ReadUInt32();
            for (int i = 0; i < separatorsCount; i++)
            {
                throw new NotImplementedException("AiHints-Separators parsing not implemented yet");
            }
            
            // Read DirectedPoints
            var directedPointsVersion = _reader.ReadUInt16();
            var directedPointsCount = _reader.ReadUInt32();
            for (int i = 0; i < directedPointsCount; i++)
            {
                throw new NotImplementedException("AiHints-DirectedPoints parsing not implemented yet");
            }
            
            // Read PolyLines
            var polyLinesVersion = _reader.ReadUInt16();
            var polyLinesCount = _reader.ReadUInt32();
            for (int i = 0; i < polyLinesCount; i++)
            {
                var polyLine = new HintPolyLine();
                polyLine.Version = _reader.ReadUInt16();
                polyLine.Type = ReadString();

                var pointsCount = _reader.ReadUInt32();
                for (int j = 0; j < pointsCount; j++)
                {
                    var point = new RmvVector2();
                    point.X = _reader.ReadSingle();
                    point.Y = _reader.ReadSingle();
                    polyLine.Points.Add(point);
                }
                
                if (polyLine.Version > 3)
                    polyLine.ScriptId = ReadString();
                if (polyLine.Version > 2) //version is a guess
                    polyLine.OnlyVanguard = _reader.ReadByte() != 0;
                if (polyLine.Version > 3)
                {
                    polyLine.OnlyDeployWhenClear = _reader.ReadByte() != 0;
                    polyLine.SpawnVfx = _reader.ReadByte() != 0;
                }
                
                aiHints.PolyLines.Add(polyLine);
            }
            
            // Read PolyLinesList
            var polyLinesListVersion = _reader.ReadUInt16();
            var polyLinesListCount = _reader.ReadUInt32();
            for (int i = 0; i < polyLinesListCount; i++)
            {
                var polyLineList = new HintPolyLineList();
                polyLineList.Version = _reader.ReadUInt16();
                
                // Read type string
                var typeLength = _reader.ReadUInt16();
                if (typeLength > 0)
                {
                    polyLineList.Type = Encoding.UTF8.GetString(_reader.ReadBytes(typeLength));
                }
                
                // Read district
                polyLineList.District = _reader.ReadUInt32();
                
                // Read polygon list
                var polygonListLength = _reader.ReadUInt32();
                for (int j = 0; j < polygonListLength; j++)
                {
                    var polygon = new Polygon();
                    
                    // Read points for this polygon
                    var pointsLength = _reader.ReadUInt32();
                    for (int k = 0; k < pointsLength; k++)
                    {
                        var point = new RmvVector2();
                        point.X = _reader.ReadSingle();
                        point.Y = _reader.ReadSingle();
                        polygon.Points.Add(point);
                    }
                    
                    polyLineList.PolygonList.Add(polygon);
                }
                
                aiHints.PolyLinesList.Add(polyLineList);
            }
            
            _logger.Here().Information($"BMD Parser - Read AiHints: {separatorsCount} separators, {directedPointsCount} directed points, {polyLinesCount} polylines, {polyLinesListCount} polylines list");
            return aiHints;
        }

        private LightProbeInfo ReadLightProbeInfo()
        {
            var probe = new LightProbeInfo();
            probe.Version = _reader.ReadUInt16();
            probe.Position = ReadRmvVector3();
            probe.OuterRadius = _reader.ReadSingle();
            probe.InnerRadius = _reader.ReadSingle();
            probe.SomeZero = _reader.ReadByte();
            probe.Primary = _reader.ReadByte() != 0;
            probe.HeightMode = ReadString();
            return probe;
        }

        private TerrainHoleTriangleInfo ReadTerrainHoleTriangleInfo()
        {
            var hole = new TerrainHoleTriangleInfo();
            hole.TerrainHoleVersion = _reader.ReadUInt16();
            hole.FirstVert = ReadRmvVector3();
            hole.SecondVert = ReadRmvVector3();
            hole.ThirdVert = ReadRmvVector3();
            hole.HeightMode = ReadString();
            if (hole.TerrainHoleVersion > 2)
            {
                hole.Booleans = _reader.ReadBytes(10);
            }
            return hole;
        }

        private PointLightInfo ReadPointLightInfo()
        {
            var light = new PointLightInfo();
            light.PointLightInfoVersion = _reader.ReadUInt16();
            light.Position = ReadRmvVector3();
            light.Radius = _reader.ReadSingle();
            light.Red = _reader.ReadSingle();
            light.Green = _reader.ReadSingle();
            light.Blue = _reader.ReadSingle();
            light.ColorScale = _reader.ReadSingle();
            light.AnimationTypeEnum = _reader.ReadByte();
            light.AnimationSpeedScale1 = _reader.ReadSingle();
            light.AnimationSpeedScale2 = _reader.ReadSingle();
            light.ColorMin = _reader.ReadSingle();
            light.RandomOffset = _reader.ReadSingle();
            light.FalloffType = ReadString();
            light.LFRelative = _reader.ReadByte();
            if (light.PointLightInfoVersion > 1)
                light.HeightMode = ReadString();
            if (light.PointLightInfoVersion > 2)
            {
                light.LightProbeOnly = _reader.ReadByte() != 0;
                
                if (light.PointLightInfoVersion > 5)
                    light.PdlcMask = _reader.ReadUInt64();
                else
                    light.PdlcMask = _reader.ReadUInt32();
            }
            if (light.PointLightInfoVersion > 6)
            {
                light.MoreData2 = _reader.ReadBytes(10);
            }
            return light;
        }

        private BuildingProjectileEmitter ReadBuildingProjectileEmitter()
        {
            var emitter = new BuildingProjectileEmitter();
            
            emitter.BuildingProjectileEmitterVersion = _reader.ReadUInt16();
            
            emitter.Location = ReadRmvVector3();
            emitter.Rotation[0] = _reader.ReadSingle();
            emitter.Rotation[1] = _reader.ReadSingle();
            emitter.Rotation[2] = _reader.ReadSingle();
            
            emitter.BuildingIndex = _reader.ReadUInt32();
            emitter.HeightMode = ReadString();
            
            if (emitter.BuildingProjectileEmitterVersion > 2)
                emitter.SpecializedBuildingProjectileEmitterKey = ReadString();
            
            return emitter;
        }

        private PlayableArea ReadPlayableArea()
        {
            var area = new PlayableArea();
            area.PlayableAreaVersion = _reader.ReadUInt16();
                        
            area.BoundingBox = new float[4];
            for (int i = 0; i < 4; i++)
                area.BoundingBox[i] = _reader.ReadSingle();

            area.HasBeenSet = _reader.ReadByte() != 0;
            
            if (area.PlayableAreaVersion > 1)
            {
                if (area.PlayableAreaVersion > 2)
                    area.FlagVersion = _reader.ReadUInt16();
                area.Flag1 = _reader.ReadByte() != 0;
                area.Flag2 = _reader.ReadByte() != 0;
                area.Flag3 = _reader.ReadByte() != 0;
                area.Flag4 = _reader.ReadByte() != 0;
            }
            
            return area;
        }

        private PolyMeshInfo ReadPolyMeshInfo()
        {
            var mesh = new PolyMeshInfo();
            mesh.PolyMeshVersion = _reader.ReadUInt16();
            
            var vertexCount = _reader.ReadUInt32();
            mesh.VertexList = new RmvVector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                mesh.VertexList[i] = ReadRmvVector3();
            }
            
            var triangleCount = _reader.ReadUInt32();
            mesh.TriangleList = new ushort[triangleCount];
            for (int i = 0; i < triangleCount; i++)
            {
                mesh.TriangleList[i] = _reader.ReadUInt16();
            }
            
            mesh.MaterialString = ReadString();
            mesh.HeightMode = ReadString();

            if (mesh.PolyMeshVersion > 2)
            {
                mesh.MoreData = _reader.ReadBytes(8);
                mesh.VisibleInTactical = _reader.ReadByte() != 0;
                mesh.OnlyVisibleInTactical = _reader.ReadByte() != 0;
            }
            if (mesh.PolyMeshVersion > 3)
            {
                // Read transform matrix (3x4 matrix stored in row-major order)
                mesh.Transform = ReadRowMajorMatrix(false);
                mesh.Booleans = _reader.ReadBytes(4);
                mesh.VisibleInShroud = _reader.ReadByte() != 0;
                mesh.MoreBooleans = _reader.ReadBytes(1);
            }
            
            return mesh;
        }

        private TerrainStencilBlendTriangle ReadTerrainStencilBlendTriangle()
        {
            throw new NotImplementedException("TerrainStencilBlendTriangle parsing not implemented yet");
        }

        private SpotLightInfo ReadSpotLightInfo()
        {
            var light = new SpotLightInfo();
            light.Version = _reader.ReadUInt16();
            light.Position = ReadRmvVector3();
            light.QuartX = _reader.ReadSingle();
            light.QuartY = _reader.ReadSingle();
            light.QuartZ = _reader.ReadSingle();
            light.QuartW = _reader.ReadSingle();
            light.Length = _reader.ReadSingle();
            light.InnerAngleRadians = _reader.ReadSingle();
            light.OuterAngleRadians = _reader.ReadSingle();
            light.IntensityRed = _reader.ReadSingle();
            light.IntensityGreen = _reader.ReadSingle();
            light.IntensityBlue = _reader.ReadSingle();
            light.Falloff = _reader.ReadSingle();
            light.Gobo = ReadString();
            light.Volumetric = _reader.ReadByte() != 0;
            light.HeightMode = ReadString();
            light.MoreData = _reader.ReadBytes(18);
            return light;
        }

        private TerrainHoleTriangleInfo ReadTerrainHoleInfo()
        {
            var hole = new TerrainHoleTriangleInfo();
            hole.TerrainHoleVersion = _reader.ReadUInt16();
            hole.FirstVert = ReadRmvVector3();
            hole.SecondVert = ReadRmvVector3();
            hole.ThirdVert = ReadRmvVector3();
            hole.HeightMode = ReadString();
            
            if (hole.TerrainHoleVersion > 2)
            {
                hole.Booleans = _reader.ReadBytes(10);
            }
            
            return hole;
        }

        private SoundInfo ReadSoundInfo()
        {
            var sound = new SoundInfo();
            sound.Version = _reader.ReadUInt16();
            sound.SoundString = ReadString();
            sound.TypeString = ReadString();
            
            var coordCount = _reader.ReadUInt32();
            sound.CoordList = new RmvVector3[coordCount];
            for (int i = 0; i < coordCount; i++)
                sound.CoordList[i] = ReadRmvVector3();
            
            sound.InnerRadius = _reader.ReadSingle();
            sound.OuterRadius = _reader.ReadSingle();
            
            sound.InnerCubeBoundingBox = (ReadRmvVector3(), ReadRmvVector3());
            sound.OuterCubeBoundingBox = (ReadRmvVector3(), ReadRmvVector3());
            
            sound.RiverNodesLength = _reader.ReadUInt32();
            // River nodes are always empty according to comments, so we skip reading them
            
            sound.ClampToSurface = _reader.ReadByte();
            sound.HeightMode = ReadString();
            
            // Campaign type mask - conditional based on version
            if (sound.Version > 9)
                sound.CampaignTypeMask = _reader.ReadUInt64();
            else
                sound.CampaignTypeMask = _reader.ReadUInt32();
            
            sound.CultureMask = ReadCultureMask();
            sound.DirectionVector = ReadRmvVector3();
            sound.UpVector = ReadRmvVector3();
            if (sound.Version > 8)
                sound.Scope = ReadString();
            
            return sound;
        }

        private CscInfo ReadCscInfo()
        {
            var csc = new CscInfo();
            csc.Version = _reader.ReadUInt16();
            
            // Read transform matrix (3x4 matrix stored in row-major order)
            csc.Transform = ReadRowMajorMatrix(false);
            csc.SceneFile = ReadString();
            csc.HeightMode = ReadString();
            csc.PdlcMask = _reader.ReadUInt64();
            csc.Autoplay = _reader.ReadByte() != 0;
            csc.VisibleInShroud = _reader.ReadByte() != 0;
            csc.NoCulling = _reader.ReadByte() != 0;
            if (csc.Version > 7)
            {
                csc.ScriptId = ReadString();
                csc.ParentScriptId = ReadString();
            }
            if (csc.Version > 9)
                csc.VisibleWithoutShroud = _reader.ReadByte() != 0;
            if (csc.Version > 10)
            {
                csc.VisibleInTacticalView = _reader.ReadByte() != 0;
                csc.VisibleInTacticalViewOnly = _reader.ReadByte() != 0;
            }
            if (csc.Version > 11)
            {
                csc.HoldFirst = _reader.ReadByte() != 0;
                csc.HoldLast = _reader.ReadByte() != 0;
            }
            
            return csc;
        }

        private Deployment ReadDeployment()
        {
            var deployment = new Deployment();
            deployment.Version = _reader.ReadUInt16();
            
            // Read category
            deployment.Category = ReadString();
            
            // Read DeploymentZone list
            var deploymentZoneListLength = _reader.ReadUInt32();
            for (int i = 0; i < deploymentZoneListLength; i++)
            {
                deployment.DeploymentZones.Add(ReadDeploymentZone());
            }
            
            return deployment;
        }

        private DeploymentZone ReadDeploymentZone()
        {
            var deploymentZone = new DeploymentZone();
            deploymentZone.Version = _reader.ReadUInt16();
            
            // Read DeploymentZoneRegion list
            var deploymentZoneRegionListLength = _reader.ReadUInt32();
            for (int i = 0; i < deploymentZoneRegionListLength; i++)
            {
                deploymentZone.DeploymentZoneRegions.Add(ReadDeploymentZoneRegion());
            }
            
            return deploymentZone;
        }

        private DeploymentZoneRegion ReadDeploymentZoneRegion()
        {
            var region = new DeploymentZoneRegion();
            region.Version = _reader.ReadUInt16();
            
            // Read Boundary list
            var boundaryListLength = _reader.ReadUInt32();
            for (int i = 0; i < boundaryListLength; i++)
            {
                region.Boundaries.Add(ReadBoundary());
            }
            
            region.Orientation = _reader.ReadSingle();
            region.SnapFacing = _reader.ReadByte();
            region.Id = _reader.ReadUInt32();
            
            return region;
        }

        private Boundary ReadBoundary()
        {
            var boundary = new Boundary();
            boundary.Version = _reader.ReadUInt16();
            
            // Read boundary type
            boundary.BoundaryType = ReadString();
            
            // Read point list
            var pointListLength = _reader.ReadUInt32();
            for (int i = 0; i < pointListLength; i++)
            {
                var coord = new RmvVector2
                {
                    X = _reader.ReadSingle(),
                    Y = _reader.ReadSingle()
                };
                boundary.PointList.Add(coord);
            }
            
            return boundary;
        }

        private BmdCachedArea ReadBmdCachedArea()
        {
            throw new NotImplementedException("BmdCachedArea parsing not implemented yet");
        }

        private ToggleableBuildingSlot ReadToggleableBuildingSlot()
        {
            throw new NotImplementedException("ToggleableBuildingSlot parsing not implemented yet");
        }

        private TerraindDecal ReadTerraindDecal()
        {
            throw new NotImplementedException("TerraindDecal parsing not implemented yet");
        }

        private TreeListReference ReadTreeListReference()
        {
            throw new NotImplementedException("TreeListReference parsing not implemented yet");
        }

        private GrassListReference ReadGrassListReference()
        {
            throw new NotImplementedException("GrassListReference parsing not implemented yet");
        }

        private WaterOutline ReadWaterOutline()
        {
            throw new NotImplementedException("WaterOutline parsing not implemented yet");
        }



        //Helper Functions
        private BmdComponentFlags ReadBmdComponentFlags()
        {
            var flags = new BmdComponentFlags();
            flags.FlagVersion = _reader.ReadUInt16();
            flags.AllowInOutfield = _reader.ReadByte() != 0;
            if (flags.FlagVersion == 2)
                flags.ClampToSurface = _reader.ReadByte() != 0;
            flags.ClampToWaterSurface = _reader.ReadByte() != 0;
            flags.SeasonSpring = _reader.ReadByte() != 0;
            flags.SeasonSummer = _reader.ReadByte() != 0;
            flags.SeasonAutumn = _reader.ReadByte() != 0;
            flags.SeasonWinter = _reader.ReadByte() != 0;
            if (flags.FlagVersion > 3)
            {
                flags.VisibleInTactical = _reader.ReadByte() != 0;
                flags.OnlyVisibleInTactical = _reader.ReadByte() != 0;
            }
            return flags;
        }

        private string ReadString()
        {
            var length = _reader.ReadUInt16();
            if (length == 0) return string.Empty;
            return Encoding.UTF8.GetString(_reader.ReadBytes(length));
        }

        private CultureMask ReadCultureMask()
        {
            var mask = new CultureMask();
            var bytes = _reader.ReadBytes(8);
            
            _logger.Here().Information($"BMD Parser - ReadCultureMask: read {bytes.Length} bytes, expected 8");
            
            if (bytes.Length < 8)
            {
                _logger.Here().Error($"BMD Parser - ReadCultureMask: Not enough bytes. Got {bytes.Length}, expected 8. Stream position: {_stream.Position}, Length: {_stream.Length}");
                throw new EndOfStreamException($"Not enough bytes to read CultureMask. Got {bytes.Length}, expected 8");
            }
            
            // First byte
            mask.CultMaskBase = (bytes[0] & 0x01) != 0;
            mask.CultMaskBst = (bytes[0] & 0x02) != 0;
            mask.CultMaskBrt = (bytes[0] & 0x80) != 0;

            // Second byte
            mask.CultMaskChs = (bytes[1] & 0x01) != 0;
            mask.CultMaskDwf = (bytes[1] & 0x02) != 0;
            mask.CultMaskEmp = (bytes[1] & 0x04) != 0;
            mask.CultMaskGrn = (bytes[1] & 0x08) != 0;
            mask.CultMaskVmp = (bytes[1] & 0x10) != 0;
            mask.CultMaskWef = (bytes[1] & 0x20) != 0;

            // Third byte
            mask.CultMaskDef = (bytes[2] & 0x02) != 0;
            mask.CultMaskHef = (bytes[2] & 0x04) != 0;
            mask.CultMaskLzd = (bytes[2] & 0x08) != 0;
            mask.CultMaskSkv = (bytes[2] & 0x10) != 0;
            mask.CultMaskTmb = (bytes[2] & 0x20) != 0;
            mask.CultMaskRogue = (bytes[2] & 0x40) != 0;
            mask.CultMaskKsl = (bytes[2] & 0x80) != 0;

            // Fourth byte
            mask.CultMaskOgr = (bytes[3] & 0x01) != 0;
            mask.CultMaskCst = (bytes[3] & 0x02) != 0;
            mask.CultMaskKho = (bytes[3] & 0x08) != 0;
            mask.CultMaskTze = (bytes[3] & 0x10) != 0;
            mask.CultMaskNur = (bytes[3] & 0x20) != 0;
            mask.CultMaskSla = (bytes[3] & 0x40) != 0;
            mask.CultMaskDae = (bytes[3] & 0x80) != 0;

            // Fifth byte
            mask.CultMaskCth = (bytes[4] & 0x01) != 0;
            mask.CultMaskNor = (bytes[4] & 0x02) != 0;
            mask.CultMaskChd = (bytes[4] & 0x04) != 0;

            return mask;
        }

        private RmvVector3 ReadRmvVector3()
        {
            return new RmvVector3
            {
                X = _reader.ReadSingle(),
                Y = _reader.ReadSingle(),
                Z = _reader.ReadSingle()
            };
        }

        private Matrix ReadRowMajorMatrix(bool is4x4 = false)
        {
            var matrix = new Matrix();
            
            // Row 1
            matrix.M11 = _reader.ReadSingle();  // Row 1, Column 1
            matrix.M12 = _reader.ReadSingle();  // Row 1, Column 2
            matrix.M13 = _reader.ReadSingle();  // Row 1, Column 3
            if (is4x4)
                matrix.M14 = _reader.ReadSingle();  // Row 1, Column 4
            else
                matrix.M14 = 0f;  // Row 1, Column 4 (default for 3x4)
            
            // Row 2
            matrix.M21 = _reader.ReadSingle();  // Row 2, Column 1
            matrix.M22 = _reader.ReadSingle();  // Row 2, Column 2
            matrix.M23 = _reader.ReadSingle();  // Row 2, Column 3
            if (is4x4)
                matrix.M24 = _reader.ReadSingle();  // Row 2, Column 4
            else
                matrix.M24 = 0f;  // Row 2, Column 4 (default for 3x4)
            
            // Row 3
            matrix.M31 = _reader.ReadSingle();  // Row 3, Column 1
            matrix.M32 = _reader.ReadSingle();  // Row 3, Column 2
            matrix.M33 = _reader.ReadSingle();  // Row 3, Column 3
            if (is4x4)
                matrix.M34 = _reader.ReadSingle();  // Row 3, Column 4
            else
                matrix.M34 = 0f;  // Row 3, Column 4 (default for 3x4)
            
            // Row 4
            matrix.M41 = _reader.ReadSingle();  // Row 4, Column 1 (position X)
            matrix.M42 = _reader.ReadSingle();  // Row 4, Column 2 (position Y)
            matrix.M43 = _reader.ReadSingle();  // Row 4, Column 3 (position Z)
            if (is4x4)
                matrix.M44 = _reader.ReadSingle();  // Row 4, Column 4
            else
                matrix.M44 = 1f;  // Row 4, Column 4 (default for 3x4)
            
            return matrix;
        }
    }
}
