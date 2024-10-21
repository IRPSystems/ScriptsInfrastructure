
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows.Media;
using System.Windows;

namespace ScriptHandler.Converter
{
	public class IsChangedToBackgroundConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is bool isChanged) || Application.Current == null)
				return value;

			if (isChanged)
				return Brushes.Orange;

			return Application.Current.MainWindow.TryFindResource("MahApps.Brushes.Accent2") as SolidColorBrush;

		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
