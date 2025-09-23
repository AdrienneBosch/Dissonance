using System.Windows;

using Dissonance.Windows.Controls;

namespace Dissonance.Tests.Windows.Controls
{
        public class BoolToVisibilityConverterTests
        {
                [Fact]
                public void Convert_ReturnsVisible_WhenTrue()
                {
                        var converter = new BoolToVisibilityConverter();

                        var result = converter.Convert(true, typeof(Visibility), null, null);

                        Assert.Equal(Visibility.Visible, result);
                }

                [Fact]
                public void Convert_ReturnsCollapsed_WhenFalse()
                {
                        var converter = new BoolToVisibilityConverter();

                        var result = converter.Convert(false, typeof(Visibility), null, null);

                        Assert.Equal(Visibility.Collapsed, result);
                }

                [Fact]
                public void Convert_Inverts_WhenParameterRequests()
                {
                        var converter = new BoolToVisibilityConverter();

                        var result = converter.Convert(true, typeof(Visibility), "invert", null);

                        Assert.Equal(Visibility.Collapsed, result);
                }

                [Fact]
                public void ConvertBack_ReturnsTrue_WhenVisible()
                {
                        var converter = new BoolToVisibilityConverter();

                        var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null, null);

                        Assert.Equal(true, result);
                }

                [Fact]
                public void ConvertBack_ReturnsFalse_ForOtherValues()
                {
                        var converter = new BoolToVisibilityConverter();

                        var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, null);

                        Assert.Equal(false, result);
                }
        }
}
