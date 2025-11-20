
using CommunityToolkit.Mvvm.ComponentModel;
using CsvHelper;
using DeviceCommunicators.DBC;
using DeviceCommunicators.Enums;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using DeviceHandler.Services;
using Entities.Models;
using ScriptHandler.Models;
using ScriptRunner.ViewModels;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using static ScriptRunner.ViewModels.CANMessageSenderViewModel;

namespace ScriptRunner.Services
{
	public class ParamRecordingService : ObservableObject, IDisposable
	{
		#region Properties

		public int RecordingRate { get; set; }
		public string RecordDirectory { get; set; }

		public RunSingleScriptService CurrentScript
		{
			set
			{
				if (value == null)
					return;

				value.CurrentStepChangedEvent += CurrentScript_CurrentStepChangedEvent;

			}
		}

		#endregion Properties

		#region Fields



		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private TextWriter _textWriter;
		private CsvWriter _csvWriter;

		public ObservableCollection<DeviceParameterData> LogParametersList;
		private DevicesContainer _devicesContainer;

		public bool IsRecording;


		private bool _isFirstReceived;
		private int _receivedCounter;

		private GetUUTDataForRecordingService _getUUTData;
		private Entities.Enums.DeviceTypesEnum _recordingDeviceType;
		private bool _isFirstLineInFile;

		private string _scriptName;
		private string _date;
		private string _version;

		private DateTime _prevTime;
		private double _secCounter;

		private CANMessageSenderViewModel _canMessageSender;

#if _USE_TIMER
		private System.Timers.Timer _timer;
#endif

		private object _lockObj;

		private ScriptStepBase _currentStep;

		private bool _isPaused;

		#endregion Fields

		#region Constructor

		public ParamRecordingService(
			DevicesContainer devicesContainer,
			CANMessageSenderViewModel canMessageSender)
		{
			_devicesContainer = devicesContainer;
			_canMessageSender = canMessageSender;
			_getUUTData = new GetUUTDataForRecordingService();

			_lockObj = new object();

			if(_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.MCU))
			{
				_recordingDeviceType = Entities.Enums.DeviceTypesEnum.MCU;
                DeviceFullData mcuData =
					_devicesContainer.TypeToDevicesFullData[_recordingDeviceType];
				mcuData.DeviceCommunicator.ConnectionEvent += DeviceCommunicator_ConnectionEvent;
				DeviceCommunicator_ConnectionEvent();
			}
			else if (_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.CANBus))
			{
				_recordingDeviceType = Entities.Enums.DeviceTypesEnum.CANBus;
                DeviceFullData_CANBus canBusData =
					_devicesContainer.TypeToDevicesFullData[_recordingDeviceType] as DeviceFullData_CANBus;
				canBusData.DeviceCommunicator.ConnectionEvent += DeviceCommunicator_ConnectionEvent;
				DeviceCommunicator_ConnectionEvent();
            }


#if _USE_TIMER
			_timer = new System.Timers.Timer();
			_timer.Elapsed += _timer_Elapsed;
#endif

