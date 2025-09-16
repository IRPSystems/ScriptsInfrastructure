
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

		private const double _toolHeight = 35;
		private const double _toolWidth = 300;
		private const double _betweenTools = 45;
		private const double _toolOffsetX = 100;

		private bool _isMouseDown;
		private Point _startPoint;

		private int _idCounter;

		private DevicesContainer _devicesContainer;

		private ScriptUserData _scriptUserData;

		private ScriptValidationService _scriptValidation;

		private ExportDiagramService _exportDiagram;

		#endregion Fields

		#region Constructor

		public DesignDiagramViewModel(
			ScriptUserData scriptUserData,
			DevicesContainer devicesContainer,
			NodePropertiesViewModel nodeProperties, 
			double offsetX = 100)
		{
			_devicesContainer = devicesContainer;
			_scriptUserData = scriptUserData;

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

			SaveDiagramCommand = new RelayCommand(Save);

			SetSnapAndGrid();

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
			NodeViewModel node = new NodeViewModel();
			node.Content = DesignDiagram;
			node.ContentTemplate = 
				Application.Current.FindResource("ScriptLogDiagramTemplate_Script") as DataTemplate;
			node.UnitHeight = 50;
			node.UnitWidth = _toolWidth;

			node.OffsetX = 50;
			node.OffsetY = OffsetY;

			OffsetY += 35;

			node.Pivot = new Point(0, 0);

			Nodes.Add(node);
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
				if (DesignDiagram.Name == "Active Discharge") { }
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

			if (connector.ID is string str && 
				(str.Contains("PassNext_") || str.Contains("SubScript_")))
			{
				return;
			}

			if (!(connector.SourceNode is NodeViewModel sourceNode))
				return;

			if (!(connector.SourcePort is NodePortViewModel port))
				return;

			//if (port.NodeOffsetX != 1)
			//{
			//	Connectors.Remove(connector);
			//	return;
			//}

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


					//IsLoadingScript = true;

					path = openFileDialog.FileName;
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

					scriptNode.PostLoad(
						_devicesContainer,
						DesignDiagram);
				}


				_idCounter = 0;

				foreach (ScriptNodeBase node in DesignDiagram.ScriptItemsList)
				{
					if (_idCounter < node.ID)
						_idCounter = node.ID;

					node.IsExpanded = true;
					//node.NodePropertyChangeEvent += NodePropertyChangedHandler;



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




				//SetPassFailNextTool();
				//await InitNods();
			}
			catch (Exception ex)
			{
				LoggerService.Error(this, $"Failed to load the script \"{Name}\"", "Error", ex);
			}

			IsChanged = false;

			Mouse.OverrideCursor = null;
		}

		public async Task DrawNodes()
		{
			AddHeaderNode();
			SetPassFailNextTool();
			await InitNods();
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

		private async Task InitNods()
		{
			foreach (ScriptNodeBase tool in DesignDiagram.ScriptItemsList)
			{
				string toolName = tool.Name;
				InitNodeBySymbol(null, toolName, tool);

				if(tool is ScriptNodeSubScript)
				{
					AddSubString(tool, _toolOffsetX);
				}

				//System.Threading.Thread.Sleep(1);
				await Task.Delay(1);
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

		private void AddSubString(
			ScriptNodeBase tool,
			double xOffset)
		{
			if (!(tool is ScriptNodeSubScript subScript))
				return;

			if(subScript.Script == null || 
				subScript.Script.ScriptItemsList == null ||
				subScript.Script.ScriptItemsList.Count == 0)
			{
				return;
			}

			xOffset += 100;
			bool isFirstSubScriptTool = true;
			foreach (ScriptNodeBase subTool in subScript.Script.ScriptItemsList)
			{
				string toolName = subTool.Name;
				InitNodeBySymbol(
					null, 
					toolName, 
					subTool,
					xOffset);

				if(isFirstSubScriptTool)
				{
					var subScriptNode = Nodes[Nodes.Count - 2];
					var subScriptFirstNode = Nodes[Nodes.Count - 1];
					AddConnectorToSubString(subScriptNode, subScriptFirstNode);
				}
				

				isFirstSubScriptTool = false;

				if (subTool is ScriptNodeSubScript subScript2)
					AddSubString(subTool, xOffset);

			}
		}

		private void AddConnectorToSubString(
			NodeViewModel subScriptNode,
			NodeViewModel subScriptFirstNode)
		{
			

			NodePortViewModel port = new NodePortViewModel()
			{
				NodeOffsetX = 0.833,
				NodeOffsetY = 1,
			};
			(subScriptNode.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 0.5,
				NodeOffsetY = 0,
			};
			(subScriptFirstNode.Ports as PortCollection).Add(port);

			ConnectorViewModel simpleConnector = new ConnectorViewModel()
			{
				ID = $"SubScript_{subScriptNode.ID}",
				SourceNode = subScriptNode,
				SourcePort = (subScriptNode.Ports as PortCollection)
					[(subScriptNode.Ports as PortCollection).Count - 1],

				TargetNode = subScriptFirstNode,
				TargetPort = (subScriptFirstNode.Ports as PortCollection)
					[(subScriptFirstNode.Ports as PortCollection).Count - 1],
			};

			Connectors.Add(simpleConnector);
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
			double xOffset = _toolOffsetX)
		{
			if(node == null)
			{
				node = new NodeViewModel();
				node.Content = tool;
				Nodes.Add(node);

				_idCounter++;
			}

			node.ID = toolName;
			node.Pivot = new Point(0, 0);

			if (tool == null)
			{
				SetContent(node, toolName);
			}

			

			SetNodeTemplateAndSize(node, toolName, xOffset);
			SetPorts(node);
			

			node.PropertyChanged += Node_PropertyChanged;

			if (tool == null)
			{
				SetNextPassConnector(node);
				DesignDiagram.ScriptItemsList.Add(node.Content as ScriptNodeBase);
			}

			if((node.Content as ScriptNodeBase) != null)
				(node.Content as ScriptNodeBase).PropertyChanged += Tool_PropertyChanged;

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
			NodePortViewModel port = new NodePortViewModel()
			{
				NodeOffsetX = 0,
				NodeOffsetY = 0.25,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 0,
				NodeOffsetY = 0.75,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 1,
				NodeOffsetY = 0.25,
			};
			(node.Ports as PortCollection).Add(port);

			port = new NodePortViewModel()
			{
				NodeOffsetX = 1,
				NodeOffsetY = 0.75,
			};
			(node.Ports as PortCollection).Add(port);
		}

		private void SetContent(
			NodeViewModel node,
			string toolName)
		{
			//switch (toolName)
			//{
			//	case "Compare":
			//		node.Content = new ScriptNodeCompare();
			//		break;
			//	case "Delay":
			//		node.Content = new ScriptNodeDelay();
			//		break;
			//	case "Dynamic Control":
			//		node.Content = new ScriptNodeDynamicControl();
			//		break;
			//	case "Sweep":
			//		node.Content = new ScriptNodeSweep();
			//		break;
			//	case "Set Parameter":
			//		node.Content = new ScriptNodeSetParameter();
			//		break;
			//	case "Set and Save Parameter":
			//		node.Content = new ScriptNodeSetSaveParameter();
			//		break;
			//	case "Save Parameter":
			//		node.Content = new ScriptNodeSaveParameter();
			//		break;
			//	case "Notification":
			//		node.Content = new ScriptNodeNotification();
			//		break;
			//	case "Sub Script":
			//		node.Content = new ScriptNodeSubScript();
			//		break;
			//	case "Increment Value":
			//		node.Content = new ScriptNodeIncrementValue();
			//		break;
			//	case "Loop Increment":
			//		node.Content = new ScriptNodeLoopIncrement();
			//		break;
			//	case "Converge":
			//		node.Content = new ScriptNodeConverge();
			//		break;
			//	case "Compare Range":
			//		node.Content = new ScriptNodeCompareRange();
			//		break;
			//	case "Compare With Tolerance":
			//		node.Content = new ScriptNodeCompareWithTolerance();
			//		break;
			//	case "CAN Message":
			//		node.Content = new ScriptNodeCANMessage();
			//		break;
			//	case "CAN Message Update":
			//		node.Content = new ScriptNodeCANMessageUpdate();
			//		break;
			//	case "CAN Message Stop":
			//		node.Content = new ScriptNodeCANMessageStop();
			//		break;
			//	case "Stop Continuous":
			//		node.Content = new ScriptNodeStopContinuous();
			//		break;
			//	case "EOL Flash":
			//		node.Content = new ScriptNodeEOLFlash();
			//		break;
			//	case "EOL Calibrate":
			//		node.Content = new ScriptNodeEOLCalibrate();
			//		break;
			//	case "EOL Send SN":
			//		node.Content = new ScriptNodeEOLSendSN();
			//		break;
			//	case "Reset Parent Sweep":
			//		node.Content = new ScriptNodeResetParentSweep();
			//		break;
			//	case "Scope Save":
			//		node.Content = new ScriptNodeScopeSave();
			//		break;
			//	case "EOL Print":
			//		node.Content = new ScriptNodeEOLPrint();
			//		break;
			//	case "Compare Bit":
			//		node.Content = new ScriptNodeCompareBit();
			//		break;
			//}

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

			if (toolName.ToLower().Contains("sub script"))
			{
				node.ContentTemplate =
					Application.Current.FindResource("ScriptLogDiagramTemplate_Step_SubScript") as DataTemplate;
			}
			else
			{
				node.ContentTemplate =
					Application.Current.FindResource("ScriptLogDiagramTemplate_Step") as DataTemplate;
			}


			node.UnitHeight = _toolHeight;
			node.UnitWidth = _toolWidth;
			node.OffsetX = xOffset;
			node.OffsetY = OffsetY;

			if (node.Content is ScriptNodeBase)
			{
				(node.Content as ScriptNodeBase).Height = _toolHeight;
				(node.Content as ScriptNodeBase).Width = _toolWidth;
				(node.Content as ScriptNodeBase).OffsetX = xOffset;
				(node.Content as ScriptNodeBase).OffsetY = OffsetY;
			}

			OffsetY += _betweenTools;
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
				if (_isInPropertyChanged)
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
			

			if(node.Content is ScriptNodeSubScript subScript)
			{
				foreach(ScriptNodeBase tool in subScript.Script.ScriptItemsList)
				{
					NodeViewModel subNode = Nodes.ToList().Find(n => n.Content == tool);
					if(subNode == null) 
						continue;

					SubNodeDeletedEvent?.Invoke(subNode);
				}
			}

			ReAragneNodex();

			Mouse.OverrideCursor = null;
		}

		private void DeleteConnector(ConnectorViewModel connector)
		{
			NodeViewModel node = connector.SourceNode as NodeViewModel;
			if (node == null) 
				return;

			if (connector.ID == null)
				return;

			if((connector.ID as string).StartsWith("PassNext"))
				(node.Content as ScriptNodeBase).PassNext = null;
			else if ((connector.ID as string).StartsWith("FailNext"))
				(node.Content as ScriptNodeBase).FailNext = null;
		}

		private void ReAragneNodex()
		{

			OffsetY = 50 + 35;

			int idCounter = 1;
			foreach (NodeViewModel nodeItem in Nodes)
			{
				if (!(nodeItem.Content is ScriptNodeBase scriptNodeBase))
					continue;

				scriptNodeBase.ID = idCounter++;

				//scriptNodeBase.OffsetX = _toolOffsetX;
				scriptNodeBase.OffsetY = OffsetY;
				//nodeItem.OffsetX = _toolOffsetX;
				nodeItem.OffsetY = OffsetY;

				OffsetY += _betweenTools;
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
						if (item is NodeViewModel nodeViewModel)
						{
							if (nodeViewModel.OffsetX > _toolOffsetX) // Don't allow draging nodes of sub scripts
								continue;
						}

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

					if (nvm.Content is ScriptNodeSubScript subScript)
					{
						GetListOfNodesToDrop_SubScript(tempList, subScript);
					}
				}
			}

			return tempList;
		}

		private void GetListOfNodesToDrop_SubScript(
			List<NodeViewModel> tempList,
			ScriptNodeSubScript subScript)
		{
			foreach (IScriptItem step in subScript.Script.ScriptItemsList)
			{
				NodeViewModel stepNode = Nodes.ToList().Find((n) => n.Content == step);
				if (stepNode != null)
				{
					tempList.Add(stepNode);

					if(step is ScriptNodeSubScript subSubScript)
					{
						GetListOfNodesToDrop_SubScript(
							tempList,
							subSubScript);
					}
				}
			}
		}

		private void DropListOfObject(
			ObservableCollection<object> dragNodeList,
			NodeViewModel nodeVMDropedOn)
		{
			int dropedOnIndex = Nodes.IndexOf(nodeVMDropedOn);

			int firstDroppedIndex = Nodes.IndexOf(dragNodeList[0] as NodeViewModel);
			bool isUp = firstDroppedIndex > dropedOnIndex;

			if(dropedOnIndex >= 0)
			{
				GetNodeAndIndexForSubScript(
					isUp,
					ref dropedOnIndex,
					ref nodeVMDropedOn);
			}



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

				if (dropedIndex > dropedOnIndex)
					dropedOnIndex++;
			}

			ReAragneNodex();
			SetAllPassNext();
			InitNextArrows();
		}

		private void GetNodeAndIndexForSubScript(
			bool isUp,
			ref int dropedOnIndex,
			ref NodeViewModel nodeVMDropedOn)
		{
			// The droped on node is part of sub-script
			if (nodeVMDropedOn.OffsetX > _toolOffsetX)
			{
				// Since we're moving UP, we need to find the 
				// node of the START of the sub-script
				if (isUp)
				{
					for (int i = dropedOnIndex; i >= 0; i--)
					{
						if (Nodes[i].Content is ScriptNodeSubScript)
						{
							nodeVMDropedOn = Nodes[i];
							dropedOnIndex = Nodes.IndexOf(nodeVMDropedOn);
							break;
						}
					}
				}
				// Since we're moving DOWN, we need to find the 
				// node of the END of the sub-script
				else
				{
					for (int i = dropedOnIndex; i < Nodes.Count; i++)
					{
						if (nodeVMDropedOn.OffsetX == _toolOffsetX)
						{
							nodeVMDropedOn = Nodes[i];
							dropedOnIndex = Nodes.IndexOf(nodeVMDropedOn);
							break;
						}
					}
				}

				return;
			}

			if(nodeVMDropedOn.Content is ScriptNodeSubScript subScript)
			{
				// If we're going down, we need to find the
				// last item of the sub-script
				if (!isUp)
				{
					dropedOnIndex += subScript.Script.ScriptItemsList.Count;
					nodeVMDropedOn = Nodes[dropedOnIndex];
				}
			}

		}

		private void DropSingleObject(
			NodeViewModel dropedNode,
			int dropedOnIndex)
		{
			NodeViewModel temp = dropedNode;
			int dragedIndex = Nodes.IndexOf(dropedNode);
			Nodes.RemoveAt(dragedIndex);


			if (dropedOnIndex < 0)
			{
				Nodes.Add(temp);
			}
			else
			{
				Nodes.Insert(dropedOnIndex, dropedNode);
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

				if(firstNode is ScriptNodeSubScript subScript)
				{
					AddConnectorToSubString(Nodes[i], Nodes[i + 1]);
					int secondIndex = i + subScript.Script.ScriptItemsList.Count + 1;
					if (secondIndex < Nodes.Count - 1)
						secondNode = (Nodes[secondIndex].Content as ScriptNodeBase);
				}
				if (Nodes[i].OffsetX > _toolOffsetX &&
					Nodes[i + 1].OffsetX == _toolOffsetX)
				{
					continue;
				}

				firstNode.PassNext = secondNode;
			}
		}

		private void InitNextArrows()
		{
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
					if (item is NodeViewModel nodeViewModel)
					{
						if (nodeViewModel.OffsetX > _toolOffsetX) // Don't allow draging nodes of sub scripts
							continue;
					}

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
