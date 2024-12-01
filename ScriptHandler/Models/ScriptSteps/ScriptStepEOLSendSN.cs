
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;

namespace ScriptHandler.Models.ScriptSteps
{
    public class ScriptStepEOLSendSN : ScriptStepBase, IScriptStepWithCommunicator
    {
        #region Properties and Fields

        public DeviceParameterData SN_Param { get; set; }
        public DeviceCommunicator Communicator { get; set; }

        public string SerialNumber { get; set; }
        public string UserSN { get; set; }

        private ScriptStepSetParameter _setValue;
        private ScriptStepGetParamValue _getValue;
        private ScriptStepSetSaveParameter _saveValue;

        #endregion Properties and Fields

        #region Constructor

        public ScriptStepEOLSendSN()
        {
            _totalNumOfSteps = 4;
            UserSN = "E7P-22-00-00032";
        }

        #endregion Constructor

        #region Methods

        public override void Execute()
        {
            //Get SN from UI - Temp userSN

            //remove daash and letters
            _setValue = new ScriptStepSetParameter();
            _getValue = new ScriptStepGetParamValue();
            _saveValue = new ScriptStepSetSaveParameter();

            _isExecuted = true;

            _stepsCounter = 1;

            if (_setValue != null && _getValue != null && _saveValue != null)
            {
                _getValue.EOLReportsSelectionData = EOLReportsSelectionData;
                _setValue.EOLReportsSelectionData = EOLReportsSelectionData;
                _saveValue.EOLReportsSelectionData = EOLReportsSelectionData;
            }

            EOLStepSummeryData eolStepSummeryData;

            SerialNumber = Regex.Replace(SerialNumber, "[A-Za-z ]", "");
            SerialNumber = SerialNumber.Remove(0, 1);
            SerialNumber = SerialNumber.Replace("-", "");

            //Set SN
            _setValue.Parameter = SN_Param;
            _setValue.Communicator = Communicator;
            _setValue.Value = SerialNumber;
            _setValue.Execute();
            EOLStepSummerysList.AddRange(_setValue.EOLStepSummerysList);

            string description = Description;
            if (!string.IsNullOrEmpty(UserTitle))
                description = UserTitle;

            if (!_setValue.IsPass)
            {
                ErrorMessage = "Unable to set: " + SN_Param.Name;
                IsPass = false;
                eolStepSummeryData = new EOLStepSummeryData(
                    "",
                    description,
                    this);
                eolStepSummeryData.IsPass = IsPass;
                eolStepSummeryData.ErrorDescription = ErrorMessage;
                EOLStepSummerysList.Add(eolStepSummeryData);
                return;
            }

            _stepsCounter++;

            //Verify SN- get

            _getValue.Parameter = SN_Param;
            _getValue.Communicator = Communicator;
            _getValue.SendAndReceive(out eolStepSummeryData, Description);
            EOLStepSummerysList.Add(eolStepSummeryData);
            if (_getValue.IsPass)
            {
                //Validate SN
                if (SN_Param.Value as string == UserSN)
                {
                    ErrorMessage = "Wrong SN \r\n"
                    + _getValue.ErrorMessage;
                    IsPass = false;
                    eolStepSummeryData = new EOLStepSummeryData(
                        "",
                        description,
                        this);
                    eolStepSummeryData.IsPass = IsPass;
                    eolStepSummeryData.ErrorDescription = ErrorMessage;
                    EOLStepSummerysList.Add(eolStepSummeryData);
                    return;
                }
            }
            else
            {
                ErrorMessage = "Failed to get the SN parameter";
                IsPass = false;
                eolStepSummeryData = new EOLStepSummeryData(
                        "",
                        description,
                        this);
                eolStepSummeryData.IsPass = IsPass;
                eolStepSummeryData.ErrorDescription = ErrorMessage;
                EOLStepSummerysList.Add(eolStepSummeryData);
                return;
            }

            _stepsCounter++;

            //If succeed save param

            _saveValue.Parameter = SN_Param;
            _saveValue.Communicator = Communicator;
            _saveValue.Value = Convert.ToDouble(SerialNumber);
            _saveValue.Execute();
            EOLStepSummerysList.AddRange(_saveValue.EOLStepSummerysList);

            if (!_saveValue.IsPass)
            {
                IsPass = false;
                ErrorMessage = "Unable to save SN: " + _saveValue.ErrorMessage;
                return;
            }
            IsPass = true;

            AddToEOLSummary();

            return;
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

        }

        public override void GetRealParamAfterLoad(
            DevicesContainer devicesContainer)
        {
            SN_Param = new MCU_ParamData()
            {
                Name = "Serial Number",
                Cmd = "serialnumber",
                DeviceType = Entities.Enums.DeviceTypesEnum.MCU
            };

            SN_Param = GetRealParam(
                SN_Param,
                devicesContainer);
        }

        public override List<string> GetReportHeaders()
        {
            List<string> headers = base.GetReportHeaders();

            string stepDescription = headers[0].Trim('\"');

            string description =
                    $"{stepDescription}\r\nSet {SN_Param.Name} = {SerialNumber}";
            headers.Add($"\"{description}\"");

            description =
                    $"{stepDescription}\r\nGet {SN_Param.Name}";
            headers.Add($"\"{description}\"");



            return headers;
        }

        public override List<string> GetReportValues()
        {
            List<string> values = base.GetReportValues();

            values.Add(SerialNumber);


            EOLStepSummeryData stepSummeryData =
                EOLStepSummerysList.Find((e) =>
                    !string.IsNullOrEmpty(e.Description) && e.Description.Contains(SN_Param.Name));

            if (stepSummeryData != null)
                values.Add(stepSummeryData.TestValue.ToString());
            else
                values.Add("");

            _isExecuted = false;

            return values;
        }

        #endregion Methods
    }
}
