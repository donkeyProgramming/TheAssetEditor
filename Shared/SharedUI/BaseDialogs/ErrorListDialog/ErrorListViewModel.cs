// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Shared.Core.ErrorHandling;

namespace Shared.Ui.BaseDialogs.ErrorListDialog
{

    public partial class ErrorListViewModel
    {
        public ObservableCollection<ErrorListDataItem> ErrorItems { get; set; } = new ObservableCollection<ErrorListDataItem>();
        public string WindowTitle { get; set; } = "Error";



        /*   Dump to csv         // Entry : For each mount item:
            // Slot(id), status (OK, ERROR, MISSING IN RIDER), IsMountAnim, mount animation name, new rider animation name

            var csvLog = new List<object>();
            var mountSlots = MountLinkController.GetAllMountFragments();

            foreach (var mountFragment in mountSlots)
            {
                var riderFragment = MountLinkController.GetRiderFragmentFromMount(mountFragment);
                if (riderFragment == null)
                {
                    csvLog.Add(new { Status = "MISSING IN RIDER", MountSlot = mountFragment.Slot.ToString(),  MountAnimation = mountFragment.AnimationFile, RiderSlot = "", RiderAnimation = "" });
                    continue;
                }

                try
                {
                    var mountAnimPackFile = _pfs.FindFile(mountFragment.AnimationFile) ;
                    var mountAnim = new AnimationClip(AnimationFile.Create(mountAnimPackFile));

                    var riderAnimPackFile = _pfs.FindFile(riderFragment.AnimationFile) ;
                    var riderAnim = new AnimationClip(AnimationFile.Create(riderAnimPackFile));

                    var newRiderAnim = GenerateMountAnimation(mountAnim, _mount.Skeleton, riderAnim, _rider.Skeleton, _mountVertexOwner, _mountVertexes.First(), SelectedRiderBone.BoneIndex, SelectedRiderBone.ParentBoneIndex, AnimationSettings);
                    SaveAnimation(riderFragment.AnimationFile, newRiderAnim, _rider.Skeleton);

                    csvLog.Add(new { Status = "OK", MountSlot = mountFragment.Slot.ToString(), MountAnimation = mountFragment.AnimationFile, RiderSlot = riderFragment.Slot.ToString(), RiderAnimation = riderFragment.AnimationFile });
                }
                catch
                {
                    csvLog.Add(new { Status = "ERROR", MountSlot = mountFragment.Slot.ToString(), MountAnimation = mountFragment.AnimationFile, RiderSlot = riderFragment.Slot.ToString(), RiderAnimation = riderFragment.AnimationFile});
                }
            }


            ErrorListWindow.ShowDialog("Combine Errors", errorList);





            var fileName = $"C:\\temp\\{Path.GetFileNameWithoutExtension(MountLinkController.SeletedRider.DisplayName)}_log.csv";
            _logger.Here().Information("Batch export log can be found at - " + fileName);
            using var writer = new StreamWriter(fileName);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(csvLog);*/
    }
}
