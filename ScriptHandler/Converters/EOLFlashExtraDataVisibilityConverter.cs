
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;
using DeviceCommunicators.Models;
using System.Windows;

namespace ScriptHandler.Converter
{
	public class EOLFlashExtraDataVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(!(value is string fileExtension))
				return Visibility.Collapsed;

			if(fileExtension == ".brn.hex" ||
				fileExtension == ".bin")
				return Visibility.Visible;
			

			return Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
