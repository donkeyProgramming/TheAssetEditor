// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommonControls.Common;
using CommonControls.FileTypes.MetaData;

namespace CommonControls.Editors.AnimMeta
{
    public class MetaDataTagCopyItem : ICopyPastItem
    {
        public string Description { get; set; } = "Copy object for MetaDataTag";
        public UnknownMetaEntry Data { get; set; }
    }
}
