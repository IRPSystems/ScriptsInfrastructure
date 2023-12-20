
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CsvHelper;
using DeviceCommunicators.Enums;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Models;
using ScriptRunner.Models;
using Services.Services;
using System;
using System.Collections.Generic;
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
	public class ParamRecordingService: ObservableObject, IDisposable
	{
		#region Properties

		public int RecordingRate { get; set; }
		public string RecordDirectory { get; set; }

		#endregion Properties

		#region Fields

		private const int _defaultAcquisitionTime = 5;

		private List<ParameterLogListData> _logListDataPool;		
		private int _currentLogListIndex;

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private TextWriter _textWriter;
		private CsvWriter _csvWriter;

		public ObservableCollection<DeviceParameterData> LogParametersList;
		private DevicesContainer _devicesContainer;

		public bool IsRecording;

		//private System.Timers.Timer _recordingTimer;

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

		#endregion Fields

		#region Constructor

		public ParamRecordingService(
			ObservableCollection<DeviceParameterData> logParametersList,
			DevicesContainer devicesContainer)
		{
			LogParametersList = logParametersList;
			_devicesContainer = devicesContainer;

			

			_logListDataPool = new List<ParameterLogListData>();
			for (int i = 0; i < 1000; i++)
			{
				_logListDataPool.Add(new ParameterLogListData() { Values = new List<ParameterLogData>(), });
			}

			_currentLogListIndex = 0;

			//_recordingTimer = new System.Timers.Timer(
			//	1000 / _defaultAcquisitionTime);
			//_recordingTimer.Elapsed += RecordingTimerElapsedEventHandler;

			_getUUTData = new GetUUTDataForRecordingService();

			WeakReferenceMessenger.Default.Register<RECORD_LIST_CHANGEDMessage>(
				this, new MessageHandler<object, RECORD_LIST_CHANGEDMessage>(HandleRECORD_LIST_CHANGED));
		}

		#endregion Constructor

		#region Methods

		public void Dispose()
		{
			

			foreach (DeviceParameterData data in LogParametersList)
			{
				DeviceFullData deviceFullData =
					_devicesContainer.TypeToDevicesFullData[data.DeviceType];
				if (deviceFullData == null)
					continue;

				deviceFullData.ParametersRepository.Remove(data, null);
			}

			_textWriter.Close();
			_csvWriter.Dispose();
		}

		private void HandleRECORD_LIST_CHANGED(object sender, RECORD_LIST_CHANGEDMessage recordListChanged)
		{
			if (IsRecording)
				return;

			LogParametersList = recordListChanged.LogParametersList;
			OnPropertyChanged(nameof(LogParametersList));
		}

		public void StartRecording(
			string scriptName,
			string recordingPath)
		{
			_scriptName = scriptName;

			try
			{
				if (Application.Current == null)
					return;

				_isFirstLineInFile = true;

				Application.Current.Dispatcher.Invoke(() =>
				{
					Mouse.OverrideCursor = Cursors.Wait;
				});

				_isFirstReceived = false;
				_receivedCounter = 0;


				if (string.IsNullOrEmpty(recordingPath))
					recordingPath = RecordDirectory;

				if (Directory.Exists(recordingPath) == false)
				{
					LoggerService.Error(this, "The recording path \"" + recordingPath + "\" was not found", "Run Error");

					Application.Current.Dispatcher.Invoke(() =>
					{
						Mouse.OverrideCursor = null;
					});
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


				_getUUTData.GetUUTData(_devicesContainer);

				if (_csvWriter == null)
					return;



				


				_csvWriter.WriteField("Time [sec]");
				foreach (DeviceParameterData data in LogParametersList)
				{
					_csvWriter.WriteField(data.Name + " [" + data.Units + "]");
				}

				_csvWriter.WriteField("Serial number");
				_csvWriter.WriteField("FW Version");
				_csvWriter.WriteField("Core Version");

				_csvWriter.WriteField("Test Name");
				_csvWriter.WriteField("Date");
				_csvWriter.WriteField("EVVA Version");

				_csvWriter.NextRecord();

				_currentLogListIndex = 0;



				

				foreach (DeviceParameterData data in LogParametersList)
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
				HandleLogParam();

				IsRecording = true;

				Application.Current.Dispatcher.Invoke(() =>
				{
					Mouse.OverrideCursor = null;
				});
			}
			catch(Exception ex) 
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
			if (!IsRecording)
				return;

			lock (_lockObj)
			{
				if(_csvWriter != null)
					_csvWriter.Dispose();
				_csvWriter = null;

				if (_textWriter != null)
					_textWriter.Close();

				if(_cancellationTokenSource != null)
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
			if(!_isFirstReceived)
			{
				_receivedCounter++;
				if(_receivedCounter > LogParametersList.Count) 
				{
					_isFirstReceived = true;
				}
			}
		}


		private object _lockObj = new object();
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

							

							

							if (_currentLogListIndex >= _logListDataPool.Count)
								_currentLogListIndex = 0;

							DateTime now = DateTime.UtcNow;
							if(_csvWriter.Row > 2)
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

										_csvWriter.WriteField("NaN");
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
							//else
							//{
							//	_csvWriter.WriteField("");
							//	_csvWriter.WriteField("");
							//	_csvWriter.WriteField("");
							//	_csvWriter.WriteField("");
							//	_csvWriter.WriteField("");
							//	_csvWriter.WriteField("");
							//}

							_csvWriter.NextRecord();
							System.Threading.Thread.Sleep(1000 / RecordingRate);
						}
					}
					catch (Exception ex)
					{
						LoggerService.Error(this, "Failed to log data", ex);
					}

				}
			}, _cancellationToken);
		}

		//private void RecordingTimerElapsedEventHandler(object sender, ElapsedEventArgs e)
		//{
		//	lock (_lockObj)
		//	{
		//		if (_csvWriter == null)
		//			return;

		//		if (_currentLogListIndex >= _logListDataPool.Count)
		//			_currentLogListIndex = 0;

		//		_csvWriter.WriteField(DateTime.Now.ToString("HH:mm:ss.fff"));

		//		foreach (DeviceParameterData data in LogParametersList)
		//		{

		//			double value = Convert.ToDouble(data.Value);
		//			//if (data is MCU_ParamData mcuParam)
		//			//	value = value / mcuParam.Scale;
		//			//else if (data is Dyno_ParamData dynoParam)
		//			//	value = value / dynoParam.Coefficient;

		//			_csvWriter.WriteField(value);
		//		}

		//		_csvWriter.NextRecord();
		//	//	System.Threading.Thread.Sleep(200);
		//	}
		//}

		#endregion Methods

		#region Events

		#endregion Events
	}
}
