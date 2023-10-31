

using System.Globalization;
using System.Windows.Controls;

namespace ScriptHandler.ValidationRules
{
    public class DBCSignalValidationRule : ValidationRule
	{
		public DBCSignalWrapper SignalWrapper { get; set; }

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)		
		{
			if (SignalWrapper.Signal.Minimum == 0 && SignalWrapper.Signal.Maximum == 0)
			{
				return new ValidationResult(true, null);
			}	

			double d = double.NaN;
			if (value is string str)
			{				
				bool res = double.TryParse(str, out d);
				if(!res) 
					return new ValidationResult(false, "The value should be numeric");
				
			}
			else if(value is double dbl)
				d = dbl;

			if (d < SignalWrapper.Signal.Minimum || d > SignalWrapper.Signal.Maximum)
			{
				return new ValidationResult(
					false, 
					"The value is out of range (" + 
						SignalWrapper.Signal.Minimum + ", " + 
						SignalWrapper.Signal.Maximum + ")");
			}


			return ValidationResult.ValidResult;
		}
	}

	

	
}
