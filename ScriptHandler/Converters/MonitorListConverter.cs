
using System.Globalization;
using System.Windows.Data;
using System;

namespace ScriptHandler.Converter
{
	public class MonitorListConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value == null) 
				return "Select recording file";

			return value;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
