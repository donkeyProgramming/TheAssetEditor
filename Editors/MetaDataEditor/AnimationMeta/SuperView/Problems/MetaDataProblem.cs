using Editors.AnimationMeta.Presentation;
using Editors.AnimationMeta.SuperView.Timeline;
using Editors.AnimationMeta.SuperView.Visualisation;
using Shared.Core.Services;
using Shared.GameFormats.AnimationMeta.Parsing;

namespace Editors.AnimationMeta.SuperView.Problems
{
    /// <summary>
    /// One line in SuperView's problem list. An error stops the file saving; a warning is something the preview could not
    /// draw.
    /// </summary>
    public sealed record MetaDataProblem(MetaDataDocumentOwner Owner, ParsedMetadataAttribute Source, bool IsError, string Text)
    {
        /// <summary>
        /// The problems in both META files, in file order: fields that do not hold a valid value, times that are not numbers,
        /// and what the preview could not draw.
        /// After Szy-Cathay/TheAssetEditor-CN MetaDataInspectionIndex.CreateProblems (c55241a5b). Unlike the fork, negative,
        /// reversed and past-the-clip times are not problems: vanilla has 941 tags authored a few milliseconds before 0 and
        /// one reversed SPLICE.
        /// </summary>
        public static List<MetaDataProblem> Create(IEnumerable<MetaDataEntry> persistentTags, IEnumerable<MetaDataEntry> animationTags,
            IReadOnlyList<MetaDataBuildProblem> buildProblems, LocalizationManager localization)
        {
            var output = new List<MetaDataProblem>();
            var persistentName = localization.Get("MetadataEditor.Tabs.Persistent");
            var animationName = localization.Get("MetadataEditor.Tabs.Animation");

            void Add(IEnumerable<MetaDataEntry> tags, MetaDataDocumentOwner owner, string ownerName)
            {
                foreach (var tag in tags)
                {
                    var at = $"{ownerName} · {tag.DisplayName}: ";
                    foreach (var variable in tag.Variables.Where(x => !x.IsValid))
                        output.Add(new(owner, tag._input, true, at + string.Format(localization.Get("SuperView.Problems.InvalidField"), variable.FieldName)));

                    if (ParsedMetadataTimeRange.TryCreate(tag._input, out var range) && (!float.IsFinite(range.StartTime) || !float.IsFinite(range.EndTime)))
                        output.Add(new(owner, tag._input, false, at + localization.Get("SuperView.Problems.NonFiniteTime")));

                    foreach (var problem in buildProblems.Where(x => ReferenceEquals(x.Source, tag._input)))
                        output.Add(new(owner, tag._input, false, at + string.Format(localization.Get(problem.ReasonKey), problem.Detail)));
                }
            }

            Add(persistentTags, MetaDataDocumentOwner.Persistent, persistentName);
            Add(animationTags, MetaDataDocumentOwner.Animation, animationName);
            return output;
        }
    }
}
