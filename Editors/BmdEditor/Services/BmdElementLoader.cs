using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shared.Core.PackFiles;
using Shared.GameFormats.Bmd;
using Serilog;
using Editors.BmdEditor.ViewModels;

namespace Editors.BmdEditor.Services
{
    public class BmdElementLoader(IPackFileService packFileService)
    {
        private readonly IPackFileService _packFileService = packFileService;
        private readonly ILogger _logger = Serilog.Log.ForContext<BmdElementLoader>();

        // Referenced BMDs parsed so far, by path (null = missing or unparsable). Kept across Add and
        // Delete rebuilds so a reference is parsed once, not once per rebuild or per reference.
        private readonly Dictionary<string, BmdFile?> _referencedBmds = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Forgets every parsed referenced BMD, so the next load reads them from the packs again.</summary>
        public void ClearReferencedBmdCache() => _referencedBmds.Clear();

        public void LoadElements(BmdFile bmdFile, 
            ObservableCollection<BmdElementViewModel> allElements,
            ObservableCollection<BmdInfoViewModel> bmdInfos,
            ObservableCollection<BattlefieldBuildingViewModel> battlefieldBuildings,
            ObservableCollection<BattlefieldBuildingFarViewModel> battlefieldBuildingFars,
            ObservableCollection<CaptureLocationViewModel> captureLocations,
            ObservableCollection<EFLineViewModel> efLines,
            ObservableCollection<GoOutlineViewModel> goOutlines,
            ObservableCollection<NonTerrainOutlineViewModel> nonTerrainOutlines,
            ObservableCollection<BuildingProjectileEmitterViewModel> buildingProjectileEmitters,
            ObservableCollection<ZonesTemplateViewModel> zonesTemplates,
            ObservableCollection<PropInfoViewModel> props,
            ObservableCollection<VfxInfoViewModel> vfxInfos,
            ObservableCollection<PointLightInfoViewModel> pointLights,
            ObservableCollection<SpotLightInfoViewModel> spotLights,
            ObservableCollection<SoundInfoViewModel> sounds,
            ObservableCollection<PolyMeshInfoViewModel> polyMeshes,
            ObservableCollection<LightProbeInfoViewModel> lightProbes,
            ObservableCollection<TerrainHoleInfoViewModel> terrainHoles,
            ObservableCollection<PlayableAreaViewModel> playableAreas,
            ObservableCollection<CscInfoViewModel> cscInfos,
            ObservableCollection<DeploymentViewModel> deployments,
            bool loadChildBmds = true)
        {
            if (bmdFile == null) return;

            LoadBmdInfos(bmdFile, bmdInfos, allElements, loadChildBmds);
            LoadBattlefieldBuildings(bmdFile, battlefieldBuildings, allElements);
            LoadBattlefieldBuildingFars(bmdFile, battlefieldBuildingFars, allElements);
            LoadCaptureLocations(bmdFile, captureLocations, allElements);
            LoadEFLines(bmdFile, efLines, allElements);
            LoadGoOutlines(bmdFile, goOutlines, allElements);
            LoadNonTerrainOutlines(bmdFile, nonTerrainOutlines, allElements);
            LoadBuildingProjectileEmitters(bmdFile, buildingProjectileEmitters, allElements);
            LoadZonesTemplates(bmdFile, zonesTemplates, allElements);
            LoadProps(bmdFile, props, allElements);
            LoadVfxInfos(bmdFile, vfxInfos, allElements);
            LoadPointLights(bmdFile, pointLights, allElements);
            LoadSpotLights(bmdFile, spotLights, allElements);
            LoadSounds(bmdFile, sounds, allElements);
            LoadPolyMeshes(bmdFile, polyMeshes, allElements);
            LoadLightProbes(bmdFile, lightProbes, allElements);
            LoadTerrainHoles(bmdFile, terrainHoles, allElements);
            LoadPlayableAreas(bmdFile, playableAreas, allElements);
            LoadCscInfos(bmdFile, cscInfos, allElements);
            LoadDeployments(bmdFile, deployments, allElements);
        }

