
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ScriptHandler.Models;
using Services.Services;
using ScriptHandler.Messanger;
using DeviceHandler.Models;
using System.Collections.Generic;
using System.IO;
using ScriptHandler.Services;
using Newtonsoft.Json;
using System;
using Entities.Models;
using DeviceHandler.ViewModel;
using System.Windows.Input;
using System.Collections.ObjectModel;
using ScriptHandler.Views;
using ScriptHandler.ViewModel;
using System.Windows;
using ScriptHandler.DesignDiagram.ViewModels;
using ScriptHandler.DesignDiagram.Views;

namespace ScriptHandler.ViewModels
{
    public class DesignViewModel: ObservableObject
    {
		private enum ListTypesEnum { Tools, Params }
		public enum GenerateStateEnum { Running, Pass, Fail, None, }

		public class DesignItemType
		{
			public string Name { get; set; }

			public override string ToString()
			{
				return Name;
			}
		}

		#region Properties

		public DevicesContainer DevicesContainer { get; set; }
		public DockingScriptViewModel DockingScript { get; set; }
		public StencilViewModel DesignTools { get; set; }
		public ParametersViewModel DesignParameters { get; set; }
		public ExplorerViewModel Explorer { get; set; }
		public NodePropertiesViewModel NodeProperties { get; set; }


		public List<DesignItemType> DesignItemTypesList { get; set; }
		public List<DesignItemType> OpenDesignItemTypesList { get; set; }

		public GenerateStateEnum GenerateState { get; set; }

		//public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }
		public object GenerateToolTip { get; set; }

		public ErrorsListView GenerateErrorsListView { get; set; }
		public Visibility GenerateMenuVisibility { get; set; }

		#endregion Properties

		#region Fields

		DragDropData _designDragDropData;



		public DesignDiagramViewModel CurrentScript;

		private ScriptUserData _scriptUserData;

		private FlashingHandler _flashingHandler;

		

		#endregion Fields

		#region Constructor

		public DesignViewModel(
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler,
			ScriptUserData scriptUserData,
			string toolsNamespaceToSearch = null)
        {


			Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
				"MTAwODQzOUAzMjMwMmUzNDJlMzBsQlRMeUl0THVueXVMcWhEMnlCeVJLTnZZdFhLRUh2aEZGKytIdUVIRTRBPQ==");

			DevicesContainer = devicesContainer;
			_scriptUserData = scriptUserData;
			_flashingHandler = flashingHandler;

			GenerateToolTip = "Generate button";
			if(Application.Current != null)
				GenerateErrorsListView = new ErrorsListView();


			SaveCommand = new RelayCommand(Save);
			SaveAllCommand = new RelayCommand(SaveAll);
			WhatchErrorsCommand = new RelayCommand(WhatchErrors);



			_designDragDropData = new DragDropData();
			DesignTools = new StencilViewModel();
			DesignParameters = new ParametersViewModel(
				_designDragDropData, 
				devicesContainer, 
				false,
				false);
			NodeProperties = new NodePropertiesViewModel();


			Explorer = new ExplorerViewModel(
				scriptUserData, 
				devicesContainer, 
				NodeProperties);

			if (Application.Current != null)
			{
				DockingScript = new DockingScriptViewModel(
					DesignTools,
					DesignParameters,
					Explorer,
					NodeProperties);
				Explorer.DockingScript = DockingScript;
			}
			

			WeakReferenceMessenger.Default.Register<SCRIPT_SELECTION_CHANGED>(
				this, new MessageHandler<object, SCRIPT_SELECTION_CHANGED>(HandleSCRIPT_SELECTION_CHANGED));


			DesignItemTypesList = new List<DesignItemType>()
			{
				new DesignItemType() { Name = "Project" },
				new DesignItemType() { Name = "Test" },
				new DesignItemType() { Name = "Script" },
			};

			OpenDesignItemTypesList = new List<DesignItemType>()
			{
				new DesignItemType() { Name = "Project" },
				new DesignItemType() { Name = "Test, Script" },
			};

			NewDropDownMenuItemCommand = new RelayCommand<string>(NewDropDownMenuItem);
			OpenDropDownMenuItemCommand = new RelayCommand<string>(OpenDropDownMenuItem);

			GenerateScriptCommand = new RelayCommand(GenerateScript);

			GenerateState = GenerateStateEnum.None;
			GenerateMenuVisibility = Visibility.Collapsed;

			LoggerService.Inforamtion(this, "Finished init of Design");
		}

		#endregion Constructor

		#region Methods

		public void RefreshTheme(bool isLightTheme)
		{
			if(Application.Current != null) 
				App.ChangeDarkLight(isLightTheme);
		}

