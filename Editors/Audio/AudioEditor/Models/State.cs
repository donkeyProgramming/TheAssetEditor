namespace Editors.Audio.AudioEditor.Models
{
    public class State : AudioProjectItem
    {
        public static State Create(string name)
        {
            return new State
            {
                Name = name
            };
        }
    }
}
