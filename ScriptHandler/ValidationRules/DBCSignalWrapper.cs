
using DBCFileParser.Model;
using System.Windows;

namespace ScriptHandler.ValidationRules
{
	public class DBCSignalWrapper : DependencyObject
	{
		public static readonly DependencyProperty SignalProperty =
			 DependencyProperty.Register("Signal", typeof(Signal),
			 typeof(DBCSignalWrapper));

		public Signal Signal
		{
			get { return (Signal)GetValue(SignalProperty); }
			set { SetValue(SignalProperty, value); }
		}
	}
}
