
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.ZimmerPowerMeter;

namespace ScriptHandler.Converter
{
	public class CompareWithTolerancePortVisiNIConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is NI6002_ParamData)
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
