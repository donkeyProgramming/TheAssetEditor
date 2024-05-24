// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xna.Framework;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Shared.GameFormats.AnimationMeta.Definitions
{
    [MetaData("FIRE_POS", 0)]
    public class FirePos_v0 : DecodedMetaEntryBase_v0
    {
        [MetaDataTag(2, "Measured from the unit's animroot bone, in meters.")]
        public Vector3 Position { get; set; }
    }

    [MetaData("FIRE_POS", 2)]
    public class FirePos_v2 : DecodedMetaEntryBase_v2
    {
        [MetaDataTag(4, "Measured from the unit's animroot bone, in meters.")]
        public Vector3 Position { get; set; }
    }

    [MetaData("FIRE_POS", 10)]
    public class FirePos_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5, "Measured from the unit's animroot bone, in meters.")]
        public Vector3 Position { get; set; }
    }
}


/*
 * 
 * 		<PROPERTY name="FIRE_POS" type="property_set">
			<PROPERTY name="Value" type="vector3" attribute_manipulator="p">0 2.02231622 -1.80137265</PROPERTY>
			<PROPERTY name="StartEndTime" type="float_range">0.0168807339 0.0168807339</PROPERTY>
			<PROPERTY name="Filter" type="string"></PROPERTY>
		</PROPERTY>
 * 
  "TableName": "FIRE_POS",
        "Version": 2,
        "ColumnDefinitions": [
          {
            "Name": "Version",
            "FieldReference": null,
            "TableReference": null,
            "IsKey": false,
            "IsOptional": false,
            "MaxLength": 0,
            "IsFileName": false,
            "Description": null,
            "FilenameRelativePath": null,
            "Type": "Integer"
          },
          {
            "Name": "StartTime",
            "FieldReference": null,
            "TableReference": null,
            "IsKey": false,
            "IsOptional": false,
            "MaxLength": 0,
            "IsFileName": false,
            "Description": null,
            "FilenameRelativePath": null,
            "Type": "Single"
          },
          {
            "Name": "EndTime",
            "FieldReference": null,
            "TableReference": null,
            "IsKey": false,
            "IsOptional": false,
            "MaxLength": 0,
            "IsFileName": false,
            "Description": null,
            "FilenameRelativePath": null,
            "Type": "Single"
          },
          {
            "Name": "Filter",
            "FieldReference": null,
            "TableReference": null,
            "IsKey": false,
            "IsOptional": false,
            "MaxLength": 0,
            "IsFileName": false,
            "Description": null,
            "FilenameRelativePath": null,
            "Type": "string"
          },
          {
            "Name": "Position",
            "FieldReference": null,
            "TableReference": null,
            "IsKey": false,
            "IsOptional": false,
            "MaxLength": 0,
            "IsFileName": false,
            "Description": null,
            "FilenameRelativePath": null,
            "Type": "Single"
          }
        ]
 */