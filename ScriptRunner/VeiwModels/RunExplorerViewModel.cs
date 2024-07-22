
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Models;
using ScriptRunner.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using ScriptHandler.Services;

namespace ScriptRunner.ViewModels
{
	public class RunExplorerViewModel:ObservableObject
	{
		#region Properties

		public ObservableCollection<GeneratedProjectData> ProjectsList { get; set; }

		public GeneratedScriptData SelectedScript { get; set; }

		#endregion Properties

		#region Fields

		private OpenProjectForRunService _openProjectForRun;
		private DevicesContainer _devicesContainer;
		private RunScriptService _runScript;
		private ScriptUserData _scriptUserData;

		private GeneratedProjectData _selectedProject;

		private FlashingHandler _flashingHandler;

		#endregion Fields

		#region Constructor

		public RunExplorerViewModel(
			DevicesContainer devicesContainer,
			FlashingHandler flashingHandler,
			RunScriptService runScript,
			ScriptUserData scriptUserData)
		{
			_devicesContainer = devicesContainer;
			_flashingHandler = flashingHandler;
			_runScript = runScript;
			_scriptUserData = scriptUserData;

			ReloadProjectCommand = new RelayCommand<GeneratedProjectData>(ReloadProject);
			SelectRecordingPathCommand = new RelayCommand<GeneratedProjectData>(SelectRecordingPath);
			ScriptUpCommand = new RelayCommand<GeneratedProjectData>(ScriptUp);
			ScriptDownCommand = new RelayCommand<GeneratedProjectData>(ScriptDown);

			OpenProjectCommand = new RelayCommand(OpenProject);
			DeleteProjectCommand = new RelayCommand(DeleteProject);
			SaveProjectsListCommand = new RelayCommand(SaveProjectsList);
			LoadProjectsListCommand = new RelayCommand(LoadProjectsList);

			ProjectsList = new ObservableCollection<GeneratedProjectData>();
			_openProjectForRun = new OpenProjectForRunService();
		}

		#endregion Constructor

		#region Methods

		private void ReloadProject(GeneratedProjectData e)
		{
			GeneratedProjectData reloadedProject = 
				_openProjectForRun.Open(e.ProjectPath, _devicesContainer, _flashingHandler, _runScript);

			
			for(int i = 0; i < ProjectsList.Count; i++)
			{
				if (ProjectsList[i] == e)
				{
					ProjectsList[i] = reloadedProject;
					break;
				}
			}

			OnPropertyChanged(nameof(ProjectsList));

		}

		private void TestsList_MouseDoubleClick(MouseButtonEventArgs e)
		{
			if (!(e.OriginalSource is TextBlock tb))
				return;

			if (!(tb.DataContext is GeneratedTestData testData))
				return;

			TestDoubleClickedEvent?.Invoke(testData);
		}

		private void ScriptUp(GeneratedProjectData project)
		{
			int itemIndex = ProjectsList.IndexOf(project);
			Move(itemIndex, itemIndex - 1);
		}

		private void ScriptDown(GeneratedProjectData project)
		{
			int itemIndex = ProjectsList.IndexOf(project);
			Move(itemIndex, itemIndex + 1);
		}

		private void Move(int oldIndex, int newIndex)
		{
			if (newIndex < 0 || newIndex >= ProjectsList.Count)
				return;

			var item = ProjectsList[oldIndex];

			ProjectsList.RemoveAt(oldIndex);


			ProjectsList.Insert(newIndex, item);
		}

		#region Selection changed

		private void TestsList_SelectionChanged(SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count <= 0)
				return;

			SelectedScript = e.AddedItems[0] as GeneratedScriptData;

			foreach(GeneratedProjectData project in ProjectsList) 
			{ 
				foreach(GeneratedScriptData script in project.TestsList) 
				{
					if (script == SelectedScript)
						continue;

					script.IsSelected = false;
				}
			}
		}

		private void ProjectsList_SelectionChanged(SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count <= 0)
				return;

			_selectedProject = e.AddedItems[0] as GeneratedProjectData;