        public void LoadChildElements(BmdInfoViewModel parentViewModel, BmdFile referencedBmd)
        {
            if (parentViewModel == null || referencedBmd == null) return;

            parentViewModel.ChildElements.Clear();
            parentViewModel.HasChildren = false;

            var childAllElements = new ObservableCollection<BmdElementViewModel>();
            var childBmdInfos = new ObservableCollection<BmdInfoViewModel>();
            var childBattlefieldBuildings = new ObservableCollection<BattlefieldBuildingViewModel>();
            var childBattlefieldBuildingFars = new ObservableCollection<BattlefieldBuildingFarViewModel>();
            var childCaptureLocations = new ObservableCollection<CaptureLocationViewModel>();
            var childEfLines = new ObservableCollection<EFLineViewModel>();
            var childGoOutlines = new ObservableCollection<GoOutlineViewModel>();
            var childNonTerrainOutlines = new ObservableCollection<NonTerrainOutlineViewModel>();
            var childBuildingProjectileEmitters = new ObservableCollection<BuildingProjectileEmitterViewModel>();
            var childZonesTemplates = new ObservableCollection<ZonesTemplateViewModel>();
            var childProps = new ObservableCollection<PropInfoViewModel>();
            var childVfxInfos = new ObservableCollection<VfxInfoViewModel>();
            var childPointLights = new ObservableCollection<PointLightInfoViewModel>();
            var childSpotLights = new ObservableCollection<SpotLightInfoViewModel>();
            var childSounds = new ObservableCollection<SoundInfoViewModel>();
            var childPolyMeshes = new ObservableCollection<PolyMeshInfoViewModel>();
            var childLightProbes = new ObservableCollection<LightProbeInfoViewModel>();
            var childTerrainHoles = new ObservableCollection<TerrainHoleInfoViewModel>();
            var childPlayableAreas = new ObservableCollection<PlayableAreaViewModel>();
            var childCscInfos = new ObservableCollection<CscInfoViewModel>();
            var childDeployments = new ObservableCollection<DeploymentViewModel>();

            // Load all elements from the referenced BMD (but don't load nested BMDs to avoid infinite recursion)
            LoadElements(referencedBmd, 
                childAllElements, childBmdInfos, childBattlefieldBuildings, childBattlefieldBuildingFars,
                childCaptureLocations, childEfLines, childGoOutlines, childNonTerrainOutlines,
                childBuildingProjectileEmitters, childZonesTemplates, childProps, childVfxInfos,
                childPointLights, childSpotLights, childSounds, childPolyMeshes, childLightProbes,
                childTerrainHoles, childPlayableAreas, childCscInfos, childDeployments,
                loadChildBmds: false);

            foreach (var element in childAllElements)
            {
                parentViewModel.ChildElements.Add(element);
                parentViewModel.HasChildren = true;
            }
        }

        private BmdFile? GetReferencedBmd(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            if (_referencedBmds.TryGetValue(path, out var cached))
                return cached;

            BmdFile? parsed = null;
            try
            {
                var file = _packFileService.FindFile(path);
                if (file == null)
                    _logger.Warning($"Referenced BMD file not found: {path}");
                else
                    parsed = BmdParser.Parse(file.DataSource.ReadData());
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to load referenced BMD '{path}': {ex.Message}");
            }

            _referencedBmds[path] = parsed;
            return parsed;
        }

        #region Helper Methods for Loading Specific Element Types

