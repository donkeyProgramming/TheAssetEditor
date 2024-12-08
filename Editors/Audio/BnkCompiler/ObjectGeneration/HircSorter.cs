using System.Collections.Generic;
using System.Linq;

namespace Editors.Audio.BnkCompiler.ObjectGeneration
{
    public class HircSorter
    {
        public static List<IAudioProjectHircItem> Sort(CompilerData project)
        {
            var sortedProjectItems = new List<IAudioProjectHircItem>();
            var addedItems = new HashSet<IAudioProjectHircItem>();
            var mixers = SortActorMixerList(project);

            foreach (var mixer in mixers)
            {
                var children = mixer.Children.ToList();
                children.Reverse();

                var mixerChildren = new List<IAudioProjectHircItem>();
                foreach (var child in children)
                {
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
                        continue;
                    }

                    // Find random containers with the child name and process.
                    foreach (var container in project.RandomContainers)
                    {
                        if (container.Id == child && !addedItems.Contains(container))
                        {
                            var containerChildren = new List<IAudioProjectHircItem>();
                            foreach (var containerchild in container.Children)
                            {
                                var gameSoundChild = project.Sounds.FirstOrDefault(sound => sound.Id == containerchild);
                                if (gameSoundChild != null && !addedItems.Contains(gameSoundChild))
                                    containerChildren.Add(gameSoundChild);
                            }

                            var sortedContainerChildren = containerChildren.OrderBy(child => child.Id).ToList();
                            foreach (var containerChild in sortedContainerChildren)
                            {
                                sortedProjectItems.Add(containerChild);
                                addedItems.Add(containerChild);
                            }

                            // Add the random container itself after its children.
                            sortedProjectItems.Add(container);
                            addedItems.Add(container);
                        }
                    }
                }

                foreach (var child in mixerChildren)
                {
                    if (!addedItems.Contains(child))
                    {
                        sortedProjectItems.Add(child);
                        addedItems.Add(child);
                    }
                }

                if (!addedItems.Contains(mixer))
                {
                    sortedProjectItems.Add(mixer);
                    addedItems.Add(mixer);
                }
            }

            var sortedEvents = project.Events.OrderBy(x => x).ToList();
            foreach (var currentEvent in sortedEvents)
            {
                var actions = currentEvent.Actions.Select(x => project.Actions.First(action => action.Id == x)).ToList();
                var sortedActions = actions.OrderBy(x => x).ToList();
                sortedProjectItems.AddRange(sortedActions);

                if (!addedItems.Contains(currentEvent))
                {
                    sortedProjectItems.Add(currentEvent);
                    addedItems.Add(currentEvent);
                }
            }

            var sortedDialogueEvents = project.DialogueEvents.OrderBy(x => x.Id).ToList();
            foreach (var currentDialogueEvent in sortedDialogueEvents)
                if (!addedItems.Contains(currentDialogueEvent))
                {
                    sortedProjectItems.Add(currentDialogueEvent);
                    addedItems.Add(currentDialogueEvent);
                }

            return sortedProjectItems;
        }

        static List<ActorMixer> SortActorMixerList(CompilerData project)
        {
            var output = new List<ActorMixer>();
            var mixers = project.ActorMixers;

            // Find the root.
            var roots = mixers.Where(x => HasReferences(x, mixers) == false).ToList();

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
