using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Audio.BnkCompiler.ObjectGeneration
{
    public class HircSorter
    {
        public List<IAudioProjectHircItem> Sort(CompilerData project)
        {
            if (project.RandomContainers.Count() != 0)
            {
                // Sort
                var sortedProjectItems = new List<IAudioProjectHircItem>();

                // Add mixers and random containers
                var mixers = SortActorMixerList(project);
                foreach (var mixer in mixers)
                {
                    //var mixerChildren = mixer.Children.Select(x => project.RandomContainers.First(randomContainer => randomContainer.Name == x)).ToList();
                    //var sortedMixerChildren = mixerChildren.OrderBy(x => GetSortingId(x)).ToList();

                    //sortedProjectItems.AddRange(sortedMixerChildren);
                    sortedProjectItems.Add(mixer);
                }

                // Add Events and actions
                var sortedEvents = project.Events.OrderBy(x => GetSortingId(x)).ToList();
                foreach (var currentEvent in sortedEvents)
                {
                    var actions = currentEvent.Actions.Select(x => project.Actions.First(action => action.Name == x)).ToList();
                    var sortedActions = actions.OrderBy(x => GetSortingId(x)).ToList();

                    sortedProjectItems.AddRange(sortedActions);
                    sortedProjectItems.Add(currentEvent);
                }

                // Add random containers and sounds
                var sortedRandomContainers = project.RandomContainers.OrderBy(x => GetSortingId(x)).ToList();
                foreach (var currentRandomContainer in sortedRandomContainers)
                {
                    var randomContainerChildren = currentRandomContainer.Children.Select(x => project.GameSounds.First(gameSound => gameSound.Name == x)).ToList();
                    var sortedrandomContainerChildren = randomContainerChildren.OrderBy(x => GetSortingId(x)).ToList();

                    sortedProjectItems.AddRange(sortedrandomContainerChildren); 
                    sortedProjectItems.Add(currentRandomContainer); 
                }

                return sortedProjectItems;
            }
            else
            {
                // Sort
                var sortedProjectItems = new List<IAudioProjectHircItem>();

                // Add mixers and sounds
                var mixers = SortActorMixerList(project);
                foreach (var mixer in mixers)
                {
                    var mixerChildren = mixer.Children.Select(x => project.GameSounds.First(gameSound => gameSound.Name == x)).ToList();
                    var sortedMixerChildren = mixerChildren.OrderBy(x => GetSortingId(x)).ToList();

                    sortedProjectItems.AddRange(sortedMixerChildren);
                    sortedProjectItems.Add(mixer);
                }

                // Add Events and actions
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