		//public void RefreshDiagram()
		//{
		//	if (DockingScript == null)
		//		return;

		//	foreach (DesignDiagramViewModel vm in DockingScript.DesignScriptsList)
		//		vm.RefreshDiagram();
		//}

		private void HandleSCRIPT_SELECTION_CHANGED(object sender, SCRIPT_SELECTION_CHANGED e)
		{
			CurrentScript = e.DesignScriptVM as DesignDiagramViewModel;
		}

		


		#region Script file handling

		private void NewDropDownMenuItem(string name)
		{
			string scriptName = null;

			switch (name)
			{
				case "Project":
					Explorer.NewProject();
					break;

				case "Test":
					if (Explorer.Project != null)
					{
						Explorer.ProjectAddNewTest();
					}

					else
					{
						scriptName = ExplorerViewModel.GetScriptName(
							"New Test",
							"Test name",
							".tst",
							"Create",
							"",
							false,
							null);
						if (scriptName == null)
							return;



						CurrentScript = new DesignDiagramViewModel(
							_scriptUserData, 
							DevicesContainer, 
							NodeProperties);
						CurrentScript.New(true, scriptName);

						DesignDiagramView designDiagramView = new DesignDiagramView()
						{ DataContext = CurrentScript };
						DockingScript.AddDocument(CurrentScript, designDiagramView);
						//DockingScript.ScriptIsChangedEventHandler(CurrentScript, true);
					}
					break;

				case "Script":
					if (Explorer.Project != null)
					{
						Explorer.ProjectAddNewScript();
					}

					else
					{
						scriptName = ExplorerViewModel.GetScriptName(
						"New Script",
						"Script name",
						".scr",
						"Create",
						"",
						false,
						null);
						if (scriptName == null)
							return;

						CurrentScript = new DesignDiagramViewModel(
							_scriptUserData, 
							DevicesContainer, 
							NodeProperties);
						CurrentScript.New(false, scriptName);

						DesignDiagramView DesignDiagramView = new DesignDiagramView()
						{ DataContext = CurrentScript };
						DockingScript.AddDocument(CurrentScript, DesignDiagramView);
						//DockingScript.ScriptIsChangedEventHandler(CurrentScript, true);
					}
					break;
			}
		}

		private async void OpenDropDownMenuItem(string name)
		{
			try
			{
				switch (name)
				{
					case "Project":
						Explorer.OpenProject();
						break;

					case "Test, Script":
						_designDragDropData.IsIgnor = true;
						DesignDiagramViewModel vm = new DesignDiagramViewModel(
							_scriptUserData, 
							DevicesContainer,
							NodeProperties);
						vm.Open();
						bool isScriptShouldBeOpened = IsScriptShouldBeOpened(vm);
						if (isScriptShouldBeOpened == false)
							return;

						CurrentScript = vm;

						DesignDiagramView DesignDiagramView = new DesignDiagramView()
						{ DataContext = CurrentScript };
						DockingScript.AddDocument(CurrentScript, DesignDiagramView);
						_designDragDropData.IsIgnor = false;

						await vm.DrawNodes();

						break;
				}
			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to open item", ex);
			}
		}

		private bool IsScriptShouldBeOpened(DesignDiagramViewModel vm)
		{
			if(vm == null || vm.DesignDiagram == null) 
				return true;

			if (Explorer.Project != null)
			{
				foreach (DesignDiagramViewModel projVm in Explorer.Project.ScriptsList)
				{
					if (projVm.DesignDiagram.ScriptPath == vm.DesignDiagram.ScriptPath)
					{
						CurrentScript = projVm;

						DesignDiagramView DesignDiagramView = new DesignDiagramView()
						{ DataContext = CurrentScript };
						DockingScript.AddDocument(CurrentScript, DesignDiagramView);
						_designDragDropData.IsIgnor = false;
						return false;
					}
				}
			}

			foreach (DesignDiagramViewModel projVm in DockingScript.DesignScriptsList)
			{
				if (projVm.DesignDiagram.ScriptPath == vm.DesignDiagram.ScriptPath)
				{
					CurrentScript = projVm;

					DesignDiagramView DesignDiagramView = new DesignDiagramView()
					{ DataContext = CurrentScript };
					DockingScript.AddDocument(CurrentScript, DesignDiagramView);
					_designDragDropData.IsIgnor = false;
					return false;
				}
			}

			return true;
		}


		public void Save()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			if (CurrentScript != null)
				CurrentScript.Save(CurrentScript.DesignDiagram is TestData);

