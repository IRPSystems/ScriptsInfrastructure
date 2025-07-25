﻿

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
		#region Properties and Fields

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
				if(value is string str)
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

		private int _valueDropDwonIndex;
		public int CompareValueDropDwonIndex
		{
			get => _valueDropDwonIndex;
			set 
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				_valueDropDwonIndex = value;

				if (_valueDropDwonIndex < 0 || _valueDropDwonIndex >= dropDown.DropDown.Count)
					return;

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueDropDwonIndex].Value, out iVal);
				if (res)
					CompareValue = iVal;

				OnPropertyChanged("ValueDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}

		//private int _valueDropDwonIndex_NumatoGPIOPort;
		//public int ValueDropDwonIndex_NumatoGPIOPort
		//{
		//	get => _valueDropDwonIndex_NumatoGPIOPort;
		//	set
		//	{
		//		if (!(ValueLeft is NumatoGPIO_ParamData numatoGPIOParamData))
		//			return;

		//		_valueDropDwonIndex_NumatoGPIOPort = value;

		//		if (_valueDropDwonIndex_NumatoGPIOPort < 0 || _valueDropDwonIndex_NumatoGPIOPort >= numatoGPIOParamData.DropDown.Count)
		//			return;

		//		int iVal;
		//		bool res = int.TryParse(numatoGPIOParamData.DropDown[_valueDropDwonIndex_NumatoGPIOPort].Value, out iVal);
		//		if (res)
		//			numatoGPIOParamData.Io_port = iVal;

		//		OnPropertyChanged("ValueDropDwonIndex_NumatoGPIOPort");
		//	}
		//}

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

		public bool IsUseAverage { get; set; }
		public int AverageOfNRead { get; set; }

		public ExtraDataForParameter Parameter_ExtraData { get; set; }
		public ExtraDataForParameter CompareValue_ExtraData { get; set; }

		public override string Description 
		{
			get
			{
				string stepDescription = "Compare ";
				if (_parameter is DeviceParameterData deviceParameter)
				{
					stepDescription += " " + deviceParameter;
				}

				stepDescription += " " + GetComperationDescription(Comparation);

				stepDescription += " ";// + _valueRight;
				if (_compareValue is DeviceParameterData param)
					stepDescription += param;
				else
					stepDescription += _compareValue;

				stepDescription += " - ID:" + ID;
				return stepDescription;

				
			}
		}

		#endregion Properties and Fields

		#region Constructor

		public ScriptNodeCompare()
		{
			Name = "Compare";
			Comparation = ComparationTypesEnum.Equal;
			_valueDropDwonIndex = -1;

			Parameter_ExtraData = new ExtraDataForParameter();
			CompareValue_ExtraData = new ExtraDataForParameter();
		}

		#endregion Constructor

		#region Methods

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
			if (Parameter is DeviceParameterData compareParamLeft)
			{
				DeviceParameterData data = GetParameter(
					compareParamLeft.DeviceType,
					compareParamLeft,
					devicesContainer);
				if (data != null)
					Parameter = data;
			}

			if (CompareValue is DeviceParameterData compareParamRight)
			{
				DeviceParameterData data = GetParameter(
					compareParamRight.DeviceType,
					compareParamRight,
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

			if (CompareValue == null)
				return true;

			return false;
		}

		public override object Clone()
		{
			ScriptNodeCompare compare = MemberwiseClone() as
				ScriptNodeCompare;

			compare.CompareValue_ExtraData = this.CompareValue_ExtraData.Clone()
				as ExtraDataForParameter;
			compare.Parameter_ExtraData = this.Parameter_ExtraData.Clone()
				as ExtraDataForParameter;

			return compare;
		}

		#endregion Methods
	}

}