				_isPaused = false;

		}

		#endregion Constructor

		#region Methods



		public void Dispose()
		{

			if (LogParametersList != null)
			{
				foreach (DeviceParameterData data in LogParametersList)
				{
					DeviceFullData deviceFullData =
						_devicesContainer.TypeToDevicesFullData[data.DeviceType];
					if (deviceFullData == null)
						continue;

					deviceFullData.ParametersRepository.Remove(data, null);
				}
			}

			_textWriter.Close();
			_csvWriter.Dispose();
		}

		private void CurrentScript_CurrentStepChangedEvent(ScriptStepBase newStep)
		{
			_currentStep = newStep;
		}

		public void StartRecording(
			string scriptName,
			string recordingPath,
			ObservableCollection<DeviceParameterData> logParametersList,
			bool isAddDiviceToHeader = false)
		{
			if (logParametersList == null)
				return;

			LogParametersList = logParametersList;
			_scriptName = scriptName;

			try
			{
				if (Application.Current == null)
					return;


				_isFirstLineInFile = true;

				_isFirstReceived = false;
				_receivedCounter = 0;


				if (string.IsNullOrEmpty(recordingPath))
					recordingPath = RecordDirectory;

				if (Directory.Exists(recordingPath) == false)
				{
					LoggerService.Error(this, "The recording path \"" + recordingPath + "\" was not found", "Run Error");
					return;
				}



				_getUUTData.GetUUTData(_devicesContainer,_recordingDeviceType);

				_version = Assembly.GetEntryAssembly().GetName().Version.ToString();


				_date = _date = DateTime.Now.ToString("dd-MMM-yyyy");

				string path = Path.Combine(
					recordingPath,
					scriptName + " " +
						DateTime.Now.ToString("dd-MMM-yyyy HH-mm-ss") + ".csv");

				_textWriter = new StreamWriter(path, false, System.Text.Encoding.UTF8);
				_csvWriter = new CsvWriter(_textWriter, CultureInfo.CurrentCulture);


				

				if (_csvWriter == null)
					return;






				_csvWriter.WriteField("Time [sec]");
				_csvWriter.WriteField("Script Step");
				foreach (DeviceParameterData data in logParametersList)
				{
					string header = string.Empty;
					if (isAddDiviceToHeader && data.Device != null)
						header = data.Device.Name + "-";
					header += data.Name + " (" + data.DeviceType + ")" + " [" + data.Units + "]";
					_csvWriter.WriteField(header);
				}

				_csvWriter.WriteField("Serial number");
				_csvWriter.WriteField("FW Version");
				_csvWriter.WriteField("Core Version");

				_csvWriter.WriteField("Test Name");
				_csvWriter.WriteField("Date");
				_csvWriter.WriteField("EVVA Version");

				_csvWriter.NextRecord();






				foreach (DeviceParameterData data in logParametersList)
				{
					if (_devicesContainer.TypeToDevicesFullData.ContainsKey(data.DeviceType) == false && !data.IsInCANBus)
						continue;


					DeviceFullData deviceFullData =
                        _devicesContainer.GetDeviceFullData(data);
					if (deviceFullData == null)
						continue;
					data.Device = deviceFullData.Device;
					if (data is DBC_ParamData dbcParam)
					{
						CANMessageForSenderData canMsgData = _canMessageSender.CANMessagesList.ToList().Find((d) =>
							d.Message.NodeId == dbcParam.ParentMessage.ID);
						if (canMsgData != null)
							continue;
					}


					deviceFullData.ParametersRepository.Add(data, RepositoryPriorityEnum.Medium, ParamReceived);
				}

				if (_cancellationTokenSource != null)
					_cancellationTokenSource.Cancel();
				_cancellationTokenSource = new CancellationTokenSource();
				_cancellationToken = _cancellationTokenSource.Token;

				_secCounter = 0;

#if _USE_TIMER
				_timer.Interval = 1000 / RecordingRate;
				_timer.Start();
#else
				HandleLogParam();
#endif // _USE_TIMER

				IsRecording = true;
				_isPaused = false;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to start the recording", "Run Error", ex);

				Application.Current.Dispatcher.Invoke(() =>
				{
					Mouse.OverrideCursor = null;
				});
			}
		}

		public void StopRecording()
		{
#if _USE_TIMER
			_timer.Stop();
#endif

			if (!IsRecording)
				return;


			lock (_lockObj)
			{
				if (_csvWriter != null)
					_csvWriter.Dispose();
				_csvWriter = null;

				if (_textWriter != null)
					_textWriter.Close();

				if (_cancellationTokenSource != null)
					_cancellationTokenSource.Cancel();
			}



			foreach (DeviceParameterData data in LogParametersList)
			{
				if (_devicesContainer.TypeToDevicesFullData.ContainsKey(data.DeviceType))
				{
					DeviceFullData deviceFullData =
						_devicesContainer.TypeToDevicesFullData[data.DeviceType];
					if (deviceFullData == null)
						continue;

					deviceFullData.ParametersRepository.Remove(data, ParamReceived);
				}
			}

			IsRecording = false;
		}

		public void Pause(bool isPause)
		{
			_isPaused = isPause;
		}

		private void ParamReceived(DeviceParameterData param, CommunicatorResultEnum result, string errDescription)
		{
			if (!_isFirstReceived)
			{
				_receivedCounter++;
				if (_receivedCounter > LogParametersList.Count)
				{
					_isFirstReceived = true;
				}
			}
		}

#if _USE_TIMER

		private void _timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			try
			{
				if (_csvWriter == null)
					return;

				lock (_lockObj)
				{

					DateTime now = DateTime.UtcNow;
					if (_csvWriter.Row > 2)
					{
						TimeSpan diff = now - _prevTime;
						_secCounter += diff.TotalSeconds;
					}

					_csvWriter.WriteField(_secCounter);
					_prevTime = now;

					foreach (DeviceParameterData paramData in LogParametersList)
					{
						try
						{

							if (paramData.Value == null)
							{
								_csvWriter.WriteField("");
								continue;
							}

							if (paramData.Value.GetType().Name == "String")
							{
								if (string.IsNullOrEmpty((string)paramData.Value))
									LoggerService.Inforamtion(this, "string empty ");
								LoggerService.Inforamtion(this, "string: " + paramData.Value.ToString());

								_csvWriter.WriteField("NaN-rec");
								continue;
							}

							double value = Convert.ToDouble(paramData.Value);

							_csvWriter.WriteField(value);

							System.Threading.Thread.Sleep(1);
						}
						catch (Exception ex)
						{
							LoggerService.Error(this, "Failed to write record field", ex);
						}
					}

					if (_isFirstLineInFile)
					{
						_csvWriter.WriteField(_getUUTData.FirmwareVersion);
						_csvWriter.WriteField(_getUUTData.SerialNumber);
						_csvWriter.WriteField(_getUUTData.CoreVersion);
						_csvWriter.WriteField(_scriptName);
						_csvWriter.WriteField(_date);
						_csvWriter.WriteField(_version);

						_isFirstLineInFile = false;
					}

					_csvWriter.NextRecord();
				}
				
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to log data", ex);
			}
		}

