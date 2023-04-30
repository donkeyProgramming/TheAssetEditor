using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace CommonControls.BaseDialogs.ErrorListDialog
{
  
    public class ErrorListViewModel
    {
        public ObservableCollection<ErrorListDataItem> ErrorItems { get; set; } = new ObservableCollection<ErrorListDataItem>();
        public string WindowTitle { get; set; } = "Error";

        [DebuggerDisplay("{ErrorType}:{ItemName}-{Description}")]
        public class ErrorListDataItem
        {
            public string ErrorType { get; set; }
            public string ItemName { get; set; }
            public string Description { get; set; }
            public bool IsError { get; set; } = false;
        }



        public class ErrorList
        {
            public List<ErrorListDataItem> Errors { get; set; } = new List<ErrorListDataItem>();

            public bool HasData { get => Errors.Count != 0; }

            public ErrorListDataItem Error(string itemName, string description)
            {
                var item = new ErrorListDataItem() { ErrorType = "Error", ItemName = itemName, Description = description, IsError = true };
                Errors.Add(item);
                return item;
            }

            public ErrorListDataItem Warning(string itemName, string description)
            {
                var item = new ErrorListDataItem() { ErrorType = "Warning", ItemName = itemName, Description = description, IsError = true };
                Errors.Add(item);
                return item;
            }

            public ErrorListDataItem Ok(string itemName, string description)
            {
                var item = new ErrorListDataItem() { ErrorType = "Ok", ItemName = itemName, Description = description };
                Errors.Add(item);
                return item;
            }

            public void AddAllErrors(ErrorList instanceErrorList)
            {
                foreach (var error in instanceErrorList.Errors)
                {
                    var item = new ErrorListDataItem() { ErrorType = error.ErrorType, ItemName = error.ItemName, Description = error.Description };
                    Errors.Add(item);
                }
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
