using Filetypes.ByteParsing;
using FileTypes.PackFiles.Models;
using FileTypes.Sound.WWise;
using FileTypes.Sound.WWise.Bkhd;
using FileTypes.Sound.WWise.Hirc;
using FileTypes.Sound.WWise.Stid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FileTypes.Sound
{

    public class Bnkparser
    {
        public static SoundDataBase Parse(PackFile file)
        {
            var chunk = file.DataSource.ReadDataAsChunk();

            var soundDb = new SoundDataBase();
            var parsers = new Dictionary<string, IParser>();
            parsers["BKHD"] = new BkhdParser();
            parsers["HIRC"] = new HircParser();
            parsers["STID"] = new StidParser();

            while (chunk.BytesLeft != 0)
            {
                var cc4 = Encoding.UTF8.GetString(chunk.ReadBytes(4));
                if(cc4 == "\0\0\0\0")
                    cc4 = Encoding.UTF8.GetString(chunk.ReadBytes(4));
                parsers[cc4].Parse(file.Name, chunk, soundDb);
            }

            return soundDb;
        }
    }
}

//https://wiki.xentax.com/index.php/Wwise_SoundBank_(*.bnk)#DIDX_section
//https://github.com/bnnm/wwiser/blob/cd5c086ef2c104e7133e361d385a1023408fb92f/wwiser/wmodel.py#L205
//https://github.com/Maddoxkkm/bnk-event-extraction
//https://github.com/vgmstream/vgmstream/blob/37cc12295c92ec6aa874118fb237bd3821970836/src/meta/bkhd.c
// https://github.com/admiralnelson/total-warhammer-RE-audio/blob/master/BnkExtract.py
// https://github.com/eXpl0it3r/bnkextr