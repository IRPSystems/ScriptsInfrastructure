
using CommunityToolkit.Mvvm.ComponentModel;
using System.Threading.Tasks;
using System.Threading;
using ScriptRunner.Enums;
using ScriptHandler.Models;
using Services.Services;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System;
using ScriptHandler.Services;
using System.Windows;
using DeviceHandler.Models;
using DeviceCommunicators.MCU;
using DeviceHandler.Models.DeviceFullDataModels;

namespace ScriptRunner.Services
{
	public class RunSingleScriptService: ObservableObject, IScriptRunner
	{

		#region Properties

		public GeneratedScriptData CurrentScript { get; set; }

		public ScriptStepBase CurrentStep
		{
			get
			{
				if (_subScript != null)
					return _subScript.CurrentStep;
				return _currentStep;
			}
		}

		public string ScriptErrorMessage { get; set; }

		#endregion Properties

		#region Fields

		private ScriptStepBase _currentStep;


		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private object _lockCurrentStep;

		private bool _isPaused;
		private bool _isAborted;

		private ScriptInternalStateEnum _state;

		private ScriptStepPause _pause;
		private ScriptStepBase _pausedStep;

		private ManualResetEvent _userDecision;

		private TimeSpan _runTime;

		private TestStudioLoggerService _mainScriptLogger;

		private RunSingleScriptService _subScript;

		private ScriptStepSubScript _scriptStep;

		private StopScriptStepService _stopScriptStep;

		

		private ScriptStepSelectMotorType _selectMotor;
		private SaftyOfficerService _saftyOfficer; 
		private DevicesContainer _devicesContainer;
		private CANMessagesService _canMessagesService;

		#endregion Fields


		#region Constructor

