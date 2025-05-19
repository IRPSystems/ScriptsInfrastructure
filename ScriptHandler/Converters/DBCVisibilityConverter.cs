
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows;
using DeviceCommunicators.DBC;

namespace ScriptHandler.Converter
{
	public class DBCVisibilityConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is DBC_ParamData)
				return Visibility.Visible;
			return Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