        private void LoadBmdInfos(BmdFile bmdFile, ObservableCollection<BmdInfoViewModel> bmdInfos, 
            ObservableCollection<BmdElementViewModel> allElements, bool loadChildBmds)
        {
            for (var i = 0; i < bmdFile.BmdInfos.Count; i++)
            {
                var bmd = bmdFile.BmdInfos[i];
                var vm = new BmdInfoViewModel(bmd);

                if (loadChildBmds && GetReferencedBmd(bmd.BmdString) is { } referencedBmd)
                {
                    vm.ReferencedBmd = referencedBmd;
                    LoadChildElements(vm, referencedBmd);
                }

                bmdInfos.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadBattlefieldBuildings(BmdFile bmdFile, ObservableCollection<BattlefieldBuildingViewModel> battlefieldBuildings, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.BattlefieldBuildings.Count; i++)
            {
                var building = bmdFile.BattlefieldBuildings[i];
                var vm = new BattlefieldBuildingViewModel(building);
                battlefieldBuildings.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadBattlefieldBuildingFars(BmdFile bmdFile, ObservableCollection<BattlefieldBuildingFarViewModel> battlefieldBuildingFars, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var buildingFar in bmdFile.BattlefieldBuildingFars)
            {
                var vm = new BattlefieldBuildingFarViewModel(buildingFar);
                battlefieldBuildingFars.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadCaptureLocations(BmdFile bmdFile, ObservableCollection<CaptureLocationViewModel> captureLocations, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var captureLocation in bmdFile.CaptureLocationLists.SelectMany(l => l.Locations))
            {
                var vm = new CaptureLocationViewModel(captureLocation);
                captureLocations.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadEFLines(BmdFile bmdFile, ObservableCollection<EFLineViewModel> efLines, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var efLine in bmdFile.EFLines)
            {
                var vm = new EFLineViewModel(efLine);
                efLines.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadGoOutlines(BmdFile bmdFile, ObservableCollection<GoOutlineViewModel> goOutlines, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var goOutline in bmdFile.GoOutlines)
            {
                var vm = new GoOutlineViewModel(goOutline);
                goOutlines.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadNonTerrainOutlines(BmdFile bmdFile, ObservableCollection<NonTerrainOutlineViewModel> nonTerrainOutlines, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.NonTerrainOutlines.Count; i++)
            {
                var nonTerrainOutline = bmdFile.NonTerrainOutlines[i];
                var vm = new NonTerrainOutlineViewModel(nonTerrainOutline);
                nonTerrainOutlines.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadBuildingProjectileEmitters(BmdFile bmdFile, ObservableCollection<BuildingProjectileEmitterViewModel> buildingProjectileEmitters, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.BuildingProjectileEmitters.Count; i++)
            {
                var buildingProjectileEmitter = bmdFile.BuildingProjectileEmitters[i];
                var vm = new BuildingProjectileEmitterViewModel(buildingProjectileEmitter);
                buildingProjectileEmitters.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadZonesTemplates(BmdFile bmdFile, ObservableCollection<ZonesTemplateViewModel> zonesTemplates, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var zonesTemplate in bmdFile.ZonesTemplates)
            {
                var vm = new ZonesTemplateViewModel(zonesTemplate);
                zonesTemplates.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadProps(BmdFile bmdFile, ObservableCollection<PropInfoViewModel> props, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.PropInfos.Count; i++)
            {
                var propInfo = bmdFile.PropInfos[i];
                var vm = new PropInfoViewModel(propInfo, propInfo.Rmv2Path);
                props.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadVfxInfos(BmdFile bmdFile, ObservableCollection<VfxInfoViewModel> vfxInfos, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.VfxInfos.Count; i++)
            {
                var vfx = bmdFile.VfxInfos[i];
                var vm = new VfxInfoViewModel(vfx);
                vfxInfos.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadPointLights(BmdFile bmdFile, ObservableCollection<PointLightInfoViewModel> pointLights, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.PointLights.Count; i++)
            {
                var light = bmdFile.PointLights[i];
                var vm = new PointLightInfoViewModel(light);
                pointLights.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadSpotLights(BmdFile bmdFile, ObservableCollection<SpotLightInfoViewModel> spotLights, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.SpotLights.Count; i++)
            {
                var light = bmdFile.SpotLights[i];
                var vm = new SpotLightInfoViewModel(light);
                spotLights.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadSounds(BmdFile bmdFile, ObservableCollection<SoundInfoViewModel> sounds, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            foreach (var sound in bmdFile.Sounds)
            {
                var vm = new SoundInfoViewModel(sound);
                sounds.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadPolyMeshes(BmdFile bmdFile, ObservableCollection<PolyMeshInfoViewModel> polyMeshes, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.PolyMeshes.Count; i++)
            {
                var mesh = bmdFile.PolyMeshes[i];
                var vm = new PolyMeshInfoViewModel(mesh);
                polyMeshes.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadLightProbes(BmdFile bmdFile, ObservableCollection<LightProbeInfoViewModel> lightProbes, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.LightProbes.Count; i++)
            {
                var probe = bmdFile.LightProbes[i];
                var vm = new LightProbeInfoViewModel(probe);
                lightProbes.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadTerrainHoles(BmdFile bmdFile, ObservableCollection<TerrainHoleInfoViewModel> terrainHoles, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.TerrainHoles.Count; i++)
            {
                var hole = bmdFile.TerrainHoles[i];
                var vm = new TerrainHoleInfoViewModel(hole);
                terrainHoles.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadPlayableAreas(BmdFile bmdFile, ObservableCollection<PlayableAreaViewModel> playableAreas, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            if (bmdFile.PlayableArea != null)
            {
                var vm = new PlayableAreaViewModel(bmdFile.PlayableArea);
                playableAreas.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadCscInfos(BmdFile bmdFile, ObservableCollection<CscInfoViewModel> cscInfos, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.CscInfos.Count; i++)
            {
                var csc = bmdFile.CscInfos[i];
                var vm = new CscInfoViewModel(csc);
                cscInfos.Add(vm);
                allElements.Add(vm);
            }
        }

        private static void LoadDeployments(BmdFile bmdFile, ObservableCollection<DeploymentViewModel> deployments, 
            ObservableCollection<BmdElementViewModel> allElements)
        {
            for (var i = 0; i < bmdFile.Deployments.Count; i++)
            {
                var deployment = bmdFile.Deployments[i];
                var vm = new DeploymentViewModel(deployment);
                deployments.Add(vm);
                allElements.Add(vm);
            }
        }

        #endregion
    }
}
