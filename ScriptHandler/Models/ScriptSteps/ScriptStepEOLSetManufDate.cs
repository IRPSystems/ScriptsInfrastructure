
using DeviceCommunicators.CANBus;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.SwitchRelay32;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using LibUsbDotNet.DeviceNotify;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScriptHandler.Models.ScriptSteps
{
    public class ScriptStepEOLSetManufDate : ScriptStepBase, IScriptStepWithCommunicator
    {
        #region Properties and Fields

        public DeviceParameterData Parameter { get; set; }
        public DeviceCommunicator Communicator { get; set; }
        public DeviceTypesEnum DeviceType { get; set; }
        public DevicesContainer DevicesContainer { get; set; }

        private ScriptStepGetParamValue _getValue;
        private ScriptStepSetParameter _setValue;
        private ScriptStepSetSaveParameter _saveValue;

        private string ManufDayParam = "ecumanday";
        private string ManufMonthParam = "ecumanmonth";
        private string ManufYearParam = "ecumanyear";

        private int ManufDay;
        private int ManufMonth;
        private int ManufYear;

        private Dictionary<string, int> manufParams;


        #endregion Properties and Fields

        #region Constructor

        public ScriptStepEOLSetManufDate()
        {
            _totalNumOfSteps = 4;

            DateTime now = DateTime.Now;
            ManufDay = now.Day;
            ManufMonth = now.Month;
            ManufYear = now.Year;

            manufParams = new Dictionary<string, int>
            {
                { ManufDayParam, ManufDay },
                { ManufMonthParam, ManufMonth },
                { ManufYearParam, ManufYear }
            };
        }

        #endregion Constructor

        #region Methods

        public override void Execute()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            EOLStepSummeryData eolStepSummeryData;
            string description = Description;

            try
            {
                _getValue = new ScriptStepGetParamValue();
                _setValue = new ScriptStepSetParameter();
                _saveValue = new ScriptStepSetSaveParameter();


                if (DevicesContainer != null)
                {
                    DeviceFullData devicefulldata = DevicesContainer.DevicesFullDataList.FirstOrDefault(d => d.Device.DeviceType == DeviceType);
                    Communicator = devicefulldata?.DeviceCommunicator;
                    Parameter.Device = ((devicefulldata as DeviceFullData_CANBus).Device as CANBus_DeviceData).DeviceDataList.FirstOrDefault(d => d.DeviceType == DeviceTypesEnum.MCU);
                }

                IsExecuted = true;
                _stepsCounter = 1;

                if (_getValue != null && _saveValue != null)
                {
                    _getValue.EOLReportsSelectionData = EOLReportsSelectionData;
                    _saveValue.EOLReportsSelectionData = EOLReportsSelectionData;
                }

                // Probe day: if already set (non-zero), exit early
                //Parameter = new MCU_ParamData()
                //{
                //    Name = "ManufDate",
                //    Cmd = "ecumanday",
                //    DeviceType = DeviceType
                //};

                _getValue.Parameter = Parameter;
                _getValue.Communicator = Communicator;
                _getValue.SendAndReceive(out eolStepSummeryData, Description);

                if (_getValue.IsPass)
                {
                    _stepsCounter++;
                    // Use int comparison consistently
                    if (Convert.ToInt32(Parameter.Value) != 0)
                    {
                        IsPass = true;
                        eolStepSummeryData = new EOLStepSummeryData("", string.IsNullOrEmpty(UserTitle) ? description : UserTitle, this)
                        {
                            IsPass = IsPass,
                            ErrorDescription = ErrorMessage
                        };
                        EOLStepSummerysList.Add(eolStepSummeryData);
                        return;
                    }
                }
                else
                {
                    ErrorMessage = "Failed to get the Manufacture Day parameter";
                    IsPass = false;
                    eolStepSummeryData = new EOLStepSummeryData("", string.IsNullOrEmpty(UserTitle) ? description : UserTitle, this)
                    {
                        IsPass = IsPass,
                        ErrorDescription = ErrorMessage
                    };
                    IsError = true;
                    EOLStepSummerysList.Add(eolStepSummeryData);
                    return;
                }

                // Set and save each param (day, month, year)
                foreach (KeyValuePair<string, int> kvp in manufParams)
                {
                    _stepsCounter++;

                    // Build the specific param to set (same Name/Cmd)
                    DeviceParameterData currentParam = new MCU_ParamData()
                    {
                        Name = kvp.Key,
                        Cmd = kvp.Key,
                        DeviceType = Entities.Enums.DeviceTypesEnum.MCU,
                        IsInCANBus = Parameter.IsInCANBus
                    };

                    if (DevicesContainer != null)
                        currentParam.Device = Parameter.Device;

                    // SET
                    _setValue.Parameter = currentParam;
                    _setValue.Communicator = Communicator;
                    _setValue.Value = kvp.Value;              // use intended value, not currentParam.Value
                    _setValue.Execute();
                    EOLStepSummerysList.AddRange(_setValue.EOLStepSummerysList);

                    if (!_setValue.IsPass)
                    {
                        ErrorMessage = "Unable to set: " + currentParam.Name;
                        IsPass = false;
                        eolStepSummeryData = new EOLStepSummeryData("", string.IsNullOrEmpty(UserTitle) ? description : UserTitle, this)
                        {
                            IsPass = IsPass,
                            ErrorDescription = ErrorMessage
                        };
                        IsError = true;
                        EOLStepSummerysList.Add(eolStepSummeryData);
                        return;
                    }

                    // SAVE — save the same param you just set, and the same value you intended to set
                    _stepsCounter++;
                    _saveValue.Parameter = currentParam;      // <-- bug fix: was 'Parameter'
                    _saveValue.Communicator = Communicator;
                    _saveValue.Value = kvp.Value;             // don't rely on currentParam.Value being updated
                    _saveValue.Execute();
                    EOLStepSummerysList.AddRange(_saveValue.EOLStepSummerysList);

                    if (!_saveValue.IsPass)
                    {
                        IsPass = false;
                        IsError = true;
                        ErrorMessage = "Unable to save " + currentParam.Name + ": " + _saveValue.ErrorMessage;
                        return;
                    }
                }

                IsPass = true;
                AddToEOLSummary();
                return;
            }
            catch (Exception ex)
            {
                // Handle/log as needed
                IsPass = false;
                IsError = true;
                ErrorMessage = ex.Message;

                eolStepSummeryData = new EOLStepSummeryData(
                        "",
                        description,
                        this);
                eolStepSummeryData.IsPass = IsPass;
                eolStepSummeryData.ErrorDescription = ErrorMessage;
                IsError = true;
                EOLStepSummerysList.Add(eolStepSummeryData);
                return; 
            }
            finally
            {
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
            }
        }

        protected override void Stop()
        {

        }

        protected override void Generate(
            ScriptNodeBase sourceNode,
            Dictionary<int, ScriptStepBase> stepNameToObject,
            ref List<DeviceCommunicator> usedCommunicatorsList,
            GenerateProjectService generateService,
            DevicesContainer devicesContainer)
        {
            DeviceType = (sourceNode as ScriptNodeEOLSetManufDate).DeviceType;
        }
        public override void GetRealParamAfterLoad(
            DevicesContainer devicesContainer)
        {
            Parameter = new MCU_ParamData()
            {
                Name = "ManufDate",
                Cmd = "ecumanday",
                DeviceType = DeviceTypesEnum.MCU
            };
            if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.CANBus))
            {
                Parameter.IsInCANBus = true;
            }

            DevicesContainer = devicesContainer;
        }
        public override List<string> GetReportHeaders()
        {
            List<string> headers = base.GetReportHeaders();

            string stepDescription = headers[0].Trim('\"');

            if (Parameter != null)
            {
                string description =
                $"{stepDescription}\r\nSet {Parameter.Name} = {Parameter}";
                headers.Add($"\"{description}\"");

                description =
                        $"{stepDescription}\r\nGet {Parameter.Name}";
                headers.Add($"\"{description}\"");
            }

            return headers;
        }

        public override List<string> GetReportValues()
        {
            List<string> values = base.GetReportValues();

            //values.Add(SerialNumber);


            //EOLStepSummeryData stepSummeryData =
            //    EOLStepSummerysList.Find((e) =>
            //        !string.IsNullOrEmpty(e.Description) && e.Description.Contains(Parameter.Name));

            //if (stepSummeryData != null)
            //    values.Add(stepSummeryData.TestValue.ToString());
            //else
            //    values.Add("");

            //IsExecuted = false;

            return values;
        }

        #endregion Methods
    }
}
