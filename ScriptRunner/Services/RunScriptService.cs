
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
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using DeviceHandler.Models;
using DeviceCommunicators.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using System.Windows.Input;
using ScriptRunner.ViewModels;

namespace ScriptRunner.Services
{
	public class RunScriptService : ObservableObject, IDisposable
	{

		#region Properties

		public RunSingleScriptService CurrentScript { get; set; }

		public ParamRecordingService ParamRecording { get; set; }

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
					_canMessageSender);
					_abortScript.ScriptEndedEvent += AbortScriptEndedEventHandler;
			}
		}

		public ObservableCollection<int> RecordingRateList { get; set; }



		


		//public MotorSettingsData SelectedMotor { get; set; }
		//public ControllerSettingsData SelectedController { get; set; }

		public TimeSpan RunTime { get; set; }

		public SaftyOfficerService SaftyOfficer { get; set; }

		public ScriptStepSelectMotorType SelectMotor { get; set; }

		public int ExecutedStepsPercentage { get; set; }

		#endregion Properties

		#region Fields


		private ScriptStepAbort _abortScriptStep;


		public TestStudioLoggerService MainScriptLogger;

		

		
		private bool _isStopped;
		public bool IsAborted;

		private RunSingleScriptService _abortScript;

		


		public StopScriptStepService StopScriptStep;

		


		private ScriptStepNotification _stepFailed;
		private RunSingleScriptService _stepFailedScript;

		private DevicesContainer _devicesContainer;

		private HandleContinuousStepsService _handleContinuousSteps;

		private CANMessageSenderViewModel _canMessageSender;

		private string _testName;

		private double _numOfSteps;
		private double _stepsCounter;

		#endregion Fields

		#region Constructor

		public RunScriptService(
			ObservableCollection<DeviceParameterData> logParametersList,
			DevicesContainer devicesContainer,
			StopScriptStepService stopScriptStep,
			CANMessageSenderViewModel canMessageSender)
		{
			_devicesContainer = devicesContainer;
			StopScriptStep = stopScriptStep;
			_canMessageSender = canMessageSender;



			string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			path = Path.Combine(path, "Logs");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			path = Path.Combine(path, "SciptsLogs");
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);

			MainScriptLogger = new TestStudioLoggerService();

			ParamRecording = new ParamRecordingService(
				devicesContainer);

			SaftyOfficer = new SaftyOfficerService();
			SaftyOfficer.AbortScriptEvent += AbortScript;


			ParamRecording.RecordingRate = 5;

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
				_canMessageSender);
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
			ObservableCollection<DeviceParameterData> logParametersList,
			GeneratedScriptData currentScript,
			string recordingPath,
			bool isRecord)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				Mouse.OverrideCursor = Cursors.Wait;
			});

			IsAborted = false;
			foreach (ScriptStepBase step in currentScript.ScriptItemsList)
				step.StepState = SciptStateEnum.None;


			if (isRecord)
				ParamRecording.StartRecording(currentScript.Name, recordingPath, logParametersList);

			

			System.Threading.Thread.Sleep(1000);

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
				_canMessageSender);
			CurrentScript.ScriptEndedEvent += ScriptEndedEventHandler;
			CurrentScript.CurrentStepChangedEvent += CurrentStepChangedEventHandler;
			CurrentScript.ContinuousStepEvent += Script_ContinuousStepEvent;
			CurrentScript.StopContinuousStepEvent += Script_StopContinuousStepEvent;
			CurrentScript.AbortEvent += CurrentScript_AbortEvent;


			InitiateSweepItem(CurrentScript.CurrentScript);

			if (IsAborted)
			{
				LoggerService.Inforamtion(this, "Exist Run do to IsAborted = true");
				ScriptEndedEvent?.Invoke(ScriptStopModeEnum.Aborted);

				Application.Current.Dispatcher.Invoke(() =>
				{
					Mouse.OverrideCursor = null;
				});

				return;
			}

			_stepsCounter = 0;
			_numOfSteps = GetNumberOfScriptSteps(CurrentScript.CurrentScript);
			CurrentScript.Start();


			Application.Current.Dispatcher.Invoke(() =>
			{
				ScriptStartedEvent?.Invoke();
				Mouse.OverrideCursor = null;
			});

		}

		

		private int GetNumberOfScriptSteps(IScript script)
		{
			int numberOfSteps = 0;
			foreach(IScriptItem item in script.ScriptItemsList) 
			{
				numberOfSteps++;
				if(item is ScriptStepSubScript subScript) 
				{
					int subNumberOfSteps = GetNumberOfScriptSteps(subScript.Script);
					if (subScript.ContinueUntilType == SubScriptContinueUntilTypeEnum.Repeats)
					{
						subNumberOfSteps *= subScript.Repeats;
						subNumberOfSteps += subScript.Repeats;
					}
					else if (subScript.ContinueUntilType == SubScriptContinueUntilTypeEnum.Repeats)
					{
						int repeats = (int)subScript.TimeoutSpan.TotalSeconds;
						subNumberOfSteps *= repeats;
						subNumberOfSteps += repeats;
					}

					numberOfSteps += subNumberOfSteps;
				}
			}

			return numberOfSteps;
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

			if (ParamRecording.IsRecording)
				ParamRecording.StopRecording();

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
			if (IsAborted)
				return;

			IsAborted = true;
			_stepFailed.Notification = message;

			if (ParamRecording != null)
				ParamRecording.StopRecording();

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

		private void CurrentStepChangedEventHandler(ScriptStepBase step)
		{
			OnPropertyChanged(nameof(CurrentScript));
			_stepsCounter++;

			if(step != null)
				ExecutedStepsPercentage = (int)((_stepsCounter / _numOfSteps) * 100);

			CurrentStepChangedEvent?.Invoke(step);
		}

		private void CurrentScript_AbortEvent(string abortDescription)
		{
			AbortScript(abortDescription);
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
					_canMessageSender);

				Application.Current.Dispatcher.Invoke(() =>
				{
					sweepItemForRun.CurrentScriptDiagram = new ScriptHandler.ViewModels.ScriptDiagramViewModel();
					sweepItemForRun.CurrentScriptDiagram.DrawScript(sweepItem.SubScript);
				});
			}
		}

		#endregion Sweep

		#endregion Methods

		#region Events

		public event Action ScriptStartedEvent;
		public event Action<ScriptStopModeEnum> ScriptEndedEvent;
		public event Action<ScriptStepBase> CurrentStepChangedEvent;

		#endregion Events
	}
}
