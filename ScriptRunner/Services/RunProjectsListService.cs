
using ScriptHandler.Models;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Threading;
using System;
using Services.Services;
using DeviceHandler.Models;
using ScriptRunner.Enums;
using ScriptRunner.Models;
using System.Windows;
using ScriptHandler.Enums;
using DeviceCommunicators.Models;
using CommunityToolkit.Mvvm.Messaging;

namespace ScriptRunner.Services
{
	public class RunProjectsListService
	{
		private enum RunProjectsState { None, RunProject, RunTest, RunProjectEnded, RunTestEnded, WaitForTest}

		#region Fields

		private CancellationTokenSource _cancellationTokenSource;
		private CancellationToken _cancellationToken;

		private RunProjectsState _state;
		private int _projectIndex;
		private int _testIndex;

		private RunScriptService _runScript;
		private DevicesContainer _devicesContainer;

		private ObservableCollection<GeneratedProjectData> _projectsList;

		private ObservableCollection<DeviceParameterData> _logParametersList;

		private bool _isChangingLogParamList;

		public bool IsAbortClicked;

		#endregion Fields

		#region Constructor

		public RunProjectsListService(
			ObservableCollection<DeviceParameterData> logParametersList,
			RunScriptService runScript,
			DevicesContainer devicesContainer)
		{
			_logParametersList = logParametersList;
			_runScript = runScript;
			_devicesContainer = devicesContainer;

			_runScript.ScriptEndedEvent += ScriptEndedEventHandler;

			_isChangingLogParamList = false;
			WeakReferenceMessenger.Default.Register<RECORD_LIST_CHANGEDMessage>(
				this, new MessageHandler<object, RECORD_LIST_CHANGEDMessage>(HandleRECORD_LIST_CHANGED));
		}

		#endregion Constructor

		#region Methods

		private void HandleRECORD_LIST_CHANGED(object sender, RECORD_LIST_CHANGEDMessage recordListChanged)
		{
			if (_isChangingLogParamList)
				return;

			_logParametersList = recordListChanged.LogParametersList;
		}

		public void Dispose()
		{
			_cancellationTokenSource?.Cancel();
		}

		private bool IsAllDataSet(
			bool isSOIncluded,
			GeneratedScriptData soScript)
		{
			if (string.IsNullOrEmpty(_runScript.AbortScriptPath))
			{
				MessageBox.Show("Please select the abort script", "Run Script");
				End(ScriptStopModeEnum.Ended, null);
				return false;
			}
			else if (isSOIncluded && soScript == null)
			{
				MessageBox.Show("Please select the controller and motor type", "Run Script");
				End(ScriptStopModeEnum.Ended, null);
				return false;
			}
			else
			{
				_runScript.AbortScriptStep = new ScriptStepAbort(_runScript.AbortScriptPath, _devicesContainer);
				if (_runScript.AbortScriptStep.Script == null)
				{
					MessageBox.Show("The abort script is invalid", "Run Script");
					End(ScriptStopModeEnum.Ended, null);
					return false;
				}
			}


			return true;
		}

		public void StartSingle(
			GeneratedProjectData projects,
			GeneratedScriptData scriptData,
			bool isRecord,
			GeneratedScriptData soScript)
		{
			bool isAllDataSet = IsAllDataSet(
				scriptData.IsContainsSO, 
				soScript);
			if(isAllDataSet == false) 
			{ 
				End(ScriptStopModeEnum.Ended, null);
				return;
			}

			IsAbortClicked = false;
			InitRecordingListForProject(projects);

			scriptData.State = SciptStateEnum.Running;
			_runScript.Run(_logParametersList, scriptData, null, soScript, isRecord);
		}

		public void StartAll(
			ObservableCollection<GeneratedProjectData> projectsList,
			bool isRecord,
			GeneratedScriptData stoppedScript,
			GeneratedScriptData soScript,
			ObservableCollection<DeviceParameterData> logParametersList = null)
		{
			bool isSOIncluded = IsSOIncluded(projectsList);

			if (logParametersList != null) 
				_logParametersList = logParametersList;

			_projectsList = projectsList;

			if (_projectsList == null || _projectsList.Count == 0)
			{
				End(ScriptStopModeEnum.Ended, null);
				return;
			}

			bool isAllDataSet = IsAllDataSet(isSOIncluded, soScript);
			if (isAllDataSet == false)
			{
				End(ScriptStopModeEnum.Ended, null);
				return;
			}


			_cancellationTokenSource = new CancellationTokenSource();
			_cancellationToken = _cancellationTokenSource.Token;

			_state = RunProjectsState.RunProject;
			_projectIndex = 0;
			_testIndex = 0;
			IsAbortClicked = false;

			GoToStoppedScript(stoppedScript);
			if(_projectIndex >= projectsList.Count) // this will happen if the stopped script is the last script, so we start from the begining.
			{
				_projectIndex = 0;
				_testIndex = 0;
			}


			Run(_projectsList,
				isRecord);
		}

		private bool IsSOIncluded(ObservableCollection<GeneratedProjectData> projectsList)
		{
			foreach (GeneratedProjectData project in projectsList)
			{
				foreach (GeneratedScriptData test in project.TestsList)
				{
					if(test.IsContainsSO)
						return true;
				}
			}

			return false;
		}

