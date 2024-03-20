
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceCommunicators.Dyno;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using DeviceHandler.Models.DeviceFullDataModels;
using Entities.Enums;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ScriptHandler.Enums;
using ScriptHandler.Models;
using ScriptHandler.Services;
using ScriptRunner.Enums;
using ScriptRunner.Models;
using ScriptRunner.Services;
using ScriptRunner.ViewModels;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace ScriptRunner.ViewModels
{
	public class RunViewModel : ObservableObject
	{
		#region Properties


		public RunScriptService RunScript { get; set; }




		public bool IsPlayEnabled
		{
			get => _isConnected && _isPlayEnabled && _isScriptsLoaded;
		}

		public bool IsPlayNotEnabled
		{
			get => !_isPlayEnabled && _isGeneralPlayEnabled;
		}

		public bool IsGeneralEnabled
		{
			get => _isGeneralEnabled;
		}

		public Visibility NoAbortingVisibility { get; set; }

		public bool IsRecord { get; set; }

		public RunExplorerViewModel RunExplorer { get; set; }

		#endregion Properties

		#region Fields

		private bool _isAllSelected;

		private System.Timers.Timer _runTimeTimer;

	//	private List<string> _parametersLogList;



		private bool _isConnected;
		private bool _isPlayEnabled;
		private bool _isGeneralPlayEnabled;
		private bool _isGeneralEnabled;
		private bool _isScriptsLoaded;

		private ScriptUserData _scriptUserData;

		private ScriptLogDiagramViewModel _scriptLogViewModel;

		private DevicesContainer _devicesContainer;

		private DateTime _scriptStartTime;

		private OpenProjectForRunService _openProjectForRun;
		private RunProjectsListService _runProjectsList;

		private GeneratedScriptData _stoppedScript;

		#endregion Fields

		#region Constructor

		public RunViewModel(
			ObservableCollection<DeviceParameterData> logParametersList,
			DevicesContainer devicesContainer,
			ScriptUserData ScriptUserData,
			CANMessagesService canMessagesService)
		{
			
			IsRecord = true;
			

			_devicesContainer = devicesContainer;

			DeviceFullData deviceFullDataSource = null;
			if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.MCU) == true)
				deviceFullDataSource = devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.MCU];
			MCU_Communicator mcu_Communicator = null;
			if (deviceFullDataSource != null)
				mcu_Communicator = deviceFullDataSource.DeviceCommunicator as MCU_Communicator;

			deviceFullDataSource = null;
			if (devicesContainer.TypeToDevicesFullData.ContainsKey(DeviceTypesEnum.Dyno) == true)
				deviceFullDataSource = devicesContainer.TypeToDevicesFullData[DeviceTypesEnum.Dyno];
			Dyno_Communicator dyno_Communicator = null;
			if (deviceFullDataSource != null)
				dyno_Communicator = deviceFullDataSource.DeviceCommunicator as Dyno_Communicator;

			_scriptUserData = ScriptUserData;

			//_parametersLogList = new List<string>();

			try
			{

				foreach (DeviceFullData deviceFullData in devicesContainer.DevicesFullDataList)
				{
					deviceFullData.ConnectionEvent += ConnectionEvent;
				}
				_isConnected = IsConnected();

				_isPlayEnabled = true;
				_isGeneralPlayEnabled = true;
				_isGeneralEnabled = true;
				_isScriptsLoaded = false;

				_isAllSelected = true;

				SelectAllCommand = new RelayCommand(SelectAll);
				StartAllCommand = new RelayCommand(StartAll);
				AbortCommand = new RelayCommand(Abort);


				StartCommand = new RelayCommand(Start);
				ForewardCommand = new RelayCommand(Foreward);
				StopCommand = new RelayCommand(Stop);
				PauseCommand = new RelayCommand(Pause);


				ShowScriptOutputCommand = new RelayCommand(ShowScriptOutput);

				BrowseRecordFileCommand = new RelayCommand(BrowseRecordFile);
				BrowseAbortScriptPathCommand = new RelayCommand(BrowseAbortScriptPath);

				StopScriptStepService stopScriptStep = new StopScriptStepService();
				RunScript = new RunScriptService(
					logParametersList,
					devicesContainer,
					stopScriptStep,
					canMessagesService);
				RunScript.ScriptEndedEvent += ScriptEndedEventHandler;
				RunScript.ScriptStartedEvent += ScriptStartedEventHandler;

				_runTimeTimer = new System.Timers.Timer(200);
				_runTimeTimer.Elapsed += RunTimeTimerElapsedEventHandler;

				NoAbortingVisibility = Visibility.Visible;
				//NoAbortingVisibility = Visibility.Collapsed;

				_openProjectForRun = new OpenProjectForRunService();
				_runProjectsList = new RunProjectsListService(logParametersList, RunScript, _devicesContainer);
				_runProjectsList.RunEndedEvent += RunProjectsListEnded;

				RunExplorer = new RunExplorerViewModel(_devicesContainer, RunScript, _scriptUserData);
				RunExplorer.TestDoubleClickedEvent += TestsDoubleClickEventHandler;
				RunExplorer.ProjectAddedEvent += ProjectAddedEventHandler;

				if (_scriptUserData != null)
				{
					if (!string.IsNullOrEmpty(_scriptUserData.LastRecordPath))
					{
						RunScript.ParamRecording.RecordDirectory =
							_scriptUserData.LastRecordPath;
					}

					//if (!string.IsNullOrEmpty(_ScriptUserData.LastAbortScriptPath))
					//{
					//	RunScript.AbortScriptPath =
					//		_ScriptUserData.LastAbortScriptPath;
					//}
				}
				else
				{
					string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
					string path = Path.Combine(documentsPath, "Logs");
					if (!Directory.Exists(path))
						Directory.CreateDirectory(path);
					path = Path.Combine(path, "ScriptRecording");
					if (!Directory.Exists(path))
						Directory.CreateDirectory(path);

					if (_scriptUserData == null)
						_scriptUserData = new ScriptUserData();
					RunScript.ParamRecording.RecordDirectory =
						_scriptUserData.LastRecordPath = path;
				}


			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to init the Run view model", "Error", ex);

			}

			LoggerService.Inforamtion(this, "Run initiated");
		}

		#endregion Constructor

		#region Methods

		public void ChangeDiagramBackground()
		{
			_scriptLogViewModel.ChangeBackground(
				Application.Current.MainWindow.FindResource("MahApps.Brushes.Control.Background") as SolidColorBrush);
		}

		public void CreateScriptLoggerWindow()
		{
			_scriptLogViewModel =
				new ScriptLogDiagramViewModel(RunScript);
			CreateScriptLogDiagramViewEvent?.Invoke(_scriptLogViewModel);

		}

		private void ConnectionEvent()
		{
			_isConnected = IsConnected();
			OnPropertyChanged(nameof(IsPlayEnabled));
			OnPropertyChanged(nameof(IsPlayNotEnabled));
			OnPropertyChanged(nameof(IsGeneralEnabled));
		}

		private bool IsConnected()
		{
			bool isConnected = true;
			if (_devicesContainer == null || _devicesContainer.DevicesFullDataList == null)
				return false;


			foreach (DeviceFullData deviceFullData in _devicesContainer.DevicesFullDataList)
			{
				if (deviceFullData.DeviceCommunicator == null)
					continue;

				isConnected |= deviceFullData.DeviceCommunicator.IsInitialized;
			}

			return isConnected;
		}

		private void SetIsPlayEnabled(bool isEnabled)
		{
			_isPlayEnabled = isEnabled;

			OnPropertyChanged(nameof(IsPlayEnabled));
			OnPropertyChanged(nameof(IsPlayNotEnabled));
		}

		private void SetIsGeneralEnabled(bool isEnabled)
		{
			_isGeneralEnabled = isEnabled;

			OnPropertyChanged(nameof(IsGeneralEnabled));
		}

		private void RunProjectsListEnded(
			ScriptStopModeEnum stopMode,
			GeneratedScriptData scriptData)
		{
			if(stopMode == ScriptStopModeEnum.Stopped)
				_stoppedScript = scriptData;

			SetIsPlayEnabled(true);
			SetIsGeneralEnabled(true);
		}


		private void ScriptStartedEventHandler()
		{
			_scriptStartTime = DateTime.Now;
			_runTimeTimer.Start();
		}

		private void ScriptEndedEventHandler(ScriptStopModeEnum e)
		{
			_isAborted = false;
			_runTimeTimer.Stop();
		}


		private void Start()
		{
			if(RunExplorer.SelectedScript == null) 
				return;

			_isAborted = false;

			SetIsPlayEnabled(false);
			SetIsGeneralEnabled(false);

			_runProjectsList.StartSingle(RunExplorer.SelectedScript, IsRecord);
		}



		private void Foreward()
		{
			RunScript.User_Next();
		}

		private void Stop()
		{
			OnPropertyChanged(nameof(IsPlayNotEnabled));
			RunScript.User_Stop();
		}

		private void Pause()
		{
			RunScript.User_Pause();
		}

		
		

		private void SelectAll()
		{
			if (RunExplorer.ProjectsList == null || RunExplorer.ProjectsList.Count == 0)
				return;

			_isAllSelected = !_isAllSelected;

			foreach (GeneratedProjectData project in RunExplorer.ProjectsList)
			{
				project.IsDoRun = _isAllSelected;

				foreach (GeneratedScriptData scriptData in project.TestsList)
				{
					scriptData.IsDoRun = _isAllSelected;
				}
			}
		}

		private void StartAll()
		{
			_isAborted = false;

			SetIsPlayEnabled(false);
			SetIsGeneralEnabled(false);

			_runProjectsList.StartAll(RunExplorer.ProjectsList, IsRecord, _stoppedScript);

			
		}

		private bool _isAborted;
		private void Abort()
		{
			string str = "Abort clicked";
			LoggerService.Inforamtion(this, str);
			if (_isAborted)
				return;

			_isAborted = true;
			LoggerService.Inforamtion(this, "User clicked abort");
			_isGeneralPlayEnabled = true;
			OnPropertyChanged(nameof(IsPlayNotEnabled));

			if (RunScript.AbortScriptStep == null)
			{
				if (string.IsNullOrEmpty(RunScript.AbortScriptPath))
				{
					LoggerService.Error(this, "No abort script is defined", "Run Script");
					return;
				}

				RunScript.AbortScriptStep = new ScriptStepAbort(RunScript.AbortScriptPath, _devicesContainer);
				if (RunScript.AbortScriptStep == null)
				{
					LoggerService.Error(this, "The abort script is invalid", "Run Script");
					return;
				}


			}

			if (RunScript.CurrentScript == null || RunScript.CurrentScript.CurrentScript == null ||
				(RunScript.CurrentScript != null && RunScript.CurrentScript.CurrentScript.State != SciptStateEnum.Running))
			{
				SetIsPlayEnabled(false);
			}

			RunScript.AbortScript("User abort");
		}

		



		private void RunTimeTimerElapsedEventHandler(object sender, ElapsedEventArgs e)
		{
			if (Application.Current == null)
				return;

			Application.Current.Dispatcher.Invoke(() =>
			{
				RunScript.RunTime = DateTime.Now - _scriptStartTime;
			});
		}

		private void ShowScriptOutput()
		{
			ShowScriptLogDiagramViewEvent?.Invoke();
		}

		

		private void BrowseRecordFile()
		{
			
			string initDir = _scriptUserData.LastRecordPath;
			if (Directory.Exists(initDir) == false)
				initDir = "";
			CommonOpenFileDialog commonOpenFile = new CommonOpenFileDialog();
			commonOpenFile.IsFolderPicker = true;
			commonOpenFile.InitialDirectory = initDir;
			CommonFileDialogResult results = commonOpenFile.ShowDialog();
			if (results != CommonFileDialogResult.Ok)
				return;

			_scriptUserData.LastRecordPath =
				commonOpenFile.FileName;
			RunScript.ParamRecording.RecordDirectory = commonOpenFile.FileName;
		}

		private void BrowseAbortScriptPath()
		{
			string initDir = _scriptUserData.LastAbortScriptPath;
			if (string.IsNullOrEmpty(initDir))
				initDir = "";
			if (Directory.Exists(initDir) == false)
				initDir = "";
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Script Files | *.scr";
			openFileDialog.InitialDirectory = initDir;
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			_scriptUserData.LastAbortScriptPath =
				Path.GetDirectoryName(openFileDialog.FileName);
			RunScript.AbortScriptPath = openFileDialog.FileName;
		}


		private void TestsDoubleClickEventHandler(GeneratedTestData testData)
		{
			_scriptLogViewModel.DrawScript(testData);
		}

		private void ProjectAddedEventHandler()
		{
			_isScriptsLoaded = true;

			SetIsPlayEnabled(true);
			SetIsGeneralEnabled(true);
		}

		#endregion Methods

		#region Commands

		public RelayCommand SelectAllCommand { get; private set; }
		public RelayCommand StartAllCommand { get; private set; }
		public RelayCommand AbortCommand { get; private set; }


		public RelayCommand StartCommand { get; private set; }
		public RelayCommand ForewardCommand { get; private set; }
		public RelayCommand PauseCommand { get; private set; }
		public RelayCommand StopCommand { get; private set; }


		public RelayCommand ShowScriptOutputCommand { get; private set; }



		public RelayCommand BrowseRecordFileCommand { get; private set; }
		public RelayCommand BrowseAbortScriptPathCommand { get; private set; }



		#endregion Commands

		#region Events

		public event Action<ScriptLogDiagramViewModel> CreateScriptLogDiagramViewEvent;
		public event Action ShowScriptLogDiagramViewEvent;

		#endregion Events
	}
}
