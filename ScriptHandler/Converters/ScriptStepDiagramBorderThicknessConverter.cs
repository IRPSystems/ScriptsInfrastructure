
using System.Windows.Data;
using System;
using System.Windows;
using System.Windows.Media;
using ScriptHandler.Enums;

namespace ScriptHandler.Converter
{
	public class ScriptStepDiagramBorderThicknessConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(values[0] is SciptStateEnum stepState))
				return new Thickness(1);

			if (!(values[1] is bool isPass))
				return new Thickness(1);

			if (stepState == SciptStateEnum.None)
				return new Thickness(1);
			else if (stepState == SciptStateEnum.Running)
				return new Thickness(3);
			if (stepState == SciptStateEnum.Ended)
			{
				return new Thickness(2);
			}

			return Application.Current.MainWindow.FindResource("MahApps.Brushes.Gray1") as SolidColorBrush;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
