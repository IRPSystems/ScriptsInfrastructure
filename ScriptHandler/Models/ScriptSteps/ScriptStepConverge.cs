
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace ScriptHandler.Models.ScriptSteps
{
	public class ScriptStepConverge : ScriptStepGetParamValue
	{
		public enum ExecuteStateEnum { None, Start, WaitToEnterValue, WaitForConvergeTime, End }


		#region Properties

		[JsonIgnore]
		public DeviceCommunicator TargetValueCommunicator { get; set; }

		public TimeUnitsEnum ConvergeTimeUnite { get; set; }
		public int ConvergeTime { get; set; }

		public TimeUnitsEnum TimeoutTimeUnite { get; set; }
		public int Timeout { get; set; }

		public object TargetValue { get; set; }
		public double Tolerance { get; set; }


		public TimeSpan Diff_WaitToEnterValue { get; set; }
		public TimeSpan TsTimeout { get; set; }
		public int TimeoutPercentage { get; set; }

		public TimeSpan Diff_WaitForConvergeTime { get; set; }
		public TimeSpan TsConvergeTime { get; set; }
		public int ConvergeTimePercentage { get; set; }

		#endregion Properties

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private ExecuteStateEnum _executeState;

		private DateTime _startWaitToEnter;
		private System.Timers.Timer _checkValueTimer;

		
		private double _maxVal;
		private double _minVal;

		private bool _isStartingState;		
		private DateTime _startConvergeTime;

		private TimeSpan _pauseTime;
		private DateTime _waitToEnterValueTime;
		private DateTime _waitForConvergeTime;

		private string _errorMessageHeader;

		private ScriptStepGetParamValue _targetValueGetter;

		#endregion Fields

		#region Constructor

		public ScriptStepConverge()
		{
			Template = Application.Current.MainWindow.FindResource("ConvergeTemplate") as DataTemplate;
			_checkValueTimer = new System.Timers.Timer(500);
			_checkValueTimer.Elapsed += checkValueTimer_ElapsedEventHandler;
			_executeState = ExecuteStateEnum.None;

			_targetValueGetter = new ScriptStepGetParamValue();
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			_executeState = ExecuteStateEnum.Start;

			_errorMessageHeader = "Converge \"" + Parameter + "\" to ";

			TimeoutPercentage = 0;
			ConvergeTimePercentage = 0;

			Diff_WaitToEnterValue = new TimeSpan();
			Diff_WaitForConvergeTime = new TimeSpan();

			switch (TimeoutTimeUnite)
			{
				case TimeUnitsEnum.ms: TsTimeout = new TimeSpan(0, 0, 0, 0, Timeout); break;
				case TimeUnitsEnum.sec: TsTimeout = new TimeSpan(0, 0, 0, Timeout, 0); break;
				case TimeUnitsEnum.min: TsTimeout = new TimeSpan(0, 0, Timeout, 0, 0); break;
				case TimeUnitsEnum.hour: TsTimeout = new TimeSpan(0, Timeout, 0, 0, 0); break;
			}

			switch (ConvergeTimeUnite)
			{
				case TimeUnitsEnum.ms: TsConvergeTime = new TimeSpan(0, 0, 0, 0, ConvergeTime); break;
				case TimeUnitsEnum.sec: TsConvergeTime = new TimeSpan(0, 0, 0, ConvergeTime, 0); break;
				case TimeUnitsEnum.min: TsConvergeTime = new TimeSpan(0, 0, ConvergeTime, 0, 0); break;
				case TimeUnitsEnum.hour: TsConvergeTime = new TimeSpan(0, ConvergeTime, 0, 0, 0); break;
			}


			IsPass = true;


			SetMinMaxTarget();



			_pauseTime = new TimeSpan();

			var myTask = Task.Factory
					.StartNew(() => Execute_Do(), _cancellationTokenSource.Token);


			myTask.Wait();

			//IsPass = true;
		}

		private void SetMinMaxTarget()
		{
			double targetValue = GetTargetValue();
			if (!IsPass)
			{
				ErrorMessage += "Failed to get the target value";
				IsPass = false;
				return;
			}

			_maxVal = targetValue + Tolerance;
			_minVal = targetValue - Tolerance;

			LoggerService.Inforamtion(this, "Target: Value=" + targetValue + "; Max=" + _maxVal + "; Min=" + _minVal);
		}

		private double GetTargetValue()
		{
			
			try
			{
				if (TargetValue is DeviceParameterData param)
				{

					_targetValueGetter.Communicator = TargetValueCommunicator;
					_targetValueGetter.Parameter = param;

					string tempErrMessage = _errorMessageHeader + "\"" + param + "\"\r\n\r\n";
					bool isOK = _targetValueGetter.SendAndReceive(param);					
					if (!isOK)
					{
						ErrorMessage = tempErrMessage + ErrorMessage;
						IsPass = false;
						return 0;
					}

					if (param.Value == null)
					{
						ErrorMessage = tempErrMessage + ErrorMessage;
						IsPass = false;
						return 0;
					}

					double dVal = Convert.ToDouble(param.Value);
					_errorMessageHeader += "(\"" + param + "\" = " + dVal + ")\r\n\r\n";
					return dVal;
				}
				else if (TargetValue is string str)
				{
					_errorMessageHeader = _errorMessageHeader + str + "\r\n\r\n";
					double dVal;
					bool res = double.TryParse(str, out dVal);
					if (!res)
					{
						IsPass = false;
						return 0;
					}

					return dVal;
				}
				else
				{
					_errorMessageHeader = _errorMessageHeader + TargetValue + "\r\n\r\n";
					return (double)TargetValue;
				}


			}
			catch (Exception ex) 
			{
				LoggerService.Error(this, "Failed to get the target value", ex);
				ErrorMessage += _errorMessageHeader + "\r\nFailed to get the target value";
				End(false);
				return 0;
			}

			
		}

		private void Execute_Do()
		{
			if(Parameter == null)
			{
				ErrorMessage = "Converge: No parameter defined";
				IsPass = false;
				return;
			}


			ErrorMessage = "\"" + Parameter.Name + "\" = " + TargetValue + " ± " + Tolerance;
			while (!_cancellationToken.IsCancellationRequested)
			{
				switch(_executeState)
				{
					case ExecuteStateEnum.Start:
						_startWaitToEnter = DateTime.Now;
						_checkValueTimer.Start();

						_executeState = ExecuteStateEnum.WaitToEnterValue;
						break;

					case ExecuteStateEnum.WaitToEnterValue:

						_waitToEnterValueTime = DateTime.Now;
						Diff_WaitToEnterValue = (_waitToEnterValueTime - _startWaitToEnter) - _pauseTime;
						TimeoutPercentage = (int)((Diff_WaitToEnterValue.TotalMilliseconds / TsTimeout.TotalMilliseconds) * 100);
						if (Diff_WaitToEnterValue.TotalMilliseconds >= TsTimeout.TotalMilliseconds)
						{
							ErrorMessage = _errorMessageHeader + ErrorMessage + "\r\nFailed to get to the converge before timeout";
							End(false);
						}
						break;

					case ExecuteStateEnum.WaitForConvergeTime:
						if(_isStartingState)
						{
							_isStartingState = false;
							_startConvergeTime = DateTime.Now;
							break;
						}

						_waitForConvergeTime = DateTime.Now;
						Diff_WaitForConvergeTime = (_waitForConvergeTime - _startConvergeTime) - _pauseTime;
						ConvergeTimePercentage = (int)((Diff_WaitForConvergeTime.TotalMilliseconds / TsConvergeTime.TotalMilliseconds) * 100);
						if (Diff_WaitForConvergeTime.TotalMilliseconds >= TsConvergeTime.TotalMilliseconds)
						{
							End(true);
						}
						break;
				}

				System.Threading.Thread.Sleep(1);
			}			
		}

		private void End(bool isPass)
		{
			if(IsPass) 
				LoggerService.Inforamtion(this, "End " + IsPass);
			else
				LoggerService.Inforamtion(this, "End " + IsPass + " " + ErrorMessage);
			_cancellationTokenSource.Cancel();
			IsPass = isPass;
			_executeState = ExecuteStateEnum.None;
			_checkValueTimer.Stop();
		}

		private void checkValueTimer_ElapsedEventHandler(object sender, ElapsedEventArgs e)
		{
			bool isOK = SendAndReceive();
			if (!isOK)
			{
				ErrorMessage = _errorMessageHeader + ErrorMessage +  "\r\nCommunication error";
				End(false);
				return;
			}

			ErrorMessage = "";

			if (Parameter.Value == null) 
			{
				return;
			}

			SetMinMaxTarget();

			double dVal = (double)Parameter.Value;
			LoggerService.Inforamtion(this, "Parm Value=" + dVal);

			if (dVal <= _maxVal && dVal >= _minVal)
			{
				if(_executeState == ExecuteStateEnum.WaitToEnterValue)
				{
					_isStartingState = true;
					_executeState = ExecuteStateEnum.WaitForConvergeTime;
				}
			}
			else
			{
				if(_executeState == ExecuteStateEnum.WaitForConvergeTime)
				{
					ErrorMessage = _errorMessageHeader + ErrorMessage + "\r\nValue is out of range during the converge time";
					End(false);
				}
			}
		}

		protected override void Stop()
		{
			_cancellationTokenSource?.Cancel();
		}

		public override void Resume()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			if(_executeState == ExecuteStateEnum.WaitToEnterValue)
				_pauseTime = DateTime.Now - _waitToEnterValueTime;
			else if (_executeState == ExecuteStateEnum.WaitForConvergeTime)
				_pauseTime = DateTime.Now - _waitForConvergeTime;

			_checkValueTimer.Start();

			var myTask = Task.Factory
					.StartNew(() => Execute_Do(), _cancellationTokenSource.Token);


			myTask.Wait();
		}

		public void SetTargetValueCommunicator(DevicesContainer devicesContainer)
		{
			if (!(TargetValue is DeviceParameterData param))
				return;

			if (devicesContainer.TypeToDevicesFullData.ContainsKey(param.DeviceType) == false)
				return;


			DeviceFullData fullData = devicesContainer.TypeToDevicesFullData[param.DeviceType];
			if (fullData != null)
			{
				TargetValueCommunicator = fullData.DeviceCommunicator;
			}
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


		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeConverge).Parameter;
			ConvergeTimeUnite = (sourceNode as ScriptNodeConverge).ConvergeTimeUnite;
			ConvergeTime = (sourceNode as ScriptNodeConverge).ConvergeTime;
			TimeoutTimeUnite = (sourceNode as ScriptNodeConverge).TimeoutTimeUnite;
			Timeout = (sourceNode as ScriptNodeConverge).Timeout;
			TargetValue = (sourceNode as ScriptNodeConverge).TargetValue;
			Tolerance = (sourceNode as ScriptNodeConverge).Tolerance;


			//if(TargetValue is DeviceParameterData param)
			//{
			//	if (devicesContainer.TypeToDevicesFullData.ContainsKey(param.DeviceType))
			//	{
			//		DeviceFullData fullData = devicesContainer.TypeToDevicesFullData[param.DeviceType];
			//		if(fullData != null) 
			//		{
			//			TargetValueCommunicator = fullData.DeviceCommunicator;
			//		}
			//	}
			//}
		}

		#endregion Methods
	}
}