			foreach (GeneratedProjectData project in ProjectsList)
			{
				if(project == _selectedProject) 
					continue;

				project.IsSelected = false;
			}
		}

		#endregion Selection changed


		#region OpenProject

		private void OpenProject(string path)
		{
			GeneratedProjectData projectData = _openProjectForRun.Open(
				path,
				_devicesContainer,
				_flashingHandler,
				_runScript);

			if (projectData == null)
				return;

			ProjectsList.Add(projectData);
			ProjectAddedEvent?.Invoke();
		}



		private void OpenProject()
		{
			GeneratedProjectData projectData = _openProjectForRun.Open(
				_scriptUserData,
				_devicesContainer,
				_flashingHandler,
				_runScript);

			if (projectData == null)
				return;

			ProjectsList.Add(projectData);
			ProjectAddedEvent?.Invoke();
		}

		#endregion OpenProject

		private void DeleteProject()
		{
			int index = ProjectsList.IndexOf(_selectedProject);
			if (index < 0)
				return;

			ProjectsList.RemoveAt(index);
		}

		private void SaveProjectsList()
		{
			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "JSON Files | *.json";
			bool? result = saveFileDialog.ShowDialog();
			if (result != true)
				return;

			string path = saveFileDialog.FileName;


			List<string> projectsPathsList = new List<string>();

			foreach(GeneratedProjectData project in ProjectsList)
			{
				projectsPathsList.Add(project.ProjectPath);
			}

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			var sz = JsonConvert.SerializeObject(projectsPathsList, settings);
			System.IO.File.WriteAllText(path, sz);
		}

		private void LoadProjectsList()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "JSON Files | *.json";
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			string path = openFileDialog.FileName;


			string jsonString = System.IO.File.ReadAllText(path);

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			List<string> projectsPathsList = JsonConvert.DeserializeObject(jsonString, settings) as
				List<string>;

			foreach(string projectPath in projectsPathsList) 
			{
				OpenProject(projectPath);
			}
		}


		private void SelectRecordingPath(GeneratedProjectData project)
		{
			CommonOpenFileDialog commonOpenFile = new CommonOpenFileDialog();
			commonOpenFile.IsFolderPicker = true;
			//commonOpenFile.InitialDirectory = initDir;
			CommonFileDialogResult results = commonOpenFile.ShowDialog();
			if (results != CommonFileDialogResult.Ok)
				return;

			project.RecordingPath = commonOpenFile.FileName;
		}

		#endregion Methods

		#region Commands

		public RelayCommand<GeneratedProjectData> ScriptUpCommand { get; private set; }
		public RelayCommand<GeneratedProjectData> ScriptDownCommand { get; private set; }

		public RelayCommand<GeneratedProjectData> ReloadProjectCommand { get; private set; }


		public RelayCommand<GeneratedProjectData> SelectRecordingPathCommand { get; private set; }



		public RelayCommand OpenProjectCommand { get; private set; }
		public RelayCommand DeleteProjectCommand { get; private set; }
		public RelayCommand SaveProjectsListCommand { get; private set; }
		public RelayCommand LoadProjectsListCommand { get; private set; }


		private RelayCommand<MouseButtonEventArgs> _TestsList_MouseDoubleClickCommand;
		public RelayCommand<MouseButtonEventArgs> TestsList_MouseDoubleClickCommand
		{
			get
			{
				return _TestsList_MouseDoubleClickCommand ?? (_TestsList_MouseDoubleClickCommand =
					new RelayCommand<MouseButtonEventArgs>(TestsList_MouseDoubleClick));
			}
		}

		private RelayCommand<SelectionChangedEventArgs> _TestsList_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> TestsList_SelectionChangedCommand
		{
			get
			{
				return _TestsList_SelectionChangedCommand ?? (_TestsList_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(TestsList_SelectionChanged));
			}
		}

		private RelayCommand<SelectionChangedEventArgs> _ProjectsList_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> ProjectsList_SelectionChangedCommand
		{
			get
			{
				return _ProjectsList_SelectionChangedCommand ?? (_ProjectsList_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(ProjectsList_SelectionChanged));
			}
		}

		#endregion Commands

		#region Events

		public event Action<GeneratedTestData> TestDoubleClickedEvent;
		public event Action ProjectAddedEvent;

		#endregion Events
	}
}
