using System.Dynamic;
using System.Globalization;
using System.Windows;
using CsvHelper;
using Shared.Core.ErrorHandling.Exceptions;
using Shared.Core.Events;
using Shared.Core.Misc;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Utility;
using Shared.Core.Settings;
using Shared.GameFormats.Bmd;

namespace Editors.Reports.Bmd
{
    public class BmdReportCommand(BmdReportGenerator generator) : IUiCommand
    {
        public void Execute() => generator.Create();
    }

    public class BmdReportGenerator
    {
        private readonly IPackFileService _pfs;
        private readonly ApplicationSettingsService _applicationSettingsService;

        public BmdReportGenerator(IPackFileService pfs, ApplicationSettingsService applicationSettingsService)
        {
            _pfs = pfs;
            _applicationSettingsService = applicationSettingsService;
        }

        public void Create(string outputDir = null)
        {
            var gameName = GameInformationDatabase.GetGameById(_applicationSettingsService.CurrentSettings.CurrentGame).DisplayName;
            var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");
            outputDir = $"{DirectoryHelper.ReportsDirectory}\\Bmd\\{gameName}_{timeStamp}\\";
            
            var filesWithPaths = PackFileServiceUtility.FindAllWithExtentionIncludePaths(_pfs, ".bmd");
            var fileList = filesWithPaths.Select(x => x.Pack).ToList();
            
            // Create a mapping from PackFile to its correct path
            var packFileToPathMap = filesWithPaths.ToDictionary(x => x.Pack, x => x.FileName);

            var failedRecords = new List<dynamic>();
            var summaryRecords = new List<dynamic>();
            var battlefieldBuildingRecords = new List<dynamic>();
            var battlefieldBuildingFarRecords = new List<dynamic>();
            var captureLocationRecords = new List<dynamic>();
            var efLineRecords = new List<dynamic>();
            var goOutlineRecords = new List<dynamic>();
            var nonTerrainOutlineRecords = new List<dynamic>();
            var zonesTemplateRecords = new List<dynamic>();
            var bmdInfoRecords = new List<dynamic>();
            var bmdOutlineRecords = new List<dynamic>();
            var terrainOutlineRecords = new List<dynamic>();
            var liteBuildingOutlineRecords = new List<dynamic>();
            var cameraZoneRecords = new List<dynamic>();
            var civilianDeploymentRecords = new List<dynamic>();
            var civilianShelterRecords = new List<dynamic>();
            var propInfoRecords = new List<dynamic>();
            var vfxRecords = new List<dynamic>();
            var aiHintsRecords = new List<dynamic>();
            var lightProbeRecords = new List<dynamic>();
            var terrainHoleRecords = new List<dynamic>();
            var pointLightRecords = new List<dynamic>();
            var buildingProjectileEmitterRecords = new List<dynamic>();
            var playableAreaRecords = new List<dynamic>();
            var meshRecords = new List<dynamic>();
            var terrainStencilBlendTriangleRecords = new List<dynamic>();
            var spotLightRecords = new List<dynamic>();
            var soundRecords = new List<dynamic>();
            var cscRecords = new List<dynamic>();
            var deploymentRecords = new List<dynamic>();
            var bmdCachedAreaRecords = new List<dynamic>();
            var toggleableBuildingSlotRecords = new List<dynamic>();
            var terrainDecalRecords = new List<dynamic>();
            var treeListReferenceRecords = new List<dynamic>();
            var grassListReferenceRecords = new List<dynamic>();
            var waterOutlineRecords = new List<dynamic>();

            for (var i = 0; i < fileList.Count; i++)
            {
                var bmdFile = fileList[i];
                var path = packFileToPathMap[bmdFile];
                var bmdData = bmdFile.DataSource.ReadData();
                
                try
                {
                    var parsedFile = BmdParser.Parse(bmdData);
                    
                    // Validate parsed data for corruption
                    var validationResult = ValidateParsedData(parsedFile, path);
                    if (!validationResult.IsValid)
                    {
                        dynamic failedRecord = new ExpandoObject();
                        failedRecord.Path = path;
                        failedRecord.Error = $"Data validation failed: {validationResult.ErrorMessage}";
                        failedRecords.Add(failedRecord);
                        continue;
                    }

                    // Summary record
                    dynamic summaryRecord = new ExpandoObject();
                    summaryRecord.Path = path;
                    summaryRecord.FastBinVersion = parsedFile.Header.FastBinVersion;
                    summaryRecord.BattlefieldBuildingCount = parsedFile.BattlefieldBuildings.Count;
                    summaryRecord.BattlefieldBuildingFarCount = parsedFile.BattlefieldBuildingFars.Count;
                    summaryRecord.CaptureLocationCount = parsedFile.CaptureLocations.Count;
                    summaryRecord.EFLineCount = parsedFile.EFLines.Count;
                    summaryRecord.GoOutlineCount = parsedFile.GoOutlines.Count;
                    summaryRecord.NonTerrainOutlineCount = parsedFile.NonTerrainOutlines.Count;
                    summaryRecord.ZonesTemplateCount = parsedFile.ZonesTemplates.Count;
                    summaryRecord.BmdInfoCount = parsedFile.BmdInfos.Count;
                    summaryRecord.PropCount = parsedFile.Props.Count;
                    summaryRecord.PropInfoCount = parsedFile.PropInfos.Count;
                    summaryRecord.VfxCount = parsedFile.VfxInfos.Count;
                    summaryRecord.LightProbeCount = parsedFile.LightProbes.Count;
                    summaryRecord.TerrainHoleCount = parsedFile.TerrainHoles.Count;
                    summaryRecord.PointLightCount = parsedFile.PointLights.Count;
                    summaryRecord.PolyMeshCount = parsedFile.PolyMeshes.Count;
                    summaryRecord.SpotLightCount = parsedFile.SpotLights.Count;
                    summaryRecord.SoundCount = parsedFile.Sounds.Count;
                    summaryRecord.CscCount = parsedFile.CscInfos.Count;
                    summaryRecords.Add(summaryRecord);

                    // Battlefield Building records
                    foreach (var building in parsedFile.BattlefieldBuildings)
                    {
                        dynamic buildingRecord = new ExpandoObject();
                        buildingRecord.Path = path;
                        buildingRecord.PositionX = building.Transform.M41;
                        buildingRecord.PositionY = building.Transform.M42;
                        buildingRecord.PositionZ = building.Transform.M43;
                        buildingRecord.BuildingKey = building.BuildingKey;
                        battlefieldBuildingRecords.Add(buildingRecord);
                    }

                    // Battlefield Building Far records
                    foreach (var buildingFar in parsedFile.BattlefieldBuildingFars)
                    {
                        dynamic buildingFarRecord = new ExpandoObject();
                        buildingFarRecord.Path = path;
                        buildingFarRecord.Version = buildingFar.Version;
                        battlefieldBuildingFarRecords.Add(buildingFarRecord);
                    }

                    // Capture Location records
                    foreach (var captureLocation in parsedFile.CaptureLocations)
                    {
                        dynamic captureLocationRecord = new ExpandoObject();
                        captureLocationRecord.Path = path;
                        captureLocationRecord.PositionX = captureLocation.Coords.Length > 0 ? captureLocation.Coords[0] : 0;
                        captureLocationRecord.PositionY = captureLocation.Coords.Length > 1 ? captureLocation.Coords[1] : 0;
                        captureLocationRecord.Str = captureLocation.Str;
                        captureLocationRecords.Add(captureLocationRecord);
                    }

                    // EF Line records
                    foreach (var efLine in parsedFile.EFLines)
                    {
                        dynamic efLineRecord = new ExpandoObject();
                        efLineRecord.Path = path;
                        efLineRecord.Version = efLine.Version;
                        efLineRecords.Add(efLineRecord);
                    }

                    // Go Outline records
                    foreach (var goOutline in parsedFile.GoOutlines)
                    {
                        dynamic goOutlineRecord = new ExpandoObject();
                        goOutlineRecord.Path = path;
                        goOutlineRecord.Version = goOutline.Version;
                        goOutlineRecords.Add(goOutlineRecord);
                    }

                    // Non Terrain Outline records
                    foreach (var nonTerrainOutline in parsedFile.NonTerrainOutlines)
                    {
                        dynamic nonTerrainOutlineRecord = new ExpandoObject();
                        nonTerrainOutlineRecord.Path = path;
                        nonTerrainOutlineRecord.VertexCount = nonTerrainOutline.VertexList.Count;
                        nonTerrainOutlineRecords.Add(nonTerrainOutlineRecord);
                    }

                    // Zones Template records
                    foreach (var zonesTemplate in parsedFile.ZonesTemplates)
                    {
                        dynamic zonesTemplateRecord = new ExpandoObject();
                        zonesTemplateRecord.Path = path;
                        zonesTemplateRecord.PositionX = zonesTemplate.Outline.Count > 0 ? zonesTemplate.Outline[0].X : 0;
                        zonesTemplateRecord.PositionY = zonesTemplate.Outline.Count > 0 ? zonesTemplate.Outline[0].Y : 0;
                        zonesTemplateRecord.ZoneName = zonesTemplate.ZoneName;
                        zonesTemplateRecords.Add(zonesTemplateRecord);
                    }

                    // BMD Info records
                    foreach (var bmdInfo in parsedFile.BmdInfos)
                    {
                        dynamic bmdInfoRecord = new ExpandoObject();
                        bmdInfoRecord.Path = path;
                        bmdInfoRecord.PositionX = bmdInfo.Transform.M41;
                        bmdInfoRecord.PositionY = bmdInfo.Transform.M42;
                        bmdInfoRecord.PositionZ = bmdInfo.Transform.M43;
                        bmdInfoRecord.BmdString = bmdInfo.BmdString;
                        bmdInfoRecords.Add(bmdInfoRecord);
                    }

                    // BMD Outline records
                    foreach (var bmdOutline in parsedFile.BmdOutlines)
                    {
                        dynamic bmdOutlineRecord = new ExpandoObject();
                        bmdOutlineRecord.Path = path;
                        bmdOutlineRecord.Version = bmdOutline.Version;
                        bmdOutlineRecords.Add(bmdOutlineRecord);
                    }

                    // Terrain Outline records
                    foreach (var terrainOutline in parsedFile.TerrainOutlines)
                    {
                        dynamic terrainOutlineRecord = new ExpandoObject();
                        terrainOutlineRecord.Path = path;
                        terrainOutlineRecord.Version = terrainOutline.Version;
                        terrainOutlineRecords.Add(terrainOutlineRecord);
                    }

                    // Lite Building Outline records
                    foreach (var liteBuildingOutline in parsedFile.LiteBuildingOutlines)
                    {
                        dynamic liteBuildingOutlineRecord = new ExpandoObject();
                        liteBuildingOutlineRecord.Path = path;
                        liteBuildingOutlineRecord.Version = liteBuildingOutline.Version;
                        liteBuildingOutlineRecords.Add(liteBuildingOutlineRecord);
                    }

                    // Camera Zone records
                    foreach (var cameraZone in parsedFile.CameraZones)
                    {
                        dynamic cameraZoneRecord = new ExpandoObject();
                        cameraZoneRecord.Path = path;
                        cameraZoneRecord.Version = cameraZone.Version;
                        cameraZoneRecords.Add(cameraZoneRecord);
                    }

                    // Civilian Deployment records
                    foreach (var civilianDeployment in parsedFile.CivilianDeployments)
                    {
                        dynamic civilianDeploymentRecord = new ExpandoObject();
                        civilianDeploymentRecord.Path = path;
                        civilianDeploymentRecord.Version = civilianDeployment.Version;
                        civilianDeploymentRecords.Add(civilianDeploymentRecord);
                    }

                    // Civilian Shelter records
                    foreach (var civilianShelter in parsedFile.CivilianShelters)
                    {
                        dynamic civilianShelterRecord = new ExpandoObject();
                        civilianShelterRecord.Path = path;
                        civilianShelterRecord.Version = civilianShelter.Version;
                        civilianShelterRecords.Add(civilianShelterRecord);
                    }

                    
                    // Prop Info records
                    foreach (var propInfo in parsedFile.PropInfos)
                    {
                        dynamic propInfoRecord = new ExpandoObject();
                        propInfoRecord.Path = path;
                        propInfoRecord.PositionX = propInfo.Transform.M41;
                        propInfoRecord.PositionY = propInfo.Transform.M42;
                        propInfoRecord.PositionZ = propInfo.Transform.M43;
                        propInfoRecord.Rmv2Path = propInfo.Rmv2Path;
                        propInfoRecords.Add(propInfoRecord);
                    }

                    // VFX records
                    foreach (var vfx in parsedFile.VfxInfos)
                    {
                        dynamic vfxRecord = new ExpandoObject();
                        vfxRecord.Path = path;
                        vfxRecord.PositionX = vfx.Transform.M41;
                        vfxRecord.PositionY = vfx.Transform.M42;
                        vfxRecord.PositionZ = vfx.Transform.M43;
                        vfxRecord.VfxString = vfx.VfxString;
                        vfxRecords.Add(vfxRecord);
                    }

                    // AI Hints records
                    if (parsedFile.AiHints != null)
                    {
                        dynamic aiHintRecord = new ExpandoObject();
                        aiHintRecord.Path = path;
                        aiHintRecord.SeparatorCount = parsedFile.AiHints.Separators.Count;
                        aiHintRecord.DirectedPointCount = parsedFile.AiHints.DirectedPoints.Count;
                        aiHintsRecords.Add(aiHintRecord);
                    }

                    // Light Probe records
                    foreach (var lightProbe in parsedFile.LightProbes)
                    {
                        dynamic lightProbeRecord = new ExpandoObject();
                        lightProbeRecord.Path = path;
                        lightProbeRecord.PositionX = lightProbe.Position.X;
                        lightProbeRecord.PositionY = lightProbe.Position.Y;
                        lightProbeRecord.PositionZ = lightProbe.Position.Z;
                        lightProbeRecord.HeightMode = lightProbe.HeightMode;
                        lightProbeRecords.Add(lightProbeRecord);
                    }

                    // Terrain Hole records
                    foreach (var terrainHole in parsedFile.TerrainHoles)
                    {
                        dynamic terrainHoleRecord = new ExpandoObject();
                        terrainHoleRecord.Path = path;
                        terrainHoleRecord.PositionX = terrainHole.FirstVert.X;
                        terrainHoleRecord.PositionY = terrainHole.FirstVert.Y;
                        terrainHoleRecord.PositionZ = terrainHole.FirstVert.Z;
                        terrainHoleRecord.HeightMode = terrainHole.HeightMode;
                        terrainHoleRecords.Add(terrainHoleRecord);
                    }

                    // Point Light records
                    foreach (var pointLight in parsedFile.PointLights)
                    {
                        dynamic pointLightRecord = new ExpandoObject();
                        pointLightRecord.Path = path;
                        pointLightRecord.PositionX = pointLight.Position.X;
                        pointLightRecord.PositionY = pointLight.Position.Y;
                        pointLightRecord.PositionZ = pointLight.Position.Z;
                        pointLightRecord.Color = $"{pointLight.Red},{pointLight.Green},{pointLight.Blue}";
                        pointLightRecords.Add(pointLightRecord);
                    }

                    // Building Projectile Emitter records
                    foreach (var buildingProjectileEmitter in parsedFile.BuildingProjectileEmitters)
                    {
                        dynamic buildingProjectileEmitterRecord = new ExpandoObject();
                        buildingProjectileEmitterRecord.Path = path;
                        buildingProjectileEmitterRecord.PositionX = buildingProjectileEmitter.Location.X;
                        buildingProjectileEmitterRecord.PositionY = buildingProjectileEmitter.Location.Y;
                        buildingProjectileEmitterRecord.PositionZ = buildingProjectileEmitter.Location.Z;
                        buildingProjectileEmitterRecord.BuildingIndex = buildingProjectileEmitter.BuildingIndex;
                        buildingProjectileEmitterRecords.Add(buildingProjectileEmitterRecord);
                    }

                    // Playable Area records
                    if (parsedFile.PlayableArea != null)
                    {
                        dynamic playableAreaRecord = new ExpandoObject();
                        playableAreaRecord.Path = path;
                        playableAreaRecord.BoundingBoxMinX = parsedFile.PlayableArea.BoundingBox.Length > 0 ? parsedFile.PlayableArea.BoundingBox[0] : 0;
                        playableAreaRecord.BoundingBoxMinY = parsedFile.PlayableArea.BoundingBox.Length > 1 ? parsedFile.PlayableArea.BoundingBox[1] : 0;
                        playableAreaRecord.HasBeenSet = parsedFile.PlayableArea.HasBeenSet;
                        playableAreaRecords.Add(playableAreaRecord);
                    }

                    // Mesh records
                    foreach (var mesh in parsedFile.PolyMeshes)
                    {
                        dynamic meshRecord = new ExpandoObject();
                        meshRecord.Path = path;
                        meshRecord.PositionX = mesh.Transform.M41;
                        meshRecord.PositionY = mesh.Transform.M42;
                        meshRecord.PositionZ = mesh.Transform.M43;
                        meshRecord.MaterialString = mesh.MaterialString;
                        meshRecords.Add(meshRecord);
                    }

                    // Terrain Stencil Blend Triangle records
                    foreach (var terrainStencilBlendTriangle in parsedFile.TerrainStencilBlendTriangles)
                    {
                        dynamic terrainStencilBlendTriangleRecord = new ExpandoObject();
                        terrainStencilBlendTriangleRecord.Path = path;
                        terrainStencilBlendTriangleRecord.Version = terrainStencilBlendTriangle.Version;
                        terrainStencilBlendTriangleRecords.Add(terrainStencilBlendTriangleRecord);
                    }

                    // Spot Light records
                    foreach (var spotLight in parsedFile.SpotLights)
                    {
                        dynamic spotLightRecord = new ExpandoObject();
                        spotLightRecord.Path = path;
                        spotLightRecord.PositionX = spotLight.Position.X;
                        spotLightRecord.PositionY = spotLight.Position.Y;
                        spotLightRecord.PositionZ = spotLight.Position.Z;
                        spotLightRecord.Color = $"{spotLight.IntensityRed},{spotLight.IntensityGreen},{spotLight.IntensityBlue}";
                        spotLightRecords.Add(spotLightRecord);
                    }

                    // Sound records
                    foreach (var sound in parsedFile.Sounds)
                    {
                        dynamic soundRecord = new ExpandoObject();
                        soundRecord.Path = path;
                        soundRecord.PositionX = sound.CoordList.Length > 0 ? sound.CoordList[0].X : 0;
                        soundRecord.PositionY = sound.CoordList.Length > 0 ? sound.CoordList[0].Y : 0;
                        soundRecord.PositionZ = sound.CoordList.Length > 0 ? sound.CoordList[0].Z : 0;
                        soundRecord.SoundString = sound.SoundString;
                        soundRecord.TypeString = sound.TypeString;
                        soundRecord.InnerRadius = sound.InnerRadius;
                        soundRecord.OuterRadius = sound.OuterRadius;
                        soundRecord.InnerBoundingBoxMinX = sound.InnerCubeBoundingBox.Min.X;
                        soundRecord.InnerBoundingBoxMinY = sound.InnerCubeBoundingBox.Min.Y;
                        soundRecord.InnerBoundingBoxMinZ = sound.InnerCubeBoundingBox.Min.Z;
                        soundRecord.InnerBoundingBoxMaxX = sound.InnerCubeBoundingBox.Max.X;
                        soundRecord.InnerBoundingBoxMaxY = sound.InnerCubeBoundingBox.Max.Y;
                        soundRecord.InnerBoundingBoxMaxZ = sound.InnerCubeBoundingBox.Max.Z;
                        soundRecord.OuterBoundingBoxMinX = sound.OuterCubeBoundingBox.Min.X;
                        soundRecord.OuterBoundingBoxMinY = sound.OuterCubeBoundingBox.Min.Y;
                        soundRecord.OuterBoundingBoxMinZ = sound.OuterCubeBoundingBox.Min.Z;
                        soundRecord.OuterBoundingBoxMaxX = sound.OuterCubeBoundingBox.Max.X;
                        soundRecord.OuterBoundingBoxMaxY = sound.OuterCubeBoundingBox.Max.Y;
                        soundRecord.OuterBoundingBoxMaxZ = sound.OuterCubeBoundingBox.Max.Z;
                        soundRecord.RiverNodesLength = sound.RiverNodesLength;
                        soundRecord.ClampToSurface = sound.ClampToSurface;
                        soundRecord.HeightMode = sound.HeightMode;
                        soundRecord.CampaignTypeMask = sound.CampaignTypeMask;
                        soundRecord.DirectionVectorX = sound.DirectionVector.X;
                        soundRecord.DirectionVectorY = sound.DirectionVector.Y;
                        soundRecord.DirectionVectorZ = sound.DirectionVector.Z;
                        soundRecord.UpVectorX = sound.UpVector.X;
                        soundRecord.UpVectorY = sound.UpVector.Y;
                        soundRecord.UpVectorZ = sound.UpVector.Z;
                        soundRecord.Scope = sound.Scope;
                        soundRecords.Add(soundRecord);
                    }

                    // CSC records
                    foreach (var csc in parsedFile.CscInfos)
                    {
                        dynamic cscRecord = new ExpandoObject();
                        cscRecord.Path = path;
                        cscRecord.PositionX = csc.Transform.M41;
                        cscRecord.PositionY = csc.Transform.M42;
                        cscRecord.PositionZ = csc.Transform.M43;
                        cscRecord.SceneFile = csc.SceneFile;
                        cscRecord.HeightMode = csc.HeightMode;
                        cscRecord.PdlcMask = csc.PdlcMask;
                        cscRecord.Autoplay = csc.Autoplay;
                        cscRecord.VisibleInShroud = csc.VisibleInShroud;
                        cscRecord.NoCulling = csc.NoCulling;
                        cscRecord.ScriptId = csc.ScriptId;
                        cscRecord.ParentScriptId = csc.ParentScriptId;
                        cscRecord.VisibleWithoutShroud = csc.VisibleWithoutShroud;
                        cscRecord.VisibleInTacticalView = csc.VisibleInTacticalView;
                        cscRecord.VisibleInTacticalViewOnly = csc.VisibleInTacticalViewOnly;
                        cscRecord.HoldFirst = csc.HoldFirst;
                        cscRecord.HoldLast = csc.HoldLast;
                        cscRecords.Add(cscRecord);
                    }

                    // Deployment records
                    foreach (var deployment in parsedFile.Deployments)
                    {
                        foreach (var deploymentZone in deployment.DeploymentZones)
                        {
                            foreach (var region in deploymentZone.DeploymentZoneRegions)
                            {
                                dynamic deploymentRecord = new ExpandoObject();
                                deploymentRecord.Path = path;
                                deploymentRecord.DeploymentVersion = deployment.Version;
                                deploymentRecord.Category = deployment.Category;
                                deploymentRecord.ZoneVersion = deploymentZone.Version;
                                deploymentRecord.RegionVersion = region.Version;
                                deploymentRecord.RegionId = region.Id;
                                deploymentRecord.Orientation = region.Orientation;
                                deploymentRecord.SnapFacing = region.SnapFacing;
                                deploymentRecord.BoundaryCount = region.Boundaries.Count;
                                
                                if (region.Boundaries.Count > 0)
                                {
                                    var firstBoundary = region.Boundaries[0];
                                    deploymentRecord.FirstBoundaryType = firstBoundary.BoundaryType;
                                    deploymentRecord.FirstBoundaryPointCount = firstBoundary.PointList.Count;
                                    if (firstBoundary.PointList.Count > 0)
                                    {
                                        deploymentRecord.FirstBoundaryPointX = firstBoundary.PointList[0].X;
                                        deploymentRecord.FirstBoundaryPointY = firstBoundary.PointList[0].Y;
                                    }
                                }
                                
                                deploymentRecords.Add(deploymentRecord);
                            }
                        }
                    }

                    // BMD Cached Area records
                    foreach (var bmdCachedArea in parsedFile.BmdCachedAreas)
                    {
                        dynamic bmdCachedAreaRecord = new ExpandoObject();
                        bmdCachedAreaRecord.Path = path;
                        bmdCachedAreaRecord.Version = bmdCachedArea.Version;
                        bmdCachedAreaRecords.Add(bmdCachedAreaRecord);
                    }

                    // Toggleable Building Slot records
                    foreach (var toggleableBuildingSlot in parsedFile.ToggleableBuildingSlots)
                    {
                        dynamic toggleableBuildingSlotRecord = new ExpandoObject();
                        toggleableBuildingSlotRecord.Path = path;
                        toggleableBuildingSlotRecord.Version = toggleableBuildingSlot.Version;
                        toggleableBuildingSlotRecords.Add(toggleableBuildingSlotRecord);
                    }

                    // Terrain Decal records
                    foreach (var terrainDecal in parsedFile.TerraindDecals)
                    {
                        dynamic terrainDecalRecord = new ExpandoObject();
                        terrainDecalRecord.Path = path;
                        terrainDecalRecord.Version = terrainDecal.Version;
                        terrainDecalRecords.Add(terrainDecalRecord);
                    }

                    // Tree List Reference records
                    foreach (var treeListReference in parsedFile.TreeListReferences)
                    {
                        dynamic treeListReferenceRecord = new ExpandoObject();
                        treeListReferenceRecord.Path = path;
                        treeListReferenceRecord.Version = treeListReference.Version;
                        treeListReferenceRecords.Add(treeListReferenceRecord);
                    }

                    // Grass List Reference records
                    foreach (var grassListReference in parsedFile.GrassListReferences)
                    {
                        dynamic grassListReferenceRecord = new ExpandoObject();
                        grassListReferenceRecord.Path = path;
                        grassListReferenceRecord.Version = grassListReference.Version;
                        grassListReferenceRecords.Add(grassListReferenceRecord);
                    }

                    // Water Outline records
                    foreach (var waterOutline in parsedFile.WaterOutlines)
                    {
                        dynamic waterOutlineRecord = new ExpandoObject();
                        waterOutlineRecord.Path = path;
                        waterOutlineRecord.Version = waterOutline.Version;
                        waterOutlineRecords.Add(waterOutlineRecord);
                    }
                }
                catch (Exception e)
                {
                    dynamic failedRecord = new ExpandoObject();
                    failedRecord.Path = path;
                    failedRecord.Error = ExceptionHelper.GetErrorString(e, " - ");
                    failedRecords.Add(failedRecord);
                }
            }

            try
            {
                DirectoryHelper.EnsureCreated(outputDir);
                Write(failedRecords, outputDir + "LoadResult.csv");
                Write(summaryRecords, outputDir + "BmdSummary.csv");
                Write(battlefieldBuildingRecords, outputDir + "BattlefieldBuildings.csv");
                Write(battlefieldBuildingFarRecords, outputDir + "BattlefieldBuildingFars.csv");
                Write(captureLocationRecords, outputDir + "CaptureLocations.csv");
                Write(efLineRecords, outputDir + "EFLines.csv");
                Write(goOutlineRecords, outputDir + "GoOutlines.csv");
                Write(nonTerrainOutlineRecords, outputDir + "NonTerrainOutlines.csv");
                Write(zonesTemplateRecords, outputDir + "ZonesTemplates.csv");
                Write(bmdInfoRecords, outputDir + "BmdInfos.csv");
                Write(bmdOutlineRecords, outputDir + "BmdOutlines.csv");
                Write(terrainOutlineRecords, outputDir + "TerrainOutlines.csv");
                Write(liteBuildingOutlineRecords, outputDir + "LiteBuildingOutlines.csv");
                Write(cameraZoneRecords, outputDir + "CameraZones.csv");
                Write(civilianDeploymentRecords, outputDir + "CivilianDeployments.csv");
                Write(civilianShelterRecords, outputDir + "CivilianShelters.csv");
                                Write(propInfoRecords, outputDir + "PropInfos.csv");
                Write(vfxRecords, outputDir + "VfxInfos.csv");
                Write(aiHintsRecords, outputDir + "AiHints.csv");
                Write(lightProbeRecords, outputDir + "LightProbes.csv");
                Write(terrainHoleRecords, outputDir + "TerrainHoles.csv");
                Write(pointLightRecords, outputDir + "PointLights.csv");
                Write(buildingProjectileEmitterRecords, outputDir + "BuildingProjectileEmitters.csv");
                Write(playableAreaRecords, outputDir + "PlayableAreas.csv");
                Write(meshRecords, outputDir + "PolyMeshes.csv");
                Write(terrainStencilBlendTriangleRecords, outputDir + "TerrainStencilBlendTriangles.csv");
                Write(spotLightRecords, outputDir + "SpotLights.csv");
                Write(soundRecords, outputDir + "Sounds.csv");
                Write(cscRecords, outputDir + "CscInfos.csv");
                Write(deploymentRecords, outputDir + "Deployments.csv");
                Write(bmdCachedAreaRecords, outputDir + "BmdCachedAreas.csv");
                Write(toggleableBuildingSlotRecords, outputDir + "ToggleableBuildingSlots.csv");
                Write(terrainDecalRecords, outputDir + "TerrainDecals.csv");
                Write(treeListReferenceRecords, outputDir + "TreeListReferences.csv");
                Write(grassListReferenceRecords, outputDir + "GrassListReferences.csv");
                Write(waterOutlineRecords, outputDir + "WaterOutlines.csv");

                MessageBox.Show($"Done - Created at {outputDir}");
            }
            catch
            {
                MessageBox.Show("Unable to write reports to file!");
            }
        }

