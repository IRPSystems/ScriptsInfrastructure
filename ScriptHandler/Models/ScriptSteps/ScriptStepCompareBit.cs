
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompareBit : ScriptStepGetParamValue
	{
		
		public int BitIndex { get; set; }

		public int ComparedValue { get; set; }

		public ScriptStepCompareBit()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

		public override void Execute()
		{
			try
			{
				IsPass = false;
                uint? bit = null;
                ErrorMessage = Description;

				string description = Description;
				if(string.IsNullOrEmpty(UserTitle) == false ) 
					description = UserTitle;

				EOLStepSummeryData eolStepSummeryData;
				bool isOK = SendAndReceive(Parameter, out eolStepSummeryData, description);
				if (!isOK)
				{
					return;
				}

				EOLStepSummerysList.Add(eolStepSummeryData);

				int value = 0;
				if (Parameter.Value is string str)
				{
					int index = 0;
                    //bool res = int.TryParse(str, out value);

                    if (Parameter is MCU_ParamData param)
                    {
                        index = param.DropDown.FindIndex(dropdown => dropdown.Name == str);
						if (index == BitIndex)
							bit = 1;
						else
							bit = 0;
                    }

                    if (index == -1)
					{
						IsPass = false;
						ErrorMessage += "Recived value is not an integer value";
						return;
					}					
				}
				else
				{
					bool res = int.TryParse(Parameter.Value.ToString(), out value);
					if (res == false)
					{
						IsPass = false;
						ErrorMessage += "Recived value is not an integer value";
						return;
					}
				}

				if(bit == null)
					bit = (uint)((value >> (BitIndex - 1)) & 1);

				if (bit != ComparedValue)
				{
					IsPass = false;

					string bitName = BitIndex.ToString();
					if (Parameter is IParamWithDropDown dropDown)
						bitName = $"\"{dropDown.DropDown[BitIndex].Name}\"";
					ErrorMessage += $"Bit {bitName} is not " + ComparedValue;
					return;
				}

				eolStepSummeryData = new EOLStepSummeryData(
					"",
					description,
					this);
				eolStepSummeryData.IsPass = IsPass;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
				EOLStepSummerysList.Add(eolStepSummeryData);
				IsPass = true;
			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Failed to execute CompareBit", ex);
				IsPass = false;
				ErrorMessage += ex.Message;
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

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeCompareBit).Parameter;
			BitIndex = (sourceNode as ScriptNodeCompareBit).BitIndex;
			ComparedValue = (sourceNode as ScriptNodeCompareBit).ComparedValue;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (Parameter is ICalculatedParamete)
				return;

			DeviceParameterData parameter = GetRealParam(
				Parameter,
				devicesContainer);
		}

		public override List<string> GetReportHeaders()
		{
			List<string> headers = base.GetReportHeaders();

			string stepDescription = headers[0].Trim('\"');

			string description =
					$"{stepDescription}\r\nGet {Parameter.Name}";

			headers.Add($"\"{description}\"");

			return headers;
		}
	}
}
