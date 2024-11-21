
using System.Globalization;
using System.Windows.Data;
using System;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;

namespace ScriptHandler.Converter
{
	public class ScriptStepDiagramBackgroundConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ToolColorSelectionService.SelectColor(value as IScriptItem);
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
