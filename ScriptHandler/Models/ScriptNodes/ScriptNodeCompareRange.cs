

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



		private object _value;
		public object Value 
		{
			get => _value;
			set
			{
				_value = value;
				OnPropertyChanged("Value");
			}
		}

		private object _valueLeft;
		public object ValueLeft
		{
			get => _valueLeft;
			set
			{
				_valueLeft = value;
				OnPropertyChanged("ValueLeft");
			}

		}

		private int _valueLeftDropDwonIndex;
		public int ValueLeftDropDwonIndex
		{
			get => _valueLeftDropDwonIndex;
			set
			{
				if (!(Value is IParamWithDropDown dropDown))
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
					ValueLeft = iVal;

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
				OnPropertyChanged("ValueRight");
			}

		}

		private int _valueRightDropDwonIndex;
		public int ValueRightDropDwonIndex
		{
			get => _valueRightDropDwonIndex;
			set
			{
				if (!(Value is IParamWithDropDown dropDown))
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

		private int _valueDropDwonIndex_NumatoGPIOPort;
		public int ValueDropDwonIndex_NumatoGPIOPort
		{
			get => _valueDropDwonIndex_NumatoGPIOPort;
			set
			{
				if (!(ValueLeft is NumatoGPIO_ParamData numatoGPIOParamData))
					return;

				_valueDropDwonIndex_NumatoGPIOPort = value;

				if (_valueDropDwonIndex_NumatoGPIOPort < 0 || _valueDropDwonIndex_NumatoGPIOPort >= numatoGPIOParamData.DropDown.Count)
					return;

				int iVal;
				bool res = int.TryParse(numatoGPIOParamData.DropDown[_valueDropDwonIndex_NumatoGPIOPort].Value, out iVal);
				if (res)
					numatoGPIOParamData.Io_port = iVal;

				OnPropertyChanged("ValueDropDwonIndex_NumatoGPIOPort");
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

		public override string Description 
		{
			get
			{
				string stepDescription = "Compare Range: ";

				if (IsBetween2Values)
				{

					stepDescription += 
						GetValueDescription(_valueLeft) +
						" " + ScriptNodeCompare.GetComperationDescription(Comparation1) +
						" " + GetValueDescription(_value) +					
						" " + ScriptNodeCompare.GetComperationDescription(Comparation2) + 
						" " + GetValueDescription(_valueRight);
				}
				else
				{

					stepDescription += 
						" " + GetValueDescription(_value) + " = " +
						GetValueDescription(_valueLeft) + " ± " +
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
		}

		#endregion Constructor

		#region Methods

		private string GetValueDescription(object value)
		{
			string description = null;

			if(value is DeviceParameterData param)
			{
				description = param.ToString();
			}
			else if (value is string str)
			{
				double d;
				bool res = double.TryParse(str, out d);
				if (res) 
					description = str;
				else
					description = str;
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
			if(Value is DeviceParameterData compareRangeValue)
			{
				DeviceParameterData data = GetParameter(
							compareRangeValue.DeviceType,
							compareRangeValue,
							devicesContainer);
				if (data != null)
					Value = data;
			}

			if (ValueLeft is DeviceParameterData compareParamLeft)
			{
				DeviceParameterData data = GetParameter(
					compareParamLeft.DeviceType,
					compareParamLeft,
					devicesContainer);
				if (data != null)
					ValueLeft = data;
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
			if (Value == null)
				return true;

			return false;
		}

		#endregion Methods

	}

}
