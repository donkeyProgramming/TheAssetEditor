using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Shared.Core.Misc;

namespace Shared.Ui.Common
{
    public class FilterCollection<T> : NotifyPropertyChangedImpl where T : class
    {
        public event ValueChangedDelegate<T> SelectedItemChanged;
        public event BeforeValueChangedDelegate<T> BeforeSelectedItemChanged;

        public List<T> PossibleValues { get; set; }

        ObservableCollection<T> _values;
        public ObservableCollection<T> Values { get => _values; set => SetAndNotify(ref _values, value); }

        T _selectedItem;
        public T? SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (BeforeSelectedItemChanged != null)
                {
                    var res = BeforeSelectedItemChanged.Invoke(value);
                    if (res == false)
                        return;
                }
                SetAndNotify(ref _selectedItem, value, SelectedItemChanged);
            }
        }

        // Filter stuff
        string _filter;
        public string Filter { get => _filter; set { SetAndNotify(ref _filter, value); FilterChanged(_filter); } }


        bool _filterValid = true;
        public bool FilterValid { get => _filterValid; set => SetAndNotify(ref _filterValid, value); }


        public delegate bool FilterDelegate(T value, Regex regex);
        public delegate void FilterExtendedDelegate(FilterCollection<T> sender, Regex regex);

        public FilterDelegate SearchFilter { get; set; } 
        public FilterExtendedDelegate SearchFilterExtended { get; set; }


        public FilterCollection(IEnumerable<T> data, ValueChangedDelegate<T> valueChangedEvent = null, BeforeValueChangedDelegate<T> beforeValueChangeEvent = null)
        {
            UpdatePossibleValues(data);
            if (valueChangedEvent != null)
                SelectedItemChanged += valueChangedEvent;

            if (beforeValueChangeEvent != null)
                BeforeSelectedItemChanged += beforeValueChangeEvent;
        }

        public void UpdatePossibleValues(IEnumerable<T>? data, T? emptyItem = null)
        {
            if (data == null)
            {
                PossibleValues = new List<T>();
                Values = new ObservableCollection<T>();
            }
            else
            {
                // TODO fix this so it works. Index maybe?
                var selectedItem = SelectedItem;
                PossibleValues = new List<T>(data);
                if (emptyItem != null)
                    PossibleValues.Insert(0, emptyItem);

                Values = new ObservableCollection<T>(PossibleValues);
                SelectedItem = selectedItem;
            }
        }

        public void RefreshFilter()
        {
            FilterChanged(_filter);
        }

        void FilterChanged(string filterValue)
        {
            try
            {
                if (filterValue == null)
                    filterValue = "";
                var rx = new Regex(filterValue, RegexOptions.Compiled | RegexOptions.IgnoreCase);

                if (SearchFilterExtended != null)
                {
                    SearchFilterExtended.Invoke(this, rx);
                    //Values = new ObservableCollection<T>(returnVals);
                }
                else
                {
                    var newValues = new List<T>(PossibleValues.Count);
                    foreach (var item in PossibleValues)
                    {
                        var addItem = false;
                        if (SearchFilter != null)
                            addItem = SearchFilter(item, rx);
                        else
                            addItem = rx.Match(item.ToString()).Success;

                        if (addItem)
                            newValues.Add(item);

                    }

                    Values = new ObservableCollection<T>(newValues);
                }

                FilterValid = true;
            }
            catch
            {
                FilterValid = false;
            }
        }
    }
}
