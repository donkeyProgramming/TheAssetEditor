using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using CommonControls.BaseDialogs;
using GameWorld.Core.Utility;
using Shared.Core.Misc;
using TextureEditor.Views;

namespace Editors.TextureEditor.ViewModels
{
    public class TexturePreviewViewModel : NotifyPropertyChangedImpl
    {
        ImageInformation _information;
        readonly List<bool> _formatCheckbox = new() { true, false, false, false, false };
        ImageSource[] _previewImage = new ImageSource[5];

        public NotifyAttr<ImageSource> ActiveImage { get; set; } = new NotifyAttr<ImageSource>();
        public NotifyAttr<ImageSource> CheckBoardImage { get; set; } = new NotifyAttr<ImageSource>();

        public NotifyAttr<string> ImagePath { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<string> Format { get; set; } = new NotifyAttr<string>();
        public NotifyAttr<int> Width { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<int> Height { get; set; } = new NotifyAttr<int>();
        public NotifyAttr<uint> NumMipMaps { get; set; } = new NotifyAttr<uint>();

        public bool FormatRgbaCheckbox { get => _formatCheckbox[0]; set => UpdateFormat(0, value); }
        public bool FormatRCheckbox { get => _formatCheckbox[1]; set => UpdateFormat(1, value); }
        public bool FormatGCheckbox { get => _formatCheckbox[2]; set => UpdateFormat(2, value); }
        public bool FormatBCheckbox { get => _formatCheckbox[3]; set => UpdateFormat(3, value); }
        public bool FormatACheckbox { get => _formatCheckbox[4]; set => UpdateFormat(4, value); }

        public ImageSource[] PreviewImage
        {
            get { return _previewImage; }
            set
            {
                _previewImage = value;
                NotifyPropertyChanged();
            }
        }

        void UpdateFormat(int index, bool value)
        {
            _formatCheckbox[index] = value;
            if (value == true)
                ActiveImage.Value = PreviewImage[index];
        }

        public void SetImageInformation(ImageInformation imageInformation)
        {
            _information = imageInformation;
            Width.Value = _information.Width;
            Height.Value = _information.Height;
            Format.Value = _information.Format.ToString();
            NumMipMaps.Value = _information.Header_MipMapCount;
        }

        public void ShowTextureDetailsInfo()
        {
            // MOve this to a general concept 
            var containingWindow = new ControllerHostWindow(false, ResizeMode.CanResize);
            containingWindow.Title = "Texture Details";
            containingWindow.Width = 550;
            containingWindow.Height = 600;
            containingWindow.Content = new TextureInformationView() { DataContext = _information.GetAsText() };
            containingWindow.ShowDialog();
        }

    }
}
