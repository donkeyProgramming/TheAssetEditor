using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Shading.Capabilities;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.ColourPickerButton;

namespace Editors.KitbasherEditor.ViewModels.SceneNodeEditor.Nodes.MeshNode.Mesh.WsMaterial.Emissive
{
    public partial class EmissiveViewModel : ObservableObject
    {
        [ObservableProperty] ShaderTextureViewModel _emissiveTexture;
        [ObservableProperty] ShaderTextureViewModel _emissiveDistortionTexture;

        [ObservableProperty] float _testFloat;
        [ObservableProperty] ColourPickerViewModel _gradiant0;
        [ObservableProperty] ColourPickerViewModel _gradiant1;
        [ObservableProperty] ColourPickerViewModel _gradiant2;
        [ObservableProperty] ColourPickerViewModel _gradiant3;

        public EmissiveViewModel(EmissiveCapability emissiveCapability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _gradiant0 = new ColourPickerViewModel(new Vector3(0, 1, 0), OnGradientColour0Changed);
            _testFloat = 12.22f;
        }

        void OnGradientColour0Changed(Vector3 color)
        { }
    }
}
