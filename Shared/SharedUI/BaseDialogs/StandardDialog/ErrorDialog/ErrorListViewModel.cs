// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.ErrorListDialog
{

    public partial class ErrorListViewModel
    {
        public ObservableCollection<ErrorListDataItem> ErrorItems { get; set; } = new ObservableCollection<ErrorListDataItem>();
        public string WindowTitle { get; set; } = "Error";

    }
}
