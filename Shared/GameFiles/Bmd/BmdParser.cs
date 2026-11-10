using System.Text;
using Microsoft.Xna.Framework;
using Shared.GameFormats.RigidModel.Transforms;

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
                if (_fastBinVersion < 11)
                {
                    //In a super early version of FastBin, there was stuff up here
                    //(and it was eventually moved down?)

                    //Props (the modern version of props is way down the page)
                    //no version
                    var early_props_length = _reader.ReadUInt32();
                    _logger.Here().Information($"BMD Parser - Early props length: {early_props_length}");
                    
                    // Read early props entries
                    for (var i = 0; i < early_props_length; i++)
                    {
                        
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

                    //VFX (the modern version of VFX is way down the page)
                    //no version
                    var early_vfxs_length = _reader.ReadUInt32();
                    _logger.Here().Information($"BMD Parser - Early vfxs length: {early_vfxs_length}");

                    // Read early vfx entries
                    for (var i = 0; i < early_vfxs_length; i++)
                    {

                        var earlyVfx = new VfxInfo
                        {
                            VfxString = ReadString(),
                            VfxInfoVersion = _reader.ReadUInt16(), //seems to be the version
                            Transform = ReadRowMajorMatrix(true),

                            //12 bytes of bytes I can't make heads or tails of, especially last 2
                            EarlyVersionUnknownBytes = _reader.ReadBytes(12)
                        };

                        bmdFile.VfxInfos.Add(earlyVfx);
                    }

                    //Probably more early version of stuff
                    var otherthing2_length = _reader.ReadUInt32();
                    var otherthing3_length = _reader.ReadUInt32();
                    var otherthing4_length = _reader.ReadUInt32();
                }
                
                if (_fastBinVersion < 22)
                {
                    // Some unknown thing that was removed for some ungodly reason
                    var some_version = _reader.ReadUInt16();
                    bmdFile.SectionVersions["LegacyPreV22"] = some_version;
                    var some_length = _reader.ReadUInt32();
                    _logger.Here().Information($"BMD Parser - Some version: {some_version}, Some length: {some_length}");
                }

                // BattlefieldBuilding
                var battlefieldBuildingVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "BattlefieldBuilding", bmdFile.BattlefieldBuildings, ReadBattlefieldBuilding, battlefieldBuildingVersion);

                // BattlefieldBuildingFar
                var battlefieldBuildingFarVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "BattlefieldBuildingFar", bmdFile.BattlefieldBuildingFars, ReadBattlefieldBuildingFar, battlefieldBuildingFarVersion);

                // CaptureLocation
                var captureLocationVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "CaptureLocation", bmdFile.CaptureLocations, ReadCaptureLocation, captureLocationVersion);

                // EFLine
                // no version
                ReadCollection(bmdFile, "EFLine", bmdFile.EFLines, ReadEFLine);

                // GoOutline
                // no version
                ReadCollection(bmdFile, "GoOutline", bmdFile.GoOutlines, ReadGoOutline);

                // NonTerrainOutline
                // no version
                ReadCollection(bmdFile, "NonTerrainOutline", bmdFile.NonTerrainOutlines, ReadNonTerrainOutline);

                // ZonesTemplate
                var zonesTemplateVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "ZonesTemplate", bmdFile.ZonesTemplates, ReadZonesTemplate, zonesTemplateVersion);

                // Bmd
                var bmdVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "BmdInfo", bmdFile.BmdInfos, ReadBmdInfo, bmdVersion);

                // BmdOutlines
                var bmdOutlineVersion = _reader.ReadUInt16();
                ReadCollection(bmdFile, "BmdOutline", bmdFile.BmdOutlines, ReadBmdOutline, bmdOutlineVersion);

                // TerrainOutlines
                // no version
                ReadCollection(bmdFile, "TerrainOutline", bmdFile.TerrainOutlines, ReadTerrainOutline);

                // LiteBuildingOutlines
                // no version
                ReadCollection(bmdFile, "LiteBuildingOutline", bmdFile.LiteBuildingOutlines, ReadLiteBuildingOutline);

                if (_fastBinVersion > 4)
                {
                    // CameraZones
                    var cameraZoneVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "CameraZone", bmdFile.CameraZones, ReadCameraZone, cameraZoneVersion);
                }
                if (_fastBinVersion > 7)
                {
                    // CivilianDeployments
                    // no version
                    ReadCollection(bmdFile, "CivilianDeployment", bmdFile.CivilianDeployments, ReadCivilianDeployment);

                    // CivilianShelters
                    // no version
                    ReadCollection(bmdFile, "CivilianShelter", bmdFile.CivilianShelters, ReadCivilianShelter);

                    // Prop
                    var propVersion = _reader.ReadUInt16();
                    bmdFile.SectionVersions["Prop"] = propVersion;
                    ReadPropInfos(propVersion, bmdFile);
                }
                if (_fastBinVersion > 8)
                {
                    // Vfx
                    var vfxVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "VfxInfo", bmdFile.VfxInfos, ReadVfxInfo, vfxVersion);

                    // AiHints
                    var aiHintsVersion = _reader.ReadUInt16();
                    bmdFile.SectionVersions["AiHints"] = aiHintsVersion;
                    _logger.Here().Information($"BMD Parser - AiHints version: {aiHintsVersion}");
                    bmdFile.AiHints = ReadAiHints(bmdFile);
                }
                if (_fastBinVersion > 10)
                {
                    // LightProbe
                    var lightProbeVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "LightProbe", bmdFile.LightProbes, ReadLightProbeInfo, lightProbeVersion);

                    // TerrainHole
                    var terrainHoleVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "TerrainHole", bmdFile.TerrainHoles, ReadTerrainHoleInfo, terrainHoleVersion);

                    // PointLight
                    var pointLightVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "PointLight", bmdFile.PointLights, ReadPointLightInfo, pointLightVersion);
                }
                // From here on the list gates are the kit's own (BATTLE_MAP_DEFINITION's serialiser in
                // empireutility.modder.x64.dll).
                if (_fastBinVersion > 14)
                {
                    // BuildingProjectileEmitters
                    var buildingProjectileEmitterVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "BuildingProjectileEmitter", bmdFile.BuildingProjectileEmitters, ReadBuildingProjectileEmitter, buildingProjectileEmitterVersion);
                }
                if (_fastBinVersion > 15)
                {
                    // PlayableArea
                    _logger.Here().Information($"BMD Parser - PlayableArea");
                    bmdFile.PlayableArea = ReadPlayableArea();
                }
                if (_fastBinVersion > 16)
                {
                    // PolyMesh
                    var polyMeshVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "PolyMesh", bmdFile.PolyMeshes, ReadPolyMeshInfo, polyMeshVersion);
                }
                if (_fastBinVersion > 17)
                {
                    // TerrainStencilBlendTriangles
                    var terrainStencilBlendTriangleVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "TerrainStencilBlendTriangle", bmdFile.TerrainStencilBlendTriangles, ReadTerrainStencilBlendTriangle, terrainStencilBlendTriangleVersion);
                }
                if (_fastBinVersion > 18)
                {
                    // SpotLight
                    var spotLightVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "SpotLight", bmdFile.SpotLights, ReadSpotLightInfo, spotLightVersion);
                }
                if (_fastBinVersion > 19)
                {
                    // Sound
                    var soundVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "Sound", bmdFile.Sounds, ReadSoundInfo, soundVersion);
                }
                if (_fastBinVersion > 20)
                {
                    // CSC (Composite Scene Container)
                    var cscVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "CSC", bmdFile.CscInfos, ReadCscInfo, cscVersion);
                }
                if (_fastBinVersion > 21)
                {
                    // Deployment
                    var deploymentVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "Deployment", bmdFile.Deployments, ReadDeployment, deploymentVersion);
                }
                if (_fastBinVersion > 22)
                {
                    // BmdCachedAreas (the kit's BMD_CATCHMENT_AREA_LIST)
                    var bmdCachedAreaVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "BmdCachedArea", bmdFile.BmdCachedAreas, ReadBmdCachedArea, bmdCachedAreaVersion);
                }
                if (_fastBinVersion > 23)
                {
                    // ToggleableBuildingSlots
                    var toggleableBuildingSlotVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "ToggleableBuildingSlot", bmdFile.ToggleableBuildingSlots, ReadToggleableBuildingSlot, toggleableBuildingSlotVersion);
                }
                if (_fastBinVersion > 24)
                {
                    // TerraindDecals
                    var terraindDecalVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "TerraindDecal", bmdFile.TerraindDecals, ReadTerraindDecal, terraindDecalVersion);
                }
                if (_fastBinVersion > 25)
                {
                    // TreeListReferences
                    var treeListReferenceVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "TreeListReference", bmdFile.TreeListReferences, ReadTreeListReference, treeListReferenceVersion);

                    // GrassListReferences
                    var grassListReferenceVersion = _reader.ReadUInt16();
                    ReadCollection(bmdFile, "GrassListReference", bmdFile.GrassListReferences, ReadGrassListReference, grassListReferenceVersion);
                }
                if (_fastBinVersion > 26)
                {
                    // WaterOutlines
                    // no version
                    ReadCollection(bmdFile, "WaterOutline", bmdFile.WaterOutlines, ReadWaterOutline);
                }

                // A record read a few bytes short shifts everything after it without necessarily running off
                // the end, so a parse that doesn't land exactly on the end of the file has misread something.
                if (_stream.Position != _stream.Length)
                    throw new InvalidDataException(
                        $"BMD parse ended at byte {_stream.Position} of {_stream.Length}: {_stream.Length - _stream.Position} bytes were not read, " +
                        "so a record version this parser doesn't know has probably changed size.");

                return bmdFile;
            }
            catch (Exception ex)
            {
                _logger.Here().Error($"BMD Parser - Exception caught: {ex.Message}");
                _logger.Here().Error($"BMD Parser - Stream position: {_stream.Position}, Length: {_stream.Length}");
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

        private void ReadCollection<T>(BmdFile bmdFile, string collectionName, List<T> collection, Func<T> readFunc, ushort? version = null)
        {
            if (version.HasValue)
                bmdFile.SectionVersions[collectionName] = version.Value;

            var count = _reader.ReadUInt32();
            var versionText = version.HasValue ? $" version: {version.Value}," : "";
            _logger.Here().Information($"BMD Parser - {collectionName}{versionText} count: {count}");

            for (var i = 0; i < count; i++)
            {
                collection.Add(readFunc());
            }
        }

        private BattlefieldBuilding ReadBattlefieldBuilding()
        {
            var building = new BattlefieldBuilding();

            building.Version = _reader.ReadUInt16();

            building.BuildingId = ReadString();

            if (building.Version > 10)
                building.ParentId = _reader.ReadInt32();
            else if (building.Version > 6)
                building.ParentId = _reader.ReadInt16();

            if (building.Version > 4)
                building.BuildingKey = ReadString();

            building.PositionType = ReadString();

            if (building.Version < 6)
            {
                // Raw position / rotation / scale
                var position = ReadRmvVector3();
                var translationMatrix = Matrix.CreateTranslation(position.ToVector3());

                var rotation = new Quaternion(
                    _reader.ReadSingle(),
                    _reader.ReadSingle(),
                    _reader.ReadSingle(),
                    _reader.ReadSingle());
                var rotationMatrix = Matrix.CreateFromQuaternion(rotation);

                var scale = ReadRmvVector3();
                var scaleMatrix = Matrix.CreateScale(scale.ToVector3());

                building.Transform = scaleMatrix * rotationMatrix * translationMatrix;
            }
            else
            {
                //Transformation matrix
                building.Transform = ReadRowMajorMatrix(false);
            }
            
            building.Properties = building.Version > 3 ? ReadBuildingProperties() : ReadLegacyBuildingProperties(building.Version);

            // Back to normal stuff
            if (building.Version > 7)
                building.HeightMode = ReadString();
            if (building.Version > 9)
                building.Uid = _reader.ReadInt64();
            
            return building;
        }

        private BuildingProperties ReadBuildingProperties()
        {
            var properties = new BuildingProperties();
            properties.Version = _reader.ReadUInt16();
            if (properties.Version > 1)
                properties.BuildingId = ReadString();
            properties.StartingDamageUnary = _reader.ReadSingle();
            properties.OnFire = _reader.ReadByte() != 0;
            properties.StartDisabled = _reader.ReadByte() != 0;
            properties.WeakPoint = _reader.ReadByte() != 0;
            properties.AiBreachable = _reader.ReadByte() != 0;
            properties.Indestructible = _reader.ReadByte() != 0;
            properties.Dockable = _reader.ReadByte() != 0;
            properties.Toggleable = _reader.ReadByte() != 0;
            properties.Lite = _reader.ReadByte() != 0;
            if (properties.Version is > 2 and < 8)
                properties.ClampToSurface = _reader.ReadByte() != 0;
            if (properties.Version > 3)
                properties.CastShadows = _reader.ReadByte() != 0;
            if (properties.Version > 4)
                properties.KeyBuilding = _reader.ReadByte() != 0;
            if (properties.Version > 5)
                properties.KeyBuildingUseFort = _reader.ReadByte() != 0;
            if (properties.Version > 6)
                properties.IsPropInOutfield = _reader.ReadByte() != 0;
            if (properties.Version > 8)
                properties.SettlementLevelConfigurable = _reader.ReadByte() != 0;
            if (properties.Version > 9)
                properties.HideTooltip = _reader.ReadByte() != 0;
            if (properties.Version > 10)
                properties.IncludeInFog = _reader.ReadByte() != 0;
            if (properties.Version > 11)
                properties.BlendEdges = _reader.ReadSingle();
            if (properties.Version > 12)
                properties.ReceiveDecals = _reader.ReadByte() != 0;
            return properties;
        }

        /// <summary>Battlefield buildings before version 4 carry their properties inline, with no version or id.</summary>
        private BuildingProperties ReadLegacyBuildingProperties(ushort buildingVersion)
        {
            var properties = new BuildingProperties();
            properties.StartingDamageUnary = _reader.ReadSingle();
            properties.OnFire = _reader.ReadByte() != 0;
            properties.StartDisabled = _reader.ReadByte() != 0;
            properties.WeakPoint = _reader.ReadByte() != 0;
            properties.AiBreachable = _reader.ReadByte() != 0;
            properties.Indestructible = _reader.ReadByte() != 0;
            properties.Dockable = _reader.ReadByte() != 0;
            if (buildingVersion > 1)
                properties.Toggleable = _reader.ReadByte() != 0;
            if (buildingVersion > 2)
                properties.Lite = _reader.ReadByte() != 0;
            return properties;
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

            if (location.Version > 2)
                location.Str2 = ReadString();
            
            var coordCount = _reader.ReadUInt32();
            location.Coords = new float[coordCount * 2];
            for (var i = 0; i < location.Coords.Length; i++)
            {
                location.Coords[i] = _reader.ReadSingle();
            }
            
            location.Str3 = ReadString();
            location.Something6 = _reader.ReadSingle();
            location.Something7 = _reader.ReadSingle();
            location.Bools = _reader.ReadBytes(4); //redo this 
            if (location.Version > 2)
            {
                location.Bools = _reader.ReadBytes(3); //redo this 
                location.Something8 = _reader.ReadUInt16();
                location.Something9 = _reader.ReadSingle();
                location.Something10 = _reader.ReadSingle();
            }
            return location;
        }

        private EFLine ReadEFLine()
        {
            throw new NotImplementedException("BmdOutline parsing not implemented yet");
        }

        private GoOutline ReadGoOutline()
        {
            var goOutline = new GoOutline();

            //no version
            
            var vertexListLength = _reader.ReadUInt32();
            goOutline.VertexList = [];
            for (var i = 0; i < vertexListLength; i++)
            {
                var x = _reader.ReadSingle();
                var y = _reader.ReadSingle();
                goOutline.VertexList.Add(new RmvVector2(x, y));
            }
            
            return goOutline;
        }

        private NonTerrainOutline ReadNonTerrainOutline()
        {
            var ntOutline = new NonTerrainOutline();

            //no version
            
            var vertexListLength = _reader.ReadUInt32();
            ntOutline.VertexList = [];
            for (var i = 0; i < vertexListLength; i++)
            {
                var x = _reader.ReadSingle();
                var y = _reader.ReadSingle();
                ntOutline.VertexList.Add(new RmvVector2(x, y));
            }
            
            return ntOutline;
        }

        private ZonesTemplate ReadZonesTemplate()
        {
            var template = new ZonesTemplate();

            //no version
            
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
            
            template.Transform = ReadRowMajorMatrix(true);
            
            return template;
        }

        private BmdInfo ReadBmdInfo()
        {
            var bmd = new BmdInfo();

            bmd.Version = _reader.ReadUInt16();
            
            bmd.BmdString = ReadString();
            bmd.Transform = ReadRowMajorMatrix(true);

            // One building-properties block per overridden building of the child prefab (battle prefabs only)
            if (bmd.Version > 1)
            {
                var overrideCount = _reader.ReadUInt32();
                for (var i = 0; i < overrideCount; i++)
                    bmd.PropertyOverrides.Add(ReadBuildingProperties());
            }

            if (bmd.Version > 6)
                bmd.CampaignTypeMask = ReadCultureMask();
            else if (bmd.Version > 2)
                bmd.CampaignTypeMask = ReadCultureMask(4);
            if (bmd.Version > 3)
                bmd.RegionString = ReadString();
            if (bmd.Version is > 4 and < 8)
                bmd.ClampToSurface = _reader.ReadByte() != 0;

            if (bmd.Version > 5)
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
                
                for (var i = 0; i < propCount; i++)
                {
                    propsList.Add(ReadString());
                }
            }

            bmdFile.Props = propsList;

            // PropInfo
            var propInfoCount = _reader.ReadUInt32();
            _logger.Here().Information($"BMD Parser - PropInfo count: {propInfoCount}");
            
            for (var i = 0; i < propInfoCount; i++)
            {
                bmdFile.PropInfos.Add(ReadPropInfo(propsList));
            }
        }

        private PropInfo ReadPropInfo(List<string> propsList)
        {
            var prop = new PropInfo();
            prop.PropInfoVersion = _reader.ReadUInt16();
            
            if (prop.PropInfoVersion <= 12)
            {
                //Read string directly for old versions
                prop.Rmv2Path = ReadString();
            }
            else
            {
                // Map the PropIndex to the actual RMV2 path from the props list
                var propIndex = _reader.ReadUInt32();
                prop.Rmv2Path = propsList[(int)propIndex];
                prop.PropIndex = (int)propIndex;
            }
            
            prop.Transform = ReadRowMajorMatrix(false);

            if (prop.PropInfoVersion < 4)
            {
                //There's less mystery bytes for the later version (version 3 seen)
                if (prop.PropInfoVersion == 1)
                    _reader.ReadBytes(30); //proto-props version 3 (last seen) only had 28 mystery bytes
                else if (prop.PropInfoVersion == 2)
                {
                    _reader.ReadBytes(7);

                    //3 floats, some position thing
                    _reader.ReadSingle();
                    _reader.ReadSingle();
                    _reader.ReadSingle();

                    _reader.ReadBytes(11);
                    _reader.ReadByte(); //one extra compared to version 1
                }
                else if (prop.PropInfoVersion == 3)
                    _reader.ReadBytes(23); //proto-props version 1 had 23 mystery bytes, clue?
                
                return prop;
            }
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
            
            if (prop.PropInfoVersion > 4)
                prop.VisibleInShroud = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 5)
            {
                prop.ApplyToTerrain = _reader.ReadByte() != 0;
                prop.ApplyToPropsOrReceiveDecal = _reader.ReadByte() != 0;
                prop.RenderAboveSnow = _reader.ReadByte() != 0;
            }
            
            if (prop.PropInfoVersion > 7)
                prop.HeightMode = ReadString();
            
            if (prop.PropInfoVersion > 15)
                prop.CultureMask = _reader.ReadBytes(8);
            else if (prop.PropInfoVersion > 10)
            {
                prop.CultureMask = new byte[8];
                var oldMask = _reader.ReadBytes(4);
                Array.Copy(oldMask, prop.CultureMask, 4);
            }
            else
                prop.CultureMask = new byte[8]; //no culture mask in the binary

            if (prop.PropInfoVersion == 9)
                prop.DecalApplyToRigids = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 11)
                prop.CastsShadow = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 13)
                prop.NoCulling = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 14)
                prop.HasHeightPatch = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 16)
                prop.ApplyHeightPatch = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 18)
                prop.IncludeInFog = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 19)
                prop.VisibleWithoutShroud = _reader.ReadByte() != 0;

            if (prop.PropInfoVersion is 21 or 22)
                prop.VisibleInTacticalView = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion == 22)
                prop.VisibleInTacticalViewOnly = _reader.ReadByte() != 0;

            if (prop.PropInfoVersion > 23)
                prop.UseDynamicShadows = _reader.ReadByte() != 0;
            if (prop.PropInfoVersion > 24)
                prop.UsesTerrainVertexOffset = _reader.ReadByte() != 0;

            if (prop.PropInfoVersion > 25)
                prop.BlendEdges = _reader.ReadSingle();
            if (prop.PropInfoVersion > 26)
                prop.DecalFadeStart = _reader.ReadSingle();
            if (prop.PropInfoVersion > 27)
            {
                prop.DecalAngleThreshold = _reader.ReadSingle();
                prop.DecalMirrorX = _reader.ReadByte() != 0;
                prop.DecalMirrorZ = _reader.ReadByte() != 0;
            }
            if (prop.PropInfoVersion > 28)
                prop.DecalLayer = _reader.ReadInt32();
            if (prop.PropInfoVersion > 29)
                prop.DecalTilingAlpha = _reader.ReadByte() != 0;

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

            if (vfx.VfxInfoVersion == 1)
            {
                _reader.ReadUInt16(); //one
                _reader.ReadUInt32(); //zero
            }
            else
                vfx.Flags = ReadBmdComponentFlags();
            
            if (vfx.VfxInfoVersion > 2)
                vfx.HeightMode = ReadString();
            if (vfx.VfxInfoVersion > 3)
            {
                if (vfx.VfxInfoVersion > 6)
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

            if (vfx.VfxInfoVersion == 8)
                vfx.ParentId = _reader.ReadInt16();
            else if (vfx.VfxInfoVersion > 8)
                vfx.ParentId = _reader.ReadInt32();

            if (vfx.VfxInfoVersion > 9)
                vfx.VisibleWithoutShroud = _reader.ReadByte() != 0;
            if (vfx.VfxInfoVersion > 10)
                vfx.NoCulling = _reader.ReadByte() != 0;

            return vfx;
        }

        private AiHints ReadAiHints(BmdFile bmdFile)
        {
            var aiHints = new AiHints();

            // Read Separators
            bmdFile.SectionVersions["AiHints.Separators"] = _reader.ReadUInt16();
            var separatorsCount = _reader.ReadUInt32();
            if (separatorsCount > 0)
                throw new NotImplementedException("AiHints-Separators parsing not implemented yet");

            // Read DirectedPoints
            bmdFile.SectionVersions["AiHints.DirectedPoints"] = _reader.ReadUInt16();
            var directedPointsCount = _reader.ReadUInt32();
            if (directedPointsCount > 0)
                throw new NotImplementedException("AiHints-DirectedPoints parsing not implemented yet");

            // Read PolyLines
            bmdFile.SectionVersions["AiHints.PolyLines"] = _reader.ReadUInt16();
            var polyLinesCount = _reader.ReadUInt32();
            for (var i = 0; i < polyLinesCount; i++)
            {
                var polyLine = new HintPolyLine();
                polyLine.Version = _reader.ReadUInt16();
                polyLine.Type = ReadString();

                var pointsCount = _reader.ReadUInt32();
                for (var j = 0; j < pointsCount; j++)
                {
                    var point = new RmvVector2();
                    point.X = _reader.ReadSingle();
                    point.Y = _reader.ReadSingle();
                    polyLine.Points.Add(point);
                }
                
                if (polyLine.Version > 1)
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
            bmdFile.SectionVersions["AiHints.PolyLinesList"] = _reader.ReadUInt16();
            var polyLinesListCount = _reader.ReadUInt32();
            for (var i = 0; i < polyLinesListCount; i++)
            {
                var polyLineList = new HintPolyLineList();
                polyLineList.Version = _reader.ReadUInt16();
                
                // Read type string
                var typeLength = _reader.ReadUInt16();
                if (typeLength > 0)
                    polyLineList.Type = Encoding.UTF8.GetString(_reader.ReadBytes(typeLength));
                
                // Read district
                polyLineList.District = _reader.ReadUInt32();
                
                // Read polygon list
                var polygonListLength = _reader.ReadUInt32();
                for (var j = 0; j < polygonListLength; j++)
                {
                    var polygon = new Polygon();
                    
                    // Read points for this polygon
                    var pointsLength = _reader.ReadUInt32();
                    for (var k = 0; k < pointsLength; k++)
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

            if (probe.Version > 2)
            {
                probe.InnerRadius = _reader.ReadSingle();
                probe.IsCylinder = _reader.ReadByte() != 0;
            }

            probe.Primary = _reader.ReadByte() != 0;
            if (probe.Version > 1)
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
            if (hole.TerrainHoleVersion > 1)
                hole.HeightMode = ReadString();
            if (hole.TerrainHoleVersion > 2)
                hole.Flags = ReadBmdComponentFlags();

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
                light.LightProbeOnly = _reader.ReadByte() != 0;
            if (light.PointLightInfoVersion > 3)
            {
                if (light.PointLightInfoVersion > 5)
                    light.PdlcMask = _reader.ReadUInt64();
                else
                    light.PdlcMask = _reader.ReadUInt32();
            }
            if (light.PointLightInfoVersion > 6)
                light.Flags = ReadBmdComponentFlags();
            
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
            for (var i = 0; i < 4; i++)
                area.BoundingBox[i] = _reader.ReadSingle();

            area.HasBeenSet = _reader.ReadByte() != 0;
            
            if (area.PlayableAreaVersion > 1)
            {
                if (area.PlayableAreaVersion > 2)
                    area.FlagVersion = _reader.ReadUInt16();
                area.ValidNorth = _reader.ReadByte() != 0;
                area.ValidSouth = _reader.ReadByte() != 0;
                area.ValidEast = _reader.ReadByte() != 0;
                area.ValidWest = _reader.ReadByte() != 0;
            }
            
            return area;
        }

        private PolyMeshInfo ReadPolyMeshInfo()
        {
            var mesh = new PolyMeshInfo();
            mesh.PolyMeshVersion = _reader.ReadUInt16();
            
            var vertexCount = _reader.ReadUInt32();
            mesh.VertexList = new RmvVector3[vertexCount];
            for (var i = 0; i < vertexCount; i++)
                mesh.VertexList[i] = ReadRmvVector3();
            
            var triangleCount = _reader.ReadUInt32();
            mesh.TriangleList = new ushort[triangleCount];
            for (var i = 0; i < triangleCount; i++)
                mesh.TriangleList[i] = _reader.ReadUInt16();
            
            mesh.MaterialString = ReadString();
            mesh.HeightMode = ReadString();

            if (mesh.PolyMeshVersion > 2)
                mesh.Flags = ReadBmdComponentFlags();
            if (mesh.PolyMeshVersion > 3)
            {
                mesh.Transform = ReadRowMajorMatrix(false);
                mesh.VisibleInsideSnowRegion = _reader.ReadByte() != 0;
                mesh.VisibleOutsideSnowRegion = _reader.ReadByte() != 0;
                mesh.VisibleInsideDestructionRegion = _reader.ReadByte() != 0;
                mesh.VisibleOutsideDestructionRegion = _reader.ReadByte() != 0;
                mesh.VisibleInShroud = _reader.ReadByte() != 0;
                mesh.VisibleWithoutShroud = _reader.ReadByte() != 0;
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
            if (light.Version > 1)
                light.Volumetric = _reader.ReadByte() != 0;
            if (light.Version > 2)
                light.HeightMode = ReadString();

            if (light.Version > 6)
                light.PdlcMask = _reader.ReadUInt64();
            else if (light.Version > 3)
                light.PdlcMask = _reader.ReadUInt32();

            if (light.Version > 7)
                light.Flags = ReadBmdComponentFlags();
            
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
                hole.Flags = ReadBmdComponentFlags();
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
            for (var i = 0; i < coordCount; i++)
                sound.CoordList[i] = ReadRmvVector3();
            
            sound.InnerRadius = _reader.ReadSingle();
            sound.OuterRadius = _reader.ReadSingle();
            
            sound.InnerCubeBoundingBox = (ReadRmvVector3(), ReadRmvVector3());
            sound.OuterCubeBoundingBox = (ReadRmvVector3(), ReadRmvVector3());
            
            var riverNodeCount = sound.Version > 1 ? _reader.ReadUInt32() : 0;
            sound.RiverNodeList = new RiverNode[riverNodeCount];
            for (var i = 0; i < riverNodeCount; i++)
            {
                sound.RiverNodeList[i] = new RiverNode
                {
                    Version = _reader.ReadUInt16(),
                    Position = ReadRmvVector3(),
                    Width = _reader.ReadSingle(),
                    FlowSpeed = _reader.ReadSingle()
                };
            }
            
            if (sound.Version > 2)
                sound.ClampToSurface = _reader.ReadByte();
            if (sound.Version > 3)
                sound.HeightMode = ReadString();
            
            if (sound.Version > 9)
                sound.CampaignTypeMask = _reader.ReadUInt64();
            else if (sound.Version > 4)
                sound.CampaignTypeMask = _reader.ReadUInt32();

            if (sound.Version > 6)
                sound.CultureMask = ReadCultureMask();
            else if (sound.Version > 5)
                sound.CultureMask = ReadCultureMask(4);
            if (sound.Version > 7)
            {
                sound.DirectionVector = ReadRmvVector3();
                sound.UpVector = ReadRmvVector3();
            }
            if (sound.Version > 8)
                sound.Scope = ReadString();
            
            return sound;
        }

        private CscInfo ReadCscInfo()
        {
            var csc = new CscInfo();
            csc.Version = _reader.ReadUInt16();
            
            csc.Transform = ReadRowMajorMatrix(false);
            csc.SceneFile = ReadString();
            if (csc.Version > 1)
                csc.HeightMode = ReadString();
            if (csc.Version > 6)
                csc.PdlcMask = _reader.ReadUInt64();
            else if (csc.Version > 2)
                csc.PdlcMask = _reader.ReadUInt32();
            csc.Autoplay = _reader.ReadByte() != 0;
            if (csc.Version > 4)
                csc.VisibleInShroud = _reader.ReadByte() != 0;
            if (csc.Version > 5)
                csc.NoCulling = _reader.ReadByte() != 0;
            if (csc.Version > 7)
                csc.ScriptId = ReadString();
            if (csc.Version > 8)
                csc.ParentScriptId = ReadString();
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
            
            deployment.Category = ReadString();
            
            var deploymentZoneListLength = _reader.ReadUInt32();
            for (var i = 0; i < deploymentZoneListLength; i++)
            {
                deployment.DeploymentZones.Add(ReadDeploymentZone());
            }
            
            return deployment;
        }

        private DeploymentZone ReadDeploymentZone()
        {
            var deploymentZone = new DeploymentZone();
            deploymentZone.Version = _reader.ReadUInt16();
            
            var deploymentZoneRegionListLength = _reader.ReadUInt32();
            for (var i = 0; i < deploymentZoneRegionListLength; i++)
            {
                deploymentZone.DeploymentZoneRegions.Add(ReadDeploymentZoneRegion());
            }
            
            return deploymentZone;
        }

        private DeploymentZoneRegion ReadDeploymentZoneRegion()
        {
            var region = new DeploymentZoneRegion();
            region.Version = _reader.ReadUInt16();
            
            var boundaryListLength = _reader.ReadUInt32();
            for (var i = 0; i < boundaryListLength; i++)
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
            
            boundary.BoundaryType = ReadString();
            
            var pointListLength = _reader.ReadUInt32();
            for (var i = 0; i < pointListLength; i++)
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

            //Clamp to surface goes away after version 2, merged with clamp to water?
            if (flags.FlagVersion < 3)
                flags.ClampToSurface = _reader.ReadByte() != 0;
            flags.ClampToWaterSurface = _reader.ReadByte() != 0;

            //Seasons
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

        /// <summary>A culture/campaign mask; older record versions store only its low 4 bytes.</summary>
        private CultureMask ReadCultureMask(int size = 8)
        {
            var mask = new CultureMask();
            var bytes = new byte[8];
            var read = _reader.ReadBytes(size);
            if (read.Length < size)
                throw new EndOfStreamException($"Not enough bytes to read CultureMask. Got {read.Length}, expected {size}");
            Array.Copy(read, bytes, size);

            _logger.Here().Information($"BMD Parser - ReadCultureMask: read {bytes.Length} bytes, expected 8");

            if (bytes.Length < 8)
            {
                _logger.Here().Error($"BMD Parser - ReadCultureMask: Not enough bytes. Got {bytes.Length}, expected 8. Stream position: {_stream.Position}, Length: {_stream.Length}");
                throw new EndOfStreamException($"Not enough bytes to read CultureMask. Got {bytes.Length}, expected 8");
            }

            mask.RawBytes = bytes;

            // First byte
            mask.CultMaskBase = (bytes[0] & 0x01) != 0;
            mask.CultMaskBst = (bytes[0] & 0x40) != 0;
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
            mask.CultMaskNag = (bytes[4] & 0x08) != 0;

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
