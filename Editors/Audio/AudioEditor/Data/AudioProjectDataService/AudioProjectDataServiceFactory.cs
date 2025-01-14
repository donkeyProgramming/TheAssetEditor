namespace Editors.Audio.AudioEditor.Data.AudioProjectDataService
{
    public static class AudioProjectDataServiceFactory
    {
        public static IAudioProjectDataService GetService(object selectedItem)
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
