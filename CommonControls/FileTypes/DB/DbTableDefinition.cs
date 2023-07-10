// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace CommonControls.FileTypes.DB
{
    public class DbTableDefinition
    {
        public string TableName { get; set; }
        public int Version { get; set; }
        public List<DbColumnDefinition> ColumnDefinitions { get; set; } = new List<DbColumnDefinition>();
    }
}
