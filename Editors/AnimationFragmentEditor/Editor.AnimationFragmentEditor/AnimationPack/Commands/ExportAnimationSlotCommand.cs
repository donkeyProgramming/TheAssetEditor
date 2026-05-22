using System.IO;
using System.Text;
using Shared.Core.Events;
using Shared.GameFormats.AnimationPack;

namespace Editors.AnimationFragmentEditor.AnimationPack.Commands
{
    public class ExportAnimationSlotCommand : IUiCommand
    {
        public void Warhammer3()
        {
            var slots = AnimationSlotTypeHelperWh3.Values.Select(x => x.Id + "\t\t" + x.Value).ToList();
            SaveAnimationSlotsToFile(slots);
        }

        public void Warhammer2()
        {
            var slots = DefaultAnimationSlotTypeHelper.Values.Select(x => x.Id + "\t\t" + x.Value).ToList();
            SaveAnimationSlotsToFile(slots);
        }

        void SaveAnimationSlotsToFile(List<string> slots)
        {
            using var dlg = new System.Windows.Forms.SaveFileDialog();
            dlg.Filter = "Text files(*.txt) | *.txt | All files(*.*) | *.* ";
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;
            string path = dlg.FileName;

            StringBuilder sb = new StringBuilder();
            foreach (var slot in slots)
                sb.AppendLine(slot);

            File.WriteAllText(path, sb.ToString());
        }

    }

}
