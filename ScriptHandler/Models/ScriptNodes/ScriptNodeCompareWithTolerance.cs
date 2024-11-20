using DeviceCommunicators.MCU;
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
				Parameter_ExtraData.Parameter = value;
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
				if(_compareValue is DeviceParameterData)
					CompareValue_ExtraData.Parameter = _compareValue as DeviceParameterData;
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

		public bool IsUseParamAverage { get; set; }
		public int AverageOfNRead_Param { get; set; }

		public bool IsUseParamFactor { get; set; }
		public double ParamFactor { get; set; }


		public bool IsUseCompareValueAverage { get; set; }
		public int AverageOfNRead_CompareValue { get; set; }

		public bool IsUseCompareValueFactor { get; set; }
		public double CompareValueFactor { get; set; }


		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }

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

			Parameter_ExtraData = new ExtraDataForParameter();
			CompareValue_ExtraData = new ExtraDataForParameter();

			IsValueTolerance = true;
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

		public override object Clone()
		{
			ScriptNodeCompareWithTolerance compare = MemberwiseClone() as
				ScriptNodeCompareWithTolerance;

			compare.CompareValue_ExtraData = this.CompareValue_ExtraData.Clone()
				as ExtraDataForParameter;
			compare.Parameter_ExtraData = this.CompareValue_ExtraData.Clone()
				as ExtraDataForParameter;

			return compare;
		}
	}
}
