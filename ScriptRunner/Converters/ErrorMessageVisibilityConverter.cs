
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows;

namespace ScriptRunner.Converter
{
	public class ErrorMessageVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value == null) return Visibility.Collapsed;

			return Visibility.Visible;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
