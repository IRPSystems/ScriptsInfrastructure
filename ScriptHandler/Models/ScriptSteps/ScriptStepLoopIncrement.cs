using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Enums;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

namespace ScriptHandler.Models
{
    public class ScriptStepLoopIncrement : ScriptStepGetParamValue
    {
        private bool _isStoped;
        private ScriptStepSetParameter _setParameter;
        private ScriptStepDelay _delayparameter;

        public double IncrementValue { get; set; }
        public double SetFirstValue { get; set; }
        public int LoopsAmount { get; set; }

        public int Interval;

        public TimeUnitsEnum IntervalUnite;

		public ExtraDataForParameter ExtraData { get; set; }


		public ScriptStepLoopIncrement()
        {
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
            _setParameter = new ScriptStepSetParameter();
            _delayparameter = new ScriptStepDelay();
            _totalNumOfSteps = 3;
        }



        public override void Execute()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _isStoped = false;
                IsPass = true;
                IsExecuted = true;
                int counter = LoopsAmount;
                double value;

                _stepsCounter = 1;

                _setParameter.Communicator = Communicator;
                _setParameter.Parameter = Parameter;

                _delayparameter.Interval = Interval;
                _delayparameter.IntervalUnite = IntervalUnite;

                ErrorMessage = "";

                if (ExtraData != null)
                    ExtraData.SetToParameter(Parameter);


                value = Convert.ToDouble(SetFirstValue);
                _setParameter.Value = value;
                _setParameter.Execute();
                EOLStepSummerysList.AddRange(_setParameter.EOLStepSummerysList);
                if (!_setParameter.IsPass)
                {
                    IsError = true;
                    ErrorMessage += _setParameter.ErrorMessage;
                    IsPass = false;
                    return;
                }


                //EOLStepSummeryData eolStepSummeryData;
                //bool isOK = SendAndReceive(out eolStepSummeryData);
                ////EOLStepSummerysList.Add(eolStepSummeryData);
                //if (!isOK)
                //{
                //    LoggerService.Inforamtion(this, "Failed SendAndReceive");
                //    IsPass = false;
                //    return;
                //}

                if (_isStoped)
                    return;

                while (counter > 0)
                {
                    value += IncrementValue;
                    _setParameter.Value = value;
                    _setParameter.Execute();
                    if(!_setParameter.IsPass)
                    {
                        IsError = true;
                        ErrorMessage += _setParameter.ErrorMessage;
                        IsPass = false;
                        return;
                    }
                    _delayparameter.Execute();
                    counter--;
                }



                _stepsCounter++;

                EOLStepSummerysList.AddRange(_setParameter.EOLStepSummerysList);

                //_waitForGet.Reset();
                //Communicator.SetParamValue(Parameter, value, GetValueCallback);

                //bool isNotTimeOut = _waitForGet.WaitOne(2000);
                //_waitForGet.Reset();
                //if(!isNotTimeOut)
                //	IsPass = false;

                AddToEOLSummary();
            }
            finally
            {
                //finished derived class execute method
                stopwatch.Stop();
                ExecutionTime = stopwatch.Elapsed;
            }
        }

        private void GetValueCallback(DeviceParameterData param, CommunicatorResultEnum result, string resultDescription)
        {
            IsPass = result == CommunicatorResultEnum.OK;
            if (!IsPass) { }
            _waitForGet.Set();
        }

        protected override void Stop()
        {
            _isStoped = true;
            _waitForGet.Set();
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
            Parameter = (sourceNode as ScriptNodeLoopIncrement).Parameter;
            IncrementValue = (sourceNode as ScriptNodeLoopIncrement).IncrementValue;
            SetFirstValue = (sourceNode as ScriptNodeLoopIncrement).SetFirstValue;
            LoopsAmount = (sourceNode as ScriptNodeLoopIncrement).LoopsAmount;
            Interval = (sourceNode as ScriptNodeLoopIncrement).Interval;
            IntervalUnite = (sourceNode as ScriptNodeLoopIncrement).IntervalUnite;

			ExtraData = new ExtraDataForParameter((sourceNode as ScriptNodeLoopIncrement).ExtraData);
		}

        public override List<DeviceTypesEnum> GetUsedDevices()
        {
            List<DeviceTypesEnum> UsedDevices = new List<DeviceTypesEnum>();
            if(Parameter != null)
            {
                UsedDevices.Add(Parameter.DeviceType);
            }
            return UsedDevices;
        }
    }
}
