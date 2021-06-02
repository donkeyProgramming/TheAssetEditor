using Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;

namespace CommonControls.Common
{
    public class FilterCollection<T> : NotifyPropertyChangedImpl
    {
        List<T> _possibleValues;

        ObservableCollection<T> _values;
        public ObservableCollection<T> Values { get => _values; set => SetAndNotify(ref _values, value); }

        T _selectedItem;
        public T SelectedItem { get => _selectedItem; set => SetAndNotify<T>(ref _selectedItem, value, SelectedItemChanged); }
        public event ValueChangedDelegate<T> SelectedItemChanged;


        // Filter stuff
        string _filter;
        public string Filter { get => _filter; set { SetAndNotify(ref _filter, value); FilterChanged(_filter); } }


        bool _filterValid = true;
        public bool FilterValid { get => _filterValid; set => SetAndNotify(ref _filterValid, value); }


        public delegate bool FilterDelegate(T value, Regex regex);

        public FilterDelegate SearchFilter { get; set; }


        public FilterCollection(IEnumerable<T> data, ValueChangedDelegate<T> valueChangedEvent = null)
        {
            _possibleValues = new List<T>(data);
            Values = new ObservableCollection<T>(_possibleValues);
            if (valueChangedEvent != null)
                SelectedItemChanged += valueChangedEvent;
        }

        void FilterChanged(string filterValue)
        {
            try
            {
                var rx = new Regex(filterValue, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                var newValues = new List<T>(_possibleValues.Count);
                foreach (var item in _possibleValues)
                {
                    bool addItem = false;
                    if (SearchFilter != null)
                        addItem = SearchFilter(item, rx);
                    else
                        addItem = rx.Match(item.ToString()).Success;

                    if (addItem)
                        newValues.Add(item);

                }
                FilterValid = true;
                Values = new ObservableCollection<T>(newValues);
            }
            catch
            {
                FilterValid = false;
            }
        }
    }
}
