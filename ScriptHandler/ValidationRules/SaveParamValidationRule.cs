

using System.Globalization;
using System.Windows.Controls;
using DeviceCommunicators.MCU;

namespace ScriptHandler.ValidationRules
{
    public class SaveParamValidationRule : ValidationRule
	{

		public override ValidationResult Validate(object value, CultureInfo cultureInfo)		
		{
			if(!(value is MCU_ParamData mcuParam))
				return ValidationResult.ValidResult;

			if(mcuParam.Save == true)
				return ValidationResult.ValidResult;
			else
				return new ValidationResult(false, "The parameter cannot be saved");
		}
	}
}
