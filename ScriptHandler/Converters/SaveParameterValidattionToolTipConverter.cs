
using System.Globalization;
using System.Windows.Data;
using System;
using DeviceCommunicators.MCU;
using Entities.Models;

namespace ScriptHandler.Converter
{
	public class SaveParameterValidattionToolTipConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(!(value is DeviceParameterData param))
				return "";

			if (!(param is MCU_ParamData mcuParam))
				return "Parameter \"" + param.Name + "\" is not savable";


			if (mcuParam.Save == false)
				return "Parameter \"" + param.Name + "\" is not savable";

			return param.Name;

		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
