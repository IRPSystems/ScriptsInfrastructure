

using DeviceCommunicators.Models;
using DeviceCommunicators.NumatoGPIO;
using DeviceHandler.Models;
using Entities.Enums;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCompare : ScriptNodeBase
	{
		private DeviceParameterData _valueLeft;
		public DeviceParameterData ValueLeft 
		{
			get => _valueLeft;
			set
			{
				_valueLeft = value;
				OnPropertyChanged("ValueLeft");
			}
		}

		private object _valueRight;
		public object ValueRight 
		{
			get => _valueRight;
			set
			{
				if(value is string str)
				{
					double d;
					bool res = double.TryParse(str, out d);
					if (res)
						value = d;
				}

				_valueRight = value;
				OnPropertyChanged("ValueRight");
			}

		}

		private int _valueDropDwonIndex;
		public int ValueDropDwonIndex
		{
			get => _valueDropDwonIndex;
			set 
			{
				if (!(ValueLeft is IParamWithDropDown dropDown))
					return;

				_valueDropDwonIndex = value;

				if (_valueDropDwonIndex < 0 || _valueDropDwonIndex >= dropDown.DropDown.Count)
					return;

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueDropDwonIndex].Value, out iVal);
				if (res)
					ValueRight = iVal;

				OnPropertyChanged("ValueDropDwonIndex");
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

				OnPropertyChanged("ValueDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}

		private ComparationTypesEnum _comparation;
		public ComparationTypesEnum Comparation 
		{
			get => _comparation;
			set
			{
				_comparation = value;
				OnPropertyChanged("Comparation");
			}
		}

		public override string Description 
		{
			get
			{
				string stepDescription = "Compare ";
				if (_valueLeft is DeviceParameterData deviceParameter)
				{
					stepDescription += " " + deviceParameter;
				}

				stepDescription += " " + GetComperationDescription(Comparation);

				stepDescription += " ";// + _valueRight;
				if (_valueRight is DeviceParameterData param)
					stepDescription += param;
				else
					stepDescription += _valueRight;

				stepDescription += " - ID:" + ID;
				return stepDescription;

				
			}
		}

		public ScriptNodeCompare()
		{
			Name = "Compare";
			Comparation = ComparationTypesEnum.Equal;
			_valueDropDwonIndex = -1;
		}


		public static string GetComperationDescription(ComparationTypesEnum item)
		{
			var enumType = item.GetType();
			var memberInfos = enumType.GetMember(item.ToString());
			var enumValueMemberInfo = memberInfos.FirstOrDefault(m =>
				m.DeclaringType == enumType);
			var valueAttributes =
				enumValueMemberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false);
			var description = ((DescriptionAttribute)valueAttributes[0]).Description;

			return description;
		}


		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
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
			if (ValueLeft == null)
				return true;

			if (ValueRight == null)
				return true;

			return false;
		}
	}
	
}