		private void GoToStoppedScript(GeneratedScriptData stoppedScript)
		{
			if (stoppedScript == null)
				return;

			for (; _projectIndex < _projectsList.Count; _projectIndex++)
			{
				for (; _testIndex < _projectsList[_projectIndex].TestsList.Count; _testIndex++)
				{
					if (_projectsList[_projectIndex].TestsList[_testIndex] != stoppedScript)
						continue;
					
					_testIndex++;
					if (_testIndex < _projectsList[_projectIndex].TestsList.Count)
						return;

					_projectIndex++;
					_testIndex = 0;
					return;
				}
			}
		}

		public void Run(
			ObservableCollection<GeneratedProjectData> projectsList,
			bool isRecord)
		{
			ObservableCollection<DeviceParameterData> logParametersList = null;

			Task.Run(() =>
			{
				while (!_cancellationToken.IsCancellationRequested)
				{
					try
					{
						if(_state == RunProjectsState.WaitForTest)
						{
							System.Threading.Thread.Sleep(1);
							continue;
						}

						if (_projectIndex < 0 || _projectIndex >= projectsList.Count)
						{
							End(ScriptStopModeEnum.Ended, null);
							break;
						}

						
						

						switch (_state)
						{
							case RunProjectsState.RunProject:								

								if(projectsList[_projectIndex].IsDoRun == false)
								{
									_projectIndex++;
									break;
								}

								logParametersList = InitRecordingListForProject(projectsList[_projectIndex]);

								projectsList[_projectIndex].State = SciptStateEnum.Running;

								_testIndex = 0;
								_state = RunProjectsState.RunTest;

								break;

							case RunProjectsState.RunTest:

								if(_testIndex < 0 || _testIndex >= projectsList[_projectIndex].TestsList.Count)
								{
									_state = RunProjectsState.RunProjectEnded;
									break;
								}

								if (projectsList[_projectIndex].TestsList[_testIndex].IsDoRun == false)
								{
									_testIndex++;
									break;
								}

								GeneratedScriptData testData =
									projectsList[_projectIndex].TestsList[_testIndex];
								testData.State = SciptStateEnum.Running;
								
								_runScript.Run(
									logParametersList,
									testData,
									projectsList[_projectIndex].RecordingPath,
									isRecord);
							
								_state = RunProjectsState.WaitForTest;

								break;

							case RunProjectsState.RunTestEnded:
								_testIndex++;
								if(_testIndex >= projectsList[_projectIndex].TestsList.Count)
								{
									_state = RunProjectsState.RunProjectEnded;
									break;
								}

								_state = RunProjectsState.RunTest;
								break;

							case RunProjectsState.RunProjectEnded:

								projectsList[_projectIndex].State = SciptStateEnum.Ended;

								_projectIndex++;
								if(_projectIndex >= projectsList.Count)
								{
									End(ScriptStopModeEnum.Ended, null);
									break;
								}

								_state = RunProjectsState.RunProject;
								break;
						}

						System.Threading.Thread.Sleep(1);

					}
					catch (Exception ex)
					{
						LoggerService.Error(this, "Failed running a Project\r\n", "Run Project Error", ex);
					}



					//WeakReferenceMessenger.Default.Send(new RECORD_LIST_CHANGEDMessage() { LogParametersList = _logParametersList });
				}

			}, _cancellationToken);
		}

		private ObservableCollection<DeviceParameterData> InitRecordingListForProject(
			GeneratedProjectData currentProject)
		{
			_isChangingLogParamList = true;
			ObservableCollection<DeviceParameterData> logParametersList = _logParametersList;

			if (currentProject.RecordingParametersList != null &&
									currentProject.RecordingParametersList.Count > 0)
			{
				logParametersList = currentProject.RecordingParametersList;
			}
			WeakReferenceMessenger.Default.Send(new RECORD_LIST_CHANGEDMessage() { LogParametersList = logParametersList });

			_isChangingLogParamList = false;

			return logParametersList;
		}

		private void ScriptEndedEventHandler(ScriptStopModeEnum stopMode)
		{
			GeneratedScriptData scriptData = null;
			if (_projectsList != null && _projectsList.Count > 0 &&
				_projectIndex >= 0 && _projectIndex < _projectsList.Count &&
				_projectsList[_projectIndex].TestsList != null && _projectsList[_projectIndex].TestsList.Count > 0 &&
				_testIndex >= 0 && _testIndex < _projectsList[_projectIndex].TestsList.Count)
			{
				scriptData = _projectsList[_projectIndex].TestsList[_testIndex];
			}

			if (stopMode == ScriptStopModeEnum.Aborted || stopMode == ScriptStopModeEnum.Stopped ||
				IsAbortClicked)
			{
				
				End(stopMode, scriptData);
				if(IsAbortClicked && stopMode != ScriptStopModeEnum.Aborted)
				{
					_runScript.IsAborted = false;
					_runScript.AbortScript("User Abort");
				}
				return;
			}

			if(_projectsList == null || _projectsList.Count == 0)
			{
				End(stopMode, scriptData);
				return;
			}

			_state = RunProjectsState.RunTestEnded;
		}

		private void End(
			ScriptStopModeEnum stopMode,
			GeneratedScriptData scriptData)
		{
			if (_projectsList != null && _projectsList.Count != 0)
			{
				foreach (GeneratedProjectData project in _projectsList)
					project.State = SciptStateEnum.Ended;
			}

			RunEndedEvent?.Invoke(stopMode, scriptData);
			if (_cancellationTokenSource != null)
			{
				_cancellationTokenSource.Cancel();
				_cancellationTokenSource = null;
			}
		}

		#endregion Methods

		#region Events

		public event Action<ScriptStopModeEnum, GeneratedScriptData> RunEndedEvent;

		#endregion Events
	}
}
