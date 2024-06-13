
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeConverge : ScriptNodeBase, IScriptStepWithParameter
	{
		public DeviceParameterData Parameter { get; set; }

		public TimeUnitsEnum ConvergeTimeUnite { get; set; }
		public int ConvergeTime { get; set; }

		public TimeUnitsEnum TimeoutTimeUnite { get; set; }
		public int Timeout { get; set; }

		public object TargetValue { get; set; }
		public double Tolerance { get; set; }


		public override string Description
		{
			get
			{
				string stepDescription = "Converge ";
				if (Parameter is DeviceParameterData deviceParameter)
				{
					stepDescription += " \"" + deviceParameter + "\"";
				}

				stepDescription += " = " + TargetValue;

				stepDescription += " - ID:" + ID;
				return stepDescription;


			}
		}


		public ScriptNodeConverge()
		{
			Name = "Converge";
			ConvergeTimeUnite = TimeUnitsEnum.sec;
			TimeoutTimeUnite = TimeUnitsEnum.sec;
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			if (ConvergeTime <= 0)
				return true;

			return false;
		}

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (TargetValue is DeviceParameterData compareParamLeft)
			{
				DeviceParameterData data = GetParameter(
					compareParamLeft.DeviceType,
					compareParamLeft,
					devicesContainer);
				if (data != null)
					TargetValue = data;
			}
		}
	}
}
