using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.ErrorListDialog
{
    public class ErrorListViewModel
    {
        public List<ErrorListDataItem> ErrorItems { get; set; }
        public string WindowTitle { get; set; } = "Error";

        public class ErrorListDataItem
        { 
            public string ErrorType { get; set; }
            public string ItemName { get; set; }
            public string Description { get; set; }
            public bool IsError { get; set; } = false;

            public static ErrorListDataItem Error(string itemName, string description)
            {
                return new ErrorListDataItem() { ErrorType = "Error", ItemName = itemName, Description = description, IsError=true };
            }

            public static ErrorListDataItem Warning(string itemName, string description)
            {
                return new ErrorListDataItem() { ErrorType = "Warning", ItemName = itemName, Description = description, IsError = true };
            }

            public static ErrorListDataItem Ok(string itemName, string description)
            {
                return new ErrorListDataItem() { ErrorType = "Ok", ItemName = itemName, Description = description };
            }
        }

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
                    var mountAnimPackFile = _pfs.FindFile(mountFragment.AnimationFile) as PackFile;
                    var mountAnim = new AnimationClip(AnimationFile.Create(mountAnimPackFile));

                    var riderAnimPackFile = _pfs.FindFile(riderFragment.AnimationFile) as PackFile;
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
