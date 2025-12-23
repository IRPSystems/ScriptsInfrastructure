
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.Interfaces;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using Services.Services;
using Syncfusion.UI.Xaml.Diagram;
using Syncfusion.UI.Xaml.Diagram.Stencil;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Win32;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using Entities.Enums;
using ScriptHandler.Services;
using System.Collections;

namespace ScriptHandler.DesignDiagram.ViewModels
{
	public class DesignDiagramViewModel: ObservableObject, IDocumentVM
	{
		#region Properties

		public ScriptData DesignDiagram { get; set; }

		public NodeCollection Nodes { get; set; }
		public SnapSettings SnapSettings { get; set; }
		public ConnectorCollection Connectors { get; set; }

		public Syncfusion.UI.Xaml.Diagram.CommandManager CommandManager { get; set; }

		public PageSettings PageSettings { get; set; }

		public double OffsetX { get; set; }
		public double OffsetY { get; set; }

		public SelectorViewModel SelectedItems { get; set; }

		public string Name
		{
			get
			{
				if(DesignDiagram == null)
					return null;
				return DesignDiagram.Name;
			}
			set { }
		}

		public bool IsChanged { get; set; }

		#endregion Properties

		#region Fields

		private NodePropertiesViewModel _nodeProperties;

		private bool _isInPropertyChanged;

		public const double ToolHeight = 35;
		public const double ToolWidth = 300;
		public const double BetweenTools = 45;
		private const double _toolOffsetX = 50;

		private bool _isMouseDown;
		private Point _startPoint;

		private int _idCounter;

		private DevicesContainer _devicesContainer;

		private ScriptUserData _scriptUserData;

		private ScriptValidationService _scriptValidation;

		private ExportDiagramService _exportDiagram;

		private bool _isSubScript;

		#endregion Fields

		#region Constructor

		public DesignDiagramViewModel(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer,
			NodePropertiesViewModel nodeProperties, 
			double offsetX = _toolOffsetX,
			bool isSubScript = false)
		{
			_devicesContainer = devicesContainer;
			_scriptUserData = scriptUserData;
			_isSubScript = isSubScript;

			_nodeProperties = nodeProperties;
			OffsetX = offsetX;
			_idCounter = 1;

			_isInPropertyChanged = false;

			Nodes = new NodeCollection();
			PageSettings = new PageSettings();
			Connectors = new ConnectorCollection();

			_scriptValidation = new ScriptValidationService();
			_exportDiagram = new ExportDiagramService();

			ItemAddedCommand = new RelayCommand<object>(ItemAdded);
			ItemDeletedCommand = new RelayCommand<object>(ItemDeleted);
			ItemSelectedCommand = new RelayCommand<object>(ItemSelected);
			ConnectorSourceChangedCommand = new RelayCommand<object>(ConnectorSourceChanged);
			ConnectorTargetChangedCommand = new RelayCommand<object>(ConnectorTargetChanged);
			MoveNodeUpCommand = new RelayCommand(MoveNodeUp);
			MoveNodeDownCommand = new RelayCommand(MoveNodeDown);
			ExportScriptToPDFCommand = new RelayCommand<SfDiagram>(ExportScriptToPDF);

			CopyCommand = new RelayCommand(Copy);
			PastCommand = new RelayCommand(Past);
			SaveDiagramCommand = new RelayCommand(Save);

			SetSnapAndGrid();
			InitCommandManager();

			ChangeDarkLight();

			OffsetY = 50;

			SelectedItems = new SelectorViewModel();
		}

		#endregion Constructor

		#region Methods

		public bool Dispose()
		{
			return SaveIfNeeded();
		}

		public bool SaveIfNeeded()
		{
			if (IsChanged)
			{
				MessageBoxResult result = MessageBox.Show(
					"Changes you made to the script " + Name + " are not saved.\r\nDo you wish to save?",
					"Closing " + Name,
					MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Cancel)
				{
					return true;
				}
				else if (result == MessageBoxResult.Yes)
				{
					Save();
				}
			}

			return false;
		}

		private void AddHeaderNode()
		{
			if (_isSubScript)
			{
				OffsetY = 0;
				return;
			}

			NodeViewModel node = new NodeViewModel();
			node.Content = DesignDiagram;
			node.ContentTemplate = 
				Application.Current.FindResource("ScriptLogDiagramTemplate_Script") as DataTemplate;
			node.UnitHeight = 50;
			node.UnitWidth = ToolWidth;

			node.OffsetX = 50;
			node.OffsetY = OffsetY;

			OffsetY += 35;

			node.Pivot = new Point(0, 0);

			Nodes.Add(node);
		}

		private void InitCommandManager()
		{
			CommandManager = new Syncfusion.UI.Xaml.Diagram.CommandManager();
			CommandManager.Commands = new ObservableCollection<IGestureCommand>();

			GestureCommand copyGesture = new GestureCommand()
			{
				// Define the command with custom command
				Command = CopyCommand,
				// Define gesture for custom Command
				Gesture = new Gesture
				{
					KeyModifiers = ModifierKeys.Control,
					KeyState = KeyStates.Down,
					Key = Key.C
				},
				// Parameter for command - file name for save command
				//Parameter = "diagram"
			};

			CommandManager.Commands.Add(copyGesture);
		}

		private void SetSnapAndGrid()
		{
			SnapSettings = new SnapSettings()
			{
				SnapConstraints = SnapConstraints.ShowLines,
				SnapToObject = SnapToObject.All,
			};
		}

