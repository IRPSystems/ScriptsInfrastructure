
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows;

namespace ScriptRunner.Converter
{
	public class StepVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value == null) return Visibility.Visible;

			return Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
