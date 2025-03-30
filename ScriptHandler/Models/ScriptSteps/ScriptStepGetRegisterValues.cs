
using Communication.UDS;
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
using System.DirectoryServices.ActiveDirectory;
using System.Reflection.Metadata;
using System.Text;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepGetRegisterValues : ScriptStepGetParamValue
	{
        public string FaultName { get; set; }
		public int ComparedValue { get; set; }

        public TimeSpan ExecutionTime { get; set; }

        public ScriptStepGetRegisterValues()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

        public override void Execute()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                //if (Parameter is IParamWithDropDown dropDown)
                //    FaultName = dropDown.DropDown[BitIndex].Name;

                IsPass = false;
                ErrorMessage = Description;

                IsExecuted = true;

                string description = Description;
                if (string.IsNullOrEmpty(UserTitle) == false)
                    description = UserTitle;

                EOLStepSummeryData eolStepSummeryData;
                bool isOK = SendAndReceive(Parameter, out eolStepSummeryData, description);
                if (!isOK)
                {
                    PopulateSendResponseLog(UserTitle, this.GetType().Name, Parameter.Name, Parameter.DeviceType, Parameter.CommSendResLog);
                    return;
                }
                EOLStepSummerysList.Add(eolStepSummeryData);

                int value = 0;
                //bool res = int.TryParse(str, out value);

                if (int.TryParse(Parameter.Value.ToString(), out int numericValue))
                {
                    if (Parameter is MCU_ParamData param)
                    {
                        // Convert param.Value to an integer
                        numericValue = Convert.ToInt32(param.Value);
                        var sb = new StringBuilder();

                        foreach (var dropdownItem in param.DropDown)
                        {
                            // Convert the dropdown item's Value to an integer
                            int faultBitMask = Convert.ToInt32(dropdownItem.Value);

                            // Check if the bit(s) corresponding to the fault is set
                            if ((numericValue & faultBitMask) != 0)
                            {
                                // Append the fault name with a newline
                                sb.Append(dropdownItem.Name + "\r\n");
                            }
                        }

                        if (sb.Length == 0)
                        {
                            Parameter.Value = "No Bits On";
                        }

                        Parameter.Value = sb.ToString();
                    }
                }
                else
                {
                    ErrorMessage = "Failed to parse the value";
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
                LoggerService.Error(this, "Failed to execute Get Register Value", ex);
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
            Parameter = (sourceNode as ScriptNodeGetRegisterValues).Parameter;
            ComparedValue = (sourceNode as ScriptNodeGetRegisterValues).ComparedValue;
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
