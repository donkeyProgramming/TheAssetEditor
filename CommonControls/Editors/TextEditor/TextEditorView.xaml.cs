using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Search;

namespace CommonControls.Editors.TextEditor
{
    /// <summary>
    /// Interaction logic for TextEditorView.xaml
    /// </summary>
    public partial class TextEditorView : UserControl
    {
        FoldingManager _foldingManager;
        object _foldingStrategy;

        public TextEditorView()
        {
            InitializeComponent();

			SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);

			SearchPanel.Install(textEditor);

			DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
			foldingUpdateTimer.Start();
		}

		void HighlightingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			textEditor.SyntaxHighlighting = highlightingComboBox.SelectedValue as IHighlightingDefinition;
			if (textEditor.SyntaxHighlighting == null)
			{
				_foldingStrategy = null;
			}
			else
			{
				switch (textEditor.SyntaxHighlighting.Name)
				{
					case "XML":
						_foldingStrategy = new XmlFoldingStrategy();
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
						break;
					case "C#":
					case "C++":
					case "PHP":
					case "Java":
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.CSharp.CSharpIndentationStrategy(textEditor.Options);
						_foldingStrategy = new BraceFoldingStrategy();
						break;
					default:
						textEditor.TextArea.IndentationStrategy = new ICSharpCode.AvalonEdit.Indentation.DefaultIndentationStrategy();
						_foldingStrategy = null;
						break;
				}
			}
			if (_foldingStrategy != null)
			{
				if (_foldingManager == null)
					_foldingManager = FoldingManager.Install(textEditor.TextArea);
				UpdateFoldings();
			}
			else
			{
				if (_foldingManager != null)
				{
					FoldingManager.Uninstall(_foldingManager);
					_foldingManager = null;
				}
			}
		}

		void UpdateFoldings()
		{
			if (_foldingStrategy is BraceFoldingStrategy)
			{
				((BraceFoldingStrategy)_foldingStrategy).UpdateFoldings(_foldingManager, textEditor.Document);
			}
			if (_foldingStrategy is XmlFoldingStrategy)
			{
				((XmlFoldingStrategy)_foldingStrategy).UpdateFoldings(_foldingManager, textEditor.Document);
			}
		}
	}
}
