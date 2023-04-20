using Audio.Utility;
using CommonControls.Editors.AudioEditor.BnkCompiler;
using CommunityToolkit.Diagnostics;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircSorter
    {
        public List<IAudioProjectHircItem> Sort(AudioInputProject project) 
        {
            // Sort
            var sortedProjectItems = new List<IAudioProjectHircItem>();
            var mixers = SortActorMixerList(project);
            foreach (var mixer in mixers)
            {
                var audioChildren = mixer.Sounds.Select(x => project.GameSounds.First(gameSound => gameSound.Id == x)).ToList();
                var sortedAudioChildren = audioChildren.OrderBy(x => GetSortingId(x)).ToList();

                sortedProjectItems.AddRange(sortedAudioChildren);
                sortedProjectItems.Add(mixer);
            }

            // Add Events
            var sortedEvents = project.Events.OrderBy(x => GetSortingId(x)).ToList();
            foreach (var currentEvent in sortedEvents)
            {
                var actions = currentEvent.Actions.Select(x => project.Actions.First(action => action.Id == x)).ToList();
                var sortedActions = actions.OrderBy(x => GetSortingId(x)).ToList();

                sortedProjectItems.AddRange(sortedActions);
                sortedProjectItems.Add(currentEvent);
            }

            //var sortedIds = sortedProjectItems.Select(x => $"{GetSortingId(x)}-{x.GetType().Name}").ToList();
            return sortedProjectItems;
        }

        uint GetSortingId(IAudioProjectHircItem item) 
        {
            if (item.OverrideId != 0)
                return item.OverrideId;
            return WWiseHash.Compute(item.Id);
        }

        List<ActorMixer> SortActorMixerList(AudioInputProject project)
        {
            List<ActorMixer> output = new List<ActorMixer>();

            var mixers = project.ActorMixers.Shuffle().ToList(); // For testing

            // Find the root
            var roots = mixers.Where(x => HasReferences(x, mixers) == false).ToList();
            Guard.IsEqualTo(roots.Count(), 1);
            var root = roots.First();
            output.Add(root);

            var children = root.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Id == childId)).ToList();
            ProcessChildren(children, output, project);

            output.Reverse();

            return output;
        }


        void ProcessChildren(List<ActorMixer> children, List<ActorMixer> outputList, AudioInputProject project)
        {
            var sortedChildren = children.OrderByDescending(x => GetSortingId(x)).ToList();

            outputList.AddRange(sortedChildren);
            foreach (var child in sortedChildren)
            {
                var childOfChildren = child.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Id == childId)).ToList();
                ProcessChildren(childOfChildren, outputList, project);
            }
        }

        bool HasReferences(ActorMixer currentMixer, List<ActorMixer> mixers)
        {
            foreach (var mixer in mixers)
            {
                if (mixer == currentMixer)
                    continue;

                bool isReferenced = mixers
                    .Where(x => x != currentMixer)
                    .Any(x => x.ActorMixerChildren.Contains(currentMixer.Id));
                if (isReferenced)
                    return true;
            }
            return false;
        }


    }
}
