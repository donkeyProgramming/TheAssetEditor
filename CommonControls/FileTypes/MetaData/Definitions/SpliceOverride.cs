// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("SPLICE_OVERRIDE", 11)]
    public class SpliceOverride_v11 : Splice_v11
    {
    }

    //seems like it's basically the same as SPLICE, but with another boolean at the end
    [MetaData("SPLICE_OVERRIDE", 12)]
    public class SpliceOverride_v12 : Splice_v11
    {
        [MetaDataTag(21)]
        public string UnknownBool { get; set; } = "false";
    }
}
