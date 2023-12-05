
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.MCU;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Models;
using ScriptHandler.Services;
using ScriptRunner.Enums;
using ScriptHandler.Interfaces;
using ScriptRunner.Models;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DeviceHandler.Models;

namespace ScriptRunner.Services
{
	public class RunScriptService : ObservableObject, IDisposable
	{

		#region Properties

		public RunSingleScriptService CurrentScript { get; set; }

		public int RecordingRate
		{
			get => _recordingRate;
			set
			{
				_recordingRate = value;
				if(_paramRecording != null)
					_paramRecording.RecordingRate = value;
			}
		}

		public string RecordDirectory
		{
			get => _recordDirectory;
			set
			{
				_recordDirectory = value;
				if (_paramRecording != null)
					_paramRecording.RecordDirectory = value;
			}
		}

		public string AbortScriptPath { get; set; }
		public ScriptStepAbort AbortScriptStep 
		{
			get => _abortScriptStep;
			set
			{
				_abortScriptStep = value;

				_abortScript = new RunSingleScriptService(
					RunTime,
					MainScriptLogger,
					AbortScriptStep.Script as GeneratedScriptData,
					null,
					StopScriptStep,
					SelectMotor,
					SaftyOfficer,
					_devicesContainer,
					_canMessagesService);
					_abortScript.ScriptEndedEvent += AbortScriptEndedEventHandler;
			}
		}

		public ObservableCollection<int> RecordingRateList { get; set; }



		


		//public MotorSettingsData SelectedMotor { get; set; }
		//public ControllerSettingsData SelectedController { get; set; }

		public TimeSpan RunTime { get; set; }

		public SaftyOfficerService SaftyOfficer { get; set; }

		public ScriptStepSelectMotorType SelectMotor { get; set; }

		#endregion Properties

		#region Fields

		private int _recordingRate;
		private string _recordDirectory;

		private ScriptStepAbort _abortScriptStep;


		public TestStudioLoggerService MainScriptLogger;

		private ParamRecordingService _paramRecording;

		
		private bool _isStopped;
		private bool _isAborted;

		private RunSingleScriptService _abortScript;

		


		public StopScriptStepService StopScriptStep;

		


		private ScriptStepNotification _stepFailed;
		private RunSingleScriptService _stepFailedScript;

		private DevicesContainer _devicesContainer;

		private HandleContinuousStepsService _handleContinuousSteps;

		private CANMessagesService _canMessagesService;

		private string _testName;

		#endregion Fields

		#region Constructor

		public RunScriptService(
			ObservableCollection<DeviceParameterData> logParametersList,
			DevicesContainer devicesContainer,
			StopScriptStepService stopScriptStep,
			CANMessagesService canMessagesService)
		{
			_devicesContainer = devicesContainer;
			StopScriptStep = stopScriptStep;
			_canMessagesService = canMessagesService;



			string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			path = Path.Combine(path, "Logs");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			path = Path.Combine(path, "SciptsLogs");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			MainScriptLogger = new TestStudioLoggerService();

			_paramRecording = new ParamRecordingService(
				logParametersList,
				devicesContainer);

			SaftyOfficer = new SaftyOfficerService();
			SaftyOfficer.AbortScriptEvent += AbortScript;


			RecordingRate = 5;

			RecordingRateList = new ObservableCollection<int>()
			{
				1, 5, 10, 20
			};

			_handleContinuousSteps = new HandleContinuousStepsService();


			CreateSelectMotorType();

			CreateStepFailed();
		}

		#endregion Constructor

		#region Methods

		public void Dispose()
		{
			if(CurrentScript != null)
				CurrentScript.StopStep();
		}

		private void CreateStepFailed()
		{
			_stepFailed = new ScriptStepNotification()
			{
				NotificationLevel = NotificationLevelEnum.Error,
			};

			GeneratedScriptData failedStepScript = new GeneratedScriptData()
			{
				Name = "Failed Step Notification",
				ScriptItemsList = new ObservableCollection<IScriptItem>(),
			};

			failedStepScript.ScriptItemsList.Add(_stepFailed);

			_stepFailedScript = new RunSingleScriptService(
				RunTime,
				MainScriptLogger,
				failedStepScript,
				null,
				StopScriptStep,
				SelectMotor,
				SaftyOfficer,
				_devicesContainer,
				_canMessagesService);
			_stepFailedScript.ScriptEndedEvent += ErrorNotificationScriptEndedEventHandler;
		}

