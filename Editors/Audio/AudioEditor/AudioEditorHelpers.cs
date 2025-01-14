using System.Collections.Generic;

namespace Editors.Audio.AudioEditor
{
    public static class AudioEditorHelpers
    {
        // Apparently WPF doesn't_like_underscores so double them up in order for them to be displayed in the UI
        public static string AddExtraUnderscoresToString(string wtfWPF)
        {
            return wtfWPF.Replace("_", "__");
        }

        public static string RemoveExtraUnderscoresFromString(string wtfWPF)
        {
            return wtfWPF.Replace("__", "_");
        }

        public static string GetStateGroupFromStateGroupWithQualifier(string dialogueEvent, string stateGroupWithQualifier, Dictionary<string, Dictionary<string, string>> dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository)
        {
            if (dialogueEventsWithStateGroupsWithQualifiersAndStateGroupsRepository.TryGetValue(dialogueEvent, out var stateGroupDictionary))
                if (stateGroupDictionary.TryGetValue(stateGroupWithQualifier, out var stateGroup))
                    return stateGroup;

            return null;
        }
    }
}
