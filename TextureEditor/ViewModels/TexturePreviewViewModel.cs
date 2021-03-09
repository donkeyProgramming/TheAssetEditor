using Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace TextureEditor.ViewModels
{
    public class TexturePreviewViewModel : NotifyPropertyChangedImpl
    {
        ImageSource _image;
        public ImageSource Image
        {
            get { return _image; }
            set
            {
                _image = value;
                NotifyPropertyChanged();
            }
        }

        void UpdateFormat(int index, bool value)
        {
            for (int i = 0; i < 5; i++)
                _formatCheckbox[i] = false;

            _formatCheckbox[index] = value;
            NotifyPropertyChanged("FormatRgbaCheckbox");
            NotifyPropertyChanged("FormatRCheckbox");
            NotifyPropertyChanged("FormatGCheckbox");
            NotifyPropertyChanged("FormatBCheckbox");
            NotifyPropertyChanged("FormatACheckbox");
            Image = PreviewImage[index];
        }

        List<bool> _formatCheckbox = new List<bool>() { false, false, false, false, false };

        public bool FormatRgbaCheckbox
        {
            get { return _formatCheckbox[0]; }
            set
            {
                UpdateFormat(0, value);
            }
        }

        public bool FormatRCheckbox
        {
            get { return _formatCheckbox[1]; }
            set
            {
                UpdateFormat(1, value);
            }
        }

        public bool FormatGCheckbox
        {
            get { return _formatCheckbox[2]; }
            set
            {
                UpdateFormat(2, value);
            }
        }

        public bool FormatBCheckbox
        {
            get { return _formatCheckbox[3]; }
            set
            {
                UpdateFormat(3, value);
            }
        }

        public bool FormatACheckbox
        {
            get { return _formatCheckbox[4]; }
            set
            {
                UpdateFormat(4, value);
            }
        }

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


        string _imageName;
        public string Name
        {
            get { return _imageName; }
            set
            {
                _imageName = value;
                NotifyPropertyChanged();
            }
        }

        string _imageFormat;
        public string Format
        {
            get { return _imageFormat; }
            set
            {
                _imageFormat = value;
                NotifyPropertyChanged();
            }
        }

        int _width;
        public int Width
        {
            get { return _width; }
            set
            {
                _width = value;
                NotifyPropertyChanged();
            }
        }

        int _height;
        public int Height
        {
            get { return _height; }
            set
            {
                _height = value;
                NotifyPropertyChanged();
            }
        }
    }
}
