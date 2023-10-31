
using System.Globalization;
using System.Windows.Data;
using System;
using ScriptHandler.Interfaces;

namespace ScriptHandler.Converter
{
	public class ScriptStepDiagramToolTipConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is ISubScript subScript &&
				subScript.Script == null)
			{
				return "Could not find the sub script " + subScript.SelectedScriptName;
			}

			return null;

		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
