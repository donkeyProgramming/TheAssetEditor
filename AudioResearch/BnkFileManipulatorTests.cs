using CommonControls.Common;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommonControls.Services;
using Filetypes.ByteParsing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AudioResearch
{
    static class BnkFileManipulatorTests
    {
        public static void ExtractToOwnPack()
        {
            var pfs = ResearchUtil.GetPackFileService();
            var packFile = pfs.FindFile("audio\\wwise\\english(uk)\\battle_vo_conversational__core.bnk");

            BnkFileManipulator manipulator = new BnkFileManipulator();
            var bytes = manipulator.ExtractHircToOwnBnk(packFile.DataSource.ReadDataAsChunk(), 1577728457, "exctracted_event");

            File.WriteAllBytes("c:\\temp\\wwiseextracttest.bnk", bytes);
        }
    }
}
