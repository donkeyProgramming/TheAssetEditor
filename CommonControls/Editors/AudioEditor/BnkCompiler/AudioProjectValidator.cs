using CommonControls.BaseDialogs.ErrorListDialog;
using CommonControls.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonControls.Editors.AudioEditor.BnkCompiler
{
    public static class AudioProjectValidator
    {
        public static bool Validate(AudioProjectXml projectFile, PackFileService pfs, ref ErrorListViewModel.ErrorList errorList)
        {
            if (ValidateOutputPath(projectFile, ref errorList) == false)
                return false;

            if (ValidateAllObjects(projectFile, ref errorList) == false)
                return false;

            if (ValidateUniqueObjectIds(projectFile, ref errorList) == false)
                return false;

            if (ValidateReferences(projectFile, pfs, ref errorList) == false)
                return false;

            // Warning about unreferenced files 

            return true;
        }

        static bool ValidateOutputPath(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {
            if (string.IsNullOrEmpty(projectFile.OutputFile))
            {
                errorList.Error("OutputFile", "Output file is missing or invalid. Expecting MyFileName.bnk");
                return false;
            }

            return true;
        }

        static bool ValidateAllObjects(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {
            if (ValidateEvents(projectFile, ref errorList) == false)
                return false;

            if (ValidateActions(projectFile, ref errorList) == false)
                return false;

            if (ValidateGameSounds(projectFile, ref errorList) == false)
                return false;

            return true;
        }

        static bool ValidateEvents(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {
            foreach (var wwiseEvent in projectFile.Events)
            {
                if (string.IsNullOrEmpty(wwiseEvent.Id))
                {
                    errorList.Error("Event", "Event is missing ID");
                    return false;
                }

                if (string.IsNullOrEmpty(wwiseEvent.Action))
                {
                    errorList.Error("Event", $"{wwiseEvent.Id} is missing Action");
                    return false;
                }

                if (string.IsNullOrEmpty(wwiseEvent.AudioBus))
                {
                    errorList.Error("Event", $"{wwiseEvent.Id} is missing Audio buss");
                    return false;
                }
                else
                {
                    var validAudioBusses = new string[] { "battle" };
                    if (validAudioBusses.Contains(wwiseEvent.AudioBus.ToLower()) == false)
                    {
                        errorList.Error("Event", $"{wwiseEvent.Id} has an unkown audio buss");
                        return false;
                    }
                }
            }
            return true;
        }

        static bool ValidateActions(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {
            foreach (var action in projectFile.Actions)
            {
                if (string.IsNullOrEmpty(action.Id))
                {
                    errorList.Error("Action", "Action is missing ID");
                    return false;
                }

                if(action.ChildList == null || action.ChildList.Count == 0)
                {
                    errorList.Error("Action", $"{action.Id} is missing Children");
                    return false;
                }

                foreach (var child in action.ChildList)
                {
                    if (string.IsNullOrEmpty(child.Type))
                    {
                        errorList.Error("Action", $"{action.Id} contains a child missing ID");
                        return false;
                    }

                    if (child.Type.Equals("Play", StringComparison.OrdinalIgnoreCase) == false)
                    {
                        errorList.Error("Action", $"{action.Id} contains a child with invalid type. Only Play supported.");
                        return false;
                    }

                    if (string.IsNullOrEmpty(child.Text))
                    {
                        errorList.Error("Action", $"{action.Id} contains a child missing Key");
                        return false;
                    }

                }

               
            }
            return true;
        }

        static bool ValidateGameSounds(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {

            foreach (var gameSound in projectFile.GameSounds)
            {
                if (string.IsNullOrEmpty(gameSound.Id))
                {
                    errorList.Error("GameSound", "GameSound is missing ID");
                    return false;
                }

                if (string.IsNullOrEmpty(gameSound.Text))
                {
                    errorList.Error("GameSound", $"{gameSound.Id} is missing file path");
                    return false;
                }
            }
            return true;
        }


        static private bool ValidateReferences(AudioProjectXml projectFile, PackFileService pfs, ref ErrorListViewModel.ErrorList errorList)
        {
            var allIds = GetAllIds(projectFile);
            foreach (var wwiseEvent in projectFile.Events)
            {
                var actionKey = wwiseEvent.Action;
                if (projectFile.Actions.Any(x => string.Equals(x.Id, actionKey, StringComparison.InvariantCultureIgnoreCase) == false))
                {
                    errorList.Error("Event", $"Event {wwiseEvent.Id} is pointing to an invalid or unknown Action {actionKey}");
                    return false;
                }
            }

            foreach (var action in projectFile.Actions)
            {
                foreach (var child in action.ChildList)
                {
                    var actionKey = child.Text.ToLower().Trim();
                    if (allIds.Contains(actionKey) == false)
                    {
                        errorList.Error("Action", $"Action {action.Id} is pointing to an invalid or unknown object {actionKey}");
                        return false;
                    }
                }
            }

            foreach (var gameSound in projectFile.GameSounds)
            {
                var gameSoundPath = @"audio\wwise\" + gameSound.Text.ToLower().Trim();
                var fileRef = pfs.FindFile(gameSoundPath);
                if (fileRef == null)
                {
                    errorList.Error("GameSound", $"GameSound {gameSound.Id} not found. Reolve path: {gameSoundPath}");
                    return false;
                }
            }

            return true;
        }

        static private bool ValidateUniqueObjectIds(AudioProjectXml projectFile, ref ErrorListViewModel.ErrorList errorList)
        {
            var allIds = GetAllIds(projectFile);
            var query = allIds.GroupBy(x => x)
              .Where(g => g.Count() > 1)
              .Select(y => new { Element = y.Key, Counter = y.Count() })
              .ToList();

            var duplicates = query.Where(x => x.Counter != 1).ToList();
            foreach (var duplicate in duplicates)
                errorList.Error("Duplicate key", $"{duplicate.Element} is used {duplicate.Counter} times. Ids must be unique");

            return !duplicates.Any();
        }

        private static List<string> GetAllIds(AudioProjectXml projectFile)
        {
            var allIds = new List<string>();
            allIds.AddRange(projectFile.Events.Select(x => x.Id));
            allIds.AddRange(projectFile.Actions.Select(x => x.Id));
            allIds.AddRange(projectFile.GameSounds.Select(x => x.Id));
            allIds = allIds.Select(x => x.ToLower().Trim()).ToList();
            return allIds;
        }

    }
}
