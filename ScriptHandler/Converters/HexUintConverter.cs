
using System.Globalization;
using System.Windows.Data;
using System;
using System.Security.Cryptography;

namespace ScriptHandler.Converter
{
	public class HexUintConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			ulong uVal = Convert.ToUInt64(value);
			return uVal.ToString("X");
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			string valueStr = value.ToString();
			uint hexNumber;
			uint.TryParse(valueStr, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out hexNumber);
			return hexNumber;
		}
	}
}
