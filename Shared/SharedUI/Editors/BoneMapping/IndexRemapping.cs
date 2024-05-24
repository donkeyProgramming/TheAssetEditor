// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Shared.Ui.Editors.BoneMapping
{
    public class IndexRemapping
    {
        public IndexRemapping(int originalValue, int newValue, bool isUsedByCurrentModel = false)
        {
            OriginalValue = originalValue;
            NewValue = newValue;
            IsUsedByModel = isUsedByCurrentModel;
        }

        public int OriginalValue { get; set; }
        public int NewValue { get; set; }
        public bool IsUsedByModel { get; set; } = false;
    }
}
