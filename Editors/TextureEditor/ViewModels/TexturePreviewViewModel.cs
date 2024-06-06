using CommonControls.BaseDialogs;
using Shared.Core.Misc;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using TextureEditor.Views;
using View3D.Utility;

namespace TextureEditor.ViewModels
{
    public class TexturePreviewViewModel : NotifyPropertyChangedImpl
    {
        ImageInformation _information;
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

        readonly List<bool> _formatCheckbox = new List<bool>() { false, false, false, false, false };
        ImageSource[] _previewImage = new ImageSource[5];

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
            for (var i = 0; i < 5; i++)
                _formatCheckbox[i] = false;

            _formatCheckbox[index] = value;
            NotifyPropertyChanged("FormatRgbaCheckbox");
            NotifyPropertyChanged("FormatRCheckbox");
            NotifyPropertyChanged("FormatGCheckbox");
            NotifyPropertyChanged("FormatBCheckbox");
            NotifyPropertyChanged("FormatACheckbox");
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
