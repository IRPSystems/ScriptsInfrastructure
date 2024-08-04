
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows.Media;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptSteps;
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
