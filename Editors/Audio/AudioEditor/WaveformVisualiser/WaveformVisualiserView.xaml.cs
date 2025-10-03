using System.Windows.Controls;

namespace Editors.Audio.AudioEditor.WaveformVisualiser
{
    public partial class WaveformVisualiserView : UserControl
    {
        public WaveformVisualiserViewModel ViewModel => DataContext as WaveformVisualiserViewModel;

        public WaveformVisualiserView()
        {
            InitializeComponent();

            Loaded += (s, e) => (DataContext as WaveformVisualiserViewModel)?.SetSelectedHostWidth(AudioWaveformGrid.ActualWidth);
            AudioWaveformGrid.SizeChanged += (s, e) => (DataContext as WaveformVisualiserViewModel)?.SetSelectedHostWidth(AudioWaveformGrid.ActualWidth);
        }
    }
}
