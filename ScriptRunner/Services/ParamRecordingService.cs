
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CsvHelper;
using DeviceCommunicators.Enums;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Models;
using ScriptRunner.Models;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace ScriptRunner.Services
{
	public class ParamRecordingService : ObservableObject, IDisposable
	{
		#region Properties

		public int RecordingRate { get; set; }
		public string RecordDirectory { get; set; }

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

		private const int _readAnologInput_NumOfPorts = 2;

		private bool _isFirstLineInFile;

		private string _scriptName;
		private string _date;
		private string _version;

		private DateTime _prevTime;
		private double _secCounter;

#if _USE_TIMER
		private System.Timers.Timer _timer;
#endif

		private object _lockObj;

		#endregion Fields

		#region Constructor

		public ParamRecordingService(
			DevicesContainer devicesContainer)
		{
			_devicesContainer = devicesContainer;

			_getUUTData = new GetUUTDataForRecordingService();

			_lockObj = new object();

			if(_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.MCU))
			{
				DeviceFullData mcuData =
					_devicesContainer.TypeToDevicesFullData[Entities.Enums.DeviceTypesEnum.MCU];
				mcuData.DeviceCommunicator.ConnectionEvent += DeviceCommunicator_ConnectionEvent;
				DeviceCommunicator_ConnectionEvent();
			}


#if _USE_TIMER
			_timer = new System.Timers.Timer();
			_timer.Elapsed += _timer_Elapsed;
#endif


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


		public void StartRecording(
			string scriptName,
			string recordingPath,
			ObservableCollection<DeviceParameterData> logParametersList,
			bool isAddDiviceToHeader = false)
		{
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
					if (_devicesContainer.TypeToDevicesFullData.ContainsKey(data.DeviceType) == false)
						continue;

					DeviceFullData deviceFullData =
						_devicesContainer.TypeToDevicesFullData[data.DeviceType];
					if (deviceFullData == null)
						continue;

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

							foreach (DeviceParameterData paramData in LogParametersList)
							{
								try
								{

									if (paramData.Value == null)
									{
										_csvWriter.WriteField("NaN-rec");
										continue;
									}

									double value = 0;
									if (paramData.Value.GetType().Name == "String")
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

#endif // _USE_TIMER

		private void HandleStringValue(DeviceParameterData paramData)
		{
			if (string.IsNullOrEmpty((string)paramData.Value))
			{
				LoggerService.Inforamtion(this, "string empty ");
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
			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.MCU) == false)
				return;

			DeviceFullData mcuData =
				_devicesContainer.TypeToDevicesFullData[Entities.Enums.DeviceTypesEnum.MCU];

			if (mcuData.DeviceCommunicator.IsInitialized == false)
				return;

			_getUUTData.GetUUTData(_devicesContainer);
		}

		#endregion Methods

		#region Events

		#endregion Events
	}
}