		public void Save()
		{
			Save(DesignDiagram is TestData);
		}

		public void Save(bool isTest)
		{
			if (DesignDiagram == null)
				return;

			try
			{
				bool isValid = _scriptValidation.Validate(DesignDiagram);
				if (!isValid)
					return;


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
					string fileName = Path.GetFileName(DesignDiagram.ScriptPath);
					string extension = Path.GetExtension(DesignDiagram.ScriptPath);
					fileName = fileName.Replace(extension, string.Empty);
					string str =
						"The name of the \"" + fileName + "\" file name is different from the \"" + DesignDiagram.Name + "\" script name.\r\n" +
						"File name: " + fileName + "\r\n" +
						"saveType name: " + DesignDiagram.Name + "\r\n" +
						"Do you wish to change the file name?";
					MessageBoxResult result = MessageBox.Show(
						str,
						"Warning",
						MessageBoxButton.YesNoCancel);
					if (result == MessageBoxResult.Cancel)
						return;
					if (result == MessageBoxResult.Yes)
					{
						DesignDiagram.ScriptPath = DesignDiagram.ScriptPath.Replace(fileName, DesignDiagram.Name);
					}
				}


				if (string.IsNullOrEmpty(DesignDiagram.ScriptPath))
				{
					SaveFileDialog saveFileDialog = new SaveFileDialog();
					saveFileDialog.FileName = DesignDiagram.Name;
					saveFileDialog.Filter = saveType + " Files | *." + extention;
					bool? result = saveFileDialog.ShowDialog();
					if (result != true)
						return;

					DesignDiagram.ScriptPath = saveFileDialog.FileName;
				}

				// Save Json
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				var sz = JsonConvert.SerializeObject(DesignDiagram, settings);
				File.WriteAllText(DesignDiagram.ScriptPath, sz);

				LoggerService.Inforamtion(this, "Script saved");

				ScriptIsSavedEvent?.Invoke(this, null);
				
				IsChanged = false;
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to save a script", "Script Error", ex);
			}
		}

		private bool IsNameDifferentFromFile()
		{
			if (string.IsNullOrEmpty(DesignDiagram.ScriptPath) ||
				DesignDiagram == null || string.IsNullOrEmpty(DesignDiagram.Name))
			{
				return false;
			}

			string fileName = Path.GetFileName(DesignDiagram.ScriptPath);
			string extension = Path.GetExtension(DesignDiagram.ScriptPath);
			fileName = fileName.Replace(extension, string.Empty);

			if (fileName.ToLower() != DesignDiagram.Name.ToLower())
				return true;

			return false;
		}

		public void New(bool isTest, string name)
		{
			Name = name;

			
			if (isTest)
				DesignDiagram = new TestData();
			else
				DesignDiagram = new ScriptData();
			DesignDiagram.Name = name;

			DesignDiagram.ScriptPath = null;
			_idCounter = 1;


			AddHeaderNode();

			LoggerService.Inforamtion(this, "New script created: " + DesignDiagram.Name);
			

			IsChanged = true;
		}

		private void ConnectorSourceChanged(object item)
		{
			if (!(item is ChangeEventArgs<object, ConnectorChangedEventArgs> connectorChanged))
				return;

			if (!(connectorChanged.Item is ConnectorViewModel connector))
				return;

			if (connector.ID is string str && str.Contains("PassNext_"))
			{
				return;
			}

			if (!(connector.SourceNode is NodeViewModel sourceNode))
				return;

			if (!(connector.SourcePort is NodePortViewModel port))
				return;

			connector.ID = $"FailNext_{sourceNode.ID}";

			if (port.NodeOffsetX == 1)
			{
				connector.ConnectorGeometryStyle =
					Application.Current.FindResource("FailConnectorLineStyle") as Style;
				connector.TargetDecoratorStyle = Application.Current.FindResource("FailDecoratorFillStyle") as Style;
			}
			else
			{
				connector.ConnectorGeometryStyle =
					Application.Current.FindResource("PassConnectorLineStyle") as Style;
				connector.TargetDecoratorStyle = Application.Current.FindResource("PassDecoratorFillStyle") as Style;
			}

			connector.TargetDecorator =
				Application.Current.FindResource("ClosedSharp");

			IsChanged = true;
		}

		private void ConnectorTargetChanged(object item)
		{
			if (!(item is ChangeEventArgs<object, ConnectorChangedEventArgs> connectorChanged))
				return;

			if (!(connectorChanged.Item is ConnectorViewModel connector))
				return;

			if (connector.ID is string str && !str.Contains("FailNext_"))
				return;

			if (!(connector.SourceNode is NodeViewModel sourceNode))
				return;

			if (connector.SourceNode == connector.TargetNode)
				return;

			if (!(connector.TargetNode is NodeViewModel targetNode))
				return;

			if (!(connector.SourcePort is NodePortViewModel port))
				return;

			if (!(sourceNode.Content is ScriptNodeBase source))
				return;

			if (!(targetNode.Content is ScriptNodeBase target))
				return;

			if (port.NodeOffsetX == 1)
				source.FailNext = target;
			else
				source.PassNext = target;
		}

		#region Load from file

		public void Open(string path = null)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			try
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



