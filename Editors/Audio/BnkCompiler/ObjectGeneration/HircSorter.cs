using CommunityToolkit.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class HircSorter
    {
        public List<IAudioProjectHircItem> Sort(CompilerData project)
        {
            // Sort
            var sortedProjectItems = new List<IAudioProjectHircItem>();

            // HashSet to keep track of added items
            var addedItems = new HashSet<IAudioProjectHircItem>();

            // Add mixers and their children
            var mixers = SortActorMixerList(project);
            foreach (var mixer in mixers)
            {
                var children = mixer.Children.ToList();
                children.Reverse();

                // Create a list to store children
                var mixerChildren = new List<IAudioProjectHircItem>();

                // Iterate over the mixer children
                foreach (var child in children)
                {
                    // Find game sounds with the child name
                    IAudioProjectHircItem gameSound = null;
                    foreach (var sound in project.Sounds)
                    {
                        if (sound.Id == child)
                        {
                            gameSound = sound;
                            break;
                        }
                    }

                    if (gameSound != null)
                    {
                        mixerChildren.Add(gameSound);
                        continue; // Move to the next child
                    }

                    // Find random containers with the child name
                    foreach (var container in project.RandomContainers)
                    {
                        if (container.Id == child && !addedItems.Contains(container))
                        {
                            // Collect children of the random container
                            var containerChildren = new List<IAudioProjectHircItem>();

                            foreach (var containerchild in container.Children)
                            {
                                var gameSoundChild = project.Sounds.FirstOrDefault(sound => sound.Id == containerchild);
                                if (gameSoundChild != null && !addedItems.Contains(gameSoundChild))
                                {
                                    containerChildren.Add(gameSoundChild);
                                }
                            }

                            // Sort container children by key
                            var sortedContainerChildren = containerChildren.OrderBy(child => child.Id).ToList();

                            // Add container children to sortedProjectItems
                            foreach (var containerChild in sortedContainerChildren)
                            {
                                sortedProjectItems.Add(containerChild);
                                addedItems.Add(containerChild);
                            }

                            // Add the random container itself after its children
                            sortedProjectItems.Add(container);
                            addedItems.Add(container);
                        }
                    }
                }

                // Add mixer children
                foreach (var child in mixerChildren)
                {
                    if (!addedItems.Contains(child))
                    {
                        sortedProjectItems.Add(child);
                        addedItems.Add(child);
                    }
                }

                // Add current mixer
                if (!addedItems.Contains(mixer))
                {
                    sortedProjectItems.Add(mixer);
                    addedItems.Add(mixer);
                }
            }

            // Add Events and actions
            var sortedEvents = project.Events.OrderBy(x => x).ToList();
            foreach (var currentEvent in sortedEvents)
            {
                var actions = currentEvent.Actions.Select(x => project.Actions.First(action => action.Id == x)).ToList();
                var sortedActions = actions.OrderBy(x => x).ToList();

                sortedProjectItems.AddRange(sortedActions);

                // Add current event
                if (!addedItems.Contains(currentEvent))
                {
                    sortedProjectItems.Add(currentEvent);
                    addedItems.Add(currentEvent);
                }
            }

            // Add Dialogue Events
            var sortedDialogueEvents = project.DialogueEvents.OrderBy(x => x).ToList();
            foreach (var currentDialogueEvent in sortedDialogueEvents)
            {
                // Add current event
                if (!addedItems.Contains(currentDialogueEvent))
                {
                    sortedProjectItems.Add(currentDialogueEvent);
                    addedItems.Add(currentDialogueEvent);
                }
            }

            return sortedProjectItems;
        }

        static List<ActorMixer> SortActorMixerList(CompilerData project)
        {
            var output = new List<ActorMixer>();

            var mixers = project.ActorMixers;

            // Find the root
            var roots = mixers.Where(x => HasReferences(x, mixers) == false).ToList();
            Guard.IsEqualTo(roots.Count(), 1);

            foreach (var mixer in roots)
            {
                var children = mixer.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Id == childId)).ToList();
                output.Add(mixer);

                ProcessChildren(children, output, project);
            }

            output.Reverse();

            return output;
        }

        static void ProcessChildren(List<ActorMixer> children, List<ActorMixer> outputList, CompilerData project)
        {
            var sortedChildren = children.OrderByDescending(x => x).ToList();

            outputList.AddRange(sortedChildren);

            foreach (var child in sortedChildren)
            {
                var childOfChildren = child.ActorMixerChildren.Select(childId => project.ActorMixers.First(x => x.Id == childId)).ToList();
                ProcessChildren(childOfChildren, outputList, project);
            }
        }

        static bool HasReferences(ActorMixer currentMixer, List<ActorMixer> mixers)
        {
            foreach (var mixer in mixers)
            {
                if (mixer == currentMixer)
                    continue;

                var isReferenced = mixers
                    .Where(x => x != currentMixer)
                    .Any(x => x.ActorMixerChildren.Contains(currentMixer.Id));

                if (isReferenced)
                    return true;
            }
            return false;
        }
    }
}
