// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Shared.Core.Misc;

namespace Shared.Ui.Common
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
