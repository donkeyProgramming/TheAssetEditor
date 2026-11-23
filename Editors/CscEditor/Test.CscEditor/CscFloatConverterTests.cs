using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Editors.CscEditor.Views;

namespace Test.CscEditor
{
    public class CscFloatConverterTests
    {
        [TestCase("1.5", 1.5f)]
        [TestCase("1,5", 1.5f)]
        [TestCase("1．5", 1.5f)]
        [TestCase("1。5", 1.5f)]
        [TestCase(" -2 ", -2f)]
        [TestCase("1e3", 1000f)]
        public void Accepts_any_decimal_mark(string input, float expected)
        {
            var converted = new CscFloatConverter().ConvertBack(input, typeof(float), null!, CultureInfo.InvariantCulture);
            Assert.That(converted, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("1e40")]
        public void Rejects_text_that_is_not_a_finite_float(string input)
        {
            var converted = new CscFloatConverter().ConvertBack(input, typeof(float), null!, CultureInfo.InvariantCulture);
            Assert.That(converted, Is.SameAs(DependencyProperty.UnsetValue));
        }

        [Test]
        public void NonNegative_parameter_rejects_values_below_zero()
        {
            var converter = new CscFloatConverter();
            Assert.That(converter.ConvertBack("-0.5", typeof(float), "NonNegative", CultureInfo.InvariantCulture), Is.SameAs(DependencyProperty.UnsetValue));
            Assert.That(converter.ConvertBack("0", typeof(float), "NonNegative", CultureInfo.InvariantCulture), Is.EqualTo(0f));
        }

        [Test, Apartment(ApartmentState.STA)]
        public void Bound_text_box_keeps_the_value_and_flags_an_error_on_rejected_text()
        {
            var source = new FloatSource { Value = 2 };
            var textBox = new TextBox();
            textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(FloatSource.Value))
            {
                Source = source,
                Converter = new CscFloatConverter(),
                StringFormat = "0.###",
                UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
            });
            Assert.That(textBox.Text, Is.EqualTo("2"));

            textBox.Text = "1,25";
            textBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Assert.That(source.Value, Is.EqualTo(1.25f));
            Assert.That(Validation.GetHasError(textBox), Is.False);

            textBox.Text = "abc";
            textBox.GetBindingExpression(TextBox.TextProperty)!.UpdateSource();
            Assert.That(source.Value, Is.EqualTo(1.25f));
            Assert.That(Validation.GetHasError(textBox), Is.True);
        }

        class FloatSource : INotifyPropertyChanged
        {
            float _value;
            public float Value
            {
                get => _value;
                set { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
            }
            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
