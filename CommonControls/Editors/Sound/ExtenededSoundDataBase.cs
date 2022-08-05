using CommonControls.FileTypes.Sound.WWise;
using System.Collections.Generic;

namespace CommonControls.Editors.Sound
{
    public class ExtenededSoundDataBase
    {
        public List<uint> ReferensedSounds { get; set; } = new List<uint>();
        public List<string> UnknownObjectTypes { get; set; } = new List<string>();
        public List<uint> IdsNotFound { get; set; } = new List<uint>();
        Dictionary<uint, List<HircItem>> _hircList { get; set; } = new Dictionary<uint, List<HircItem>>();

        public Dictionary<uint, List<HircItem>> HircList { get => _hircList; }

        public void AddHircItems(List<HircItem> hircList)
        {
            foreach (var item in hircList)
            {
                if (_hircList.ContainsKey(item.Id) == false)
                    _hircList[item.Id] = new List<HircItem>();

                _hircList[item.Id].Add(item);
            }
        }

        public List<HircItem> GetHircObject(uint id, string bnkFile, VisualEventOutputNode _errorNode)
        {
            if (_hircList.ContainsKey(id))
                return _hircList[id];

            _errorNode.AddChild($"Item with id {id} not found");
            return new List<HircItem>();
        }

        public List<HircItem> GetHircObject(uint id)
        {
            if (_hircList.ContainsKey(id))
                return _hircList[id];

            return new List<HircItem>();
        }
    }
}

/*
 * 
 * Final test, add a new sound in meta tabel Karl franze running : "Look at me....Wiiiii" 
 * Vocalisation_dlc14_medusa_idle_hiss
 * 
    event > action > sound > .wem
    event > action > random-sequence > sound(s) > .wem
    event > action > switch > switch/segment/sound > ...
    event > action > music segment > music track(s) > .wem(s).
    event > action > music random-sequence > music segment(s) > ...
    event > action > music switch > switch(es)/segment(s)/random-sequence(s) > ...


    Event => action     =>  sound
                        =>  CAkActionSetAkProp
                        =>  Switch  => sound
                                    => Rand

                        =>  Rand    => Sound
 */


