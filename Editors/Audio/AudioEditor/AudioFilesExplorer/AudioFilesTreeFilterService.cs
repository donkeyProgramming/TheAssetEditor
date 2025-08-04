using System;
using System.Collections.ObjectModel;

namespace Editors.Audio.AudioEditor.AudioFilesExplorer
{
    public interface IAudioFilesTreeSearchFilterService
    {
        void FilterTree(ObservableCollection<AudioFilesTreeNode> audioFilesTree, string query);
    }

    public class AudioFilesTreeFilterService : IAudioFilesTreeSearchFilterService
    {
        public void FilterTree(ObservableCollection<AudioFilesTreeNode> audioFilesTree, string query)
        {
            foreach (var rootNode in audioFilesTree)
                FilterTreeNode(rootNode, query);
        }

        private static bool FilterTreeNode(AudioFilesTreeNode node, string query)
        {
            var doesNodeContainQuery = node.FileName.Contains(query, StringComparison.OrdinalIgnoreCase);

            var isAnyChildVisible = false;
            foreach (var child in node.Children)
            {
                var childVisible = FilterTreeNode(child, query);
                if (childVisible)
                    isAnyChildVisible = true;
            }

            node.IsVisible = doesNodeContainQuery || isAnyChildVisible;
            return node.IsVisible;
        }
    }
}
