
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceCommunicators.Services;
using DeviceHandler.Models;
using DeviceHandler.ViewModel;
using Entities.Enums;
using Entities.Models;
using Evva.Models;
using ScriptHandler.Models;
using ScriptHandler.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SingleScriptBuilder
{
    public class SingleScriptBuilderMainWindowViewModel: ObservableObject
    {
		#region Properties

		public DockingScriptViewModel DockingScript { get; set; }
		public DesignToolsViewModel DesignTools { get; set; }
		public ParametersViewModel DesignParameters { get; set; }

		public DesignScriptViewModel CurrentScript { get; set; }

		#endregion Properties

		#region Fields

		private DevicesContainer _devicesContainter;
		private ScriptUserData _scriptUserData;
		private DragDropData _designDragDropData;

		private ReadDevicesFileService _readDevicesFile;
		private EvvaUserData _EvvaUserData;

		#endregion Fields

		#region Constructor

		public SingleScriptBuilderMainWindowViewModel()
		{
			NewCommand = new RelayCommand(New);
			OpenCommand = new RelayCommand(Open);
			SaveCommand = new RelayCommand(Save);

			ClosingCommand = new RelayCommand<CancelEventArgs>(Closing);

			LoadEvvaUserData();

			_readDevicesFile = new ReadDevicesFileService();

			InitDeviceContainer();




			_scriptUserData = new ScriptUserData();

			_designDragDropData = new DragDropData();

			DesignTools = new DesignToolsViewModel(_designDragDropData);
			DesignParameters = new ParametersViewModel(_designDragDropData, _devicesContainter, false);

			DockingScript = new DockingScriptViewModel(DesignTools, DesignParameters, null);
		}

		#endregion Constructor

		#region Methods

		private void InitDeviceContainer()
		{
			_devicesContainter = new DevicesContainer();
			_devicesContainter.DevicesFullDataList = new ObservableCollection<DeviceFullData>();
			_devicesContainter.DevicesList = new ObservableCollection<DeviceData>();
			_devicesContainter.TypeToDevicesFullData = new Dictionary<DeviceTypesEnum, DeviceFullData>();




			ObservableCollection<DeviceBase> devicesList = _readDevicesFile.ReadAllFiles(
				@"Data\Device Communications\",
				_EvvaUserData.MCUJsonPath,
				_EvvaUserData.MCUB2BJsonPath,
				_EvvaUserData.DynoCommunicationPath,
				_EvvaUserData.NI6002CommunicationPath);

			foreach (DeviceBase device in devicesList)
			{
				DeviceFullData deviceFullData = new DeviceFullData(device as DeviceData);
				deviceFullData.Init();
				_devicesContainter.DevicesFullDataList.Add(deviceFullData);
				_devicesContainter.DevicesList.Add(device as DeviceData);
				if (_devicesContainter.TypeToDevicesFullData.ContainsKey(device.DeviceType) == false)
					_devicesContainter.TypeToDevicesFullData.Add(device.DeviceType, deviceFullData);
			}


			
		}

		private void New()
		{

			string scriptName = ExplorerViewModel.GetScriptName(
				"New Script", 
				"Script name", 
				".scr", 
				"Create", 
				"", 
				false, 
				null);
			if (scriptName == null)
				return;


			CurrentScript = new DesignScriptViewModel(_scriptUserData, _devicesContainter, true);
			CurrentScript.New(false, scriptName);
			DockingScript.OpenScript(CurrentScript);
			//DockingScript.ScriptIsChangedEventHandler(CurrentScript, true);
		}

		private void Open()
		{
			_designDragDropData.IsIgnor = true;
			CurrentScript = new DesignScriptViewModel(_scriptUserData, _devicesContainter, false);
			CurrentScript.Open(allowTests: false);
			DockingScript.OpenScript(CurrentScript);
			_designDragDropData.IsIgnor = false;
		}

		private void Save()
		{
			if (CurrentScript != null)
				CurrentScript.Save(false);
		}


		private void LoadEvvaUserData()
		{
			_EvvaUserData = EvvaUserData.LoadEvvaUserData("SingleScriptBuilder");

			if (_EvvaUserData == null)
				_EvvaUserData = new EvvaUserData();

			if (string.IsNullOrEmpty(_EvvaUserData.MCUJsonPath))
				_EvvaUserData.MCUJsonPath = @"Data\Device Communications\param_defaults.json";
			if (string.IsNullOrEmpty(_EvvaUserData.MCUB2BJsonPath))
				_EvvaUserData.MCUB2BJsonPath = @"Data\Device Communications\param_defaults.json";
			if (string.IsNullOrEmpty(_EvvaUserData.DynoCommunicationPath))
				_EvvaUserData.DynoCommunicationPath = @"Data\Device Communications\Dyno Communication.json";
			if (string.IsNullOrEmpty(_EvvaUserData.NI6002CommunicationPath))
				_EvvaUserData.NI6002CommunicationPath = @"Data\Device Communications\NI_6002.json";
		}

		private void SaveEvvaUserData()
		{
			EvvaUserData.SaveEvvaUserData(
				"Evva",
				_EvvaUserData);
		}

		private void Closing(CancelEventArgs e)
		{
			SaveEvvaUserData();

			bool isCancel = SaveIfNeeded();
			if (isCancel)
			{
				e.Cancel = true;
				return;
			}
			


			if (_devicesContainter != null)
			{
				foreach (DeviceFullData deviceFullData in _devicesContainter.DevicesFullDataList)
				{
					deviceFullData.Disconnect();

					if (deviceFullData.CheckCommunication == null)
						continue;

					deviceFullData.CheckCommunication.Dispose();
				}
			}
		}

		public bool SaveIfNeeded()
		{
			foreach (DesignScriptViewModel vm in DockingScript.DesignScriptsList)
			{
				bool isCancel = vm.SaveIfNeeded();
				if (isCancel)
					return true;
			}

			return false;
		}

		#endregion Methods

		#region Commands

		public RelayCommand NewCommand { get; private set; }
		public RelayCommand OpenCommand { get; private set; }
		public RelayCommand SaveCommand { get; private set; }

		public RelayCommand<CancelEventArgs> ClosingCommand { get; private set; }

		#endregion Commands
	}
}