#else // _USE_TIMER

		private void HandleLogParam()
		{
			Task.Run(() =>
			{
				while (!_cancellationToken.IsCancellationRequested)
				{
					if (!_isFirstReceived)
						continue;

					if(_isPaused)
					{
						Thread.Sleep(1);
						continue;
					}

					try
					{
						lock (_lockObj)
						{

							if (_csvWriter == null)
								break;

							DateTime start = DateTime.Now;

							DateTime now = DateTime.UtcNow;
							if (_csvWriter.Row > 2)
							{
								TimeSpan diff = now - _prevTime;
								_secCounter += diff.TotalSeconds;
							}

							_csvWriter.WriteField(_secCounter);
							_prevTime = now;

							string stepName = null;
							if (_currentStep != null)
							{
								stepName = _currentStep.Description;
								if (string.IsNullOrEmpty(_currentStep.UserTitle) == false)
									stepName = _currentStep.UserTitle;
							}
							else
								stepName = "Pre-step";

							_csvWriter.WriteField(stepName);

							foreach (DeviceParameterData paramData in LogParametersList)
							{
								try
								{
									

									if (paramData.Value == null &&
										!(paramData is DBC_ParamData))
									{
										_csvWriter.WriteField("NaN-rec");
										continue;
									}

									

									double value = 0;
									if (paramData is DBC_ParamData dbcParam)
									{
										double? dVal = ExtractDataFromDBCParam(dbcParam);										
										if (dVal == null)
										{
											_csvWriter.WriteField("NaN-rec");
											continue;
										}

										value = (double)dVal;
									}
									else if (paramData.Value.GetType().Name == "String")
									{
										HandleStringValue(paramData);
										continue;
									}
									else
										value = Convert.ToDouble(paramData.Value);

									_csvWriter.WriteField(value);

									System.Threading.Thread.Sleep(1);
								}
								catch (Exception ex)
								{
									LoggerService.Error(this, "Failed to write record field", ex);
								}
							}

							if (_isFirstLineInFile)
							{
								_csvWriter.WriteField(_getUUTData.SerialNumber);
								_csvWriter.WriteField(_getUUTData.FirmwareVersion);
								_csvWriter.WriteField(_getUUTData.CoreVersion);
								_csvWriter.WriteField(_scriptName);
								_csvWriter.WriteField(_date);
								_csvWriter.WriteField(_version);

								_isFirstLineInFile = false;
							}

							_csvWriter.NextRecord();

							TimeSpan lineHandleTime = DateTime.Now - start;

							int sleepTime = 1000 / RecordingRate;
							sleepTime -= (int)lineHandleTime.TotalMilliseconds;
							if (sleepTime > 0)
							{
								System.Threading.Thread.Sleep(sleepTime);
							}
						}
					}
					catch (Exception ex)
					{
						LoggerService.Error(this, "Failed to log data", ex);
					}

				}
			}, _cancellationToken);
		}

		private double? ExtractDataFromDBCParam(DBC_ParamData dbcParam)
		{
			foreach(CANMessageForSenderData data in _canMessageSender.CANMessagesList)
			{
				if (data.Message.NodeId == dbcParam.ParentMessage.ID)
				{
					//ulong value = data.Message.Payload;
					ulong value = 0;
					for (int i = 0; i < dbcParam.Signal.Length; i++)
					{
						ulong bit = ((data.Message.Payload >> (i + dbcParam.Signal.StartBit)) & 1);
						value += (bit << i);
					}

					double dValue = value;
					dValue -= dbcParam.Signal.Offset;
					dValue /= dbcParam.Signal.Factor;
					if(dbcParam.Signal.ValueType == DBCFileParser.Model.DbcValueType.Signed) 
						dValue *= -1;

					return dValue;
				}
			}

			return null;
		}

#endif // _USE_TIMER

		private void HandleStringValue(DeviceParameterData paramData)
		{
			if (string.IsNullOrEmpty((string)paramData.Value))
			{
				//LoggerService.Inforamtion(this, "string empty ");
				//LoggerService.Inforamtion(this, "string: " + paramData.Value.ToString());

				_csvWriter.WriteField("NaN-rec");
				return;
			}

			if (paramData is IParamWithDropDown dropDown)
			{
				if (dropDown.DropDown == null)
				{
					_csvWriter.WriteField(paramData.Value);
					return;
				}

				if (paramData.Value is string str)
				{
					DropDownParamData ddp =
						dropDown.DropDown.Find((d) => d.Name == str);
					if (ddp == null)
					{
						_csvWriter.WriteField(paramData.Value);
						return;
					}

					_csvWriter.WriteField(ddp.Name);
					return;
				}
			}
		}

		private void DeviceCommunicator_ConnectionEvent()
		{
			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(_recordingDeviceType) == false )
				return;

			DeviceFullData mcuData = null;

			mcuData = _devicesContainer.TypeToDevicesFullData[_recordingDeviceType];


            if (mcuData.DeviceCommunicator.IsInitialized == false)
				return;

			_getUUTData.GetUUTData(_devicesContainer,_recordingDeviceType);
		}

		#endregion Methods

		#region Events

		#endregion Events
	}
}