        void Write(List<dynamic> dataRecords, string filePath)
        {
            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(dataRecords);
            File.WriteAllText(filePath, writer.ToString());
        }

        private ValidationResult ValidateParsedData(BmdFile parsedFile, string filePath)
        {
            var errors = new List<string>();

            // Check for reasonable FastBin version
            if (parsedFile.Header.FastBinVersion > 100 || parsedFile.Header.FastBinVersion < 1)
            {
                errors.Add($"Invalid FastBin version: {parsedFile.Header.FastBinVersion}");
            }

            // Validate string fields for Unicode garbage
            ValidateStringField(parsedFile.BattlefieldBuildings.Select(b => b.BuildingId), "BattlefieldBuilding.BuildingId", errors);
            ValidateStringField(parsedFile.BattlefieldBuildings.Select(b => b.BuildingKey), "BattlefieldBuilding.BuildingKey", errors);
            ValidateStringField(parsedFile.BattlefieldBuildings.Select(b => b.PositionType), "BattlefieldBuilding.PositionType", errors);
            ValidateStringField(parsedFile.BmdInfos.Select(b => b.BmdString), "BmdInfo.BmdString", errors);
            ValidateStringField(parsedFile.BmdInfos.Select(b => b.RegionString), "BmdInfo.RegionString", errors);
            ValidateStringField(parsedFile.PropInfos.Select(p => p.Rmv2Path), "PropInfo.Rmv2Path", errors);
            ValidateStringField(parsedFile.PropInfos.Select(p => p.HeightMode), "PropInfo.HeightMode", errors);
            ValidateStringField(parsedFile.VfxInfos.Select(v => v.VfxString), "VfxInfo.VfxString", errors);
            ValidateStringField(parsedFile.VfxInfos.Select(v => v.HeightMode), "VfxInfo.HeightMode", errors);
            ValidateStringField(parsedFile.Sounds.Select(s => s.SoundString), "SoundInfo.SoundString", errors);
            ValidateStringField(parsedFile.Sounds.Select(s => s.TypeString), "SoundInfo.TypeString", errors);
            ValidateStringField(parsedFile.Sounds.Select(s => s.HeightMode), "SoundInfo.HeightMode", errors);
            ValidateStringField(parsedFile.Sounds.Select(s => s.Scope), "SoundInfo.Scope", errors);
            ValidateStringField(parsedFile.PolyMeshes.Select(m => m.MaterialString), "PolyMesh.MaterialString", errors);
            ValidateStringField(parsedFile.PolyMeshes.Select(m => m.HeightMode), "PolyMesh.HeightMode", errors);
            ValidateStringField(parsedFile.CscInfos.Select(c => c.SceneFile), "CscInfo.SceneFile", errors);
            ValidateStringField(parsedFile.CscInfos.Select(c => c.HeightMode), "CscInfo.HeightMode", errors);
            ValidateStringField(parsedFile.CscInfos.Select(c => c.ScriptId), "CscInfo.ScriptId", errors);
            ValidateStringField(parsedFile.CscInfos.Select(c => c.ParentScriptId), "CscInfo.ParentScriptId", errors);

            // Check for reasonable counts (files with thousands of elements are likely corrupted)
            if (parsedFile.BattlefieldBuildings.Count > 2000)
                errors.Add($"Suspicious BattlefieldBuilding count: {parsedFile.BattlefieldBuildings.Count}");
            if (parsedFile.PropInfos.Count > 20000)
                errors.Add($"Suspicious PropInfos count: {parsedFile.PropInfos.Count}");
            if (parsedFile.VfxInfos.Count > 2000)
                errors.Add($"Suspicious VFX count: {parsedFile.VfxInfos.Count}");
            if (parsedFile.PointLights.Count > 2000)
                errors.Add($"Suspicious PointLight count: {parsedFile.PointLights.Count}");
            if (parsedFile.SpotLights.Count > 2000)
                errors.Add($"Suspicious SpotLight count: {parsedFile.SpotLights.Count}");
            if (parsedFile.Sounds.Count > 2000)
                errors.Add($"Suspicious Sound count: {parsedFile.Sounds.Count}");
            if (parsedFile.PolyMeshes.Count > 2000)
                errors.Add($"Suspicious PolyMesh count: {parsedFile.PolyMeshes.Count}");

            // Check for version field corruption
            if (parsedFile.BattlefieldBuildings.Any(b => b.Version > 100))
                errors.Add("Corrupted BattlefieldBuilding version detected");
            if (parsedFile.BattlefieldBuildingFars.Any(b => b.Version > 100))
                errors.Add("Corrupted BattlefieldBuildingFar version detected");
            if (parsedFile.CaptureLocations.Any(c => c.Version > 100))
                errors.Add("Corrupted CaptureLocation version detected");
            if (parsedFile.BmdInfos.Any(b => b.Version > 100))
                errors.Add("Corrupted BmdInfo version detected");
            if (parsedFile.PropInfos.Any(p => p.PropInfoVersion > 100))
                errors.Add("Corrupted PropInfo version detected");
            if (parsedFile.VfxInfos.Any(v => v.VfxInfoVersion > 100))
                errors.Add("Corrupted VfxInfo version detected");
            if (parsedFile.LightProbes.Any(l => l.Version > 100))
                errors.Add("Corrupted LightProbe version detected");
            if (parsedFile.TerrainHoles.Any(t => t.TerrainHoleVersion > 100))
                errors.Add("Corrupted TerrainHole version detected");
            if (parsedFile.PointLights.Any(p => p.PointLightInfoVersion > 100))
                errors.Add("Corrupted PointLight version detected");
            if (parsedFile.BuildingProjectileEmitters.Any(b => b.BuildingProjectileEmitterVersion > 100))
                errors.Add("Corrupted BuildingProjectileEmitter version detected");
            if (parsedFile.PolyMeshes.Any(p => p.PolyMeshVersion > 100))
                errors.Add("Corrupted PolyMesh version detected");
            if (parsedFile.TerrainStencilBlendTriangles.Any(t => t.Version > 100))
                errors.Add("Corrupted TerrainStencilBlendTriangle version detected");
            if (parsedFile.SpotLights.Any(s => s.Version > 100))
                errors.Add("Corrupted SpotLight version detected");
            if (parsedFile.Sounds.Any(s => s.Version > 100))
                errors.Add("Corrupted Sound version detected");
            if (parsedFile.CscInfos.Any(c => c.Version > 100))
                errors.Add("Corrupted CSC version detected");
            if (parsedFile.Deployments.Any(d => d.Version > 100))
                errors.Add("Corrupted Deployment version detected");
            if (parsedFile.BmdCachedAreas.Any(b => b.Version > 100))
                errors.Add("Corrupted BmdCachedArea version detected");
            if (parsedFile.ToggleableBuildingSlots.Any(t => t.Version > 100))
                errors.Add("Corrupted ToggleableBuildingSlot version detected");
            if (parsedFile.TerraindDecals.Any(t => t.Version > 100))
                errors.Add("Corrupted TerraindDecal version detected");
            if (parsedFile.TreeListReferences.Any(t => t.Version > 100))
                errors.Add("Corrupted TreeListReference version detected");
            if (parsedFile.GrassListReferences.Any(g => g.Version > 100))
                errors.Add("Corrupted GrassListReference version detected");
            if (parsedFile.WaterOutlines.Any(w => w.Version > 100))
                errors.Add("Corrupted WaterOutline version detected");
            
            // Check single object versions
            if (parsedFile.PlayableArea != null && parsedFile.PlayableArea.PlayableAreaVersion > 100)
                errors.Add("Corrupted PlayableArea version detected");

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                ErrorMessage = string.Join("; ", errors)
            };
        }

