

using DeviceCommunicators.Models;
using DeviceCommunicators.NumatoGPIO;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCompareRange : ScriptNodeBase
	{

		#region Fields and Properties



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
				if (value is string str)
				{
					double d;
					bool res = double.TryParse(str, out d);
					if (res)
						value = d;
				}

				_compareValue = value;
				if (_compareValue is DeviceParameterData)
					CompareValue_ExtraData.Parameter = _compareValue as DeviceParameterData;
				else
					CompareValue_ExtraData.Parameter = null;
				OnPropertyChanged(nameof(CompareValue));
			}

		}

		private int _valueLeftDropDwonIndex;
		public int ValueLeftDropDwonIndex
		{
			get => _valueLeftDropDwonIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				_valueLeftDropDwonIndex = value;

				if (dropDown.DropDown == null ||
					(_valueLeftDropDwonIndex < 0 || _valueLeftDropDwonIndex >= dropDown.DropDown.Count))
				{
					return;
				}

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueLeftDropDwonIndex].Value, out iVal);
				if (res)
					CompareValue = iVal;

				OnPropertyChanged("ValueLeftDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}

		private object _valueRight;
		public object ValueRight 
		{
			get => _valueRight;
			set
			{
				_valueRight = value;
				if (value is string str)
				{
					double d;
					bool res = double.TryParse(str, out d);
					if (res)
						value = d;
				}

				_valueRight = value;
				if (_valueRight is DeviceParameterData)
					RightValue_ExtraData.Parameter = _valueRight as DeviceParameterData;
				else
					RightValue_ExtraData.Parameter = null;
				OnPropertyChanged("ValueRight");
			}

		}

		private int _valueRightDropDwonIndex;
		public int ValueRightDropDwonIndex
		{
			get => _valueRightDropDwonIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				_valueRightDropDwonIndex = value;

				if (dropDown.DropDown == null ||
					_valueRightDropDwonIndex < 0 || _valueRightDropDwonIndex >= dropDown.DropDown.Count)
				{
					return;
				}

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueRightDropDwonIndex].Value, out iVal);
				if (res)
					ValueRight = iVal;

				OnPropertyChanged("ValueRightDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}




		private ComparationTypesEnum _comparation1;
		public ComparationTypesEnum Comparation1 
		{
			get => _comparation1;
			set
			{
				_comparation1 = value;
				OnPropertyChanged("Comparation1");
			}
		}

		private ComparationTypesEnum _comparation2;
		public ComparationTypesEnum Comparation2
		{
			get => _comparation2;
			set
			{
				_comparation2 = value;
				OnPropertyChanged("Comparation2");
			}
		}



		public string CompareTypeGroupName { get => Description + "CompareTypeGroupName"; }
		public bool IsBetween2Values { get; set; }
		public bool IsValueWithTolerance { get; set; }

		public string ToleranceTypeGroupName { get => Description + "ToleranceTypeGroupName"; }
		public bool IsValueTolerance { get; set; }
		public bool IsPercentageTolerance { get; set; }

		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }
		public ExtraDataForParameter RightValue_ExtraData { get; set; }

		public override string Description 
		{
			get
			{
				string stepDescription = "Compare Range: ";

				if (IsBetween2Values)
				{

					stepDescription += 
						GetValueDescription(_compareValue) +
						" " + ScriptNodeCompare.GetComperationDescription(Comparation1) +
						" " + GetValueDescription(_parameter) +					
						" " + ScriptNodeCompare.GetComperationDescription(Comparation2) + 
						" " + GetValueDescription(_valueRight);
				}
				else
				{

					stepDescription += 
						" " + GetValueDescription(_parameter) + " = " +
						GetValueDescription(_compareValue) + " ± " +
						" " + GetValueDescription(_valueRight);
				}



				stepDescription += " - ID:" + ID;
				return stepDescription;

				
			}
		}

		#endregion Fields and Properties


		#region Constructor

		public ScriptNodeCompareRange()
		{
			Name = "Compare Range";
			Comparation1 = ComparationTypesEnum.Equal;

			IsBetween2Values = true;
			IsValueTolerance = true;

			Parameter_ExtraData = new ExtraDataForParameter();
			CompareValue_ExtraData = new ExtraDataForParameter();
			RightValue_ExtraData = new ExtraDataForParameter();
		}

		#endregion Constructor

		#region Methods

		private string GetValueDescription(object value)
		{
			string description = null;

			if(value is DeviceParameterData param)
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
				if(value != null)
					description = value.ToString();
			}

			return description;
		}





		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if(Parameter is DeviceParameterData compareRangeValue)
			{
				DeviceParameterData data = GetParameter(
							compareRangeValue.DeviceType,
							compareRangeValue,
							devicesContainer);
				if (data != null)
					Parameter = data;
			}

			if (CompareValue is DeviceParameterData compareParamLeft)
			{
				DeviceParameterData data = GetParameter(
					compareParamLeft.DeviceType,
					compareParamLeft,
					devicesContainer);
				if (data != null)
					CompareValue = data;
			}

			if (ValueRight is DeviceParameterData compareParamRight)
			{
				DeviceParameterData data = GetParameter(
					compareParamRight.DeviceType,
					compareParamRight,
					devicesContainer);
				if (data != null)
					ValueRight = data;
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			if (CompareValue == null)
				return true;

			if (ValueRight == null)
				return true;

			return false;
		}

		#endregion Methods

	}

}
