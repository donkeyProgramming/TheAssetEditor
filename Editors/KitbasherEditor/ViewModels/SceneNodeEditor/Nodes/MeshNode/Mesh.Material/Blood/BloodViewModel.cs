﻿using CommunityToolkit.Mvvm.ComponentModel;
using GameWorld.Core.Rendering.Materials.Capabilities;
using GameWorld.Core.Utility.UserInterface;
using GameWorld.Core.WpfWindow.ResourceHandling;
using Microsoft.Xna.Framework;
using Shared.Core.Events;
using Shared.Core.PackFiles;
using Shared.Ui.BaseDialogs.MathViews;

namespace Editors.KitbasherEditor.ViewModels.SceneExplorer.Nodes.MeshSubViews
{
    public partial class BloodViewModel : ObservableObject
    {
        private readonly BloodCapability _bloodCapability;

        [ObservableProperty] bool _useBlood;
        [ObservableProperty] ShaderTextureViewModel _bloodMap;
        [ObservableProperty] Vector2ViewModel _bloodUvScale;
        [ObservableProperty] FloatViewModel _bloodPreview;

        public BloodViewModel(BloodCapability bloodCapability, IUiCommandFactory uiCommandFactory, PackFileService packFileService, ResourceLibrary resourceLibrary)
        {
            _bloodCapability = bloodCapability;

            _bloodMap = new ShaderTextureViewModel(bloodCapability.BloodMask, packFileService, uiCommandFactory, resourceLibrary);
            _useBlood = _bloodCapability.UseBlood;
            _bloodUvScale = new Vector2ViewModel(_bloodCapability.UvScale, OnBloodUvScaleChanged);
            _bloodPreview = new FloatViewModel(_bloodCapability.PreviewBlood, OnBloodPreviewChanged);
        }

        void OnBloodUvScaleChanged(Vector2 value) => _bloodCapability.UvScale = value;
        partial void OnUseBloodChanged(bool value) => _bloodCapability.UseBlood = value;
        void OnBloodPreviewChanged(float value) => _bloodCapability.PreviewBlood = value;
    }
}
