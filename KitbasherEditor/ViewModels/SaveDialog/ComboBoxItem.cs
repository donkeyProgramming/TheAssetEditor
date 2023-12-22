using System;
using System.Collections.Generic;
using View3D.Services.SceneSaving.Lod;
using View3D.Services.SceneSaving.Geometry;
using View3D.Services.SceneSaving.WsModel;

namespace KitbasherEditor.ViewModels.SaveDialog
{
    public class ComboBoxItem<T> where T : Enum
    {
        public T Value { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; } = "";

        public ComboBoxItem(T value, string displayName, string description) 
        {
            Value = value;
            DisplayName = displayName;
            Description = description;
        }
    }

    // Move to actual strategis, should get throug interface
    /*public static class LodStrategyHelper
    {
        public static List<ComboBoxItem<LodStrategy>> GetPossibilities()
        {
            return new List<ComboBoxItem<LodStrategy>>()
            {
                new ComboBoxItem<LodStrategy>(){ Value = LodStrategy.None, DisplayName = "None", Description = "Dont generate lods"},
                new ComboBoxItem<LodStrategy>(){ Value = LodStrategy.Default, DisplayName = "Default", Description = "Use AssetEdior Algorithm"},
                new ComboBoxItem<LodStrategy>(){ Value = LodStrategy.Simplygon, DisplayName = "SimplyGon", Description = "Use simplygon - requires external install"},
                new ComboBoxItem<LodStrategy>(){ Value = LodStrategy.Lod0ForAll, DisplayName = "Use Lod 0", Description = "Copy lod 0 to all other lods"}
            };
        }
    }

    public static class MeshStrategyHelper
    {
        public static List<ComboBoxItem<GeometryStrategy>> GetPossibilities()
        {
            return new List<ComboBoxItem<GeometryStrategy>>()
            {
                new ComboBoxItem<GeometryStrategy>(){ Value = GeometryStrategy.None, DisplayName = "None", Description = "Dont generate a mesh"},
                new ComboBoxItem<GeometryStrategy>(){ Value = GeometryStrategy.Rmv7, DisplayName = "Rmv7", Description = ""},
                new ComboBoxItem<GeometryStrategy>(){ Value = GeometryStrategy.Rmv6, DisplayName = "Rmv6", Description = ""},
            };
        }
    }


    public static class WsModelStrategyHelper
    {
        public static List<ComboBoxItem<MaterialStrategy>> GetPossibilities()
        {
            return new List<ComboBoxItem<MaterialStrategy>>()
            {
                new ComboBoxItem<MaterialStrategy>(){ Value = MaterialStrategy.None, DisplayName = "None", Description = "Dont generate a ws model"},
                new ComboBoxItem<MaterialStrategy>(){ Value = MaterialStrategy.WsModel_Warhammer2, DisplayName = "Warhammer2", Description = ""},
                new ComboBoxItem<MaterialStrategy>(){ Value = MaterialStrategy.WsModel_Warhammer3, DisplayName = "Warhammer3", Description = ""},
            };
        }
    }*/
}
