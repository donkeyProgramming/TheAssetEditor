//using CommonControls.Common.MenuSystem;
//using KitbasherEditor.ViewModels.MenuBarViews;
//using System;
//using View3D.Services;

namespace KitbasherEditor.ViewModels.UiCommands
{
    //public class UpdateWh2TexturesCommand : IKitbasherUiCommand
    //{
    //    public string ToolTip
    //    {
    //        get => $"Update wh2 textures using {Technique}";
    //        set=> throw new NotImplementedException();
    //    }
    //    public ActionEnabledRule EnabledRule => ActionEnabledRule.Always;
    //    public Hotkey HotKey { get; } = null;
    //
    //    public UpdateWh2TexturesCommand()
    //    {
    //
    //    }
    //
    //    public Rmv2UpdaterService.BaseColourGenerationTechniqueEnum Technique { get; set; }
    //
    //    public void Execute() => UpdateWh2ModelAndConvert(Technique);
    //    
    //    internal void UpdateWh2ModelAndConvert(Rmv2UpdaterService.BaseColourGenerationTechniqueEnum conversionTechnique)
    //    {
    //        throw new NotImplementedException();
    //        //var res = MessageBox.Show("Are you sure you want to update the model? This cannot be undone!", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning);
    //        //if (res != MessageBoxResult.Yes)
    //        //    return;
    //        //
    //        //var rootNode = _sceneManager.GetNodeByName<MainEditableNode>(SpecialNodes.EditableModel);
    //        //var lods = rootNode.GetLodNodes();
    //        //var firtLod = lods.First();
    //        //var meshList = firtLod.GetAllModelsGrouped(false).SelectMany(x => x.Value).ToList();
    //        //var filename = _packFileService.GetFullPath(rootNode.MainPackFile);
    //        //
    //        //var service = new Rmv2UpdaterService(_packFileService, true);
    //        //service.UpdateWh2Models(filename, meshList, conversionTechnique, out var errorList);
    //        //
    //        //ErrorListWindow.ShowDialog("Converter", errorList);
    //    }
    //}
}