			Mouse.OverrideCursor = null;
		}

		private void SaveAll()
		{
			Mouse.OverrideCursor = Cursors.Wait;

			LoggerService.Inforamtion(this, "SaveAll start");

			Explorer.SaveProject();
			foreach (DesignDiagramViewModel vm in DockingScript.DesignScriptsList)
			{
				SaveSingleScript(vm);
			}

			foreach (DesignDiagramViewModel vm in DockingScript.DesignScriptsList)
			{
				vm.IsChanged = false;
			}

			Mouse.OverrideCursor = null;
		}

		private void SaveSingleScript(DesignDiagramViewModel vm)
		{
			try
			{
				LoggerService.Inforamtion(this, "Save \"" + vm.DesignDiagram.Name + "\"");
				if (Explorer.Project != null && Explorer.Project.ScriptsList.Contains(vm))
				{
					if (Explorer.Project != null)
						LoggerService.Inforamtion(this, "Project is NULL");
					else
						LoggerService.Inforamtion(this, "Script \"" + vm.DesignDiagram.Name + "\" was not found in the project");

					return;
				}

				vm.Save();
				vm.IsChanged = false;

				LoggerService.Inforamtion(this, "Finished saving \"" + vm.DesignDiagram.Name + "\"");
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to save script \"" + vm.DesignDiagram.Name + "\"", ex);
			}
		}

		#endregion Script file handling


		public bool SaveIfNeeded()
		{
			foreach (DesignDiagramViewModel vm in DockingScript.DesignScriptsList)
			{
				bool isCancel = vm.SaveIfNeeded();
				if (isCancel)
					return true;
			}

			bool isCancelProject = Explorer.SaveIfNeeded();
			if (isCancelProject)
				return true;

			return false;
		}

		private void GenerateScript()
		{
			try
			{
				
				GenerateState = GenerateStateEnum.Running;

				

				SaveAll();

				GenerateProjectService generateProject = new GenerateProjectService();
				InvalidScriptData invalidScriptData = new InvalidScriptData();

				GeneratedProjectData generatedProject = generateProject.Generate(
					Explorer.Project,
					invalidScriptData,
					DevicesContainer,
					_flashingHandler);

				string path = Path.GetDirectoryName(Explorer.Project.ProjectPath);
				path = Path.Combine(path, Explorer.Project.Name + ".gprj");

				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				var sz = JsonConvert.SerializeObject(generatedProject, settings);
				File.WriteAllText(path, sz);

				

				foreach(DesignDiagramViewModel vm in Explorer.Project.ScriptsList)
					vm.IsChanged = false;


				if (invalidScriptData.ErrorsList.Count > 0)
				{
					GenerateState = GenerateStateEnum.Fail;
					if(GenerateErrorsListView != null)
						GenerateErrorsListView.ErrorsList = invalidScriptData.ErrorsList;
					GenerateToolTip = GenerateErrorsListView;
					GenerateMenuVisibility = Visibility.Visible;
				}
				else
				{
					GenerateToolTip = "Generate button";
					GenerateState = GenerateStateEnum.Pass;
					GenerateMenuVisibility = Visibility.Collapsed;
				}


			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Generation of the project failed.", ex);
				GenerateState = GenerateStateEnum.Fail;
				GenerateToolTip = ex.ToString();
			}

		}

		private void WhatchErrors()
		{
			if(GenerateErrorsListView == null || GenerateErrorsListView.ErrorsList == null ||
				Explorer.Project == null)
			{
				return;
			}

			GenerateErrorsViewModel generateErrorsViewModel = new GenerateErrorsViewModel(DevicesContainer)
			{
				ErrorsList = GenerateErrorsListView.ErrorsList,
				Project = Explorer.Project,
			};

			generateErrorsViewModel.ReloadEvent += generateErrorsViewModel_ReloadEvent;

			GenerateErrorsView view = new GenerateErrorsView()
			{
				DataContext = generateErrorsViewModel
			};
			view.ShowDialog();


		}

		private void generateErrorsViewModel_ReloadEvent()
		{
			Explorer.OpenProject(Explorer.Project.ProjectPath);
		}

		#endregion Methods

		#region Commands


		public RelayCommand SaveCommand { get; private set; }
		public RelayCommand SaveAllCommand { get; private set; }

		public RelayCommand<string> NewDropDownMenuItemCommand { get; private set; }
		public RelayCommand<string> OpenDropDownMenuItemCommand { get; private set; }


		public RelayCommand GenerateScriptCommand { get; private set; }

		public RelayCommand WhatchErrorsCommand { get; private set; }


		#endregion Commands
	}
}