					path = openFileDialog.FileName;
				}
				else if (File.Exists(path) == false)
				{
					LoggerService.Error(this, $"Failed to find the script \"{path}\"", "Error");

					string name = Path.GetFileName(path);
					string extension = Path.GetExtension(path);
					Name = name.Replace(extension, string.Empty);

					Mouse.OverrideCursor = null;

					return;
				}


				string jsonString = File.ReadAllText(path);

				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				DesignDiagram = JsonConvert.DeserializeObject(jsonString, settings) as ScriptData;

				DesignDiagram.ScriptPath = path;
				Name = DesignDiagram.Name;


				foreach (ScriptNodeBase scriptNode in DesignDiagram.ScriptItemsList)
				{
					if (scriptNode is IScriptStepWithParameter withParam &&
						withParam.Parameter != null)
					{
						string name = withParam.Parameter.Name;
						if (withParam.Parameter is MCU_ParamData mcuParam)
							name = mcuParam.Cmd;

						DeviceParameterData data = GetParameter(
							withParam.Parameter.DeviceType,
							name);
						if (data != null)
							withParam.Parameter = data;
					}

					if (scriptNode != null)
					{
						scriptNode.PostLoad(
							_devicesContainer,
							DesignDiagram);
					}
				}


				_idCounter = 0;

				foreach (ScriptNodeBase node in DesignDiagram.ScriptItemsList)
				{
					if (_idCounter < node.ID)
						_idCounter = node.ID;

					node.IsExpanded = true;



					if (node.PassNextId >= 0)
					{
						foreach (ScriptNodeBase passNextNode in DesignDiagram.ScriptItemsList)
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
						foreach (ScriptNodeBase failNextNode in DesignDiagram.ScriptItemsList)
						{
							if (failNextNode.ID == node.FailNextId)
							{
								node.FailNext = failNextNode;
								break;
							}
						}
					}


				}

				_idCounter++;

			}
			catch (Exception ex)
			{
				LoggerService.Error(this, $"Failed to load the script \"{Name}\"", "Error", ex);
			}

			IsChanged = false;

			Mouse.OverrideCursor = null;
		}

		public void DrawNodes()
		{
			AddHeaderNode();
			SetPassFailNextTool();
			InitNods();

			IsChanged = false;
		}

		private DeviceParameterData GetParameter(
			DeviceTypesEnum deviceType,
			string paramName)
		{
			if (_devicesContainer.TypeToDevicesFullData.ContainsKey(deviceType) == false)
				return null;

			DeviceData deviceData =
				_devicesContainer.TypeToDevicesFullData[deviceType].Device;

			DeviceParameterData data = null;
			if (deviceType == DeviceTypesEnum.MCU)
				data = deviceData.ParemetersList.ToList().Find((p) => ((MCU_ParamData)p).Cmd == paramName);
			else
				data = deviceData.ParemetersList.ToList().Find((p) => p.Name == paramName);

			return data;
		}

		private void InitNods()
		{
			foreach (ScriptNodeBase tool in DesignDiagram.ScriptItemsList)
			{
				string toolName = tool.Name;
				InitNodeBySymbol(null, toolName, tool);

				System.Threading.Thread.Sleep(1);
				//await Task.Delay(1);
			}

			InitNextArrows();
		}

		private void SetPassFailNextTool()
		{
			foreach (ScriptNodeBase tool in DesignDiagram.ScriptItemsList)
			{
				if (tool.PassNextId < 1)
					continue;

				foreach (ScriptNodeBase tool1 in DesignDiagram.ScriptItemsList)
				{
					if (tool.PassNextId == tool1.ID)
						tool.PassNext = tool1;
				}
			}

			foreach (ScriptNodeBase tool in DesignDiagram.ScriptItemsList)
			{
				if (tool.FailNextId < 1)
					continue;

				foreach (ScriptNodeBase tool1 in DesignDiagram.ScriptItemsList)
				{
					if (tool.FailNextId == tool1.ID)
						tool.FailNext = tool1;
				}
			}
		}

		#endregion Load from file

		#region Add item

		private void ItemAdded(object item)
		{
			if (!(item is ItemAddedEventArgs itemAdded))
				return;

			if (itemAdded.Item is NodeViewModel node)
			{
				NodeAdded(itemAdded, node);
			}

		}

		private void NodeAdded(
			ItemAddedEventArgs itemAdded,
			NodeViewModel node)
		{

			if (itemAdded.OriginalSource is SymbolViewModel symbol)
			{
				InitNodeBySymbol(node, symbol.Symbol as string);
			}
			else
			{
				if (itemAdded != null && itemAdded.Info != null)
				{
					InitNodeBySymbol(
						node,
						(itemAdded.Info as PasteCommandInfo).SourceId as string);
				}
			}
		}

		private void SetScriptDataToNode(
			ScriptNodeBase node)
		{
			if (node == null)
				return;

			if (node is ScriptNodeSubScript subScript)
			{
				subScript.Parent = DesignDiagram.Parent as ProjectData;
				subScript.ParentScriptName = DesignDiagram.Name;

				if (subScript.Script != null && subScript.Parent != null)
				{
					IScript script =
						subScript.Parent.ScriptsOnlyList.ToList().Find((s) => s.Name == subScript.Script.Name);
					if (script != null)
						subScript.Script = script;
				}
			}
			if (node is ScriptNodeSweep sweep)
			{
				sweep.Parent = DesignDiagram.Parent as ProjectData;
			}
			if (node is ScriptNodeScopeSave scopeSave)
			{
				scopeSave.PostLoad(_devicesContainer, DesignDiagram);
			}

			if (node is ScriptNodeCANMessageUpdate ||
				node is ScriptNodeStopContinuous)
			{
				ScriptReloadedEvent?.Invoke(this, EventArgs.Empty);
			}
		}

		private void InitNodeBySymbol(
			NodeViewModel node,
			string toolName,
			ScriptNodeBase tool = null,
			double xOffset = _toolOffsetX,
			int insertIndex = -1)
		{
			if(node == null)
			{
				node = new NodeViewModel();
				node.Content = tool;
				if(insertIndex < 0)
					Nodes.Add(node);
				else 
					Nodes.Insert(insertIndex, node);

				_idCounter++;
			}

			node.ID = toolName;
			node.Pivot = new Point(0, 0);

			if (tool == null)
			{
				SetContent(node, toolName);
			}

			

			SetNodeTemplateAndSize(node, toolName, xOffset);

			if (node.Content is ScriptNodeSubScript subScript)
				subScript.ScriptChangedEvent += SubScript_ScriptChangedEvent;
			
			SetPorts(node);
			

			node.PropertyChanged += Node_PropertyChanged;

			if (tool == null)
			{
				SetNextPassConnector(node);
				DesignDiagram.ScriptItemsList.Add(node.Content as ScriptNodeBase);
			}

			if((node.Content as ScriptNodeBase) != null)
			{
				(node.Content as ScriptNodeBase).PropertyChanged += Tool_PropertyChanged;
			}

			

			SetScriptDataToNode(node.Content as ScriptNodeBase);

			IsChanged = true;
		}

		private void SetNextPassConnector(
			NodeViewModel node)
		{
			if (Nodes.Count == 2)
				return;

			NodeViewModel prevLastNode =
				Nodes[Nodes.Count - 2];
			ScriptNodeBase prevLastTool =
				prevLastNode.Content as ScriptNodeBase;
			if (prevLastTool == null)
				return;

			ScriptNodeBase tool = node.Content as ScriptNodeBase;
			prevLastTool.PassNext = tool;

			AddConnector_Pass(prevLastNode, node);
		}

		private void AddConnector_Pass(
			NodeViewModel sourceNode,
			NodeViewModel targetNode)
		{ 
			ConnectorViewModel simpleConnector = new ConnectorViewModel()
			{
				ID = $"PassNext_{sourceNode.ID}",
				SourceNode = sourceNode,
				SourcePort = (sourceNode.Ports as PortCollection)[1],

				TargetNode = targetNode,
				TargetPort = (targetNode.Ports as PortCollection)[0],

				ConnectorGeometryStyle =
					Application.Current.FindResource("PassConnectorLineStyle") as Style,
				TargetDecorator =
					Application.Current.FindResource("ClosedSharp"),
				TargetDecoratorStyle = Application.Current.FindResource("PassDecoratorFillStyle") as Style
			};

			Connectors.Add(simpleConnector);

		}

		private void AddConnector_Fail(
			NodeViewModel sourceNode,
			NodeViewModel targetNode)
		{
			ConnectorViewModel simpleConnector = new ConnectorViewModel()
			{
				ID = $"FailNext_{sourceNode.ID}",
				SourceNode = sourceNode,
				SourcePort = (sourceNode.Ports as PortCollection)[3],

				TargetNode = targetNode,
				TargetPort = (targetNode.Ports as PortCollection)[2],

				ConnectorGeometryStyle =
					Application.Current.FindResource("FailConnectorLineStyle") as Style,
				TargetDecorator =
					Application.Current.FindResource("ClosedSharp"),
				TargetDecoratorStyle = Application.Current.FindResource("FailDecoratorFillStyle") as Style
			};

			Connectors.Add(simpleConnector);

		}

		private void SetPorts(NodeViewModel node)
		{
			double y1 = 0.25;
			double y2 = 0.75;

			if (node.Content is ScriptNodeSubScript subScript &&
				subScript.Script != null)
			{
				double height = subScript.GetHeight();
				double scriptNode = 35 / height;
				y1 = scriptNode * 0.25;
				y2 = scriptNode * 0.75;
			}

			NodePortViewModel port = new NodePortViewModel()
			{
				NodeOffsetX = 0,
				NodeOffsetY = y1,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 0,
				NodeOffsetY = y2,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 1,
				NodeOffsetY = y1,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 1,
				NodeOffsetY = y2,
			};
			(node.Ports as PortCollection).Add(port);
		}

		private void SetContent(
			NodeViewModel node,
			string toolName)
		{
			var assemblyList = AppDomain.CurrentDomain.GetAssemblies();
			Assembly assembly = assemblyList.
				SingleOrDefault(assembly => assembly.GetName().Name == "ScriptHandler");

			List<Type> typesList = assembly.GetTypes().ToList();
			string name = "ScriptHandler.Models.ScriptNodes";
			typesList = typesList.Where((t) => t.Namespace == name).ToList();

			foreach (Type type in typesList)
			{
				if (!StencilViewModel.IsNodeBase(type))
					continue;

				if (type.Name == "ScriptNodeScopeSave" ||
					type.Name == "ScriptNodeStopContinuous")
					continue;

				var c = Activator.CreateInstance(type);
				if((c as ScriptNodeBase).Name == toolName)
				{
					node.Content = c;
					break;
				}
			}

			if (node.Content is ScriptNodeBase)
				(node.Content as ScriptNodeBase).ID = _idCounter++;
		}

		private void SetNodeTemplateAndSize(
			NodeViewModel node,
			string toolName,
			double xOffset)
		{

			if (node.Content is ISubScript)
			{
				node.ContentTemplate =
					Application.Current.FindResource("ScriptLogDiagramTemplate_Step_SubScript") as DataTemplate;
			}
			else
			{
				node.ContentTemplate =
					Application.Current.FindResource("ScriptLogDiagramTemplate_Step") as DataTemplate;
			}

			if (node.Content is ScriptNodeSubScript)
			{
				node.UnitHeight += (node.Content as ScriptNodeSubScript).GetHeight();
				node.UnitWidth = (node.Content as ScriptNodeSubScript).GetWidth();
			}
			else
			{
				node.UnitHeight = ToolHeight;
				node.UnitWidth = ToolWidth;
			}

			node.OffsetX = xOffset;
			node.OffsetY = OffsetY;

			if (node.Content is ScriptNodeBase)
			{
				if (node.Content is ScriptNodeSubScript)
					(node.Content as ScriptNodeBase).Height += (node.Content as ScriptNodeSubScript).GetHeight();
				else
					(node.Content as ScriptNodeBase).Height = ToolHeight;

				(node.Content as ScriptNodeBase).Width = ToolWidth;
				(node.Content as ScriptNodeBase).OffsetX = xOffset;
				(node.Content as ScriptNodeBase).OffsetY = OffsetY;
			}

			if (node.Content is ScriptNodeSubScript)
				OffsetY += (node.Content as ScriptNodeSubScript).GetHeight() + 20;
			else
				OffsetY += BetweenTools;
		}

		#endregion Add item

		private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if(!(sender is NodeViewModel node))
				return;

			if (!(node.Content is ScriptNodeBase tool))
				return;

			if(e.PropertyName == "OffsetX" ||
				e.PropertyName == "OffsetY" ||
				e.PropertyName == "UnitWidth" ||
				e.PropertyName == "UnitHeight")
			{
				if (_isInPropertyChanged || _isScriptChangedEvent)
					return;

				_isInPropertyChanged = true;
				
				switch(e.PropertyName)
				{
					case "OffsetX": node.OffsetX = (node.Content as ScriptNodeBase).OffsetX; break;
					case "OffsetY": node.OffsetY = (node.Content as ScriptNodeBase).OffsetY; break;
					case "UnitWidth": node.UnitWidth = (node.Content as ScriptNodeBase).Width; break;
					case "UnitHeight": node.UnitHeight = (node.Content as ScriptNodeBase).Height; break;
				}

				_isInPropertyChanged = false;

				IsChanged = true;
			}
			
		}

		private void Tool_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			IsChanged = true;
		}

		private void ItemSelected(object e)
		{
			if (!(e is ItemSelectedEventArgs itemSelectedData))
				return;

			if (!(itemSelectedData.Item is NodeViewModel node))
				return;

			if (!(node.Content is ScriptNodeBase toolBase))
				return;

			SetPropertyGridSelectedNode(toolBase);
		}

		private void SetPropertyGridSelectedNode(ScriptNodeBase toolBase)
		{
			_nodeProperties.Node = toolBase;
		}

		private void ItemDeleted(object item)
		{
			if (!(item is ItemDeletedEventArgs itemDeleted))
				return;

			if (itemDeleted.Item is NodeViewModel node)
			{
				DeleteNode(node);
			}
			else if (itemDeleted.Item is ConnectorViewModel connector)
			{
				DeleteConnector(connector);
			}

			IsChanged = true;
		}

		private void DeleteNode(NodeViewModel node)
		{
			Mouse.OverrideCursor = Cursors.Wait;

			#region Remove relevant connectors
			List<ConnectorViewModel> connectorsToRemove = new List<ConnectorViewModel>();
			foreach (ConnectorViewModel connecter in Connectors)
			{
				if (connecter.SourceNode == node ||
					connecter.TargetNode == node)
				{
					connectorsToRemove.Add(connecter);
				}
			}

			foreach (ConnectorViewModel connecter in connectorsToRemove)
			{
				Connectors.Remove(connecter);
			}
			#endregion Remove relevant connectors

			#region Add new connector

			ScriptNodeBase deletedItem = (node.Content as ScriptNodeBase);

			int nodeIndex = DesignDiagram.ScriptItemsList.IndexOf(deletedItem);
			if (nodeIndex > 0 && nodeIndex < (DesignDiagram.ScriptItemsList.Count - 1))
			{
				ScriptNodeBase itemBefore = DesignDiagram.ScriptItemsList[nodeIndex - 1] as ScriptNodeBase;
				ScriptNodeBase itemAfter = DesignDiagram.ScriptItemsList[nodeIndex + 1] as ScriptNodeBase;

				itemBefore.PassNext = itemAfter;

				NodeViewModel nodeBefore = Nodes.ToList().Find((sn) => sn.Content == itemBefore);

				if(nodeBefore != null) 
				{
					InitNextArrow_Pass(itemBefore, nodeBefore);
				}
			}

			#endregion Add new connector

			foreach (var item in DesignDiagram.ScriptItemsList)
			{
				if (item.PassNext == node.Content as ScriptNodeBase)
					item.PassNext = null;

				if (item.FailNext == node.Content as ScriptNodeBase)
					item.FailNext = null;
			}
			

			DesignDiagram.ScriptItemsList.Remove(node.Content as ScriptNodeBase);

			ReAragneNodes();

			Mouse.OverrideCursor = null;
		}

		private void DeleteConnector(
			ConnectorViewModel connector)
		{
			NodeViewModel node = connector.SourceNode as NodeViewModel;
			if (node == null) 
				return;

			if (!(connector.ID is string strID))
				return;

			if(strID.StartsWith("PassNext"))
			{
				(node.Content as ScriptNodeBase).PassNext = null;
			}
			else if (strID.StartsWith("FailNext") &&
				!strID.EndsWith(" - Delete Node"))
			{
				(node.Content as ScriptNodeBase).FailNext = null;
			}

			connector.ID = strID.Replace(" - Delete Node", string.Empty);
		}

		private void ReAragneNodes()
		{

			OffsetY = 50 + ToolHeight;

			int idCounter = 1;
			foreach (NodeViewModel nodeItem in Nodes)
			{
				if (!(nodeItem.Content is ScriptNodeBase scriptNodeBase))
					continue;

				scriptNodeBase.ID = idCounter++;

				
				scriptNodeBase.OffsetY = OffsetY;
				nodeItem.OffsetY = OffsetY;

				if(scriptNodeBase is ScriptNodeSubScript subScript)
					OffsetY += subScript.GetHeight() + 20;
				else
					OffsetY += BetweenTools;
			}
		}

		public void ChangeDarkLight()
		{
			PageSettings.PageBackground =
				App.Current.Resources["MahApps.Brushes.ThemeBackground"] as SolidColorBrush;
		}


		#region Drag

		private void Diagram_MouseEnter(MouseEventArgs e)
		{
			if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
				_isMouseDown = true;
			else
				_isMouseDown = false;
		}

		private void Diagram_PreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			_isMouseDown = true;
			_startPoint = e.GetPosition(null);
		}

		private void Diagram_PreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_isMouseDown = false;
		}

		private void Diagram_MouseMove(MouseEventArgs e)
		{
			if (_isMouseDown == false)
				return;

			DragObject(e);
		}

		private void DragObject(MouseEventArgs e)
		{
			try
			{
				Point mousePos = e.GetPosition(null);
				Vector diff = _startPoint - mousePos;

				if (e.LeftButton == MouseButtonState.Pressed &&
					//Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
					Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
				{
					if ((SelectedItems.Nodes as ObservableCollection<object>).Count == 0)
						return;

					Node node = FindAncestorService.FindAncestor<Node>((DependencyObject)e.OriginalSource);
					if (node == null)
						return;

					ObservableCollection<object> objectsToMove = new ObservableCollection<object>();
					foreach (var item in (SelectedItems.Nodes as ObservableCollection<object>))
					{
						//if (item is NodeViewModel nodeViewModel)
						//{
						//	if (nodeViewModel.OffsetX > _toolOffsetX) // Don't allow draging nodes of sub scripts
						//		continue;
						//}

						objectsToMove.Add(item);
					}

					SelectedItems = new SelectorViewModel();

					DataObject dragData = new DataObject("SfDiagram", objectsToMove);
					DragDrop.DoDragDrop(
						node,
						dragData,
						DragDropEffects.Move);


				}
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to handle dropped item", "Error", ex);
			}
		}

		

		#endregion Drag


		#region Drop

		private void Diagram_Drop(DragEventArgs e)
		{
			Node nodeDropedOn =
					FindAncestorService.FindAncestor<Node>((DependencyObject)e.OriginalSource);

			NodeViewModel nodeVMDropedOn = null;
			if (nodeDropedOn != null)
				nodeVMDropedOn = nodeDropedOn.DataContext as NodeViewModel;


			var dragData = e.Data.GetData("SfDiagram");
			if (dragData == null)
				return;

			if (!(dragData is ObservableCollection<object> dragNodeList))
				return;

			DropListOfObject(dragNodeList, nodeVMDropedOn);
		}

		private List<NodeViewModel> GetListOfNodesToDrop(
			ObservableCollection<object> dragNodeList)
		{
			List<NodeViewModel> tempList = new List<NodeViewModel>();
			foreach (object item in dragNodeList)
			{
				if (item is NodeViewModel nvm)
				{
					tempList.Add(nvm);
				}
			}

			return tempList;
		}

		private void DropListOfObject(
			ObservableCollection<object> dragNodeList,
			NodeViewModel nodeVMDropedOn)
		{
			int dropedOnIndex = Nodes.IndexOf(nodeVMDropedOn);

			int firstDroppedIndex = Nodes.IndexOf(dragNodeList[0] as NodeViewModel);
			bool isUp = firstDroppedIndex > dropedOnIndex;

			List<NodeViewModel> tempList = GetListOfNodesToDrop(
				dragNodeList);

			tempList.Sort((a, b) => Nodes.IndexOf(a).CompareTo(Nodes.IndexOf(b)));

			if (dropedOnIndex < 0)
				dropedOnIndex = Nodes.Count - 1;

			foreach (NodeViewModel node in tempList)
			{
				int dropedIndex = Nodes.IndexOf(node);

				DropSingleObject(
					node,
					dropedOnIndex);

				if (node.Content is ScriptNodeSubScript subScript && subScript.Script != null)
					subScript.InitNextArrows();

				if (dropedIndex > dropedOnIndex)
					dropedOnIndex++;
			}

			ReAragneNodes();
			SetAllPassNext();
			InitNextArrows();
		}

		private void DropSingleObject(
			NodeViewModel dropedNode,
			int dropedOnIndex)
		{
			NodeViewModel temp = dropedNode;

			foreach (var connector in Connectors)
			{
				if(connector.SourceNode == temp ||
					connector.TargetNode == temp)
				{
					if (connector.ID is string strID)
						connector.ID = strID + " - Delete Node";
				}
			}


			int dragedIndex = Nodes.IndexOf(dropedNode);
			Nodes.RemoveAt(dragedIndex);
			

			if (dropedOnIndex < 0)
			{
				Nodes.Add(temp);
				DesignDiagram.ScriptItemsList.Add(temp.Content as ScriptNodeBase);
			}
			else
			{
				Nodes.Insert(dropedOnIndex, dropedNode);
				DesignDiagram.ScriptItemsList.Insert(
					dropedOnIndex - 1, 
					temp.Content as ScriptNodeBase);
			}
		}

		#endregion Drop		

		private void SetAllPassNext()
		{
			Connectors.Clear();
			for(int i = 0; i < Nodes.Count - 1; i++)
			{
				if (!(Nodes[i].Content is ScriptNodeBase firstNode))
					continue;

				if (!(Nodes[i + 1].Content is ScriptNodeBase secondNode))
					continue;

				firstNode.PassNext = secondNode;
			}
		}

		public void InitNextArrows()
		{
			List<ConnectorViewModel> list = new List<ConnectorViewModel>();
			foreach(var connector in Connectors)
			{
				if(connector.ID is string str &&
					(str.StartsWith("FailNext_") || str.StartsWith("PassNext_")))
				{
					list.Add(connector);
				}
			}

			foreach(var connector in list)
				Connectors.Remove(connector);

			foreach (var node in Nodes)
			{
				if (!(node.Content is ScriptNodeBase scriptNodeBase))
					continue;

				InitNextArrow_Pass(scriptNodeBase, node);
				InitNextArrow_Fail(scriptNodeBase, node);
			}
		}

		private void InitNextArrow_Pass(
			ScriptNodeBase scriptNodeBase,
			NodeViewModel node)
		{
			if (scriptNodeBase.PassNextId < 0)
				return;

			var nextNode = Nodes.ToList().Find((n) => (
				n.Content is ScriptNodeBase scriptNodeBase1) &&
					scriptNodeBase1 == scriptNodeBase.PassNext);
			if (nextNode == null)
				return;

			AddConnector_Pass(node, nextNode);
		}

		private void InitNextArrow_Fail(
			ScriptNodeBase scriptNodeBase,
			NodeViewModel node)
		{
			if (scriptNodeBase.FailNextId < 0)
				return;

			var nextNode = Nodes.ToList().Find((n) => (
				n.Content is ScriptNodeBase scriptNodeBase1) &&
					scriptNodeBase1 == scriptNodeBase.FailNext);
			if (nextNode == null)
				return;

			AddConnector_Fail(node, nextNode);
		}

		#region Up/Down

		private void MoveNodeUp()
		{
			MoveNode(true);
		}

		private void MoveNodeDown()
		{
			MoveNode(false);
		}

		private void MoveNode(bool isUp)
		{
			try
			{
				LoggerService.Inforamtion(this, "Moving node UP");

				ObservableCollection<object> objectsToMove = new ObservableCollection<object>();
				foreach (var item in (SelectedItems.Nodes as ObservableCollection<object>))
				{
					//if (item is NodeViewModel nodeViewModel)
					//{
					//	if (nodeViewModel.OffsetX > _toolOffsetX) // Don't allow draging nodes of sub scripts
					//		continue;
					//}

					objectsToMove.Add(item);
				}

				objectsToMove.ToList().Sort((a, b) => 
					Nodes.IndexOf(a as NodeViewModel).CompareTo(Nodes.IndexOf(b as NodeViewModel)));

				NodeViewModel node = GetIndexToMoveTo(
					objectsToMove,
					isUp);

				DropListOfObject(
					objectsToMove,
					node);

				// Re-select the moved nodes
				foreach (var item in objectsToMove)
				{
					if(item is NodeViewModel nodeViewModel)
						nodeViewModel.IsSelected = true;
				}

			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to move node up", "Up/Down Error", ex);
			}
		}

		private NodeViewModel GetIndexToMoveTo(
			ObservableCollection<object> objectsToMove,
			bool isUp)
		{
			int dropedOnIndex = 0;
			if (isUp)
			{
				dropedOnIndex = Nodes.IndexOf(objectsToMove[0] as NodeViewModel);
				if (dropedOnIndex == 1)
					return null;

				dropedOnIndex--;
			}
			else
			{
				dropedOnIndex = Nodes.IndexOf(objectsToMove[objectsToMove.Count - 1] as NodeViewModel);
				if (dropedOnIndex == (Nodes.Count - 1))
					return null;

				dropedOnIndex++;
			}

			NodeViewModel node = Nodes[dropedOnIndex];

			return node;
		}

		#endregion Up/Down


		private void ExportScriptToPDF(SfDiagram sfDiagram)
		{
			if (DesignDiagram == null)
				return;

			_exportDiagram.Export(DesignDiagram.ScriptPath, Name, sfDiagram);
		}


		private bool _isScriptChangedEvent;
		private void SubScript_ScriptChangedEvent(ScriptNodeSubScript subScript)
		{
			NodeViewModel nodeViewModel = Nodes.ToList().Find((n) => n.Content == subScript);
			if (nodeViewModel == null)
				return;

			_isScriptChangedEvent = true;

			nodeViewModel.UnitHeight = subScript.GetHeight();
			nodeViewModel.UnitWidth = subScript.GetWidth();

			if ((nodeViewModel.Ports as PortCollection).Count > 0)
			{
				try
				{
					(nodeViewModel.Ports as PortCollection).Clear(); 
				}
				catch { }
			}

			SetPorts(nodeViewModel);

			ReAragneNodes();
			SetAllPassNext();
			InitNextArrows();

			_isScriptChangedEvent = false;

		}



		private void Copy()
		{
			List<IScriptItem> copyList = new List<IScriptItem>();
			foreach (var item in (SelectedItems.Nodes as ObservableCollection<object>))
			{
				if (item is NodeViewModel nodeViewModel)
				{
					copyList.Add(nodeViewModel.Content  as IScriptItem);
				}
			}

			JsonSerializerSettings settings = new JsonSerializerSettings();
			settings.Formatting = Formatting.Indented;
			settings.TypeNameHandling = TypeNameHandling.All;
			var sz = JsonConvert.SerializeObject(copyList, settings);

			string format = "MyNode";
			Clipboard.Clear();
			Clipboard.SetData(format, sz);
		}

		private void Past()
		{
			if (Clipboard.ContainsData("MyNode") == false)
				return;

			try
			{
				string copyString = (string)Clipboard.GetData("MyNode");
				JsonSerializerSettings settings = new JsonSerializerSettings();
				settings.Formatting = Formatting.Indented;
				settings.TypeNameHandling = TypeNameHandling.All;
				List<IScriptItem> list =
					JsonConvert.DeserializeObject(copyString, settings) as List<IScriptItem>;

				ScriptNodeBase prev = null;
				if (Nodes.Count > 0)
				{
					prev = Nodes[Nodes.Count - 1].Content as ScriptNodeBase;
				}

				foreach (IScriptItem item in list)
				{
					if (item is IScriptStepWithParameter withParam &&
						withParam.Parameter != null)
					{
						if (withParam.Parameter.Device == null)
						{
							if (_devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType))
								withParam.Parameter.Device = _devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType].Device;
						}
					}

					string toolName = ((ScriptNodeBase)item).Name;
					InitNodeBySymbol(null, toolName, item as ScriptNodeBase);

					if (prev != null && item.PassNext == null)
						prev.PassNext = item;

					DesignDiagram.ScriptItemsList.Add(item);

					prev = item as ScriptNodeBase;

				}

				InitNextArrows();
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to pase tool", "Error in Paset", ex);
			}
		}


		#endregion Methods

		#region Commands

		public RelayCommand<object> ItemAddedCommand { get; private set; }
		public RelayCommand<object> ItemDeletedCommand { get; private set; }
		public RelayCommand<object> ItemSelectedCommand { get; private set; }
		public RelayCommand<object> ConnectorSourceChangedCommand { get; private set; }
		public RelayCommand<object> ConnectorTargetChangedCommand { get; private set; }

		public RelayCommand MoveNodeUpCommand { get; private set; }
		public RelayCommand MoveNodeDownCommand { get; private set; }
		public RelayCommand<SfDiagram> ExportScriptToPDFCommand { get; private set; }


		public RelayCommand SaveDiagramCommand { get; private set; }

		public RelayCommand CopyCommand { get; private set; }
		public RelayCommand PastCommand { get; private set; }
		public RelayCommand SaveCommand { get; private set; }


		#region Drag

		private RelayCommand<MouseEventArgs> _Diagram_MouseEnterCommand;
		public RelayCommand<MouseEventArgs> Diagram_MouseEnterCommand
		{
			get
			{
				return _Diagram_MouseEnterCommand ?? (_Diagram_MouseEnterCommand =
					new RelayCommand<MouseEventArgs>(Diagram_MouseEnter));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _Diagram_PreviewMouseLeftButtonDownCommant;
		public RelayCommand<MouseButtonEventArgs> Diagram_PreviewMouseLeftButtonDownCommant
		{
			get
			{
				return _Diagram_PreviewMouseLeftButtonDownCommant ?? (_Diagram_PreviewMouseLeftButtonDownCommant =
					new RelayCommand<MouseButtonEventArgs>(Diagram_PreviewMouseLeftButtonDown));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _Diagram_PreviewMouseLeftButtonUpCommant;
		public RelayCommand<MouseButtonEventArgs> Diagram_PreviewMouseLeftButtonUpCommant
		{
			get
			{
				return _Diagram_PreviewMouseLeftButtonUpCommant ?? (_Diagram_PreviewMouseLeftButtonUpCommant =
					new RelayCommand<MouseButtonEventArgs>(Diagram_PreviewMouseLeftButtonUp));
			}
		}

		private RelayCommand<MouseEventArgs> _Diagram_MouseMoveCommand;
		public RelayCommand<MouseEventArgs> Diagram_MouseMoveCommand
		{
			get
			{
				return _Diagram_MouseMoveCommand ?? (_Diagram_MouseMoveCommand =
					new RelayCommand<MouseEventArgs>(Diagram_MouseMove));
			}
		}





		#endregion Drag


		#region Drop

		private RelayCommand<DragEventArgs> _Diagram_DropCommand;
		public RelayCommand<DragEventArgs> Diagram_DropCommand
		{
			get
			{
				return _Diagram_DropCommand ?? (_Diagram_DropCommand =
					new RelayCommand<DragEventArgs>(Diagram_Drop));
			}
		}

		#endregion Drop

		#endregion Commands

		#region Events

		public event Action<NodeViewModel> SubNodeDeletedEvent;
		public event EventHandler ScriptReloadedEvent;
		public event EventHandler ScriptIsSavedEvent;

		#endregion Events
	}
}
