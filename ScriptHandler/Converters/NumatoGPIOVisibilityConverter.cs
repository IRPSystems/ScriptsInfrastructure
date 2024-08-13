
using System.Globalization;
using System.Windows.Data;
using System;
using DeviceCommunicators.NumatoGPIO;
using System.Windows;

namespace ScriptHandler.Converter
{
	public class NumatoGPIOVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is NumatoGPIO_ParamData)
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
