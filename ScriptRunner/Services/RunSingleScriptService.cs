
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
using ScriptRunner.ViewModels;

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
        public string OperatorErrorMessage { get; set; }

        #endregion Properties

        #region Fields

        private ScriptStepBase _currentStep;


		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private object _lockCurrentStep;

		private bool _isPaused;
		private bool _isAborted;
		private bool _isStopped;

		private ScriptInternalStateEnum _state;

		private ScriptStepPause _pause;

		private ManualResetEvent _userDecision;

		private RunScriptService.RunTimeData _runTime;

		private ScriptLoggerService _mainScriptLogger;

		private RunSingleScriptService _subScript;

		private StopScriptStepService _stopScriptStep;

		private ManualResetEvent _cancelationRequestPending;

		private Action<ScriptStepBase> _stepEndedEventGlobal;

        private DevicesContainer _devicesContainer;
		private CANMessageSenderViewModel _canMessageSender;

		#endregion Fields


		#region Constructor

		public RunSingleScriptService(
			RunScriptService.RunTimeData runTime,
			ScriptLoggerService mainScriptLogger,
			GeneratedScriptData currentScript,
			StopScriptStepService stopScriptStep,
			DevicesContainer devicesContainer,
			CANMessageSenderViewModel canMessageSender,
			Action<ScriptStepBase> stepEndedEvent = null)
		{
			_runTime = runTime;
			_mainScriptLogger = mainScriptLogger;
			CurrentScript = currentScript;
			_stopScriptStep = stopScriptStep;
			_devicesContainer = devicesContainer;
			_canMessageSender = canMessageSender;
			_lockCurrentStep = new object();
			_userDecision = new ManualResetEvent(false);
            _cancelationRequestPending = new ManualResetEvent(false);
            _stepEndedEventGlobal = stepEndedEvent;

            if (Application.Current != null)
			{
				Application.Current.Dispatcher.Invoke(() =>
				{
					_pause = new ScriptStepPause();
				});
			}
		}

		#endregion Constructor


		#region Methods

		public void Start()
		{
			if (CurrentScript == null)
				return;

			_state = ScriptInternalStateEnum.HandleSpecial;
			CurrentScript.IsPass = true;

			CurrentScript.PassRunSteps = 0;
			CurrentScript.FailRunSteps = 0;

			ScriptErrorMessage = "";

			if (CurrentScript.ScriptItemsList == null || 
				CurrentScript.ScriptItemsList.Count == 0)
			{
				CurrentScript.IsPass = true;
				ScriptEndedEvent?.Invoke(false);
				return;
			}

			if (!(this is RunSingleScriptService_SO))
			{
				_mainScriptLogger.AddLine(
					_runTime.RunTime,
					"Start script \"" + CurrentScript.Name + "\"",
					LogTypeEnum.ScriptData);
			}

			SetCurrentStep(CurrentScript.ScriptItemsList[0] as ScriptStepBase);

			//Check if a cancelation is requested for a script that is already running
			if (_cancellationToken.IsCancellationRequested)
			{
				_cancelationRequestPending.WaitOne(5000);
			}
            _cancelationRequestPending.Reset();

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
							//if (_isPaused)
							//{
							//	while (_isPaused && !_cancellationToken.IsCancellationRequested)
							//		System.Threading.Thread.Sleep(1);

							//	if (_cancellationToken.IsCancellationRequested)
							//		continue;

							//	if (!_isPaused)
							//	{
							//		if (_state != ScriptInternalStateEnum.Resume)
							//			_state = ScriptInternalStateEnum.HandleSpecial;
							//		SetCurrentStep(_pausedStep);
							//	}

							//}



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
									else if (_currentStep is ScriptStepCANMessage canMessage)
									{
										HandleCANMessageStartSending(canMessage);
										_state = ScriptInternalStateEnum.EndStep;
										break;
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


									_state = ScriptInternalStateEnum.Execute;
									break;

								case ScriptInternalStateEnum.Execute:
									if (_currentStep == null)
									{
										_state = ScriptInternalStateEnum.EndScript;
										continue;
									}

									LoggerService.Debug(this, "Execute - " + _currentStep.Description + " - " + _currentStep.StepState);

									if (!(this is RunSingleScriptService_SO))
									{
										_mainScriptLogger.AddLine(
										_runTime.RunTime,
										"Start step \"" + _currentStep.Description + "\"",
										LogTypeEnum.StepData);
									}

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
												StartSaftyOfficer();
											else
												StopSaftyOfficer();

											_currentStep.IsPass = true;
										}
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

									_userDecision = new ManualResetEvent(false);
									int eventThatSignaledIndex =
										WaitHandle.WaitAny(
											new WaitHandle[] { _userDecision, _cancellationToken.WaitHandle });

									_userDecision.Reset();

									if (_state == ScriptInternalStateEnum.Resume)
									{
										break;
									}

									_state = ScriptInternalStateEnum.EndStep;

									break;


								case ScriptInternalStateEnum.EndStep:

									if(_currentStep != null)
										LoggerService.Debug(this, "EndStep - " + _currentStep.Description + " - " + _currentStep.StepState);

									StepEnd();

									if (_currentStep == null)
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
				if(_cancellationToken.IsCancellationRequested)
				{
                    _cancelationRequestPending.Set();
                }
			}, _cancellationToken);
		}


		private void StepEnd()
		{
			lock (_lockCurrentStep)
			{
				if (_currentStep == null)
					return;

				if(_currentStep is ScriptStepSubScript sub)
				{
					_currentStep.IsPass = sub.Script.IsPass == true;

					bool isEnd = IsStopRepeatSubScript(sub);
					if (!isEnd)
					{
						_state = ScriptInternalStateEnum.HandleSpecial;
						return;
					}

					isFirstRunOfScript = true;



					sub.Dispose();
					_subScript = null;
				}
                if (_currentStep.CommSendResLog != null)
				{
                    StepEndedEvent?.Invoke(_currentStep);
                }

                _currentStep.StepState = SciptStateEnum.Ended;
				if (_currentStep.IsPass)
				{
					string data = "Step \"" + _currentStep.Description + "\" passed";
					if(_currentStep.EOLStepSummerysList != null && _currentStep.EOLStepSummerysList.Count > 0)
					{
						data += GetStepLogData(
							_currentStep.EOLStepSummerysList[_currentStep.EOLStepSummerysList.Count - 1]);
					}
					if (!(this is RunSingleScriptService_SO))
					{
						_mainScriptLogger.AddLine(
							_runTime.RunTime,
							data,
							LogTypeEnum.Pass);
					}

					SetCurrentStep(_currentStep.PassNext as ScriptStepBase);

					CurrentScript.PassRunSteps++;
				}
				else
				{
					//if (!(this is RunSingleScriptService_SO))
					//{
						_mainScriptLogger.AddLine(
						_runTime.RunTime,
						$"Step \"{_currentStep.Description}\" failed\r\n{_currentStep.ErrorMessage}",
						LogTypeEnum.Fail);
					//}

					if (CurrentScript != null && _currentStep.FailNext == null)
						CurrentScript.IsPass = false;
					
					ScriptErrorMessage += _currentStep.ErrorMessage;


     				SetCurrentStep(_currentStep.FailNext as ScriptStepBase);

					CurrentScript.FailRunSteps++;



					if (this is RunSingleScriptService_SO so)
					{
						so.IsAborted = true;
						ScriptErrorMessage += $"\r\nSafety Officer Abort";
					}

				}

				if (_isAborted)
				{
					CurrentScript.IsPass = false;
					SetCurrentStep(null);
				}

				if (_isStopped)
				{
					SetCurrentStep(null);
				}
			}

			OnPropertyChanged(nameof(_currentStep));
		}

		private string GetStepLogData(EOLStepSummeryData stepSummeryData)
		{
			if (stepSummeryData == null)
				return null;

			string str = null;

			if (stepSummeryData.TestValue != null)
				str += $"TestValue={stepSummeryData.TestValue} -- ";
			if (stepSummeryData.ComparisonValue != null)
				str += $"ComparisonValue={stepSummeryData.ComparisonValue} -- ";
			if (stepSummeryData.Method != null)
				str += $"Method={stepSummeryData.Method}";
            if (stepSummeryData.MeasuredTolerance != null)
                str += $"MeasuredTolerance={stepSummeryData.MeasuredTolerance}";

            str = $"\r\n{str}";

			return str;
		}

		private void ScriptEnd()
		{
			_state = ScriptInternalStateEnum.None;
			if (_cancellationTokenSource != null)
				_cancellationTokenSource.Cancel();

			if (CurrentScript != null)
			{
				if (!(this is RunSingleScriptService_SO))
				{
					_mainScriptLogger.AddLine(
					_runTime.RunTime,
					"End script \"" + CurrentScript.Name + "\"",
					LogTypeEnum.ScriptData);
				}
				CurrentScript.State = SciptStateEnum.Ended;
				
				if (CurrentScript.IsPass == true)
					CurrentScript.IsPass = true;

				
			}

			else 
			{
				if (!(this is RunSingleScriptService_SO))
				{
					_mainScriptLogger.AddLine(
					_runTime.RunTime,
					"End script \"Unknown\"",
					LogTypeEnum.ScriptData);
				}
			}


			_userDecision.Reset();

			
			if (this is RunSingleScriptService_SO so)
			{
				_isAborted = so.IsAborted;
			}

			ScriptEndedEvent?.Invoke(_isAborted);




			_currentStep = null;
			OnPropertyChanged(nameof(CurrentStep));
			

			
		}

		private bool IsStopRepeatSubScript(ScriptStepSubScript subScript)
		{
			if (this is RunSingleScriptService_SO)
			{
				if (_isStopped || subScript.IsPass != true)
					return true;

				System.Threading.Thread.Sleep(1);
				Start();
				return false;
			}

			if (subScript == null)
				return true;

			if (subScript.IsPass == true && subScript.IsStopOnPass)
				return true;

			if (subScript.IsPass == false && subScript.IsStopOnFail)
				return true;

			if(subScript.IsInfinity)
			{
				subScript.RunIndex++;
				return false;
			}

			if (subScript.ContinueUntilType == SubScriptContinueUntilTypeEnum.Repeats)
			{
				subScript.RunIndex++;
				if (subScript.RunIndex >= subScript.Repeats)
					return true;
			}
			else if (subScript.ContinueUntilType == SubScriptContinueUntilTypeEnum.Timeout)
			{
				if (subScript.TimeInSubScript >= subScript.TimeoutSpan)
					return true;
			}

			//Start();
			return false;
		}



		#region Sub script
		bool isFirstRunOfScript = true;
		private void StartSubScript(ISubScript subScript)
		{
			if(subScript is ScriptStepSubScript subScriptStep)
				subScriptStep.SetTimeoutSpen(isFirstRunOfScript);

			isFirstRunOfScript = false;

			if (!(subScript.Script is GeneratedScriptData generatedScript))
				return;

			if (this is RunSingleScriptService_SO)
			{
				_subScript = new RunSingleScriptService_SO(
					_runTime,
					_mainScriptLogger,
					generatedScript,
					_stopScriptStep,
					_devicesContainer,
					_canMessageSender);
			}
			else
			{
				_subScript = new RunSingleScriptService(
					_runTime,
					_mainScriptLogger,
					generatedScript,
					_stopScriptStep,
					_devicesContainer,
					_canMessageSender);
			}
			_subScript.StepEndedEvent += _stepEndedEventGlobal;
            _subScript.ScriptEndedEvent += SubScriptEndedEventHandler;
			_subScript.CurrentStepChangedEvent += CurrentStepChangedEventHandler;
			_subScript.AbortEvent += SubScript_AbortEvent;
            _subScript.StartSafetyOfficerEvent += SubScript_StartSafetyOfficerEvent;
			_subScript.StopSafetyOfficerEvent += SubScript_StopSafetyOfficerEvent;


		}

		private void SubScriptEndedEventHandler(bool isAborted)
		{
			try
			{
				_userDecision.Set();

				if (_subScript != null && _subScript.CurrentScript.IsPass == false)
				{
					ScriptErrorMessage += _subScript.ScriptErrorMessage + "\r\n\r\n";
				}

				//_subScript = null;
				//StepEnd();
				OnPropertyChanged(nameof(CurrentStep));
			}
			catch 
			{
			}
		}

		#endregion Sub script

		private void SetCurrentStep(ScriptStepBase currentStep)
		{
			_currentStep = currentStep;
			OnPropertyChanged(nameof(CurrentStep));
			CurrentStepChangedEvent?.Invoke(currentStep);
		}


		private void StartSaftyOfficer()
		{
			StartSafetyOfficerEvent?.Invoke();
		}

		private void StopSaftyOfficer()
		{
			StopSafetyOfficerEvent?.Invoke();
		}

		private void CurrentStepChangedEventHandler(ScriptStepBase step)
		{
			OnPropertyChanged(nameof(CurrentStep));
			CurrentStepChangedEvent?.Invoke(step);
		}


		public void StopStep()
		{
			_userDecision.Set();
			_stopScriptStep.StopStep();

			_currentStep = null;

			_state = ScriptInternalStateEnum.EndStep;
			if(CurrentScript != null) 
				CurrentScript.IsPass = false;

			if (_subScript != null) 
				_subScript.Abort();
		}

		public void Abort()
		{
			_isAborted = true;
			StopStep();

			if(_subScript != null)
				_subScript.Abort();
		}

		public void StopScript()
		{
			_isStopped = true;
			StopStep();

			if (_subScript != null)
				_subScript.StopScript();
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

		private void HandleCANMessageStartSending(ScriptStepCANMessage canMessage)
		{
			canMessage.IsPass = true;
			_canMessageSender.SendMessage(canMessage);
			//System.Threading.Thread.Sleep(500);
		}

		private void HandleCANMessageUpdate(ScriptStepCANMessageUpdate update)
		{
			update.IsPass = true;
			_canMessageSender.SendUpdateMessage(update);
			//System.Threading.Thread.Sleep(500);
		}

		private void HandleCANMessageStop(ScriptStepCANMessageStop stop)
		{
			stop.IsPass = true;
			_canMessageSender.SendStopMessage(stop);
			System.Threading.Thread.Sleep(500);
		}


		

		private void SubScript_AbortEvent(string abortDescription)
		{
			ScriptErrorMessage += "\r\n" + abortDescription;
			AbortEvent?.Invoke(abortDescription);
		}

		private void SubScript_StartSafetyOfficerEvent()
		{
			StartSafetyOfficerEvent?.Invoke();
		}

		private void SubScript_StopSafetyOfficerEvent()
		{
			StopSafetyOfficerEvent?.Invoke();
		}

		#endregion Methods

		#region Events

		public event Action<bool> ScriptEndedEvent;
		public event Action<ScriptStepBase> StepEndedEvent;
		public event Action<ScriptStepBase> CurrentStepChangedEvent;

        public event Action<string> AbortEvent;
		public event Action StartSafetyOfficerEvent;
		public event Action StopSafetyOfficerEvent;

		#endregion Events
	}
}
