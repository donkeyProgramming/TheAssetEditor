using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Shared.EmbeddedResources;
using Shared.Ui.Common.ToolTipSystem;

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

        private static void OnToolTipEnumChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var owner = (AeAttribute)d;
            var value = (ToolTipEnum)e.NewValue;
            if (value == ToolTipEnum.None)
                owner.InformationImage.Visibility = Visibility.Hidden;
            else
                owner.InformationImage.Visibility = Visibility.Visible;

            if (value == ToolTipEnum.None)
            {
                owner.InformationImage.Visibility = Visibility.Collapsed;
                owner.InformationImage.ToolTip = null;
                owner.Text.ToolTip = null;
                return;
            }

            var text = ToolTips.List[value];
            var toolTip = new ToolTip() { Content = text };

            owner.InformationImage.Visibility = Visibility.Visible;
            owner.InformationImage.ToolTip = toolTip;
            owner.Text.ToolTip = toolTip;
        }

        public static readonly DependencyProperty InnerContentProperty = DependencyProperty.Register("InnerContent", typeof(object), typeof(AeAttribute));
        public object InnerContent
        {
            get { return (object)GetValue(InnerContentProperty); }
            set { SetValue(InnerContentProperty, value); }
        }

        public static readonly DependencyProperty ToolTipEnumValueProperty = DependencyProperty.Register(nameof(ToolTipEnumValue), typeof(ToolTipEnum), typeof(AeAttribute), new PropertyMetadata(ToolTipEnum.None, OnToolTipEnumChanged));
        public ToolTipEnum ToolTipEnumValue
        {
            get { return (ToolTipEnum)GetValue(ToolTipEnumValueProperty); }
            set { SetValue(ToolTipEnumValueProperty, value);}
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
