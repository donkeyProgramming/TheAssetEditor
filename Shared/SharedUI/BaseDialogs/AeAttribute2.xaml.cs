using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Shared.Ui.BaseDialogs
{
    [ContentProperty("InnerContent")]
    public partial class AutoAeAttribute : UserControl
    {

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(AutoAeAttribute), new PropertyMetadata(null));
        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(nameof(LabelWidth), typeof(double), typeof(AutoAeAttribute), new PropertyMetadata(140.0));
        public double LabelWidth
        {
            get { return (double)GetValue(LabelWidthProperty); }
            set { SetValue(LabelWidthProperty, value); }
        }

        public static readonly DependencyProperty LabelMarginProperty = DependencyProperty.Register(nameof(LabelMargin), typeof(Thickness), typeof(AutoAeAttribute), new PropertyMetadata(new Thickness(20, 0, 0, 0)));
        public Thickness LabelMargin
        {
            get { return (Thickness)GetValue(LabelMarginProperty); }
            set { SetValue(LabelMarginProperty, value); }
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(object), typeof(AutoAeAttribute));
        public object InnerContent
        {
            get { return GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        public AutoAeAttribute()
        {
            InitializeComponent();
        }
    }
}
