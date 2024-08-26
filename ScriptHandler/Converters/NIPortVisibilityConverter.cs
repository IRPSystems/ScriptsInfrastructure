
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;
using System.Windows;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;

namespace ScriptHandler.Converter
{
	public class NIPortVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is NI6002_ParamData param))
				return Visibility.Collapsed;

			return Visibility.Visible;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
