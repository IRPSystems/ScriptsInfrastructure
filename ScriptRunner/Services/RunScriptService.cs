
using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Windows.Input;
using ScriptRunner.ViewModels;

namespace ScriptRunner.Services
{
	public class RunScriptService : ObservableObject, IDisposable
	{
		public class RunTimeData : ObservableObject
		{
			public TimeSpan RunTime { get; set; }
		}

		#region Properties

		public RunSingleScriptService CurrentScript { get; set; }

		public RunSingleScriptService SafetyOffecerScript { get; set; }

		public ParamRecordingService ParamRecording { get; set; }

		public string ScriptName {  get; set; }

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
					_devicesContainer,
					_canMessageSender);
					_abortScript.ScriptEndedEvent += AbortScriptEndedEventHandler;
			}
		}

		public ObservableCollection<int> RecordingRateList { get; set; }



		public bool IsSoRunning { get; set; }

		public RunTimeData RunTime { get; set; }


		public int ExecutedStepsPercentage { get; set; }

		public TestStudioLoggerService MainScriptLogger { get; set; }

		#endregion Properties

		#region Fields


		private ScriptStepAbort _abortScriptStep;


		

		

		
		private bool _isStopped;
		public bool IsAborted;

		private RunSingleScriptService _abortScript;
		private RunSingleScriptService_SO _soScript;

		public StopScriptStepService StopScriptStep;

		


		private ScriptStepNotification _stepFailed;
		private RunSingleScriptService _stepFailedScript;

		private DevicesContainer _devicesContainer;

		
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

			RunTime = new RunTimeData();

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


			ParamRecording.RecordingRate = 5;

			RecordingRateList = new ObservableCollection<int>()
			{
				1, 5, 10, 20
			};

			


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
				_devicesContainer,
				_canMessageSender);
			_stepFailedScript.ScriptEndedEvent += ErrorNotificationScriptEndedEventHandler;
		}

		private void CreateSOScript(GeneratedScriptData soScript)
		{			

			_soScript = new RunSingleScriptService_SO(
				RunTime,
				MainScriptLogger,
				soScript,
				null,
				StopScriptStep,
				_devicesContainer,
				_canMessageSender);
			_soScript.AbortEvent += _soScript_AbortEvent;
			_soScript.ScriptEndedEvent += _soScript_ScriptEndedEvent;
		}

		public void Run(
			ObservableCollection<DeviceParameterData> logParametersList,
			GeneratedScriptData currentScript,
			string recordingPath,
			GeneratedScriptData soScript,
			bool isRecord)
		{
			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					Mouse.OverrideCursor = Cursors.Wait;
				});
			}

			IsAborted = false;
			foreach (ScriptStepBase step in currentScript.ScriptItemsList)
				step.StepState = SciptStateEnum.None;


			ParamRecording.StartRecording(currentScript.Name, recordingPath, logParametersList);

			

			System.Threading.Thread.Sleep(1000);

			_isStopped = false;

			_testName = currentScript.Name;



			MainScriptLogger.Clear();
			MainScriptLogger.AddLine(
				RunTime.RunTime,
				"Start Test \"" + currentScript.Name + "\"",
				LogTypeEnum.ScriptData);

			ClearScriptStepsState(currentScript);
			CreateSOScript(soScript);

			CurrentScript = new RunSingleScriptService(
				RunTime,
				MainScriptLogger,
				currentScript,
				null,
				StopScriptStep,
				_devicesContainer,
				_canMessageSender);
			CurrentScript.ScriptEndedEvent += ScriptEndedEventHandler;
			CurrentScript.CurrentStepChangedEvent += CurrentStepChangedEventHandler;
			CurrentScript.AbortEvent += CurrentScript_AbortEvent;
			CurrentScript.StartSafetyOfficerEvent += CurrentScript_StartSafetyOfficerEvent;
			CurrentScript.StopSafetyOfficerEvent += CurrentScript_StopSafetyOfficerEvent;


			InitiateSweepItem(CurrentScript.CurrentScript);

			if (IsAborted)
			{
				LoggerService.Inforamtion(this, "Exist Run do to IsAborted = true");
				ScriptEndedEvent?.Invoke(ScriptStopModeEnum.Aborted);

				if (Application.Current != null)
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						Mouse.OverrideCursor = null;
					});
				}

				return;
			}

			_stepsCounter = 0;
			_numOfSteps = GetNumberOfScriptSteps(CurrentScript.CurrentScript);
			CurrentScript.Start();

			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					ScriptStartedEvent?.Invoke();
					Mouse.OverrideCursor = null;
				});
			}

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
			
			_soScript.StopScript();

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

		#region Safety Officer

		private void CurrentScript_StartSafetyOfficerEvent()
		{
			IsSoRunning = true;
			_soScript.Start();
		}

		private void CurrentScript_StopSafetyOfficerEvent()
		{
			IsSoRunning = false;
			_soScript.StopScript();
		}

		private void _soScript_AbortEvent(string obj)
		{
			IsSoRunning = false;
		}

		private void _soScript_ScriptEndedEvent(bool isAborted)
		{
			IsSoRunning = false;

			if(isAborted)
			{
				string message = $"Safety Officer\r\n\r\n {_soScript.ScriptErrorMessage}";
				AbortScript(message);
			}
		}

		#endregion Safety Officer

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
					_devicesContainer,
					_canMessageSender);

				if (Application.Current != null)
				{
					Application.Current.Dispatcher.Invoke(() =>
					{
						sweepItemForRun.CurrentScriptDiagram =
							new ScriptHandler.ViewModels.ScriptDiagramViewModel();
						sweepItemForRun.CurrentScriptDiagram.DrawScript(sweepItem.SubScript);
					});
				}
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
