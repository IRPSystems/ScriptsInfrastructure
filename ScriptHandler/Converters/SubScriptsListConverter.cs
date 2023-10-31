
using System.Windows.Data;
using System;
using System.Collections.ObjectModel;
using ScriptHandler.Models;
using System.Collections.Generic;
using ScriptHandler.Models.ScriptNodes;

namespace ScriptHandler.Converter
{
	public class SubScriptsListConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(values[0] is IEnumerable<ScriptData> originalList))
				return values[0];

			if (!(values[1] is ScriptNodeSubScript subScript))
				return values[0];

			ObservableCollection<ScriptData> list = new ObservableCollection<ScriptData>();
			foreach (ScriptData scriptData in originalList)
			{
				if (scriptData.Name == subScript.ParentScriptName)
					continue;

				list.Add(scriptData);
			}

			return list;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
	}
}
