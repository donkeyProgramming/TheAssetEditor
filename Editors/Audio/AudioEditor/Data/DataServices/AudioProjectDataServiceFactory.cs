namespace Editors.Audio.AudioEditor.Data.DataServices
{
    public class AudioProjectDataServiceFactory
    {
        public static IDataService GetDataService(object selectedItem)
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
