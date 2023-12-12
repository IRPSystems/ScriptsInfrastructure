
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Timers;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCANMessage : ScriptStepBase, IScriptStepWithCommunicator, IScriptStepContinuous
	{
		#region Properties

		public DeviceCommunicator Communicator { get; set; }

		public uint NodeId { get; set; }

		public string MessageName { get; set; }

		#region Repeat

		public bool IsOneTime { get; set; }
		public bool IsCyclic { get; set; }

		public int Interval { get; set; }
		public TimeUnitsEnum IntervalUnite { get; set; }

		public bool IsStopByTime { get; set; }
		public bool IsStopByInterations { get; set; }
		public bool IsStopNever { get; set; }

		//public int RepeatTimeLength { get; set; }
		public int Iterations { get; set; }


		public int RepeateLengthTime { get; set; }
		public TimeUnitsEnum RepeateLengthTimeUnite { get; set; }

		public int RepeateLengthIterations { get; set; }

		#endregion Repeat



		public bool IsDBCFile { get; set; }
		public bool IsFreeStyle { get; set; }

		public string DBCFilePath { get; set; }


		public ulong Payload { get; set; }

		public ulong NumOfMessages { get; set; }

		public int IDInProject { get; set; }

		#endregion Properties

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;



		private System.Timers.Timer _sendIntervalTimer;
		private TimeSpan _tsInterval;
		private byte[] _payloadBytes;
		private int _iterationsCount;
		private TimeSpan _stopTime;
		private DateTime _startTime;


		private ManualResetEvent _messageEnd;

		private object _lockPayloadObj;
		private int _counter;

		#endregion Fields

		#region Constructor

		public ScriptStepCANMessage()
		{
			Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;

			_sendIntervalTimer = new System.Timers.Timer();
			_sendIntervalTimer.Elapsed += ElapsedEventHandler;

			_messageEnd = new ManualResetEvent(false);

			_lockPayloadObj = new object();

			IntervalUnite = TimeUnitsEnum.sec;
			RepeateLengthTimeUnite = TimeUnitsEnum.sec;
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{			
			IsPass = true;

			NumOfMessages = 0;

			if (Communicator == null)
			{
				IsPass = false;
				ErrorMessage = "Unknown communicator";
				return;
			}

			
			try
			{

			
				_cancellationTokenSource = new CancellationTokenSource();
				_cancellationToken = _cancellationTokenSource.Token;

				


				switch (IntervalUnite)
				{
					case TimeUnitsEnum.ms: _tsInterval = new TimeSpan(0, 0, 0, 0, Interval); break;
					case TimeUnitsEnum.sec: _tsInterval = new TimeSpan(0, 0, 0, Interval, 0); break;
					case TimeUnitsEnum.min: _tsInterval = new TimeSpan(0, 0, Interval, 0, 0); break;
					case TimeUnitsEnum.hour: _tsInterval = new TimeSpan(0, Interval, 0, 0, 0); break;
				}

				switch (RepeateLengthTimeUnite)
				{
					case TimeUnitsEnum.ms: _stopTime = new TimeSpan(0, 0, 0, 0, RepeateLengthTime); break;
					case TimeUnitsEnum.sec: _stopTime = new TimeSpan(0, 0, 0, RepeateLengthTime, 0); break;
					case TimeUnitsEnum.min: _stopTime = new TimeSpan(0, 0, RepeateLengthTime, 0, 0); break;
					case TimeUnitsEnum.hour: _stopTime = new TimeSpan(0, RepeateLengthTime, 0, 0, 0); break;
				}

				_payloadBytes = BitConverter.GetBytes(Payload);
				_iterationsCount = 0;

				_startTime = DateTime.Now;

				_sendIntervalTimer.Enabled = true;
				_sendIntervalTimer.Interval = _tsInterval.TotalMilliseconds;
				_sendIntervalTimer.Start();
			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Failed to int sending of message", ex);
			}
		}

		
		private void ElapsedEventHandler(object sender, ElapsedEventArgs e)
		{
			
			NumOfMessages++;

			bool isExtendedId = false;
			NumericTypes numericTypes =  NumericService.GetTypeOfUsedBytes(NodeId);
			if (numericTypes == NumericTypes.Byte || numericTypes == NumericTypes.Short)
				isExtendedId = false;
			else if (numericTypes == NumericTypes.Integer || numericTypes == NumericTypes.Long)
				isExtendedId = true;

			lock (_lockPayloadObj)
				Communicator.SendMessage(isExtendedId, NodeId, _payloadBytes, MessageCallback);

			int eventThatSignaledIndex =
				WaitHandle.WaitAny(
					new WaitHandle[] { _messageEnd, _cancellationToken.WaitHandle }, 2000);
			_messageEnd.Reset();
			if(eventThatSignaledIndex == WaitHandle.WaitTimeout)
			{
				_counter++;

				if (_counter > 3)
				{
					ContinuousErrorEvent?.Invoke(Description + "\r\n\r\nNo response from the communicator");
					IsPass = false;
					End(true);
					return;
				}
			}

			if (IsStopByInterations)
			{
				_iterationsCount++;
				if (_iterationsCount >= Iterations)
				{
					End(true);
					return;
				}
			}
			else if(IsStopByTime)
			{
				TimeSpan diff = DateTime.Now - _startTime;
				if(diff >= _stopTime)
				{
					End(true);
					return;
				}
			}
		}

		
		private void End(bool isPassed)
		{
			_sendIntervalTimer.Stop();

			if(_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();

			IsPass = isPassed;
			ContinuousEndedEvent?.Invoke(this);
		}

		public void StopContinuous()
		{
			End(IsPass);
		}

		private void MessageCallback(DeviceParameterData param, CommunicatorResultEnum result, string errorMessage)
		{
			if(result != CommunicatorResultEnum.OK)
			{
				ContinuousErrorEvent?.Invoke(Description + "\r\n\r\n" + errorMessage);
				End(false);
			}

			_messageEnd.Set();
		}

		public void UpdatePayload(ulong payload)
		{
			lock(_lockPayloadObj)
				_payloadBytes = BitConverter.GetBytes(payload);
			Payload = BitConverter.ToUInt64(_payloadBytes, 0);
		}

		public void UpdateInterval(
			int interval,
			TimeUnitsEnum intervalUnite)
		{
			lock (_lockPayloadObj)
			{
				switch (intervalUnite)
				{
					case TimeUnitsEnum.ms: _tsInterval = new TimeSpan(0, 0, 0, 0, interval); break;
					case TimeUnitsEnum.sec: _tsInterval = new TimeSpan(0, 0, 0, interval, 0); break;
					case TimeUnitsEnum.min: _tsInterval = new TimeSpan(0, 0, interval, 0, 0); break;
					case TimeUnitsEnum.hour: _tsInterval = new TimeSpan(0, interval, 0, 0, 0); break;
				}

				_sendIntervalTimer.Stop();
				_sendIntervalTimer.Interval = _tsInterval.TotalMilliseconds;
				_sendIntervalTimer.Start();
			}

		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (NodeId == 0)
				return true;

			if (IsCyclic)
			{
				if (Interval <= 0)
					return true;
			}

			return false;
		}

		public override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			NodeId = (sourceNode as ScriptNodeCANMessage).CANID;
			MessageName = (sourceNode as ScriptNodeCANMessage).MessageName;
			IsOneTime = (sourceNode as ScriptNodeCANMessage).IsOneTime;
			IsCyclic = (sourceNode as ScriptNodeCANMessage).IsCyclic;
			Interval = (sourceNode as ScriptNodeCANMessage).Interval;
			IntervalUnite = (sourceNode as ScriptNodeCANMessage).IntervalUnite;
			IsStopByTime = (sourceNode as ScriptNodeCANMessage).IsStopByTime;
			IsStopByInterations = (sourceNode as ScriptNodeCANMessage).IsStopByInterations;
			IsStopNever = (sourceNode as ScriptNodeCANMessage).IsStopNever;
			Iterations = (sourceNode as ScriptNodeCANMessage).Iterations;
			RepeateLengthTime = (sourceNode as ScriptNodeCANMessage).RepeateLengthTime;
			RepeateLengthTimeUnite = (sourceNode as ScriptNodeCANMessage).RepeateLengthTimeUnite;
			RepeateLengthIterations = (sourceNode as ScriptNodeCANMessage).RepeateLengthIterations;
			IsDBCFile = (sourceNode as ScriptNodeCANMessage).IsDBCFile;
			IsFreeStyle = (sourceNode as ScriptNodeCANMessage).IsFreeStyle;
			DBCFilePath = (sourceNode as ScriptNodeCANMessage).DBCFilePath;
			Payload = (sourceNode as ScriptNodeCANMessage).Payload.NumericValue;
			IDInProject = (sourceNode as ScriptNodeCANMessage).IDInProject;
		}

		#endregion Methods

		#region Events

		public event Action<IScriptStepContinuous> ContinuousEndedEvent;
		public event Action<string> ContinuousErrorEvent;

		#endregion Events
	}
}
