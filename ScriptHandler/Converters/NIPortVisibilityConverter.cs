
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;
using System.Windows;

namespace ScriptHandler.Converter
{
	public class NIPortVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is DeviceParameterData param))
				return Visibility.Collapsed;

			if (param.Name != "Digital port output" &&
				param.Name != "Analog port output" &&
				param.Name != "Read digital input" &&
				param.Name != "Read Anolog input")
			{
				return Visibility.Collapsed;
			}

			return Visibility.Visible;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
