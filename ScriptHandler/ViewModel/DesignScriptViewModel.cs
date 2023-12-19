using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using DeviceHandler.ViewModel;
using Entities.Enums;
using Entities.Models;
using Microsoft.Win32;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ScriptHandler.ViewModels
{
	public class DesignScriptViewModel: ObservableObject
	{
		#region Properties

		public ObservableCollection<IScriptItem> ScriptNodeList
		{
			get
			{
				if (CurrentScript == null)
					return null;

				return CurrentScript.ScriptItemsList;
			}
		}


		public ScriptDiagramViewModel ScriptDiagram { get; set; }


		public ScriptData CurrentScript { get; set; }

		private bool _isChanged;
		public bool IsChanged 
		{
			get => _isChanged;
			set
			{
				if (_isIgnoreChanges)
					return;

				//ScriptIsChangedEvent?.Invoke(this, value);
				_isChanged = value;
				OnPropertyChanged(nameof(IsChanged));
			}
		}

		
		public bool IsScriptEnabled { get; set; }

		

		

#endregion Properties

		#region Fields

		private bool _isMouseDown;
		private Point _startPoint;

		private int _nodeIndex;

		private ScriptNodeBase _selectedNode;

		private ScriptValidationService _scriptValidation;		

		

		public bool IsLoadingScript = false;

		private ScriptUserData _scriptUserData;

		private DevicesContainer _devicesContainer;

		private bool _isNew;

		private bool _isIgnoreChanges;

		private MoveNodeService _moveNodeService;

		private IList _selectedItems;

		public bool IsScriptIsSavedEvent;

		#endregion Fields

		#region Constructor

		public DesignScriptViewModel(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer,
			bool isNew)
		{
			_isNew = isNew;
			_scriptUserData = scriptUserData;
			_devicesContainer = devicesContainer;

			_isIgnoreChanges = true;

			LoadedCommand = new RelayCommand(Loaded);
			LoadedCommand = new RelayCommand(Loaded);

			MoveNodeUpCommand = new RelayCommand(MoveNodeUp);
			MoveNodeDownCommand = new RelayCommand(MoveNodeDown);
			DeleteCommand = new RelayCommand(Delete);
			ExportScriptToPDFCommand = new RelayCommand(ExportScriptToPDF);

			DeleteNextPassCommand = new RelayCommand<ScriptNodeBase>(DeleteNextPass);
			DeleteNextFailCommand = new RelayCommand<ScriptNodeBase>(DeleteNextFail);

			CopyScriptNodeCommand = new RelayCommand(CopyScriptNode);

			ScriptExpandAllCommand = new RelayCommand(ScriptExpandAll);
			ScriptCollapseAllCommand = new RelayCommand(ScriptCollapseAll);

			CopyCommand = new RelayCommand(Copy);
			PastCommand = new RelayCommand(Past);
			SaveCommand = new RelayCommand(Save);


			_isMouseDown = false;
			IsScriptEnabled = false;


			_moveNodeService = new MoveNodeService();

			_nodeIndex = 1;

			

			_scriptValidation = new ScriptValidationService();

			ScriptDiagram = new ScriptDiagramViewModel();

			ScriptDiagram.ChangeBackground(
				Application.Current.MainWindow.FindResource("MahApps.Brushes.Control.Background") as SolidColorBrush);


			LoggerService.Inforamtion(this, "Finished init of Design");

			_isIgnoreChanges = false;
			IsChanged = false;

		}

		#endregion Constructor

		#region Methods

		private void Loaded()
		{
			if(!_isNew)
				IsChanged = false;

			_isNew = false;
		}

		public void RefreshDiagram()
		{
			ScriptDiagram.ChangeBackground(
				Application.Current.MainWindow.FindResource("MahApps.Brushes.Control.Background") as SolidColorBrush);
			GetScriptDiagram();
		}

		#region Expand/Collapse
			

		private void ScriptExpandAll()
		{
			LoggerService.Inforamtion(this, "Expanding all the script nodes");

			foreach (ScriptNodeBase nodeBase in CurrentScript.ScriptItemsList)
				nodeBase.IsExpanded = true;
		}

		private void ScriptCollapseAll()
		{
			LoggerService.Inforamtion(this, "Collapsing all the script nodes");

			foreach (ScriptNodeBase nodeBase in CurrentScript.ScriptItemsList)
				nodeBase.IsExpanded = false;
		}

		#endregion Expand/Collapse

		#region Drag

		private void ListScript_MouseEnter(MouseEventArgs e)
		{
			if (IsLoadingScript)
				return;

			if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
				_isMouseDown = true;
			else
				_isMouseDown = false;
		}

		private void ListScript_PreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (IsLoadingScript)
				return;


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
				string formate = "ScriptNodeScript";

				// Get the dragged ListViewItem
				ListView listView =
					FindAncestorService.FindAncestor<ListView>((DependencyObject)e.OriginalSource);
				if (listView == null)
					return;

				ListViewItem listViewItem =
						FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
				if (listViewItem == null)
					return;

				if (!listView.SelectedItems.Contains(listViewItem.DataContext))
				{
					DataObject dragData = new DataObject(formate, listViewItem.DataContext);
					DragDrop.DoDragDrop(listViewItem, dragData, DragDropEffects.Move);
				}
				else
				{
					DataObject dragData = new DataObject(formate, listView.SelectedItems);
					DragDrop.DoDragDrop(listView, dragData, DragDropEffects.Move);
				}
				


				
			}
		}

		private void ListScript_DragOver(DragEventArgs e)
		{
			if(!(e.Source is ListView li))
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

				if (CurrentScript == null)
					return;

				if (e.Data.GetDataPresent("ScriptNodeTool"))
				{
					ScriptNodeBase scriptNodeBase = e.Data.GetData("ScriptNodeTool") as ScriptNodeBase;
					AddNode(scriptNodeBase, e);
				}
				else if (e.Data.GetDataPresent(ParametersViewModel.DragDropFormat))
				{
					string tbName = "";
					TextBlock textBlock = null;
					TextBox textBox = null;
					if (e.OriginalSource is TextBlock)
					{
						textBlock = e.OriginalSource as TextBlock;
						tbName = textBlock.Name;
						if (!tbName.StartsWith("tbParam"))
							return;
					}
					else 
					{
						textBox =
							FindAncestorService.FindAncestor<TextBox>((DependencyObject)e.OriginalSource);
						if (textBox != null)
						{
							tbName = textBox.Name;
							if (!tbName.StartsWith("tbParam"))
								return;
						}
					}

					DeviceParameterData param = e.Data.GetData(ParametersViewModel.DragDropFormat) as DeviceParameterData;

					ListViewItem listViewItem =
						FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
					if (listViewItem == null)
						return;

					
					if (listViewItem.DataContext is ScriptNodeCompare compare)
					{
						if (tbName.EndsWith("Left"))
							compare.ValueLeft = param;
						else if (tbName.EndsWith("Right"))
							compare.ValueRight = param;
					}
					else if (listViewItem.DataContext is ScriptNodeCompareRange compareRange)
					{
						if (tbName.EndsWith("Left"))
							compareRange.ValueLeft = param;
						else if (tbName.EndsWith("Right"))
							compareRange.ValueRight = param;
						else
							compareRange.Value = param;
					}
					else if (listViewItem.DataContext is ScriptNodeConverge converge)
					{
						if (tbName.EndsWith("TargetValue"))
							converge.TargetValue = param;
						else
							converge.Parameter = param;
					}
					else if (listViewItem.DataContext is ScriptNodeDynamicControl dynamicControl)
					{
						if(textBlock != null && textBlock.DataContext is DynamicControlColumnData columnData)
						{
							columnData.Parameter = param;
						}
					}
					else if (listViewItem.DataContext is IScriptStepWithParameter withParam)
					{
						withParam.Parameter = param;
					}
				}
				else if (e.Data.GetDataPresent("ScriptNodeScript"))
				{
					if (e.OriginalSource is TextBlock textBlock)
					{
						if(textBlock.Name == "tbPassNextNode" || textBlock.Name == "tbFailNextNode")
						{
							SetPassFailNextNode(e);
							return;
						}
						else if (textBlock.Name == "tbStepToStop")
						{
							SetStepToStop(e);
							return;
						}
						else if (textBlock.Name == "tbStepToUpdate")
						{
							SetStepToUpdateCANMessage(e);
							return;
						}
					}

					ScriptNodeBase droppedOnNode;
					int indexOfDroppedOn = GetNewNodePosition(e, out droppedOnNode);
					bool isContinue = MoveNode(droppedOnNode);
					if (!isContinue)
						return;

				}

				IsChanged = true;

			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to drop", "Design Error", ex);
			}

		}

		private void SetStepToStop(DragEventArgs e)
		{
			string tbName = "";
			if (e.OriginalSource is TextBlock textBlock)
				tbName = textBlock.Name;

			if (tbName != "tbStepToStop")
				return;

			ScriptNodeBase dropped;
			ScriptNodeBase droppedOn;
			GetDroppedAndDroppedOn(
				e,
				out dropped,
				out droppedOn);

			if (droppedOn is ScriptNodeStopContinuous stopContinuous)
			{
				stopContinuous.StepToStop = dropped;
			}
		}

		private void SetStepToUpdateCANMessage(DragEventArgs e)
		{
			string tbName = "";
			if (e.OriginalSource is TextBlock textBlock)
				tbName = textBlock.Name;

			if (tbName != "tbStepToUpdate")
				return;

			ScriptNodeBase dropped;
			ScriptNodeBase droppedOn;
			GetDroppedAndDroppedOn(
				e,
				out dropped,
				out droppedOn);

			if (droppedOn is ScriptNodeCANMessageUpdate canMessageUpdate)
			{
				canMessageUpdate.StepToUpdate = dropped;
				canMessageUpdate.SetCanMessage(dropped as ScriptNodeCANMessage);
			}
		}


		private void SetPassFailNextNode(DragEventArgs e)
		{
			string tbName = "";
			if (e.OriginalSource is TextBlock textBlock)
				tbName = textBlock.Name;

			if (tbName != "tbPassNextNode" && tbName != "tbFailNextNode")
				return;

			ScriptNodeBase dropped;
			ScriptNodeBase droppedOn;
			GetDroppedAndDroppedOn(
				e,
				out dropped,
				out droppedOn);
			
			if (tbName == "tbPassNextNode")
				droppedOn.PassNext = dropped;
			else if (tbName == "tbFailNextNode")
				droppedOn.FailNext = dropped;

		}

		private void GetDroppedAndDroppedOn(
			DragEventArgs e,
			out ScriptNodeBase dropped,
			out ScriptNodeBase droppedOn)
		{
			dropped = null;
			droppedOn = null;

			IList selectedItems = e.Data.GetData("ScriptNodeScript") as IList;
			if (selectedItems != null && selectedItems.Count != 0)
			{
				IEnumerator enumerator = selectedItems.GetEnumerator();
				enumerator.MoveNext();
				if (enumerator.Current == null)
					return;

				dropped = enumerator.Current as ScriptNodeBase;
			}
			else
			{
				dropped = e.Data.GetData("ScriptNodeScript") as ScriptNodeBase;
			}
			

			ListViewItem listViewItem =
				FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
			if (listViewItem == null)
				return;

			droppedOn = listViewItem.DataContext as ScriptNodeBase;
		}

		private void ListScript_DragEnter(DragEventArgs e)
		{
			if (!e.Data.GetDataPresent("ScriptNodeTool"))
			{
				e.Effects = DragDropEffects.None;
			}
		}

		#endregion Drop

		#region Up/Down

		private void MoveNodeUp()
		{
			try
			{
				LoggerService.Inforamtion(this, "Moving node UP");

				if (_selectedItems == null || _selectedItems.Count == 0)
					return;

				IEnumerator enumerator = _selectedItems.GetEnumerator();
				enumerator.MoveNext();

				MoveNode(enumerator.Current as ScriptNodeBase);

				IsChanged = true;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to move node up", "Up/Down Error", ex);
			}
		}

		private void MoveNodeDown()
		{
			LoggerService.Inforamtion(this, "Moving node DOWN");

			try
			{
				
				if (_selectedItems == null || _selectedItems.Count == 0)
					return;

				IEnumerator enumerator = _selectedItems.GetEnumerator();
				enumerator.MoveNext();

				MoveNode(enumerator.Current as ScriptNodeBase);

				IsChanged = true;
			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to move node down", "Up/Down Error", ex);
			}
		}

		#endregion Up/Down

		#region Script file handling

		public void New(bool isTest, string name)
		{
			_isIgnoreChanges = true;

			if (isTest)
				CurrentScript = new TestData();
			else
				CurrentScript = new ScriptData();
			CurrentScript.Name = name;

			CurrentScript.ScriptPath = null;
			_nodeIndex = 1;

			IsScriptEnabled = true;

			GetScriptDiagram();

			//CurrentScript.NodesListChanged +=
			//		NodesListChangedHandler;

			LoggerService.Inforamtion(this, "New script created: " + CurrentScript.Name);
			_isIgnoreChanges = false;

			IsChanged = true;
		}

		
		public void Open(ScriptData script = null, string path = null, bool allowTests = true)
		{
			if (CurrentScript != null)
			{
				bool isCancel = SaveIfNeeded();
				if (isCancel)
					return;

			}

			try
			{

				_isIgnoreChanges = true;

				if (script == null)
				{

					if (path == null)
					{
						OpenFileDialog openFileDialog = new OpenFileDialog();
						openFileDialog.Filter = "Script files (*.scr;*.tst)|*.scr;*.tst";

						string initDir = "";
						if (!string.IsNullOrEmpty(_scriptUserData.LastDesignScriptPath))
							initDir = _scriptUserData.LastDesignScriptPath;
						if (Directory.Exists(initDir) == false)
							initDir = "";
						openFileDialog.InitialDirectory = initDir;
						bool? result = openFileDialog.ShowDialog();
						
						if (result != true)
							return;

						_scriptUserData.LastDesignScriptPath =
							System.IO.Path.GetDirectoryName(openFileDialog.FileName);


						IsLoadingScript = true;

						path = openFileDialog.FileName;
					}

					if (File.Exists(path) == false)
					{
						LoggerService.Error(this, "The file " + path + " could not be found", "Load Error");
						_isIgnoreChanges = false;
						return;
					}

					string jsonString = File.ReadAllText(path);

					JsonSerializerSettings settings = new JsonSerializerSettings();
					settings.Formatting = Formatting.Indented;
					settings.TypeNameHandling = TypeNameHandling.All;
					CurrentScript = JsonConvert.DeserializeObject(jsonString, settings) as ScriptData;

					CurrentScript.ScriptPath = path;
				}
				else
					CurrentScript = script;

				if (CurrentScript == null)
				{
					LoggerService.Error(this, "Loaded an empty script", "Open Script Error");
					_isIgnoreChanges = false;
					return;
				}

				foreach (ScriptNodeBase scriptNode in CurrentScript.ScriptItemsList)
				{
					if (scriptNode is IScriptStepWithParameter withParam &&
						withParam.Parameter != null)
					{
						DeviceParameterData data = GetParameter(
							withParam.Parameter.DeviceType,
							withParam.Parameter.Name);
						if (data != null)
							withParam.Parameter = data;
					}

					scriptNode.PostLoad(
						_devicesContainer,
						CurrentScript);
				}


				_nodeIndex = 0;

				foreach (ScriptNodeBase node in CurrentScript.ScriptItemsList)
				{
					if (_nodeIndex < node.ID)
						_nodeIndex = node.ID;

					node.IsExpanded = true;
					node.NodePropertyChangeEvent += NodePropertyChangedHandler;



					if (node.PassNextId >= 0)
					{
						foreach (ScriptNodeBase passNextNode in CurrentScript.ScriptItemsList)
						{
							if (passNextNode.ID == node.PassNextId)
							{
								node.PassNext = passNextNode;
								break;
							}
						}
					}

					if (node.FailNextId >= 0)
					{
						foreach (ScriptNodeBase failNextNode in CurrentScript.ScriptItemsList)
						{
							if (failNextNode.ID == node.FailNextId)
							{
								node.FailNext = failNextNode;
								break;
							}
						}
					}


				}

				_nodeIndex++;

				IsScriptEnabled = true;

				LoggerService.Inforamtion(this, "Script opened: " + CurrentScript.Name);

				

				IsLoadingScript = false;

				GetScriptDiagram();

				_isIgnoreChanges = false;
				IsChanged = false;

			}
			catch(Exception ex) 
			{
				LoggerService.Error(this, "Failed to open a script", "Script Error", ex);
			}

		}

		private DeviceParameterData GetParameter(
			DeviceTypesEnum deviceType,
			string paramName)
		{
			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(deviceType) == false)
				return null;

			DeviceData deviceData =
				_devicesContainer.TypeToDevicesFullData[deviceType].Device;

			DeviceParameterData data = deviceData.ParemetersList.ToList().Find((p) => p.Name == paramName);
			
			return data;
		}

		public void Save()
		{
			Save(CurrentScript is TestData);
		}

		public void Save(bool isTest)
		{
			if (CurrentScript == null)
				return;

			try
			{

				bool isValid = _scriptValidation.Validate(CurrentScript);
				if (!isValid)
					return;

				_isIgnoreChanges = true;

				string saveType = "Script";
				string extention = "scr";
				if (isTest)
				{
					saveType = "Test";
					extention = "tst";
				}

				bool isNameDifferentFromFile = IsNameDifferentFromFile();
				if (isNameDifferentFromFile)
				{
					string fileName = Path.GetFileName(CurrentScript.ScriptPath);
					string extension = Path.GetExtension(CurrentScript.ScriptPath);
					fileName = fileName.Replace(extension, string.Empty);
					string str =
						"The name of the \"" + fileName + "\" file name is different from the \"" + CurrentScript.Name + "\" script name.\r\n" +
						"File name: " + fileName + "\r\n" +
						"saveType name: " + CurrentScript.Name + "\r\n" +
						"Do you wish to change the file name?";
					MessageBoxResult result = MessageBox.Show(
						str,
						"Warning",
						MessageBoxButton.YesNoCancel);
					if (result == MessageBoxResult.Cancel)
						return;
					if (result == MessageBoxResult.Yes)
					{
						CurrentScript.ScriptPath = CurrentScript.ScriptPath.Replace(fileName, CurrentScript.Name);
					}
				}


				if (string.IsNullOrEmpty(CurrentScript.ScriptPath))
				{
					SaveFileDialog saveFileDialog = new SaveFileDialog();
					saveFileDialog.FileName = CurrentScript.Name;
					saveFileDialog.Filter = saveType + " Files | *." + extention;
					bool? result = saveFileDialog.ShowDialog();
					if (result != true)
						return;

					CurrentScript.ScriptPath = saveFileDialog.FileName;
				}

				// Save Json
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				var sz = JsonConvert.SerializeObject(CurrentScript, settings);
				File.WriteAllText(CurrentScript.ScriptPath, sz);

				LoggerService.Inforamtion(this, "Script saved");

				ScriptIsSavedEvent?.Invoke(this, null);
				_isIgnoreChanges = false;
				IsChanged = false;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to save a script", "Script Error", ex);
			}
		}

		private bool IsNameDifferentFromFile()
		{
			if(string.IsNullOrEmpty(CurrentScript.ScriptPath) || 
				CurrentScript == null || string.IsNullOrEmpty(CurrentScript.Name))
			{
				return false;
			}	

			string fileName = Path.GetFileName(CurrentScript.ScriptPath);
			string extension = Path.GetExtension(CurrentScript.ScriptPath);
			fileName = fileName.Replace(extension, string.Empty);

			if(fileName.ToLower() != CurrentScript.Name.ToLower())
				return true;

			return false;
		}

		#endregion Script file handling


		public void AddNode(
			ScriptNodeBase source_scriptNodeBase,
			DragEventArgs e)
		{
			ScriptNodeBase new_scriptNodeBase = source_scriptNodeBase.Clone() as ScriptNodeBase;

			AddNode_do(
				new_scriptNodeBase,
				source_scriptNodeBase.Name,
				e);
		}

		private void AddNode_do(
			ScriptNodeBase new_scriptNodeBase,
			string name,
			DragEventArgs e)
		{

			new_scriptNodeBase.Name = name + " " + _nodeIndex;
			new_scriptNodeBase.ID = _nodeIndex++;
			new_scriptNodeBase.IsExpanded = true;

			foreach (ScriptNodeBase node in ScriptNodeList)
				node.IsSelected = false;
			new_scriptNodeBase.IsSelected = true;
			new_scriptNodeBase.NodePropertyChangeEvent += NodePropertyChangedHandler;

			if(new_scriptNodeBase is ScriptNodeSubScript subScript)
			{	
				subScript.Parent = CurrentScript.Parent as ProjectData;
				subScript.NodePropertyChangeEvent += SubScriptPropertyChangedEventHandler;
				subScript.ParentScriptName = CurrentScript.Name;

				if (subScript.Script != null)
				{
					IScript script =
						subScript.Parent.ScriptsOnlyList.ToList().Find((s) => s.Name == subScript.Script.Name);
					if (script != null)
						subScript.Script = script;
				}
			}
			if (new_scriptNodeBase is ScriptNodeSweep sweep)
			{
				sweep.Parent = CurrentScript.Parent as ProjectData;
			}

			ScriptNodeBase droppedOnNode;
			int indexToInsertNode = GetNewNodePosition(e, out droppedOnNode);
			IScriptItem replacedItem = null;
			if ((indexToInsertNode + 1) < ScriptNodeList.Count)
				replacedItem = ScriptNodeList[indexToInsertNode + 1];

			ScriptDiagram.CreateNode(
				new_scriptNodeBase, 
				100, 
				false);
			 

			if (indexToInsertNode == -1)
			{
				if (ScriptNodeList.Count > 0)
				{
					ScriptNodeBase lastNode = ScriptNodeList[ScriptNodeList.Count - 1]
						as ScriptNodeBase;
					if (lastNode.PassNext == null)
						lastNode.PassNext = new_scriptNodeBase;
				}

				ScriptNodeList.Add(new_scriptNodeBase);
			}
			else
			{
				
				ScriptNodeList.Insert(indexToInsertNode + 1, new_scriptNodeBase);

				ScriptNodeList[indexToInsertNode].PassNext = new_scriptNodeBase;

				int index = ScriptNodeList.IndexOf(new_scriptNodeBase);
				if (index < (ScriptNodeList.Count - 1))
					new_scriptNodeBase.PassNext = ScriptNodeList[indexToInsertNode + 2];

				ScriptDiagram.MoveNode(new_scriptNodeBase, replacedItem);
			}

			if (new_scriptNodeBase is ScriptNodeCANMessageUpdate ||
				new_scriptNodeBase is ScriptNodeStopContinuous)
			{
				ScriptReloadedEvent?.Invoke(this, EventArgs.Empty);
			}
		}


		private int GetNewNodePosition(
			DragEventArgs e,
			out ScriptNodeBase dropedOnNode)
		{
			dropedOnNode = null;
			if (e == null)
				return -1;

			ListViewItem listViewItem =
						FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);
			if (listViewItem != null)
			{
				dropedOnNode = listViewItem.DataContext as ScriptNodeBase;
			}

			int index = ScriptNodeList.IndexOf(dropedOnNode);
			//if (index == -1 || index == (ScriptNodeList.Count - 1))
			//	return -1;

			return index;
		}



		private bool MoveNode(ScriptNodeBase droppedOnNode)
		{		

			

			if(_selectedItems == null || _selectedItems.Count == 0)
				return false;

			List<ScriptNodeBase> list = new List<ScriptNodeBase>();
			foreach (var item in _selectedItems)
				list.Add(item as ScriptNodeBase);


			foreach (ScriptNodeBase item in list)
			{
				bool res = _moveNodeService.MoveNode(
					item,
					droppedOnNode,
					ScriptNodeList,
					ScriptDiagram);
			}

			return true;
		}


		private void ListScript_SelectionChanged(SelectionChangedEventArgs e)
		{
			if (!(e.OriginalSource is ListView listView))
				return;

			_selectedItems = listView.SelectedItems;

			_selectedNode = listView.SelectedItem as ScriptNodeBase;
			if (_selectedNode == null)
				LoggerService.Inforamtion(this, "Node selected: None");
			else
				LoggerService.Inforamtion(this, "Node selected: " + _selectedNode.Name);
		}

		public void GetScriptDiagram()
		{
			if (IsLoadingScript)
				return;

			if (CurrentScript == null)
				return;

			_isIgnoreChanges = true;

			ScriptDiagram.DrawScript(CurrentScript);

			SetNodesId();
			_isIgnoreChanges = false;
		}

		private void SetNodesId()
		{
			if (IsLoadingScript)
				return;

			if (CurrentScript == null)
				return;

			_isIgnoreChanges = true;

			int counter = 1;
			foreach(ScriptNodeBase node in CurrentScript.ScriptItemsList) 
			{
				node.ID = counter++;
			}

			_isIgnoreChanges = false;
		}

		private void Delete()
		{
			if (_selectedItems == null)
				return;

			List<IScriptItem> list = new List<IScriptItem>();
			foreach (IScriptItem item in _selectedItems)
				list.Add(item);

			foreach (IScriptItem item in list)
			{
				int index = ScriptNodeList.IndexOf(item);
				LoggerService.Inforamtion(this, "Node removed: " + item.Description);
				ScriptNodeList.Remove(item);

				ScriptDiagram.DeleteNode(item);

				if (index == 0)
					continue;

				if (ScriptNodeList.Count == 1)
					continue;

				if(index == ScriptNodeList.Count)
				{
					ScriptNodeList[index - 1].PassNext = null;
					continue;
				}

				ScriptNodeList[index - 1].PassNext = ScriptNodeList[index];

			}

			IsChanged = true;
		}

		private void DeleteNextPass(ScriptNodeBase scriptNodeBase)
		{
			if (scriptNodeBase != null)
				scriptNodeBase.PassNext = null;

			ScriptDiagram.DeletePassFialFromNode(scriptNodeBase, true);
			IsChanged = true;
		}

		private void DeleteNextFail(ScriptNodeBase scriptNodeBase)
		{
			if (scriptNodeBase != null)
				scriptNodeBase.FailNext = null;

			ScriptDiagram.DeletePassFialFromNode(scriptNodeBase, false);
			IsChanged = true;
		}

		private void CopyScriptNode()
		{
			Copy();
			Past();
		}


		private void NodePropertyChangedHandler(ScriptNodeBase sender, string propertyName)
		{
			
			if (sender is ScriptNodeSubScript subScript && propertyName == "Script" && subScript.Script != null)
				subScript.Script.ScriptItemsList.CollectionChanged += NodesListChangedHandler;

			if (propertyName == "IsExpanded" ||
				propertyName == "IsSelected" ||
				propertyName == "MotorTypesList")
			{
				return;
			}

			if(propertyName == "ScriptPath")
				GetScriptDiagram();

			//LoggerService.Inforamtion(this, "Changed property: " + propertyName);
			IsChanged = true;
		}

		private void ListScript_PreviewKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
			{
				Save(CurrentScript is TestData);
			}
		}

		public bool SaveIfNeeded()
		{
			if (IsChanged)
			{
				MessageBoxResult result = MessageBox.Show(
					"Changes you made to the script " + CurrentScript.Name + " are not saved.\r\nDo you wish to save?",
					"Closing " + CurrentScript.Name,
					MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Cancel)
				{
					return true;
				}
				else if (result == MessageBoxResult.Yes)
				{
					Save(CurrentScript is TestData);
				}
				else if (result == MessageBoxResult.No)
				{
					string path = CurrentScript.ScriptPath;
					CurrentScript = null;
					Open(path: path);

					ScriptReloadedEvent?.Invoke(this, EventArgs.Empty);
				}
			}

			return false;
		}

		private void PassNext_SelectionChanged(SelectionChangedEventArgs e)
		{
			if (e.AddedItems != null && e.AddedItems.Count > 0)
				ScriptDiagram.AddPassFialToNode(e.AddedItems[0] as IScriptItem);
		}

		private void FailNext_SelectionChanged(SelectionChangedEventArgs e)
		{
			if(e.AddedItems != null && e.AddedItems.Count > 0)
				ScriptDiagram.AddPassFialToNode(e.AddedItems[0] as IScriptItem);
		}

		private void ExportScriptToPDF()
		{
			if(CurrentScript == null)
				return;

			ScriptDiagram.ExportDiagram(CurrentScript.ScriptPath, CurrentScript.Name);
		}


		private void SubScriptPropertyChangedEventHandler(ScriptNodeBase sender, string propertyName)
		{
			if(propertyName == "Script")
			{

				OnPropertyChanged(nameof(ScriptNodeList));
				GetScriptDiagram();
			}
		}

		private void NodesListChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
		{
		}

		private void Copy()
		{
			List<(int,IScriptItem)> list = new List<(int, IScriptItem)>();
			foreach (IScriptItem item in _selectedItems)
			{
				int index = ScriptNodeList.IndexOf(item);
				list.Add((index,item));
			}

			list.Sort((a, b) => a.Item1.CompareTo(b.Item1));
			List<IScriptItem> list1 = list.Select((a) => a.Item2).ToList();

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			string copyString = JsonConvert.SerializeObject(list1, settings);


			string format = "MyNode";
			Clipboard.Clear();
			Clipboard.SetData(format, copyString);
		}

		private void Past()
		{
			if (Clipboard.ContainsData("MyNode") == false)
				return;
			
			string copyString = (string)Clipboard.GetData("MyNode");
			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			List<IScriptItem> list =
				JsonConvert.DeserializeObject(copyString, settings) as List<IScriptItem>;

			foreach (IScriptItem item in list)
			{
				if(item is IScriptStepWithParameter withParam &&
					withParam.Parameter != null)
				{
					if(withParam.Parameter.Device == null) 
					{
						if(_devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType))
							withParam.Parameter.Device = _devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType].Device;
					}
				}

				item.PassNext = null;
				AddNode_do(
					item as ScriptNodeBase,
					item.GetType().Name,
					null);

			//	ScriptNodeList[ScriptNodeList.Count - 1].PassNext = ScriptNodeList[ScriptNodeList.Count - 2];
			}

			ScriptDiagram.SetOffsetY(true);

			IsChanged = true;
		}


		private void SweepItemsList_PreviewDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(ParametersViewModel.DragDropFormat) == false)
				return;

			e.Effects = DragDropEffects.None;
			e.Handled = true;

			if (!(e.OriginalSource is TextBlock textBlock))
				return;

			if (!(textBlock.DataContext is SweepItemData sweepItem))
				return;

			DeviceParameterData param = e.Data.GetData(ParametersViewModel.DragDropFormat) as DeviceParameterData;
			sweepItem.Parameter = param;

		}

		private void TextBox_PreviewDragOver(DragEventArgs e)
		{
			e.Handled = true;
		}


		private List<ScriptNodeBase> GetPassFailNextParents(
			ScriptNodeBase passFailNextItem,
			bool isPass)
		{
			List<ScriptNodeBase> parentsList = new List<ScriptNodeBase>();
			foreach (ScriptNodeBase item in ScriptNodeList)
			{
				if(isPass && item.PassNext == passFailNextItem)
					parentsList.Add(item);
				else if (!isPass && item.FailNext == passFailNextItem)
					parentsList.Add(item);
			}

			return parentsList;
		}

		#endregion Methods

		#region Commands
		public RelayCommand LoadedCommand { get; private set; }

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

		public RelayCommand MoveNodeUpCommand { get; private set; }
		public RelayCommand MoveNodeDownCommand { get; private set; }
		public RelayCommand DeleteCommand { get; private set; }
		public RelayCommand CopyScriptNodeCommand { get; private set; }
		public RelayCommand ExportScriptToPDFCommand { get; private set; }

		public RelayCommand ScriptExpandAllCommand { get; private set; }
		public RelayCommand ScriptCollapseAllCommand { get; private set; }

		public RelayCommand<ScriptNodeBase> DeleteNextPassCommand { get; private set; }
		public RelayCommand<ScriptNodeBase> DeleteNextFailCommand { get; private set; }



		public RelayCommand CopyCommand { get; private set; }
		public RelayCommand PastCommand { get; private set; }
		public RelayCommand SaveCommand { get; private set; }




		private RelayCommand<SelectionChangedEventArgs> _ListScript_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> ListScript_SelectionChangedCommand
		{
			get
			{
				return _ListScript_SelectionChangedCommand ?? (_ListScript_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(ListScript_SelectionChanged));
			}
		}

		private RelayCommand<KeyEventArgs> _ListScript_PreviewKeyUpCommand;
		public RelayCommand<KeyEventArgs> ListScript_PreviewKeyUpCommand
		{
			get
			{
				return _ListScript_PreviewKeyUpCommand ?? (_ListScript_PreviewKeyUpCommand =
					new RelayCommand<KeyEventArgs>(ListScript_PreviewKeyUp));
			}
		}

		private RelayCommand<SelectionChangedEventArgs> _PassNext_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> PassNext_SelectionChangedCommand
		{
			get
			{
				return _PassNext_SelectionChangedCommand ?? (_PassNext_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(PassNext_SelectionChanged));
			}
		}

		private RelayCommand<SelectionChangedEventArgs> _FailNext_SelectionChangedCommand;
		public RelayCommand<SelectionChangedEventArgs> FailNext_SelectionChangedCommand
		{
			get
			{
				return _FailNext_SelectionChangedCommand ?? (_FailNext_SelectionChangedCommand =
					new RelayCommand<SelectionChangedEventArgs>(FailNext_SelectionChanged));
			}
		}




		private RelayCommand<DragEventArgs> _SweepItemsList_PreviewDropCommand;
		public RelayCommand<DragEventArgs> SweepItemsList_PreviewDropCommand
		{
			get
			{
				return _SweepItemsList_PreviewDropCommand ?? (_SweepItemsList_PreviewDropCommand =
					new RelayCommand<DragEventArgs>(SweepItemsList_PreviewDrop));
			}
		}



		private RelayCommand<DragEventArgs> _TextBox_PreviewDragOverCommand;
		public RelayCommand<DragEventArgs> TextBox_PreviewDragOverCommand
		{
			get
			{
				return _TextBox_PreviewDragOverCommand ?? (_TextBox_PreviewDragOverCommand =
					new RelayCommand<DragEventArgs>(TextBox_PreviewDragOver));
			}
		}

		#endregion Commands

		#region Events

		//public event EventHandler<bool> ScriptIsChangedEvent;
		public event EventHandler ScriptIsSavedEvent;
		public event EventHandler ScriptReloadedEvent;

		#endregion Events
	}
}
