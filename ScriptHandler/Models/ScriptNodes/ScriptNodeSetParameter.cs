
using DeviceCommunicators.EvvaDevice;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.SwitchRelay32;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Interfaces;
using System;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeSetParameter : ScriptNodeBase, IScriptStepWithParameter
	{	

		#region Properties and Fields

		private DeviceParameterData _parameter;
		public DeviceParameterData Parameter
		{
			get => _parameter;
			set
			{
				_parameter = value;

				if (_parameter is SwitchRelay_ParamData)
				{
					if(SwitchRelayValue == null)
						SwitchRelayValue = new BitwiseNumberDisplayData(is64Bit:false, isZeroBased:false);
				}
				else
					SwitchRelayValue = null;

			//	ValueDropDwonIndex = 0;

				OnPropertyChanged("Parameter");
				OnPropertyChanged("ValueDropDwonIndex");
			}
		}

		private object _value;
		public object Value 
		{
			get => _value;
			set
			{
				_value = value;
				OnPropertyChanged("Value");
				OnPropertyChanged("Description");
			}
		}

		private int _valueDropDwonIndex;
		public int ValueDropDwonIndex
		{
			get => _valueDropDwonIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				if (dropDown.DropDown == null)
					return;

				_valueDropDwonIndex = value;

				if (_valueDropDwonIndex < 0 || _valueDropDwonIndex >= dropDown.DropDown.Count)
					return;

				int iVal;
				bool res = int.TryParse(dropDown.DropDown[_valueDropDwonIndex].Value, out iVal);
				if (res)
					Value = iVal;

				OnPropertyChanged("ValueDropDwonIndex");
				OnPropertyChanged("Description");
			}
		}


		public BitwiseNumberDisplayData SwitchRelayValue { get; set; }
		public int SwitchRelayChannel { get; set; }

		public int Ni6002_IOPort { get; set; }
		public object Ni6002_Value { get; set; }

		private DeviceParameterData _valueParameter;
		public DeviceParameterData ValueParameter 
		{
			get => _valueParameter;
			set
			{
				_valueParameter = value;
				if (_valueParameter != null)
					Value = _valueParameter.Name;
			}
		}

		public bool IsWarning { get; set; }
		public bool IsFault { get; set; }
		public bool IsCriticalFalt { get; set; }


		public override string Description
		{
			get
			{
				string stepDescription = "Set ";

				if(_parameter is Evva_ParamData)
				{
					double d = Convert.ToDouble(_value);
					if (d == 1)
						stepDescription = "Start Safty Officer" + " - ID:" + ID;
					else
						stepDescription = "Stop Safty Officer" + " - ID:" + ID;

					return stepDescription;
				}

				if (_parameter is DeviceParameterData deviceParameter)
				{
					stepDescription += " \"" + deviceParameter + "\"";

					if (_parameter is NI6002_ParamData)
					{
						if (deviceParameter.Name == "Digital port output" ||
						deviceParameter.Name == "Analog port output" ||
						deviceParameter.Name == "Read digital input" ||
						deviceParameter.Name == "Read Anolog input")
						{
							stepDescription += " - Pin out " + Ni6002_IOPort;
						}

						stepDescription += " = " + Ni6002_Value;
					}
					else 
					{
						stepDescription += " = " + _value;
					}
				}

				

				stepDescription += " - ID:" + ID;
				return stepDescription;


			}
		}

		#endregion Properties and Fields

		#region Constructor

		public ScriptNodeSetParameter()
		{
			Description = Name = "Set Parameter";
			_valueDropDwonIndex = -1;
			IsWarning = true;
		}

		#endregion Constructor

		#region Method

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}

		#endregion Method
	}
}
