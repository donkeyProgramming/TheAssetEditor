using System.Text;
using System.Text.Json;
using System.Windows;
using Shared.Core.ErrorHandling.Exceptions;

namespace Shared.Ui.Common.Exceptions
{
    public partial class CustomExceptionWindow : Window
    {
        private readonly ExceptionInformation _extendedExceptionInformation;

        public CustomExceptionWindow(ExceptionInformation extendedExceptionInformation)
        {
            InitializeComponent();
            _extendedExceptionInformation = extendedExceptionInformation;

            ErrorTextHandle.Text = string.Join("\n", extendedExceptionInformation.ExceptionMessage);

            var editorName = "";
            if (string.IsNullOrWhiteSpace(extendedExceptionInformation.CurrentEditorName) == false)
                editorName = extendedExceptionInformation.CurrentEditorName + " : ";
            Title = $"{editorName}Error - v{extendedExceptionInformation.AssetEditorVersion} {extendedExceptionInformation.CurrentGame}";

            var extraInfo = new StringBuilder();
            extraInfo.AppendLine("Packed Files:");
            foreach (var item in extendedExceptionInformation.ActivePackFiles)
                extraInfo.AppendLine($"\t'{item.Name}' @ '{item.SystemPath}' IsCa:{item.IsCa} IsMain:{item.IsMainEditable}");

            extraInfo.AppendLine($"Runtime: {extendedExceptionInformation.RunTimeInSeconds}");
            extraInfo.AppendLine($"OSVersion: {extendedExceptionInformation.OSVersion}");
            extraInfo.AppendLine($"Culture: {extendedExceptionInformation.Culture}");
            extraInfo.AppendLine($"Open editors: {extendedExceptionInformation.NumberOfOpenEditors}");
            extraInfo.AppendLine($"Total Created editors: {extendedExceptionInformation.NumberOfOpenedEditors}");

            ExtraInfoHandle.Text = extraInfo.ToString();
        }

        private void CopyButtonPressed(object sender, RoutedEventArgs e)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true,
            };
            var text = JsonSerializer.Serialize(_extendedExceptionInformation, options);
            Clipboard.SetText(text);
            MessageBox.Show("Error message copied to clipboard!");
        }

        private void CloseButtonPressed(object sender, RoutedEventArgs e) => Close();
    }
}
