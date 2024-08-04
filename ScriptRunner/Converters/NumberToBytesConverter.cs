
using System.Globalization;
using System.Windows.Data;
using System;

namespace ScriptRunner.Converter
{
	public class NumberToBytesConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is ulong payload))
				return value;

			byte[] payloadBytes = BitConverter.GetBytes(payload);
			string payloadStr = "";
			for (int i = 0; i < payloadBytes.Length; i++)
			{
				payloadStr += payloadBytes[i].ToString("X2") + " ";
			}

			return payloadStr;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}
