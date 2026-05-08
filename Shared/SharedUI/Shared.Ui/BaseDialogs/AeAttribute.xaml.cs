using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Shared.EmbeddedResources;


namespace Shared.Ui.BaseDialogs
{

    [ContentProperty("InnerContent")]
    public partial class AeAttribute : UserControl
    {
        public AeAttribute()
        {
            InitializeComponent();

            InformationImage.Source = IconLibrary.InformationIcon;
            InformationImage.Visibility = Visibility.Hidden;
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(object), typeof(AeAttribute));
        public object InnerContent
        {
            get { return (object)GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        public static readonly DependencyProperty LabelTextProperty = DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(AeAttribute), new PropertyMetadata(null));
        public string LabelText
        {
            get { return (string)GetValue(LabelTextProperty); }
            set { SetValue(LabelTextProperty, value); }
        }

        public static readonly DependencyProperty LabelWidthProperty = DependencyProperty.Register(nameof(LabelWidth), typeof(Double), typeof(AeAttribute), new PropertyMetadata(140.0));
        public Double LabelWidth
        {
            get { return (Double)GetValue(LabelWidthProperty); }
            set { SetValue(LabelWidthProperty, value); }
        }

        public static readonly DependencyProperty LabelMarginProperty = DependencyProperty.Register(nameof(LabelMargin), typeof(Thickness), typeof(AeAttribute), new PropertyMetadata(new Thickness(20,0,0,0)));
        public Thickness LabelMargin
        {
            get { return (Thickness)GetValue(LabelMarginProperty); }
            set { SetValue(LabelMarginProperty, value); }
        }
    }
}
