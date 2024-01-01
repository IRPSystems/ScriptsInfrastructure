
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CsvHelper;
using DeviceCommunicators.Enums;
using DeviceCommunicators.Models;
using DeviceHandler.Enums;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using ScriptRunner.Models;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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

		#endregion Fields

		#region Constructor

		public ParamRecordingService(
			DevicesContainer devicesContainer)
		{
			_devicesContainer = devicesContainer;

			_getUUTData = new GetUUTDataForRecordingService();
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
			ObservableCollection<DeviceParameterData> logParametersList)
		{
			LogParametersList = logParametersList;
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
				foreach (DeviceParameterData data in logParametersList)
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

		#endregion Methods

		#region Events

		#endregion Events
	}
}
