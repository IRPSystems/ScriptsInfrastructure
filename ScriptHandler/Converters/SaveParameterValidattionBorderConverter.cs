
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows.Media;
using DeviceCommunicators.MCU;
using System.Windows;

namespace ScriptHandler.Converter
{
	public class SaveParameterValidattionBorderConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value == null)
				return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray6") as SolidColorBrush;

			if (!(value is MCU_ParamData mcuParam))
				return Brushes.Red;


			if (mcuParam.Save == false)
				return Brushes.Red;

			return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray6") as SolidColorBrush;

		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