		private void CreateSelectMotorType()
		{
			SelectMotor = new ScriptStepSelectMotorType();
			SelectMotor.StopScriptStep = StopScriptStep;
			SelectMotor.Description = "Select Motor Type";

			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.MCU) == false)
			{
				return;
			}

			DeviceFullData mcu_deviceFullData = _devicesContainer.TypeToDevicesFullData[Entities.Enums.DeviceTypesEnum.MCU];

			SelectMotor.MCU_Device = mcu_deviceFullData.Device as MCU_DeviceData;
			SelectMotor.Communicator = mcu_deviceFullData.DeviceCommunicator;

		}


		public void Run(
			GeneratedScriptData currentScript,
			string recordingPath,
			bool isRecord)
		{
			_isAborted = false;
			foreach (ScriptStepBase step in currentScript.ScriptItemsList)
				step.StepState = SciptStateEnum.None;

			if (isRecord)
				_paramRecording.StartRecording(currentScript.Name, recordingPath);

			//Application.Current.Dispatcher.Invoke(() =>
			//{
			//	Mouse.OverrideCursor = Cursors.Wait;
			//});

			System.Threading.Thread.Sleep(1000);

			//Application.Current.Dispatcher.Invoke(() =>
			//{
			//	Mouse.OverrideCursor = null;
			//});





			_isStopped = false;

			_testName = currentScript.Name;



			MainScriptLogger.Clear();
			MainScriptLogger.AddLine(
				RunTime,
				"Start Test \"" + currentScript.Name + "\"",
				LogTypeEnum.ScriptData);

			ClearScriptStepsState(currentScript);





			CurrentScript = new RunSingleScriptService(
				RunTime,
				MainScriptLogger,
				currentScript,
				null,
				StopScriptStep,
				SelectMotor,
				SaftyOfficer,
				_devicesContainer,
				_canMessagesService);
			CurrentScript.ScriptEndedEvent += ScriptEndedEventHandler;
			CurrentScript.CurrentStepChangedEvent += CurrentStepChangedEventHandler;
			CurrentScript.ContinuousStepEvent += Script_ContinuousStepEvent;
			CurrentScript.StopContinuousStepEvent += Script_StopContinuousStepEvent;


			InitiateSweepItem(CurrentScript.CurrentScript);

			if (_isAborted)
			{
				LoggerService.Inforamtion(this, "Exist Run do to IsAborted = true");
				ScriptEndedEvent?.Invoke(ScriptStopModeEnum.Aborted);
				return;
			}

			CurrentScript.Start();


			Application.Current.Dispatcher.Invoke(() =>
			{
				ScriptStartedEvent?.Invoke();
			});

		}

		private void ClearScriptStepsState(GeneratedScriptData script)
		{
			foreach (ScriptStepBase step in script.ScriptItemsList)
			{
				step.StepState = SciptStateEnum.None;
			}
		}

		private void ScriptEnded()
		{
			_handleContinuousSteps.EndAll();


			ScriptStopModeEnum stopMode = ScriptStopModeEnum.Ended;
			if (_isStopped)
			{
				CurrentScript.CurrentScript.State = SciptStateEnum.Stopped;
				stopMode = ScriptStopModeEnum.Stopped;
			}

			if(CurrentScript.CurrentScript.IsPass == false)
			{
				stopMode = ScriptStopModeEnum.Aborted;
			}

			if (_paramRecording.IsRecording)
				_paramRecording.StopRecording();

			SaftyOfficer.Stop();

			if(CurrentScript.CurrentScript != null)
				MainScriptLogger.Save(_testName);

			if(CurrentScript.CurrentScript.Name != "Failed Step Notification")
				CurrentScript = null;

			ScriptEndedEvent?.Invoke(stopMode);
		}

		private void ScriptEndedEventHandler(bool isAborted)
		{
			if (isAborted && _abortScript != null)
			{
				CurrentScript = _abortScript;
				_abortScript.Start();
				return;
			}

			if(CurrentScript.CurrentScript.IsPass == false && _abortScript != null)
			{
				_stepFailed.Notification = CurrentScript.ScriptErrorMessage;
				CurrentScript = _abortScript;
				_abortScript.Start();
				return;
			}

			ScriptEnded();
		}

		private void AbortScriptEndedEventHandler(bool isAborted)
		{
			if (_abortScript != null && _abortScript.CurrentScript.IsPass == false)
				_stepFailed.Notification += "\r\n\r\nFailed to execute the abort script";

			CurrentScript = _stepFailedScript;
			CurrentScript.Start();
		}

		private void ErrorNotificationScriptEndedEventHandler(bool isAborted)
		{
			CurrentScript.CurrentScript.IsPass = false;
			ScriptEnded();
		}

		public void AbortScript(string message)
		{
			_isAborted = true;
			_stepFailed.Notification = message;

			if (_paramRecording != null)
				_paramRecording.StopRecording();

			if (CurrentScript == null)
			{
				if (_abortScript != null)
				{
					CurrentScript = _abortScript;
					_abortScript.Start();
				}

				return;
			}

			CurrentScript.Abort();			
		}


		public void User_Stop()
		{
			LoggerService.Inforamtion(this, "Stop clicked");
			_isStopped = true;
		}

		public void User_Pause()
		{
			if(CurrentScript == null) 
				return;

			CurrentScript.PausStep();
		}

		public void User_Next()
		{
			if (CurrentScript == null)
				return;

			CurrentScript.NextStep();
		}

		private void CurrentStepChangedEventHandler()
		{
			OnPropertyChanged(nameof(CurrentScript));
		}

		#region Continuous Step

		private void Script_ContinuousStepEvent(IScriptStepContinuous scriptStepContinuous)
		{
			scriptStepContinuous.ContinuousErrorEvent += Script_ContinuousErrorEvent;
			_handleContinuousSteps.StartContinuous(scriptStepContinuous);
		}

		private void Script_ContinuousErrorEvent(string errorMessage)
		{
			try
			{
				CurrentScript.CurrentScript.IsPass = false;
				AbortScript(errorMessage);
			}
			catch { }
		}

		private void Script_StopContinuousStepEvent(string continuousDescription)
		{
			_handleContinuousSteps.StopContinuous(continuousDescription);
		}

		#endregion Continuous Step

		#region Sweep

		private void InitiateSweepItem(IScript script)
		{
			if (script == null)
				return;

			foreach(IScriptItem item in script.ScriptItemsList) 
			{ 
				if(item is ScriptStepSweep sweep)
				{
					StartSweep(sweep);
				}
				else if (item is ScriptStepSubScript subScript)
				{
					InitiateSweepItem(subScript.Script);
				}
			}
		}

		private void StartSweep(ScriptStepSweep sweep)
		{
			sweep.SetDataCollections();
			sweep.SetCommunicators();


			for (int i = 0; i < sweep.SweepItemsList.Count; i++)
			{
				SweepItemData sweepItem = sweep.SweepItemsList[i];
				SweepItemForRunData sweepItemForRun = sweep.ForRunList[i];

				if (sweepItem.SubScript == null)
					continue;

				sweepItemForRun.SubScriptRunner = new RunSingleScriptService(
					RunTime,
					MainScriptLogger,
					sweepItem.SubScript as GeneratedScriptData,
					null,
					StopScriptStep,
					SelectMotor,
					SaftyOfficer,
					_devicesContainer,
					_canMessagesService);

				sweepItemForRun.CurrentScriptDiagram = new ScriptHandler.ViewModels.ScriptDiagramViewModel();
				sweepItemForRun.CurrentScriptDiagram.DrawScript(sweepItem.SubScript);
			}
		}

		#endregion Sweep

		#endregion Methods

		#region Events

		public event Action ScriptStartedEvent;
		public event Action<ScriptStopModeEnum> ScriptEndedEvent;

		#endregion Events
	}
}
