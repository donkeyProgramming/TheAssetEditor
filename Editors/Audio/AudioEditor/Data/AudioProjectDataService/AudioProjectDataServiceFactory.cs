namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public class AudioProjectDataServiceFactory
    {
        public static IAudioProjectDataService GetDataService(object selectedItem)
        {
            return selectedItem switch
            {
                SoundBank => new ActionEventDataService(),
                DialogueEvent => new DialogueEventDataService(),
                StateGroup => new StatesDataService(),
            };
        }
    }
}