        private void ValidateStringField(IEnumerable<string> strings, string fieldName, List<string> errors)
        {
            foreach (var str in strings)
            {
                if (string.IsNullOrEmpty(str)) continue;

                // Check for Unicode garbage characters
                if (ContainsUnicodeGarbage(str))
                {
                    errors.Add($"Unicode garbage detected in {fieldName}: '{str.Substring(0, Math.Min(50, str.Length))}...'");
                }

                // Check for unusually long strings (likely corrupted)
                if (str.Length > 1000)
                {
                    errors.Add($"Suspiciously long string in {fieldName}: {str.Length} characters");
                }
            }
        }

        private bool ContainsUnicodeGarbage(string str)
        {
            // Check for common Unicode garbage patterns
            var garbagePatterns = new[]
            {
                '\0', // null bytes
                '\uFFFD', // replacement character
                (char)0x1F, // control characters
                (char)0x1E,
                (char)0x1D,
                (char)0x1C,
                (char)0x0B,
                (char)0x0C,
                (char)0x0E,
                (char)0x0F
            };

            return str.Any(c => 
                (c < 32 && c != '\t' && c != '\n' && c != '\r') || // control characters except whitespace
                garbagePatterns.Contains(c) ||
                char.IsControl(c) && !char.IsWhiteSpace(c));
        }

        private class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
        }
    }
}
