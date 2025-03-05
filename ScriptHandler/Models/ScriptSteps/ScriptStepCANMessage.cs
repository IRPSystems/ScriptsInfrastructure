
using DBCFileParser.Model;
using DeviceCommunicators.Enums;
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using MicroLibrary;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
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

		[JsonIgnore]
		public ulong NumOfMessages { get; set; }

		public int IDInProject { get; set; }

		[JsonIgnore]
		public bool IsAddCRCCounter { get; set; }

		public Message Message { get; set; }

		public bool IsCRCAvailable { get; set; }
		public string CRCFieldName { get; set; }

		public bool IsCounterAvailable { get; set; }
		public string CounterFieldName { get; set; }

		#endregion Properties

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;



		//private MicroTimer _sendIntervalTimer;
		private TimeSpan _tsInterval;
		private byte[] _payloadBytes;
		private int _iterationsCount;
		private TimeSpan _stopTime;
		private DateTime _startTime;


		private ManualResetEvent _messageEnd;

		private object _lockPayloadObj;
		private int _counter;

		private int _counterForCounterField;

		private byte[] _srgCrc8_tab;
		private const byte _CRC8_START_VALUE = 0xFF;
		private const byte _CRC8_XOR = 0xFF;


		#endregion Fields

		#region Constructor

		public ScriptStepCANMessage()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;

			//_sendIntervalTimer = new MicroTimer();
			//_sendIntervalTimer.MicroTimerElapsed += ElapsedEventHandler;

			_messageEnd = new ManualResetEvent(false);

			_lockPayloadObj = new object();

			IntervalUnite = TimeUnitsEnum.sec;
			RepeateLengthTimeUnite = TimeUnitsEnum.sec;

			CRCInit();
		}

		#endregion Constructor


		#region Methods

		public override void Execute()
		{			
			IsPass = true;
			IsExecuted = true;

			NumOfMessages = 0;
			_counterForCounterField = 0;

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

				//_sendIntervalTimer.Enabled = true;
				//_sendIntervalTimer.Interval = (long)(_tsInterval.TotalMilliseconds * 1000);
				//_sendIntervalTimer.Start();

				ElapsedEventHandler();
			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Failed to int sending of message", ex);
			}
		}

		private void ElapsedEventHandler()
		{
			DateTime startTime = DateTime.Now;
			TimeSpan waitTime = _tsInterval;
			Task.Run(() =>
			{
				while (!_cancellationToken.IsCancellationRequested)
				{
					DateTime startTime = DateTime.Now;
					

					try
					{

						NumOfMessages++;

						bool isExtendedId = false;
						NumericTypes numericTypes = NumericService.GetTypeOfUsedBytes(NodeId);
						if (numericTypes == NumericTypes.Byte || numericTypes == NumericTypes.Short)
							isExtendedId = false;
						else if (numericTypes == NumericTypes.Integer || numericTypes == NumericTypes.Long)
							isExtendedId = true;

						if (IsAddCRCCounter)
						{
							AddCrcAndCounter();
						}

						lock (_lockPayloadObj)
							(Communicator as MCU_Communicator).CanService.Send(_payloadBytes, NodeId, isExtendedId);


						if (IsStopByInterations)
						{
							_iterationsCount++;
							if (_iterationsCount >= Iterations)
							{
								End(true);
								return;
							}
						}
						else if (IsStopByTime)
						{
							TimeSpan diff = DateTime.Now - _startTime;
							if (diff >= _stopTime)
							{
								End(true);
								return;
							}
						}

						while ((DateTime.Now - startTime) < waitTime)
						{
							System.Threading.Thread.Sleep(1);
						}
					}
					catch (Exception ex)
					{
						LoggerService.Error(this, "Failed to send CAN message", "Error", ex);
					}
				}
			}, _cancellationToken);
		}

		private void AddCrcAndCounter()
		{
			if (IsCounterAvailable)
			{
				foreach (Signal signal in Message.Signals)
				{
					if (signal.Name != CounterFieldName)
						continue;

					var v = ((ulong)_counterForCounterField << signal.StartBit);
					Payload += v;

					_payloadBytes = BitConverter.GetBytes(Payload);

					_counterForCounterField++;
					if (_counterForCounterField > signal.Maximum)
						_counterForCounterField = 0;
				}

				
			}

			if (IsCRCAvailable)
			{
				byte crc = Crc_CalculateCRC8(_payloadBytes, 8, _CRC8_START_VALUE, true);
				foreach (Signal signal in Message.Signals)
				{
					if (signal.Name != CRCFieldName)
						continue;


					for (int i = 0; i < signal.Length; i++)
					{
						Payload += (ulong)(crc << signal.StartBit);
					}

					_payloadBytes = BitConverter.GetBytes(Payload);

				}
			}

			
		}



		private void End(bool isPassed)
		{
			//_sendIntervalTimer.Stop();

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

			if(_cancellationToken.IsCancellationRequested)
			{
				Execute();
			}
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

				//_sendIntervalTimer.Stop();
				//_sendIntervalTimer.Interval = (long)_tsInterval.TotalMilliseconds;
				//_sendIntervalTimer.Start();
			}

			if (_cancellationToken.IsCancellationRequested)
			{
				Execute();
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

		protected override void Generate(
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
			Message = (sourceNode as ScriptNodeCANMessage).Message;


			IsCRCAvailable = (sourceNode as ScriptNodeCANMessage).IsCRCAvailable;
			CRCFieldName = (sourceNode as ScriptNodeCANMessage).CRCFieldName;
			IsCounterAvailable = (sourceNode as ScriptNodeCANMessage).IsCounterAvailable;
			CounterFieldName = (sourceNode as ScriptNodeCANMessage).CounterFieldName;
		}


		#region CRC calculation

		private void CRCInit()
		{
			_srgCrc8_tab = new byte[] {0x0, 0x1d, 0x3a, 0x27, 0x74, 0x69, 0x4e, 0x53,
									0xe8, 0xf5, 0xd2, 0xcf, 0x9c, 0x81, 0xa6, 0xbb,
									0xcd, 0xd0, 0xf7, 0xea, 0xb9, 0xa4, 0x83, 0x9e,
									0x25, 0x38, 0x1f, 0x2, 0x51, 0x4c, 0x6b, 0x76,
									0x87, 0x9a, 0xbd, 0xa0, 0xf3, 0xee, 0xc9, 0xd4,
									0x6f, 0x72, 0x55, 0x48, 0x1b, 0x6, 0x21, 0x3c,
									0x4a, 0x57, 0x70, 0x6d, 0x3e, 0x23, 0x4, 0x19,
									0xa2, 0xbf, 0x98, 0x85, 0xd6, 0xcb, 0xec, 0xf1,
									0x13, 0xe, 0x29, 0x34, 0x67, 0x7a, 0x5d, 0x40,
									0xfb, 0xe6, 0xc1, 0xdc, 0x8f, 0x92, 0xb5, 0xa8,
									0xde, 0xc3, 0xe4, 0xf9, 0xaa, 0xb7, 0x90, 0x8d,
									0x36, 0x2b, 0xc, 0x11, 0x42, 0x5f, 0x78, 0x65,
									0x94, 0x89, 0xae, 0xb3, 0xe0, 0xfd, 0xda, 0xc7,
									0x7c, 0x61, 0x46, 0x5b, 0x8, 0x15, 0x32, 0x2f,
									0x59, 0x44, 0x63, 0x7e, 0x2d, 0x30, 0x17, 0xa,
									0xb1, 0xac, 0x8b, 0x96, 0xc5, 0xd8, 0xff, 0xe2,
									0x26, 0x3b, 0x1c, 0x1, 0x52, 0x4f, 0x68, 0x75,
									0xce, 0xd3, 0xf4, 0xe9, 0xba, 0xa7, 0x80, 0x9d,
									0xeb, 0xf6, 0xd1, 0xcc, 0x9f, 0x82, 0xa5, 0xb8,
									0x3, 0x1e, 0x39, 0x24, 0x77, 0x6a, 0x4d, 0x50,
									0xa1, 0xbc, 0x9b, 0x86, 0xd5, 0xc8, 0xef, 0xf2,
									0x49, 0x54, 0x73, 0x6e, 0x3d, 0x20, 0x7, 0x1a,
									0x6c, 0x71, 0x56, 0x4b, 0x18, 0x5, 0x22, 0x3f,
									0x84, 0x99, 0xbe, 0xa3, 0xf0, 0xed, 0xca, 0xd7,
									0x35, 0x28, 0xf, 0x12, 0x41, 0x5c, 0x7b, 0x66,
									0xdd, 0xc0, 0xe7, 0xfa, 0xa9, 0xb4, 0x93, 0x8e,
									0xf8, 0xe5, 0xc2, 0xdf, 0x8c, 0x91, 0xb6, 0xab,
									0x10, 0xd, 0x2a, 0x37, 0x64, 0x79, 0x5e, 0x43,
									0xb2, 0xaf, 0x88, 0x95, 0xc6, 0xdb, 0xfc, 0xe1,
									0x5a, 0x47, 0x60, 0x7d, 0x2e, 0x33, 0x14, 0x9,
									0x7f, 0x62, 0x45, 0x58, 0xb, 0x16, 0x31, 0x2c,
									0x97, 0x8a, 0xad, 0xb0, 0xe3, 0xfe, 0xd9, 0xc4};
		}

		private byte Crc_CalculateCRC8(byte[] pCrcData, uint crcLength, byte Crc_StartValue8, bool bCrcIsFirstCall)
		{


			byte crc = 0;    /* Default return value if NULL pointer */

			if (pCrcData != null)
			{
				crc = ((bCrcIsFirstCall == true) ? (byte)_CRC8_START_VALUE : Crc_StartValue8);


				uint byteIndex;

				for (byteIndex = 0; byteIndex < crcLength; ++byteIndex)
				{
					crc = _srgCrc8_tab[crc ^ pCrcData[byteIndex]];
				}

				crc = (byte)(crc ^ _CRC8_XOR);
			}

			return crc;
		}

		#endregion CRC calculation

		#endregion Methods

		#region Events

		public event Action<IScriptStepContinuous> ContinuousEndedEvent;
		public event Action<string> ContinuousErrorEvent;

		#endregion Events
	}
}
