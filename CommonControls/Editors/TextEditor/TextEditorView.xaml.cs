using System;
using System.Collections.Generic;
using System.Linq;
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
	public interface ITextEditor
	{
		void ClearUndoStack();
		void SetSyntaxHighlighting(string type);
		void ShowLineNumbers(bool value);
		void HightLightText(int lineNumber, int offset, int length);
	}

    /// <summary>
    /// Interaction logic for TextEditorView.xaml
    /// </summary>
    public partial class TextEditorView : UserControl, ITextEditor
	{
        FoldingManager _foldingManager;
        object _foldingStrategy;

        public TextEditorView()
        {
            InitializeComponent();

			DataContextChanged += TextEditorView_DataContextChanged;

			SetValue(TextOptions.TextFormattingModeProperty, TextFormattingMode.Display);
			SearchPanel.Install(textEditor);
			
			DispatcherTimer foldingUpdateTimer = new DispatcherTimer();
			foldingUpdateTimer.Interval = TimeSpan.FromSeconds(2);
			foldingUpdateTimer.Tick += delegate { UpdateFoldings(); };
			foldingUpdateTimer.Start();
		}

		public void ClearUndoStack()
		{
			textEditor.Document.UndoStack.ClearAll();
		}

		public void SetSyntaxHighlighting(string type)
		{
			var xmlHightlight = HighlightingManager.Instance.HighlightingDefinitions.FirstOrDefault(x => x.Name == type);
			highlightingComboBox.SelectedValue = xmlHightlight;
		}

		public void ShowLineNumbers(bool value)
		{
			textEditor.ShowLineNumbers = value;
		}

		public void HightLightText(int lineNumber, int offset, int length)
		{
			var line = textEditor.Document.GetLineByNumber(lineNumber);
			textEditor.Select(line.Offset + offset, length);
			textEditor.ScrollTo(lineNumber, 0);
		}

		private void TextEditorView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
			if (DataContext is ITextEditorViewModel typedViewModel)
			{
				typedViewModel.SetEditor(this);
			}
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
