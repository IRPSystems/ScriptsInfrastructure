
using System.Globalization;
using System.Windows.Data;
using System;
using Entities.Models;

namespace ScriptHandler.Converter
{
	public class ObjectToTextConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is DeviceParameterData param)
				return param.ToString();

			return value;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}
