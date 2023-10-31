﻿
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using ScriptHandler.Views;
using Services.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ScriptHandler.ViewModels
{
    public class ExplorerViewModel: ObservableObject
	{
		#region Properties

		public ProjectData Project { get; set; }

		#endregion Properties

		#region Fields

		public string ProjectPath;
		private ScriptUserData _scriptUserData;
		private DevicesContainer _devicesContainer;
		public DockingScriptViewModel DockingScript;

		private const string _invalidFileChars = "\", <, >, |, :, *, ?, \\, /";

		private bool _isMouseDown;
		private Point _startPoint;

		#endregion Fields


		#region Constructor

		public ExplorerViewModel(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer) 
		{
			_scriptUserData = scriptUserData;
			_devicesContainer = devicesContainer;

			ProjectAddNewTestCommand = new RelayCommand(ProjectAddNewTest);
			ProjectAddNewScriptCommand = new RelayCommand(ProjectAddNewScript);
			ProjectAddExistingCommand = new RelayCommand(ProjectAddExistingTest);
			ProjectRenameCommand = new RelayCommand(ProjectRename);


			DeleteScriptCommand = new RelayCommand<DesignScriptViewModel>(DeleteScript);
			CopyScriptCommand = new RelayCommand<DesignScriptViewModel>(CopyScript);
			RenameScriptCommand = new RelayCommand<DesignScriptViewModel>(RenameScript);


		}

		#endregion Constructor

		#region Methods


		public bool SaveIfNeeded()
		{
			if (Project == null)
				return false;

			if (Project.IsChanged == false)
				return false;

			
			MessageBoxResult result = MessageBox.Show(
				"Changes you made to the project are not saved.\r\nDo you wish to save?",
				"Closing Project",
				MessageBoxButton.YesNoCancel);
			if (result == MessageBoxResult.Cancel)
			{
				return true;
			}
			else if (result == MessageBoxResult.Yes)
			{
				SaveProject();
			}
			else if (result == MessageBoxResult.No)
			{
				OpenProject(ProjectPath);
			}
			

			return false;
		}






		#region Project

		public void NewProject()
		{

			string initDir = _scriptUserData.LastProjectPath;
			if (string.IsNullOrEmpty(initDir))
				initDir = "";

			SaveFileDialog saveFileDialog = new SaveFileDialog();
			saveFileDialog.Filter = "Project files (*.prj)|*.prj";
			saveFileDialog.InitialDirectory = initDir;
			bool? result = saveFileDialog.ShowDialog();
			if (result != true)
				return;

			_scriptUserData.LastProjectPath = 
				Path.GetDirectoryName(saveFileDialog.FileName);

			ProjectPath = saveFileDialog.FileName;
			string name = Path.GetFileName(ProjectPath);
			name = name.Replace(".prj", string.Empty);

			string directory = Path.GetDirectoryName(ProjectPath);
			directory = Path.Combine(directory, name);
			Directory.CreateDirectory(directory);
			directory = Path.Combine(directory, name + ".prj");
			ProjectPath = directory;

			

			Project = new ProjectData() { Name = name };


			SaveProject();

			Project.IsChanged = false;
		}

		public void OpenProject()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Project files (*.prj)|*.prj";

			if(System.IO.Directory.Exists(_scriptUserData.LastProjectPath))
				openFileDialog.InitialDirectory = _scriptUserData.LastProjectPath;
			bool? result = openFileDialog.ShowDialog();
			if (result != true)
				return;

			_scriptUserData.LastProjectPath =
				Path.GetDirectoryName(openFileDialog.FileName);

			OpenProject(openFileDialog.FileName);
		}

		public void OpenProject(string projectPath)
		{

			ProjectPath = projectPath;



			string jsonString = File.ReadAllText(ProjectPath);

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			Project = JsonConvert.DeserializeObject(jsonString, settings) as ProjectData;

			FixOldScriptsAndProjectsService fixer = new FixOldScriptsAndProjectsService();
			fixer.FixProject(Project, _scriptUserData);


			foreach (string path in Project.ScriptsPathsList)
			{
				string absPath = GetAbsPath(path);

				DesignScriptViewModel vm = new DesignScriptViewModel(_scriptUserData, _devicesContainer, true);
				
				vm.Open(path: absPath);
				if (vm.CurrentScript == null)
				{
					string fileName = Path.GetFileName(path);
					fileName = fileName.Replace(Path.GetExtension(path), string.Empty);
					vm.CurrentScript = new ScriptData()
					{
						Name = fileName + " - Unloaded",
						ScriptPath = path
					};
				}


				Project.ScriptsList.Add(vm);
			}

			PostLoadAllScripts();

			foreach (DesignScriptViewModel vm in Project.ScriptsList)
			{
				vm.ScriptReloadedEvent += ScriptReloadedEventHandler;
			}


			Project.IsChanged = false;
		}

		

		public void SaveProject()
		{
			if(Project == null) 
				return;

			foreach(DesignScriptViewModel script in Project.ScriptsList)
			{
				if(script.IsChanged)
					script.Save(script.CurrentScript is TestData);
			}

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			var sz = JsonConvert.SerializeObject(Project, settings);
			File.WriteAllText(ProjectPath, sz);

			Project.IsChanged = false;

		}

		#endregion Project

		#region New script/test

		private void ProjectAddNewTest()
		{
			ProjectAddNewScript(true);
		}

		private void ProjectAddNewScript()
		{
			ProjectAddNewScript(false);
		}

		private void ProjectAddNewScript(bool isTest)
		{
			string title = string.Empty;
			string subTitle = string.Empty;
			string extension = string.Empty;

			if (isTest)
			{
				extension = ".tst";
				title = "New Test";
				subTitle = "Test name";
			}
			else
			{
				extension = ".scr";
				title = "New Script";
				subTitle = "Script name";
			}

			string scriptName = GetScriptName(title, subTitle, extension, "Create", "", true, ProjectPath);
			if (string.IsNullOrEmpty(scriptName))
				return;

			DesignScriptViewModel sd = Project.ScriptsList.ToList().Find((s) => s.CurrentScript.Name.ToLower() == scriptName.ToLower());
			if(sd != null)
			{
				{
					MessageBoxResult overwriteResult = MessageBox.Show(
						"An item with the name " + scriptName + " already exist.",
						title);
					if (overwriteResult != MessageBoxResult.Yes)
						return;
				}
			}

			DesignScriptViewModel vm = new DesignScriptViewModel(_scriptUserData, _devicesContainer, true);
			vm.New(isTest, scriptName);
			if (vm.CurrentScript == null)
				return;

			vm.ScriptReloadedEvent += ScriptReloadedEventHandler;
			vm.CurrentScript.Parent = Project;

			Project.ScriptsList.Add(vm);

			

			vm.CurrentScript.ScriptPath = Path.Combine(Path.GetDirectoryName(ProjectPath), vm.CurrentScript.Name + extension);
			AddScriptPath(vm.CurrentScript, Project.ScriptsPathsList);
			vm.Save(isTest);
			Project.IsChanged = true;

		}

		#endregion New script/test


		#region Add Existing

		private void ProjectAddExistingTest()
		{
			OpenFileDialog openFileDialog = new OpenFileDialog();
			openFileDialog.Filter = "Script files (*.scr;*.tst)|*.scr;*.tst";
			openFileDialog.Multiselect = true;

			string initDir = "";
			if (!string.IsNullOrEmpty(_scriptUserData.LastDesignScriptPath))
				initDir = _scriptUserData.LastDesignScriptPath;
			if (Directory.Exists(initDir) == false)
				initDir = "";
			openFileDialog.InitialDirectory = initDir;
			bool? result = openFileDialog.ShowDialog();

			if (result != true)
				return;

			foreach (string path in openFileDialog.FileNames)
				OpenSelectedExisting(path);
		}

		private void OpenSelectedExisting(string path)
		{ 

			_scriptUserData.LastDesignScriptPath =
				System.IO.Path.GetDirectoryName(path);



			string scriptName = Path.GetFileName(path);
			scriptName = scriptName.Replace(Path.GetExtension(path), string.Empty);
			foreach (DesignScriptViewModel scriptData in Project.ScriptsList)
			{
				if (scriptData == null)
					continue;

				if (scriptData.CurrentScript.Name == scriptName)
				{
					MessageBox.Show(
						"The name " + scriptName + " already exist in the project.\r\nCannot add the file",
						"Add existing");
					return;
				}
			}





			ProjectAddExistingTest(path);
		}

		private void ProjectAddExistingTest(string path)
		{
			string scriptName = Path.GetFileName(path);
			string projetDir = Path.GetDirectoryName(ProjectPath);

			string newPath = Path.Combine(projetDir, scriptName);
			if (newPath != path)
			{
				if (File.Exists(newPath) == false)
					File.Copy(path, newPath);
			}

			path = newPath;

			DesignScriptViewModel vm = new DesignScriptViewModel(_scriptUserData, _devicesContainer, false);
			vm.Open(path: path);
			if (vm.CurrentScript == null)
				return;

			vm.ScriptReloadedEvent += ScriptReloadedEventHandler;
			PostLoadAllScripts();

			string extension = ".scr";
			if(vm.CurrentScript is TestData)
				extension = ".tst";

			Project.ScriptsList.Add(vm);
			vm.CurrentScript.ScriptPath = Path.Combine(Path.GetDirectoryName(ProjectPath), vm.CurrentScript.Name + extension);
			AddScriptPath(vm.CurrentScript, Project.ScriptsPathsList);
			Project.IsChanged = true;

		}

		#endregion Add Existing

		private void ProjectRename()
		{
			RenameProjectService renameProject = new RenameProjectService();
			string projectPath = renameProject.ProjectRename(ProjectPath, Project.Name);
			if (string.IsNullOrEmpty(projectPath))
				return;


			_scriptUserData.LastProjectPath = Path.GetDirectoryName(projectPath);

			string projectDir = Path.GetDirectoryName(ProjectPath);
			Directory.Delete(projectDir, true);

			OpenProject(projectPath);
		}

		private void DeleteScript(DesignScriptViewModel script)
		{
			DeleteScript(script, true);
		}

		private void DeleteScript(DesignScriptViewModel script, bool isShowWarning)
		{
			if (script == null)
			{
				return;
			}

			string tmp = "script";
			if (script.CurrentScript is TestData)
				tmp = "test";

			if (isShowWarning)
			{
				MessageBoxResult result = MessageBox.Show(
					"The " + tmp + " file will be deleted permanently. \r\nDo you wish to continue?",
					"Delete " + tmp,
					MessageBoxButton.YesNo);
				if (result != MessageBoxResult.Yes)
					return;
			}

			DesignScriptViewModel vm = Project.ScriptsList.ToList().Find((t) => t.CurrentScript.Name == script.CurrentScript.Name);
			Project.ScriptsList.Remove(vm);

			DockingScript.CloseScript(vm.CurrentScript);

			string relativePath = script.CurrentScript.ScriptPath;
			if (!script.CurrentScript.ScriptPath.StartsWith("..\\") &&
				File.Exists(script.CurrentScript.ScriptPath))
			{
				relativePath = Path.GetRelativePath(ProjectPath, script.CurrentScript.ScriptPath);
			}

			Project.ScriptsPathsList.Remove(relativePath);

			

			File.Delete(script.CurrentScript.ScriptPath);

			PostLoadAllScripts();
			SaveProject();
			Project.IsChanged = false;
		}

		private void CopyScript(DesignScriptViewModel script)
		{
			if (script.CurrentScript.Name.EndsWith(" - Unloaded"))
			{
				MessageBox.Show("Unloaded", "Open Script");
				return;
			}

			string title = string.Empty;
			string subTitle = string.Empty;
			string extension = string.Empty;

			if (script.CurrentScript is TestData)
			{
				extension = ".tst";
				title = "Copy Test";
				subTitle = "Test name";
			}
			else
			{
				extension = ".scr";
				title = "Copy Script";
				subTitle = "Script name";
			}

			string copiedScriptName = GetScriptName(title, subTitle, extension, "Copy", script.CurrentScript.Name, true, ProjectPath);
			if (copiedScriptName == null)
				return;

			if (copiedScriptName == script.CurrentScript.Name)
				return;


			string projDir = Path.GetDirectoryName(ProjectPath);

			
			List<string> existingNameFile = new List<string>();
			string[] projectFiles = Directory.GetFiles(projDir);
			foreach (string file in projectFiles) 
			{ 
				string fileName = Path.GetFileName(file);
				if (fileName.StartsWith(copiedScriptName))
				{
					fileName = fileName.Substring(0, fileName.Length - 4);
					string end = fileName.Substring(copiedScriptName.Length);
					if(string.IsNullOrEmpty(end))
					{
						existingNameFile.Add(fileName);
						continue;
					}

					end = end.Trim();
					if(end.StartsWith("(") && end.EndsWith(")"))
					{
						end = end.Trim('(');
						end = end.Trim(')');
						int n;
						bool res = int.TryParse(end, out n);
						if(res)
						{
							existingNameFile.Add(fileName);
							continue;
						}
					}
				}
			}

			if(existingNameFile.Count > 0)
			{
				copiedScriptName += "(" + (existingNameFile.Count + 1) + ")";
			}

			CopyScript(
				script.CurrentScript.ScriptPath,
				script.CurrentScript.Name,
				copiedScriptName);
		}

		private void CopyScript(
			string originalPath,
			string originalName, 
			string newName)
		{ 
			string path = originalPath;

			string fileStr = File.ReadAllText(originalPath);
			fileStr = fileStr.Replace(originalName, newName);

			int index = path.LastIndexOf("\\");
			string startPath = path.Substring(0, index);
			string endPath = path.Substring(index + 1);
			endPath = endPath.Replace(originalName, newName);
			
			path = Path.Combine(startPath, endPath);

			File.WriteAllText(path, fileStr);

			ProjectAddExistingTest(path);
		}

		private void RenameScript(DesignScriptViewModel vm) 
		{
			if (vm.CurrentScript.Name.EndsWith(" - Unloaded"))
			{
				MessageBox.Show("Unloaded", "Open Script");
				return;
			}

			DockingScript.CloseScript(vm.CurrentScript);

			string title = string.Empty;
			string subTitle = string.Empty;
			string extension = string.Empty;

			if (vm.CurrentScript is TestData)
			{
				extension = ".tst";
				title = "Change Test Name";
				subTitle = "New test name";
			}
			else
			{
				extension = ".scr";
				title = "Change Script Name";
				subTitle = "New script name";
			}

			string scriptName = GetScriptName(
				title,
				subTitle,
				extension,
				"Change",
				vm.CurrentScript.Name,
				true,
				ProjectPath);
			if (scriptName == null)
				return;

			string orignalPath = vm.CurrentScript.ScriptPath;

			CopyScript(
				vm.CurrentScript.ScriptPath,
				vm.CurrentScript.Name,
				scriptName);

			DeleteScript(vm, false);
			File.Delete(orignalPath);

			foreach(DesignScriptViewModel scriptData in Project.ScriptsList)
			{
				HandleSubScriptInScript(scriptData.CurrentScript);
			}
		}


		private void MouseDoubleClick(MouseButtonEventArgs e)
		{
			if(!(e.Source is ListView listView)) 
				return;

			if (!(listView.SelectedItem is DesignScriptViewModel vm))
				return;

			vm.IsChanged = false;

			DesignScriptViewModel sameVM = null;
			foreach (DesignScriptViewModel dockVm in DockingScript.DesignScriptsList)
			{
				if(dockVm != vm &&
					dockVm.CurrentScript.ScriptPath == vm.CurrentScript.ScriptPath)
				{
					sameVM = dockVm;
					break;
				}
			}

			if(sameVM != null) 
				DockingScript.CloseDesignScript(sameVM);

			DockingScript.OpenScript(vm);
			vm.GetScriptDiagram();


			vm.IsChanged = false;
		}

		#region Script path

		private void AddScriptPath(
			ScriptData script, 
			List<string> pathsList)
		{
			string relativePath = Path.GetRelativePath(ProjectPath, script.ScriptPath);
			if (pathsList.Contains(relativePath))
				return;

			pathsList.Add(relativePath);
		}
		
		private string GetAbsPath(string path)
		{
			string absPath = path;
			if (absPath.StartsWith("..\\"))
				absPath = absPath.Substring("..\\".Length);
			absPath = Path.Combine(_scriptUserData.LastProjectPath, absPath);
			absPath = Path.GetFullPath(absPath);

			return absPath;
		}

		#endregion Script path

		private void GetProjectCanMessages(
			ScriptData scriptData)
		{
			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (node is ScriptNodeCANMessage canMessage)
				{
					if (node is ScriptNodeCANMessageUpdate update)
						update.ParentProject = Project;
					else
						Project.CanMessagesList.Add(canMessage);

				}
				else if (node is ScriptNodeStopContinuous stopContinuous)
				{
					stopContinuous.ParentProject = Project;
				}
			}

		}

		private void SetTestsCanMessagesUpdate(
			ScriptData scriptData)
		{
			
			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (node is ScriptNodeCANMessageUpdate update)
				{
					if (update.StepToUpdateID != 0)
					{
						foreach (ScriptNodeCANMessage canMessage in Project.CanMessagesList)
						{
							if (canMessage.IDInProject == update.StepToUpdateID)
							{
								update.StepToUpdate = canMessage;
								break;
							}
						}
					}
				}
			}

		}

		private void SetTestsStopContinuous(
			ScriptData scriptData)
		{

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (node is ScriptNodeStopContinuous stop)
				{
					if (stop.StepToStopID != 0)
					{
						foreach (ScriptNodeCANMessage canMessage in Project.CanMessagesList)
						{
							if (canMessage.IDInProject == stop.StepToStopID)
							{
								stop.StepToStop = canMessage;
								break;
							}
						}
					}
				}
			}

		}

		//private void SetCanMessageIDInTest(TestData testData)
		//{
		//	int id = 1;
		//	foreach (ScriptNodeCANMessage canMessage in testData.CanMessagesList)
		//	{
		//		canMessage.IDInTest = id++;
		//	}
		//}

		private void HandleSubScriptInScript(ScriptData scriptData)
		{
			if (scriptData == null)
				return;

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (!(node is ScriptNodeSubScript subScript))
					continue;

				subScript.Parent = Project;

				if (string.IsNullOrEmpty(subScript.SelectedScriptName))
					continue;



				foreach(DesignScriptViewModel vm in Project.ScriptsList) 
				{
					if(!(vm.CurrentScript is ScriptData testSubScriptData))
						continue;

					if (testSubScriptData.Name == subScript.SelectedScriptName)
					{
						subScript.Script = testSubScriptData;
					}
				}

				HandleSubScriptInScript(
					subScript.Script as ScriptData);
			}
		}

		private void HandleSweepSubScriptInScript(ScriptData scriptData)
		{
			if (scriptData == null)
				return;

			foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
			{
				if (!(node is ScriptNodeSweep sweep))
					continue;

				sweep.Parent = Project;


				foreach (SweepItemData sweepItem in sweep.SweepItemsList) 
				{
					
					if (sweepItem.SubScriptName == null)
						continue;

					
					ScriptData subScript = Project.ScriptsOnlyList.ToList().Find((s) => s.Name== sweepItem.SubScriptName);
					if(subScript == null)
						continue;

					sweepItem.SubScript = subScript;

				}
			}
		}

		private static bool IsFileExist(string fileName, string sourcePath)
		{
			string projDir = Path.GetDirectoryName(sourcePath);

			string path = Path.Combine(projDir, fileName);
			return File.Exists(path);
		}
		
		public static string GetScriptName(
			string title,
			string subTitle,
			string extension,
			string buttonTitle,
			string scriptName,
			bool isExistingTest,
			string sourcePath)
		{
			ScriptNameView scriptNameView = new ScriptNameView();

			scriptNameView.Title = title;
			scriptNameView.SubTitle = subTitle;
			scriptNameView.ButtonTitle = buttonTitle;
			scriptNameView.ScriptName = scriptName;

			bool? result = scriptNameView.ShowDialog();
			if (result != true)
			{
				return null;
			}

			int index = scriptNameView.ScriptName.IndexOfAny(Path.GetInvalidFileNameChars());
			if(index != -1) 
			{
				MessageBoxResult overwriteResult = MessageBox.Show(
					"The name is not valid.\r\nThe name cannot contain the folowing characters:\r\n" + _invalidFileChars,
					scriptNameView.Title);
				return null;
			}


			if (!isExistingTest)
				return scriptNameView.ScriptName;



			bool isFileExist = IsFileExist(scriptNameView.ScriptName + extension, sourcePath);
			if (isFileExist)
			{
				MessageBoxResult overwriteResult = MessageBox.Show(
					"A file with the name " + (scriptNameView.ScriptName + extension) + " already exist.\r\nDo you wish to overwrite it?",
					scriptNameView.Title,
					MessageBoxButton.YesNo);
				if (overwriteResult != MessageBoxResult.Yes)
					return null;
			}

			return scriptNameView.ScriptName;
		}

		private void ScriptSavedEventHandler(object sender, EventArgs e)
		{
			PostLoadAllScripts();
			foreach (DesignScriptViewModel vm in Project.ScriptsList)
			{
				vm.RefreshDiagram();
			}
		}

		private void PostLoadAllScripts()
		{
			Project.CanMessagesList.Clear();
			foreach (DesignScriptViewModel vm in Project.ScriptsList)
			{
				vm.CurrentScript.Parent = Project;
				HandleSubScriptInScript(vm.CurrentScript);
				HandleSweepSubScriptInScript(vm.CurrentScript);

				if (!vm.IsScriptIsSavedEvent)
				{
					vm.ScriptIsSavedEvent += ScriptSavedEventHandler;
					vm.IsScriptIsSavedEvent = true;
				}

				GetProjectCanMessages(vm.CurrentScript);				
			}

			foreach (DesignScriptViewModel vm in Project.ScriptsList)
			{
				SetTestsCanMessagesUpdate(vm.CurrentScript);
				SetTestsStopContinuous(vm.CurrentScript);
			}

			for (int i = 0; i < Project.CanMessagesList.Count; i++) 
			{
				Project.CanMessagesList[i].IDInProject = i + 1;
			}
		}

		private void ScriptReloadedEventHandler(object sender, EventArgs e)
		{
			PostLoadAllScripts();
		}








		#region Drag

		private void ListScript_MouseEnter(MouseEventArgs e)
		{
			
			if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
				_isMouseDown = true;
			else
				_isMouseDown = false;
		}

		private void ListScript_PreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			
			// If mouse down on the scroll bar, don't do anything.
			var thumb =
				FindAncestorService.FindAncestor<Thumb>((DependencyObject)e.OriginalSource);
			if (thumb != null)
				return;



			_isMouseDown = true;
			_startPoint = e.GetPosition(null);
		}

		private void ListScript_PreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_isMouseDown = false;
		}


		private void ListScript_MouseMove(MouseEventArgs e)
		{
			if (_isMouseDown == false)
				return;


			LoggerService.Inforamtion(this, "Object is draged");

			Point mousePos = e.GetPosition(null);
			Vector diff = _startPoint - mousePos;

			if (e.LeftButton == MouseButtonState.Pressed &&
				Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
				Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				string formate = "ProjectFile";

				ListViewItem listViewItem =
						FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
				if (listViewItem == null)
					return;


				if (!(listViewItem.DataContext is DesignScriptViewModel vm))
					return;
				
				DataObject dragData = new DataObject(formate, vm);
				DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);


			}
		}

		private void ListScript_DragOver(DragEventArgs e)
		{
			if (!(e.Source is ListView li))
				return;
			//ListBox li = sender as ListBox;
			ScrollViewer sv = FindChildService.FindChild<ScrollViewer>(li);
			if (sv == null)
				return;

			double tolerance = 10;
			double verticalPos = e.GetPosition(li).Y;
			double offset = 3;

			if (verticalPos < tolerance) // Top of visible list?
			{
				sv.ScrollToVerticalOffset(sv.VerticalOffset - offset); //Scroll up.
			}
			else if (verticalPos > li.ActualHeight - tolerance) //Bottom of visible list?
			{
				sv.ScrollToVerticalOffset(sv.VerticalOffset + offset); //Scroll down.    
			}
		}

		#endregion Drag

		#region Drop

		private void ListScript_Drop(DragEventArgs e)
		{
			LoggerService.Inforamtion(this, "Object is dropped");

			try
			{

				if (Project == null || Project.ScriptsList == null || Project.ScriptsList.Count == 0)
				{
					return;
				}



				if (e.Data.GetDataPresent("ProjectFile"))
				{
					DesignScriptViewModel dropped;
					DesignScriptViewModel droppedOn;
					GetDroppedAndDroppedOn(
						e,
						out dropped,
						out droppedOn);
					if(dropped == null)
						return;
					

					MoveFile(dropped, droppedOn);
				}
				

			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to drop", "Design Error", ex);
			}

		}

		

		private void GetDroppedAndDroppedOn(
			DragEventArgs e,
			out DesignScriptViewModel dropped,
			out DesignScriptViewModel droppedOn)
		{
			droppedOn = null;

			DesignScriptViewModel vm = e.Data.GetData("ProjectFile") as DesignScriptViewModel;
			dropped = vm;


			ListViewItem listViewItem =
				FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
			if (listViewItem == null)
				return;

			droppedOn = listViewItem.DataContext as DesignScriptViewModel;
		}

		private void ListScript_DragEnter(DragEventArgs e)
		{
			if (!e.Data.GetDataPresent("ScriptNodeTool"))
			{
				e.Effects = DragDropEffects.None;
			}
		}

		#endregion Drop


		private void MoveFile(
			DesignScriptViewModel dropped,
			DesignScriptViewModel droppedOn)
		{
			int droppedIndex = Project.ScriptsList.IndexOf(dropped);
			int droppedOnIndex = Project.ScriptsList.IndexOf(droppedOn);

			DesignScriptViewModel tmp = dropped;

			Project.ScriptsList.RemoveAt(droppedIndex);

			if(droppedOnIndex >= 0)
				Project.ScriptsList.Insert(droppedOnIndex, tmp);
			else
				Project.ScriptsList.Add(tmp);


			string droppedPath = Project.ScriptsPathsList[droppedIndex];

			Project.ScriptsPathsList.RemoveAt(droppedIndex);

			if (droppedOnIndex >= 0)
				Project.ScriptsPathsList.Insert(droppedOnIndex, droppedPath);
			else
				Project.ScriptsPathsList.Add(droppedPath);

			Project.IsChanged = true;
		}


		#endregion Methods

		#region Commands


		public RelayCommand ProjectAddNewTestCommand { get; private set; }
		public RelayCommand ProjectAddNewScriptCommand { get; private set; }
		public RelayCommand ProjectAddExistingCommand { get; private set; }
		public RelayCommand ProjectRenameCommand { get; private set; }

		public RelayCommand<DesignScriptViewModel> DeleteScriptCommand { get; private set; }
		public RelayCommand<DesignScriptViewModel> CopyScriptCommand { get; private set; }
		public RelayCommand<DesignScriptViewModel> RenameScriptCommand { get; private set; }


		private RelayCommand<MouseButtonEventArgs> _MouseDoubleClickCommand;
		public RelayCommand<MouseButtonEventArgs> MouseDoubleClickCommand
		{
			get
			{
				return _MouseDoubleClickCommand ?? (_MouseDoubleClickCommand =
					new RelayCommand<MouseButtonEventArgs>(MouseDoubleClick));
			}
		}







		#region Drag


		private RelayCommand<MouseEventArgs> _ListScript_MouseMoveCommand;
		public RelayCommand<MouseEventArgs> ListScript_MouseMoveCommand
		{
			get
			{
				return _ListScript_MouseMoveCommand ?? (_ListScript_MouseMoveCommand =
					new RelayCommand<MouseEventArgs>(ListScript_MouseMove));
			}
		}

		private RelayCommand<MouseEventArgs> _ListScript_MouseEnterCommand;
		public RelayCommand<MouseEventArgs> ListScript_MouseEnterCommand
		{
			get
			{
				return _ListScript_MouseEnterCommand ?? (_ListScript_MouseEnterCommand =
					new RelayCommand<MouseEventArgs>(ListScript_MouseEnter));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _ListScript_PreviewMouseLeftButtonDownCommant;
		public RelayCommand<MouseButtonEventArgs> ListScript_PreviewMouseLeftButtonDownCommant
		{
			get
			{
				return _ListScript_PreviewMouseLeftButtonDownCommant ?? (_ListScript_PreviewMouseLeftButtonDownCommant =
					new RelayCommand<MouseButtonEventArgs>(ListScript_PreviewMouseLeftButtonDown));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _ListScript_PreviewMouseLeftButtonUpCommant;
		public RelayCommand<MouseButtonEventArgs> ListScript_PreviewMouseLeftButtonUpCommant
		{
			get
			{
				return _ListScript_PreviewMouseLeftButtonUpCommant ?? (_ListScript_PreviewMouseLeftButtonUpCommant =
					new RelayCommand<MouseButtonEventArgs>(ListScript_PreviewMouseLeftButtonUp));
			}
		}


		private RelayCommand<DragEventArgs> _ListScript_DragOverCommand;
		public RelayCommand<DragEventArgs> ListScript_DragOverCommand
		{
			get
			{
				return _ListScript_DragOverCommand ?? (_ListScript_DragOverCommand =
					new RelayCommand<DragEventArgs>(ListScript_DragOver));
			}
		}

		#endregion Drag

		#region Drop

		private RelayCommand<DragEventArgs> _ListScript_DropCommand;
		public RelayCommand<DragEventArgs> ListScript_DropCommand
		{
			get
			{
				return _ListScript_DropCommand ?? (_ListScript_DropCommand =
					new RelayCommand<DragEventArgs>(ListScript_Drop));
			}
		}

		private RelayCommand<DragEventArgs> _ListScript_DragEnterCommand;
		public RelayCommand<DragEventArgs> ListScript_DragEnterCommand
		{
			get
			{
				return _ListScript_DragEnterCommand ?? (_ListScript_DragEnterCommand =
					new RelayCommand<DragEventArgs>(ListScript_DragEnter));
			}
		}

		#endregion Drop

		#endregion Commands


	}
}
