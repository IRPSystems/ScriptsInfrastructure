
using System.Windows.Data;
using System;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;

namespace ScriptHandler.Converter
{
	

	public class NodeColorSelectorConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return ToolColorSelectionService.SelectColor(values[0] as IScriptItem);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
