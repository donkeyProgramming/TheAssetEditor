using GalaSoft.MvvmLight.CommandWpf;
using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace AnimMetaEditor.ViewModels.Editor
{
    public class MetaDataTagEditorViewModel
    {
        MetaDataViewModel _data;

        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }
        public ICommand CopyCommand { get; set; }
        public ICommand PasteCommand { get; set; }
        public ICommand DeleteCommand { get; set; }
        public ICommand NewCommand { get; set; }

        public MetaDataTagEditorViewModel(MetaDataViewModel data)
        {
            _data = data;

            MoveUpCommand = new RelayCommand(MoveUp);
            MoveDownCommand = new RelayCommand(MoveDown);
        }

        void MoveUp()
        {
            var itemToMove = _data.SelectedTag;

            if (itemToMove == null)
                return;
            var currentIndex = _data.Tags.IndexOf(itemToMove);
            if (currentIndex == 0)
                return;
            
            _data.Tags.Remove(itemToMove);
            _data.Tags.Insert(currentIndex - 1, itemToMove);

            _data.SelectedTag = itemToMove;
        }

        void MoveDown()
        {
            var itemToMove = _data.SelectedTag;

            if (itemToMove == null)
                return;
            var currentIndex = _data.Tags.IndexOf(itemToMove);
            if (currentIndex == _data.Tags.Count -1)
                return;

            _data.Tags.Remove(itemToMove);
            _data.Tags.Insert(currentIndex + 1, itemToMove);

            _data.SelectedTag = itemToMove;
        }
    }
}

