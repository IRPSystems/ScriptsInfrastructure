
using System.Windows.Data;
using System;
using System.Windows;
using System.Windows.Media;
using ScriptHandler.Enums;

namespace ScriptHandler.Converter
{
	public class ScriptStepDiagramBorderBrushConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(values[0] is SciptStateEnum stepState))
				return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray1") as SolidColorBrush;

			if (!(values[1] is bool isPass))
				return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray1") as SolidColorBrush;

			if (stepState == SciptStateEnum.None)
				return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray1") as SolidColorBrush;
			else if (stepState == SciptStateEnum.Running)
				return Brushes.Magenta;
			if (stepState == SciptStateEnum.Ended)
			{
				if (isPass)
					return Brushes.Green;
				else
					return Brushes.Red;
			}

			return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray1") as SolidColorBrush;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
