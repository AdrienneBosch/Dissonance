using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dissonance.Windows.Controls
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if ( value is bool booleanValue )
			{
				if ( parameter is string invert && invert.ToLower ( ) == "invert" )
					booleanValue = !booleanValue;

				return booleanValue ? Visibility.Visible : Visibility.Collapsed;
			}
			return Visibility.Collapsed;
		}

		public object ConvertBack ( object value, Type targetType, object parameter, CultureInfo culture )
		{
			if ( value is Visibility visibility )
			{
				return visibility == Visibility.Visible;
			}
			return false;
		}
	}
}
