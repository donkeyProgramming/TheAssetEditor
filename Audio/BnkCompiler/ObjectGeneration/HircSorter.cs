using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircSorter
    {
        public List<IAudioProjectHircItem> Sort(CompilerData project)
        {
            // Sort
            var sortedProjectItems = new List<IAudioProjectHircItem>();
            var mixers = SortActorMixerList(project);
            foreach (var mixer in mixers)
            {
                var audioChildren = mixer.Sounds.Select(x => project.GameSounds.First(gameSound => gameSound.Name == x)).ToList();
                var sortedAudioChildren = audioChildren.OrderBy(x => GetSortingId(x)).ToList();

                sortedProjectItems.AddRange(sortedAudioChildren);
                sortedProjectItems.Add(mixer);
            }

            // Add Events
            var sortedEvents = project.Events.OrderBy(x => GetSortingId(x)).ToList();
            foreach (var currentEvent in sortedEvents)
            {
                var actions = currentEvent.Actions.Select(x => project.Actions.First(action => action.Name == x)).ToList();
                var sortedActions = actions.OrderBy(x => GetSortingId(x)).ToList();

                sortedProjectItems.AddRange(sortedActions);
                sortedProjectItems.Add(currentEvent);
            }

            //var sortedIds = sortedProjectItems.Select(x => $"{GetSortingId(x)}-{x.GetType().Name}").ToList();
            return sortedProjectItems;
        }

        uint GetSortingId(IAudioProjectHircItem item) => item.SerializationId;

        List<ActorMixer> SortActorMixerList(CompilerData project)
        {
            List<ActorMixer> output = new List<ActorMixer>();

            var mixers = project.ActorMixers;//.Shuffle().ToList(); // For testing

            // Find the root
            var roots = mixers.Where(x => HasReferences(x, mixers) == false).ToList();
            //Guard.IsEqualTo(roots.Count(), 1);
            
            foreach (var mixer in roots)
            {
                var children = mixer.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Name == childId)).ToList();
                output.Add(mixer);
                ProcessChildren(children, output, project);
            }

            output.Reverse();

            return output;
        }


        void ProcessChildren(List<ActorMixer> children, List<ActorMixer> outputList, CompilerData project)
        {
            var sortedChildren = children.OrderByDescending(x => GetSortingId(x)).ToList();

            outputList.AddRange(sortedChildren);
            foreach (var child in sortedChildren)
            {
                var childOfChildren = child.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Name == childId)).ToList();
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
                    .Any(x => x.ActorMixerChildren.Contains(currentMixer.Name));
                if (isReferenced)
                    return true;
            }
            return false;
        }


    }
}
