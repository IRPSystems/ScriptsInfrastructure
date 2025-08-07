
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Enums;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Reflection.Metadata;
using System.Windows;
using System.Linq;

namespace ScriptHandler.Models
{
	public class ScriptStepCompareBit : ScriptStepGetParamValue
	{
		
		public int BitIndex { get; set; }
        public string FaultName { get; set; }
		public int ComparedValue { get; set; }

		public ScriptStepCompareBit()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

        public override void Execute()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (Parameter is IParamWithDropDown dropDown)
                    FaultName = dropDown.DropDown[BitIndex].Name;

                IsPass = false;
                uint? bit = null;
                ErrorMessage = Description;

                IsExecuted = true;

                string description = Description;
                if (string.IsNullOrEmpty(UserTitle) == false)
                    description = UserTitle;

                EOLStepSummeryData eolStepSummeryData;
                bool isOK = SendAndReceive(Parameter, out eolStepSummeryData, description);
                if (!isOK)
                {
                    IsError = true;
                    PopulateSendResponseLog(UserTitle, this.GetType().Name, Parameter.Name, Parameter.DeviceType, Parameter.CommSendResLog);
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
                        IsError = true;
                        IsPass = false;
                        ErrorMessage += "Recived value is not an integer value";
                        return;
                    }
                }

                int bitshift = 0;
                if (Parameter is MCU_ParamData mcuparam)
                {
                    if (ComparedValue == 0)
                    {
                        bitshift = 0;
                    }
                    else if (ComparedValue > 0 && (ComparedValue & (ComparedValue - 1)) == 0)
                    {
                        bitshift = (int)Math.Log2(ComparedValue);
                    }
                    else
                    {
                        ErrorMessage += " ComparedValue must be a power of 2 or 0";
                        IsPass = false;
                        IsError = true;
                        return;
                    }
                }

                if (bit == null)
                     bit = (uint)((value >> (bitshift)) & 1);

                if (bit != ComparedValue)
                {
                    IsPass = false;
                    //string bitName = BitIndex.ToString();
                    //if (Parameter is IParamWithDropDown dropDown)
                        //bitName = $"\"{dropDown.DropDown[BitIndex].Name}\"";
                    ErrorMessage += $"Bit {FaultName} is not " + ComparedValue;
                    return;
                }

                eolStepSummeryData = new EOLStepSummeryData(
                    "",
                    description,
                    this);
                eolStepSummeryData.IsPass = IsPass;
                eolStepSummeryData.TestValue = value;
				eolStepSummeryData.ErrorDescription = ErrorMessage;
                EOLStepSummerysList.Add(eolStepSummeryData);
                IsPass = true;
            }
            catch (Exception ex)
            {
                LoggerService.Error(this, "Failed to execute CompareBit", ex);
                IsPass = false;
                ErrorMessage += ex.Message;
            }
            finally
            {
                //finished derived class execute method
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
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

            if(Parameter != null)
            {
                string stepDescription = headers[0].Trim('\"');

                string description =
                        $"{stepDescription}\r\nGet {Parameter?.Name}";

                headers.Add($"\"{description}\"");
            }

            return headers;
        }

        public override List<string> GetReportValues()
        {
            List<string> values = base.GetReportValues();

            EOLStepSummeryData stepSummeryData =
                EOLStepSummerysList.Find((e) =>
                    !string.IsNullOrEmpty(e.Description) && e.Description.Contains(Parameter.Name));

            if (stepSummeryData != null)
                values.Add(stepSummeryData.TestValue.ToString());
            else
                values.Add("");

            IsExecuted = false;

            return values;
        }

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();

            if (Parameter is DeviceParameterData deviceParameter)
            {
                UsedDevices.Add(deviceParameter.DeviceType);
            }
            return UsedDevices;
        }
    }
}
