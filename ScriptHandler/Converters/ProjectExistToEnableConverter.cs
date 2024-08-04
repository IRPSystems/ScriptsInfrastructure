
using System.Globalization;
using System.Windows.Data;
using System;
using ScriptHandler.Models;

namespace ScriptHandler.Converter
{
	public class ProjectExistToEnableConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is ProjectData)
				return true;

			return false;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
