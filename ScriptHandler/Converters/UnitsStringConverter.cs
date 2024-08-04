
using System.Globalization;
using System.Windows.Data;
using System;

namespace ScriptHandler.Converter
{
	public class UnitsStringConverter : IValueConverter
	{
		

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string units))
				return null;

			if(string.IsNullOrEmpty(units)) 
				return null;

			return "[" + units + "]";
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
