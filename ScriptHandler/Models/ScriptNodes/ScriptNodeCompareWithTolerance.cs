using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCompareWithTolerance : ScriptNodeBase
	{

		private DeviceParameterData _parameter;
		public DeviceParameterData Parameter
		{
			get => _parameter;
			set
			{
				_parameter = value;
				OnPropertyChanged(nameof(Parameter));
			}
		}

		private object _compareValue;
		public object CompareValue
		{
			get => _compareValue;
			set
			{
				_compareValue = value;
				OnPropertyChanged(nameof(CompareValue));
			}
		}

		private double _tolerance;
		public double Tolerance
		{
			get => _tolerance;
			set
			{
				_tolerance = value;
				OnPropertyChanged(nameof(Tolerance));
			}
		}

		private ComparationTypesEnum _comparation;
		public ComparationTypesEnum Comparation
		{
			get => _comparation;
			set
			{
				_comparation = value;
				OnPropertyChanged(nameof(Comparation));
			}
		}

		private int _compareValueDropDwonIndex;
		public int CompareValueDropDwonIndex
		{
			get => _compareValueDropDwonIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				_compareValueDropDwonIndex = value;

				if (dropDown.DropDown == null ||
					(_compareValueDropDwonIndex < 0 || _compareValueDropDwonIndex >= dropDown.DropDown.Count))
				{
					return;
				}

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_compareValueDropDwonIndex].Value, out iVal);
				if (res)
					CompareValue = iVal;

				OnPropertyChanged("CompareValueDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}

		public string ToleranceTypeGroupName { get => Description + "ToleranceTypeGroupName"; }
		public bool IsValueTolerance { get; set; }
		public bool IsPercentageTolerance { get; set; }


		public override string Description
		{
			get
			{
				string stepDescription = "Compare with tolerance: ";

				
					stepDescription +=
						" " + GetValueDescription(_parameter) + " = " +
						GetValueDescription(_compareValue) + " ± " +
						" " + GetValueDescription(_tolerance);
				



				stepDescription += " - ID:" + ID;
				return stepDescription;
			}
		}

		public ScriptNodeCompareWithTolerance()
		{
			Name = "Compare With Tolerance";
		}

		private string GetValueDescription(object value)
		{
			string description = null;

			if (value is DeviceParameterData param)
			{
				description = "\"" + param + "\"";
			}
			else if (value is string str)
			{
				double d;
				bool res = double.TryParse(str, out d);
				if (res)
					description = str;
				else
					description = "\"" + str + "\"";
			}
			else
			{
				if (value != null)
					description = value.ToString();
			}

			return description;
		}

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (Parameter is DeviceParameterData parameter)
			{
				DeviceParameterData data = GetParameter(
							parameter.DeviceType,
							parameter,
							devicesContainer);
				if (data != null)
					Parameter = data;
			}

			if (CompareValue is DeviceParameterData compareValue)
			{
				DeviceParameterData data = GetParameter(
					compareValue.DeviceType,
					compareValue,
					devicesContainer);
				if (data != null)
					CompareValue = data;
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}
	}
}