		public RunSingleScriptService(
			TimeSpan runTime,
			TestStudioLoggerService mainScriptLogger,
			GeneratedScriptData currentScript,
			ScriptStepSubScript scriptStep,
			StopScriptStepService stopScriptStep,
			ScriptStepSelectMotorType selectMotor,
			SaftyOfficerService saftyOfficer,
			DevicesContainer devicesContainer,
			CANMessagesService canMessagesService)
		{
			_runTime = runTime;
			_mainScriptLogger = mainScriptLogger;
			CurrentScript = currentScript;
			_scriptStep = scriptStep;
			_stopScriptStep = stopScriptStep;
			_selectMotor = selectMotor;
			_saftyOfficer = saftyOfficer;
			_devicesContainer = devicesContainer;
			_canMessagesService = canMessagesService;

			_lockCurrentStep = new object();
			_userDecision = new ManualResetEvent(false);

			if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					_pause = new ScriptStepPause();
				});
			}

			if (scriptStep != null)
			{
				scriptStep.RunIndex = 0;
				scriptStep.StartTime = DateTime.Now;
			}
		}

		#endregion Constructor


		#region Methods

		public void Start()
		{

			_state = ScriptInternalStateEnum.HandleSpecial;
			CurrentScript.IsPass = null;
			
			ScriptErrorMessage = "";

			if (CurrentScript.ScriptItemsList == null || 
				CurrentScript.ScriptItemsList.Count == 0)
			{
				CurrentScript.IsPass = true;
				ScriptEndedEvent?.Invoke(false);
				return;
			}

			_mainScriptLogger.AddLine(
				_runTime,
				"Start script \"" + CurrentScript.Name + "\"",
				LogTypeEnum.ScriptData);

			SetCurrentStep(CurrentScript.ScriptItemsList[0] as ScriptStepBase);
			

			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			ExecutingScriptSteps();
		}

		private void ExecutingScriptSteps()
		{
			Task.Run(() =>
			{
				while (!_cancellationToken.IsCancellationRequested)
				{
					lock (_lockCurrentStep)
					{
						try
						{
							if (_isPaused)
							{
								while (_isPaused && !_cancellationToken.IsCancellationRequested)
									System.Threading.Thread.Sleep(1);

								if (_cancellationToken.IsCancellationRequested)
									continue;

								if (!_isPaused)
								{
									if (_state != ScriptInternalStateEnum.Resume)
										_state = ScriptInternalStateEnum.HandleSpecial;
									SetCurrentStep(_pausedStep);
								}

							}



							switch (_state)
							{
								case ScriptInternalStateEnum.HandleSpecial:
									if (_currentStep == null)
										continue;

									LoggerService.Debug(this, "HandleSpecial - " + _currentStep.Description + " - " + _currentStep.StepState);

									_currentStep.StepState = SciptStateEnum.Running;

									if (_currentStep is ISubScript subScript)
									{
										StartSubScript(subScript);
									}
									else if (_currentStep is ScriptStepCANMessageUpdate update)
									{
										HandleCANMessageUpdate(update);
										_state = ScriptInternalStateEnum.EndStep;
										break;
									}
									else if (_currentStep is ScriptStepCANMessageStop stop)
									{
										HandleCANMessageStop(stop);
										_state = ScriptInternalStateEnum.EndStep;
										break;
									}
									else if (_currentStep is IScriptStepContinuous)
									{
										ContinuousStepEvent?.Invoke(_currentStep as IScriptStepContinuous);
										_currentStep.IsPass = true;
										_state = ScriptInternalStateEnum.EndStep;
										break;
									}

									_state = ScriptInternalStateEnum.Execute;
									break;

								case ScriptInternalStateEnum.Resume:
								case ScriptInternalStateEnum.Execute:
									if (_currentStep == null)
										continue;

									LoggerService.Debug(this, "Execute - " + _currentStep.Description + " - " + _currentStep.StepState);


									_mainScriptLogger.AddLine(
										_runTime,
										"Start step \"" + _currentStep.Description + "\"",
										LogTypeEnum.StepData);

									if (_currentStep == null)
										break;

									try
									{
										if (_subScript != null)
										{
											_subScript.Start();
										}
										else if (_currentStep is ScriptStepStartStopSaftyOfficer startStopSaftyOfficer)
										{
											if (startStopSaftyOfficer.IsStart)
												_currentStep.IsPass = StartSaftyOfficer();
											else
												StopSaftyOfficer();
										}
										else if (_state == ScriptInternalStateEnum.Resume)
											_currentStep.Resume();
										else
											_currentStep.Execute();

										
									}
									catch (Exception ex)
									{
										LoggerService.Error(this, "Execution failed", ex);
										AbortEvent?.Invoke("Execute failed\r\nScript: \"" + CurrentScript.Name + "\"\r\nStep: \"" + CurrentStep.Description + "\"");
									}

									if (_isPaused)
										break;

									if (_currentStep is IScriptStepWithWaitForUser ||
										_subScript != null)
									{
										_userDecision.Reset();
										_state = ScriptInternalStateEnum.WaitForUser;
									}
									else
										_state = ScriptInternalStateEnum.EndStep;

									break;


								case ScriptInternalStateEnum.WaitForUser:

									LoggerService.Debug(this, "WaitForUser - " + _currentStep.Description + " - " + _currentStep.StepState);
									if (CurrentScript.Name != "Failed Step Notification")
									{
										_userDecision = new ManualResetEvent(false);
										int eventThatSignaledIndex =
											WaitHandle.WaitAny(
												new WaitHandle[] { _userDecision, _cancellationToken.WaitHandle });

										_userDecision.Reset();

										if (_state == ScriptInternalStateEnum.Resume)
										{
											break;
										}
									}

									_state = ScriptInternalStateEnum.EndStep;

									break;


								case ScriptInternalStateEnum.EndStep:

									LoggerService.Debug(this, "EndStep - " + _currentStep.Description + " - " + _currentStep.StepState);

									StepEnd();

									if (_currentStep == null || CurrentScript.Name == "Failed Step Notification")
										_state = ScriptInternalStateEnum.EndScript;
									else
										_state = ScriptInternalStateEnum.HandleSpecial;



									break;

								case ScriptInternalStateEnum.EndScript:
									ScriptEnd();

									break;
							}

						}
						catch (Exception ex)
						{
							LoggerService.Error(this, "Failed running a script\r\n", "Run Error", ex);
						}
					}

				}

			}, _cancellationToken);
		}


		private void StepEnd()
		{
			lock (_lockCurrentStep)
			{
				if (_currentStep == null)
					return;

				_currentStep.StepState = SciptStateEnum.Ended;
				if (_currentStep.IsPass)
				{
					_mainScriptLogger.AddLine(
						_runTime,
						"Step \"" + _currentStep.Description + "\" passed",
						LogTypeEnum.Pass);

					if(CurrentScript.Name != "Failed Step Notification")
						SetCurrentStep(_currentStep.PassNext as ScriptStepBase);
				}
				else
				{
					_mainScriptLogger.AddLine(
						_runTime,
						_currentStep.ErrorMessage,
						LogTypeEnum.Fail);

					if (CurrentScript != null && _currentStep.FailNext == null)
						CurrentScript.IsPass = false;
					
					ScriptErrorMessage += _currentStep.ErrorMessage;

					SetCurrentStep(_currentStep.FailNext as ScriptStepBase);
				}

				if (_isAborted)
				{
					CurrentScript.IsPass = false;
					SetCurrentStep(null);
				}
			}

			OnPropertyChanged(nameof(_currentStep));
		}

		private void ScriptEnd()
		{
			_state = ScriptInternalStateEnum.None;
			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();

			if (CurrentScript != null)
			{
				_mainScriptLogger.AddLine(
					_runTime,
					"End script \"" + CurrentScript.Name + "\"",
					LogTypeEnum.ScriptData);

				CurrentScript.State = SciptStateEnum.Ended;
				
				if (CurrentScript.IsPass == null && CurrentScript.Name != "Failed Step Notification")
					CurrentScript.IsPass = true;

				
			}

			else 
			{
				_mainScriptLogger.AddLine(
					_runTime,
					"End script \"Unknown\"",
					LogTypeEnum.ScriptData);
			}

			_userDecision.Reset();




			if (CurrentScript.Name != "Failed Step Notification")
			{
				_currentStep = null;
				OnPropertyChanged(nameof(CurrentStep));
			}

			bool isEnd = Repeat();
			if (isEnd)
			{
				if (_scriptStep != null && CurrentScript.Name != "Failed Step Notification")
				{
					_scriptStep.IsPass = CurrentScript.IsPass == true;
					_scriptStep.Dispose();
				}

				

				ScriptEndedEvent?.Invoke(_isAborted);
			}
		}

		private bool Repeat()
		{
			if (_scriptStep == null)
				return true;

			if (CurrentScript.IsPass == true && _scriptStep.IsStopOnPass)
				return true;

			if (CurrentScript.IsPass == false && _scriptStep.IsStopOnFail)
				return true;


			if (_scriptStep.ContinueUntilType == SubScriptContinueUntilTypeEnum.Repeats)
			{
				_scriptStep.RunIndex++;
				if (_scriptStep.RunIndex >= _scriptStep.Repeats)
					return true;
			}
			else if (_scriptStep.ContinueUntilType == SubScriptContinueUntilTypeEnum.Timeout)
			{
				//TimeSpan diff = DateTime.Now - _scriptStep.StartTime;
				if (_scriptStep.TimeInSubScript >= _scriptStep.TimeoutSpan)
					return true;
			}

			Start();
			return false;
		}



		//private void StartSweep(ScriptStepSweep sweep)
		//{
		//	sweep.SetDataCollections();
		//	sweep.SetCommunicators();


		//	for (int i = 0; i < sweep.SweepItemsList.Count; i++)
		//	{
		//		SweepItemData sweepItem = sweep.SweepItemsList[i];
		//		SweepItemForRunData sweepItemForRun = sweep.ForRunList[i];

		//		if (sweepItem.SubScript == null)
		//			continue;

		//		sweepItemForRun.SubScriptRunner = new RunSingleScriptService(
		//			_runTime,
		//			_mainScriptLogger,
		//			sweepItem.SubScript as GeneratedScriptData,
		//			null,
		//			_stopScriptStep,
		//			_selectMotor,
		//			_saftyOfficer,
		//			_devicesContainer);
		//	}
		//}


		#region Sub script

		private void StartSubScript(ISubScript subScript)
		{
			if(subScript is ScriptStepSubScript subScriptStep)
				subScriptStep.SetTimeoutSpen();

			if (!(subScript.Script is GeneratedScriptData generatedScript))
				return;

			_subScript = new RunSingleScriptService(
				_runTime,
				_mainScriptLogger,
				generatedScript,
				subScript as ScriptStepSubScript,
				_stopScriptStep,
				_selectMotor,
				_saftyOfficer,
				_devicesContainer,
				_canMessagesService);
			_subScript.ScriptEndedEvent += SubScriptEndedEventHandler;
			_subScript.CurrentStepChangedEvent += CurrentStepChangedEventHandler;
			_subScript.ContinuousStepEvent += SubScript_ContinuousStepEvent;
			_subScript.StopContinuousStepEvent += SubScript_StopContinuousStepEvent;
			_subScript.AbortEvent += SubScript_AbortEvent;


		}

		private void SubScriptEndedEventHandler(bool isAborted)
		{
			_userDecision.Set();

			if(_subScript != null && _subScript.CurrentScript.IsPass == false)
			{
				ScriptErrorMessage = _subScript.ScriptErrorMessage + "\r\n\r\n";
			}

			_subScript = null;
			OnPropertyChanged(nameof(CurrentStep));
		}

		#endregion Sub script

		private void SetCurrentStep(ScriptStepBase currentStep)
		{
			_currentStep = currentStep;
			OnPropertyChanged(nameof(CurrentStep));
			CurrentStepChangedEvent?.Invoke(currentStep);
		}


		private bool StartSaftyOfficer()
		{
			_selectMotor.Execute();

			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(Entities.Enums.DeviceTypesEnum.MCU) == false)
				return false;

			

			if (_selectMotor.IsPass)
			{
				DeviceFullData deviceFullData =
					_devicesContainer.TypeToDevicesFullData[Entities.Enums.DeviceTypesEnum.MCU];
				if (deviceFullData == null)
					return false;

				_saftyOfficer.Start(
					_selectMotor.SelectedMotor,
					_selectMotor.SelectedController,
					deviceFullData.Device as MCU_DeviceData,
					deviceFullData.ParametersRepository);
			}



			return _selectMotor.IsPass;
		}

		private void StopSaftyOfficer()
		{
			_saftyOfficer.Stop();
		}

		private void CurrentStepChangedEventHandler(ScriptStepBase step)
		{
			OnPropertyChanged(nameof(CurrentStep));
			CurrentStepChangedEvent.Invoke(step);
		}


		public void StopStep()
		{
			_userDecision.Set();
			_stopScriptStep.StopStep();
		}

		public void Abort()
		{
			_isAborted = true;
			StopStep();

			if(_subScript != null)
				_subScript.Abort();
		}


		public void PausStep()
		{
			if(_subScript != null) 
			{ 
				_subScript.PausStep();
				return;
			}

			_isPaused = true;
			StopStep();
			_pausedStep = _currentStep;
			SetCurrentStep(_pause);
			_userDecision.Set();
		}

		public void NextStep()
		{
			if (_subScript != null)
			{
				_subScript.NextStep();
				return;
			}

			if (_isPaused)
			{
				_state = ScriptInternalStateEnum.Resume;
			}

			_isPaused = false;
			_userDecision.Set();
		}

		private void HandleCANMessageUpdate(ScriptStepCANMessageUpdate update)
		{
			update.IsPass = true;
			_canMessagesService.SendUpdateMessage(update);
			System.Threading.Thread.Sleep(500);
		}

		private void HandleCANMessageStop(ScriptStepCANMessageStop stop)
		{
			stop.IsPass = true;
			_canMessagesService.SendStopMessage(stop);
			System.Threading.Thread.Sleep(500);
		}


		private void SubScript_ContinuousStepEvent(IScriptStepContinuous scriptStepContinuous)
		{
			ContinuousStepEvent?.Invoke(scriptStepContinuous);
		}

		private void SubScript_StopContinuousStepEvent(string stepToStopDescription)
		{
			StopContinuousStepEvent?.Invoke(stepToStopDescription);
		}

		private void SubScript_AbortEvent(string abortDescription)
		{
			AbortEvent?.Invoke(abortDescription);
		}

		#endregion Methods

		#region Events

		public event Action<bool> ScriptEndedEvent;
		public event Action<ScriptStepBase> CurrentStepChangedEvent;

		public event Action<IScriptStepContinuous> ContinuousStepEvent;
		public event Action<string> StopContinuousStepEvent;

		public event Action<string> AbortEvent;

		#endregion Events
	}
}
