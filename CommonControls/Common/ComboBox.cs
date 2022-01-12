using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace CommonControls.Common
{
    public class ComboBox<T> : NotifyPropertyChangedImpl
    {
        public ObservableCollection<T> PossibleValues { get; set; } = new ObservableCollection<T>();
        public NotifyAttr<T> SelectedValue { get; set; } = new NotifyAttr<T>();

        public ComboBox(List<T> values, T selectedValue)
        {
            foreach (var item in values)
                PossibleValues.Add(item);
            SelectedValue.Value = selectedValue;
        }

        public ComboBox(T[] values, T selectedValue)
        {
            foreach (var item in values)
                PossibleValues.Add(item);
            SelectedValue.Value = selectedValue;
        }

        public T Value { get => SelectedValue.Value; }
    }
}
