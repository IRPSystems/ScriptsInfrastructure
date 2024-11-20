
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

		object IValueConverter.Convert(object value, Type targetType, object ConverterParameter, CultureInfo culture)
		{
			if (!(value is NI6002_ParamData param))
				return Visibility.Collapsed;

			string Type = ConverterParameter as string;
			string paramname = param.Name;

            if (paramname.Equals(Type, StringComparison.OrdinalIgnoreCase))
                return Visibility.Visible;
			if (!paramname.Equals("Analog Input Thermistor", StringComparison.OrdinalIgnoreCase) && !paramname.Equals("Analog Input Current", StringComparison.OrdinalIgnoreCase)
				&& !paramname.Equals("Digital Counter", StringComparison.OrdinalIgnoreCase) && Type.Equals("NI"))
				return Visibility.Visible;

                return Visibility.Collapsed;

        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
