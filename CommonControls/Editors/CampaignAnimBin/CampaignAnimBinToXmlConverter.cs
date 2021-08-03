using CommonControls.Editors.TextEditor;
using Filetypes.ByteParsing;
using FileTypes.AnimationPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CommonControls.Editors.CampaignAnimBin
{
    class CampaignAnimBinToXmlConverter : ITextConverter
    {
        public string GetText(byte[] bytes)
        {

            var bin = CampaignAnimationBinLoader.LoadStuff(new ByteChunk(bytes));





            /*
            var testBin = new CampaignAnimationBin2();
            testBin.SkeletonName = "bird01";
            testBin.Version = 3;

            var normalStatus = new CampaignAnimationBin2.StatusItem() { Name = "Normal" };
            normalStatus.Idle.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "walk01.anim", MetaData = "walk01.meta", MetaDataSound = "walk01.snd", BlendTime = 0, Weight = 1 });
            normalStatus.Idle.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "walk02.anim", MetaData = "walk02.meta", MetaDataSound = "walk02.snd", BlendTime = 0, Weight = 1 });
            normalStatus.Idle.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "walk03.anim", MetaData = "walk03.meta", MetaDataSound = "walk03.snd", BlendTime = 0, Weight = 1 });



            normalStatus.Selection.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "Select01.anim", MetaData = "Select01.meta", MetaDataSound = "Select01.snd", BlendTime = 0, Weight = 1 });
            normalStatus.Selection.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "Select02.anim", MetaData = "Select02.meta", MetaDataSound = "Select02.snd", BlendTime = 0, Weight = 1 });
            normalStatus.Selection.Add(new CampaignAnimationBin2.AnimationEntry() { Animation = "Select03.anim", MetaData = "Select03.meta", MetaDataSound = "Select03.snd", BlendTime = 0, Weight = 1 });


            testBin.Status.Add(normalStatus);


            var actionStatus = new CampaignAnimationBin2.StatusItem() { Name = "Battle" };
            testBin.Status.Add(actionStatus);






            var bin = new CampaignAnimationBin(new ByteChunk(bytes));*/

            var tmpStr = "";
            try
            {
                var xmlserializer = new XmlSerializer(typeof(CampaignAnimationBin));
                var stringWriter = new StringWriter();
                using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() { Indent = true }))
                {
                    xmlserializer.Serialize(writer, bin);
                    tmpStr =  stringWriter.ToString();
                    return tmpStr;
                } 
            }
            catch (Exception ex)
            {
                var s = ex.InnerException;
                throw new Exception("An error occurred", ex);
            }

            

            throw new NotImplementedException();
        }

        public byte[] ToBytes(string text)
        {
            throw new NotImplementedException();
        }

        public bool Validate(string text, out string errorText)
        {
            throw new NotImplementedException();
        }
    }
}
