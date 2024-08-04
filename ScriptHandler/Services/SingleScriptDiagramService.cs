
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.ViewModel;
using Syncfusion.UI.Xaml.Diagram;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ScriptHandler.Services
{
	public class SingleScriptDiagramService : SfDiagram
	{
		#region Properties

		public ISubScript SubScript { get; set; }

		public ObservableCollection<NodeViewModel> NodesList
		{
			get => Nodes as ObservableCollection<NodeViewModel>;
		}

		public ObservableCollection<ConnectorViewModelEx> ConnectorsList
		{
			get => Connectors as ObservableCollection<ConnectorViewModelEx>;
		}

		public double ScriptWidth { get; set; }

		public double ScriptHeight { get; set; }

		#endregion Properties

		#region Fields

		private ExportDiagramService _exportDiagram;
		private Dictionary<IScriptItem, NodeViewModelEx> _scriptItemToNode;

		private const double _height = 25;
		private const double _width = 240;
		private const double _diffVertical = 10;
		private const double _offsetHorizontal = 50;

		protected bool _isFirst;

		private IScript _script;

		public NodeViewModel ParentNode;

		#endregion Fields

		#region Constructor

		public SingleScriptDiagramService(bool isFirst = false)
		{
			_isFirst = isFirst;

			Constraints = Constraints & ~GraphConstraints.PageEditing;
			DefaultConnectorType = ConnectorType.Orthogonal;
			PageSettings = new PageSettings() { PageWidth = double.NaN, PageHeight = double.NaN };


			Nodes = new ObservableCollection<NodeViewModel>();
			Connectors = new ObservableCollection<ConnectorViewModelEx>();



			_scriptItemToNode = new Dictionary<IScriptItem, NodeViewModelEx>();
			_exportDiagram = new ExportDiagramService();


			ScriptWidth = _width + _offsetHorizontal + 10;

			ChangeBackground(
				Application.Current.MainWindow.FindResource("MahApps.Brushes.Control.Background") as
					System.Windows.Media.SolidColorBrush);

		}

		#endregion Constructor

		#region Methods

		public void ChangeBackground(System.Windows.Media.Brush backgroun)
		{
			PageSettings.PageBackground = backgroun;
		}

		public void ExportDiagram(
			string scriptPath,
			string scriptName)
		{
			_exportDiagram.Export(scriptPath, scriptName, this);
		}

		public void DrawScript(
			IScript script)
		{
			if (script == null)
				return;

			if (Nodes == null)
				return;


		//	Mouse.OverrideCursor = Cursors.Wait;

			NodesList.Clear();
			ConnectorsList.Clear();
			_scriptItemToNode.Clear();

			if(_isFirst)
			{
				CreateScript(script);
				AddSubScriptNode(null, script, null);
				return;
			}



			if (script.ScriptItemsList.Count == 0)
			{
				return;
			}


			Dictionary<IScriptItem, NodeViewModelEx> contentToNode = new Dictionary<IScriptItem, NodeViewModelEx>();
			CreateDiagram(
				script,
				contentToNode);



			SetOffsetY();

		//	Mouse.OverrideCursor = null;
		}



		private void CreateDiagram(
			IScript script,
			Dictionary<IScriptItem, NodeViewModelEx> contentToNode)
		{

			foreach (IScriptItem step in script.ScriptItemsList)
			{
				if (step is ISubScript subScript)
				{
					AddSubScriptNode(subScript, subScript.Script, contentToNode);
					if (contentToNode.ContainsKey(step) == false)
						contentToNode.Add(step, NodesList[NodesList.Count - 1] as NodeViewModelEx);
					continue;
				}

				CreateNode(step);
				if (contentToNode.ContainsKey(step) == false)
					contentToNode.Add(step, NodesList[NodesList.Count - 1] as NodeViewModelEx);

				ScriptHeight += _height + _diffVertical;

				

			}

			AddConnectors(script, contentToNode);
		}

		private void AddSubScriptNode(
			ISubScript subScript,
			IScript script,
			Dictionary<IScriptItem, NodeViewModelEx> contentToNode,
			int itemIndex = -1)
		{
			SingleScriptDiagramService subScriptDiagram = new SingleScriptDiagramService();
			subScriptDiagram.SubScript = subScript;
			subScriptDiagram._script = script;
			subScriptDiagram.DrawScript(script);

			double offsetY = 0;
			NodeViewModelEx prevNode = null;
			if (NodesList.Count > 0)
			{
				prevNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;
				offsetY = prevNode.OffsetY + _height + _diffVertical;
				if (prevNode.Content is GeneratedScriptData)
					offsetY += 5;
			}


			NodeViewModelEx node = new NodeViewModelEx()
			{
				Pivot = new System.Windows.Point(0, 0),
				Content = subScriptDiagram,				
				OffsetX = _offsetHorizontal,
				OffsetY = offsetY,				
			};

			if(subScript == null)
			{
				node.UnitWidth = subScriptDiagram.ScriptWidth;
				node.UnitHeight = subScriptDiagram.ScriptHeight;
				node.ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_SingleScript"] as DataTemplate;
			}
			else
			{
				node.UnitWidth = subScriptDiagram.ScriptWidth + (_offsetHorizontal * 2);
				node.UnitHeight = subScriptDiagram.ScriptHeight + _height + _diffVertical;
				node.ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_SingleScript_SubScript"] as DataTemplate;
			}

			if(itemIndex == -1)
				NodesList.Add(node);
			else
				NodesList.Insert(itemIndex + 1, node);
			subScriptDiagram.ParentNode = node;

			if (subScript != null && _scriptItemToNode.ContainsKey(subScript) == false)
				_scriptItemToNode.Add(subScript, node);

			double width = _offsetHorizontal + subScriptDiagram.ScriptWidth + 10;
			if(width > ScriptWidth)
				ScriptWidth = width;

			ScriptHeight += subScriptDiagram.ScriptHeight + _diffVertical + 10;


			if (subScript != null)
			{
				ScriptWidth += _offsetHorizontal;


				if (contentToNode.ContainsKey(subScript) == false)
					contentToNode.Add(subScript, node);
			}

			SetOffsetY();
		}

		#region Connectors

		private void AddConnectors(
			IScript script,
			Dictionary<IScriptItem, NodeViewModelEx> contentToNode)
		{
			if (script == null || script.ScriptItemsList == null)
				return;

			foreach (IScriptItem step in script.ScriptItemsList)
			{
				if (contentToNode.ContainsKey(step) == false)
					continue;

				if (step is ISubScript subScript)
				{
					AddConnectors(subScript.Script, contentToNode);
				}


				AddSingleNextConnector(step, true, contentToNode);
				AddSingleNextConnector(step, false, contentToNode);

			}
		}

		private void AddSingleNextConnector(
			IScriptItem step,
			bool isPass,
			Dictionary<IScriptItem, NodeViewModelEx> contentToNode)
		{


			NodeViewModel sourceNode = contentToNode[step];


			if (isPass && (step.PassNext == null || contentToNode.ContainsKey(step.PassNext) == false))
				return;
			if (!isPass && (step.FailNext == null || contentToNode.ContainsKey(step.FailNext) == false))
				return;

			NodeViewModel targetNode = null;
			if (isPass)
				targetNode = contentToNode[step.PassNext];
			else
				targetNode = contentToNode[step.FailNext];

			DrawPassFialNextConnector(
				isPass,
				sourceNode,
				targetNode);
		}

		private void DrawPassFialNextConnector(
			bool isPass,
			NodeViewModel sourceNode,
			NodeViewModel targetNode)
		{
			int x = 0;
			if (!isPass)
				x = 1;


			System.Windows.Style connectorGeometryStyle = null;
			if (isPass)
				connectorGeometryStyle = Application.Current.MainWindow.Resources["ConnectorGeometryStyle_Pass"] as System.Windows.Style;
			else
				connectorGeometryStyle = Application.Current.MainWindow.Resources["ConnectorGeometryStyle_Fail"] as System.Windows.Style;

			System.Windows.Style targetDecoratorStyle = null;
			if (isPass)
				targetDecoratorStyle = Application.Current.MainWindow.Resources["TargetDecoratorStyle_Pass"] as System.Windows.Style;
			else
				targetDecoratorStyle = Application.Current.MainWindow.Resources["TargetDecoratorStyle_Fail"] as System.Windows.Style;

			double startY = 0.75;
			double endY = 0.25;

			if (sourceNode.Content is SingleScriptDiagramService singleScriptStart)
			{
				double subScriptPersentage = _height / (singleScriptStart.ScriptHeight + 15);
				startY = subScriptPersentage * 0.75;
			}
			if (targetNode.Content is SingleScriptDiagramService singleScriptEnd)
			{
				double subScriptPersentage = _height / (singleScriptEnd.ScriptHeight + 15);
				endY = subScriptPersentage * 0.25;
			}


			AddSingleConnector(
				sourceNode,
				targetNode,
				new System.Windows.Point(x, startY),
				new System.Windows.Point(x, endY),
				connectorGeometryStyle,
				targetDecoratorStyle);
		}

		public void AddSingleConnector(
			NodeViewModel sourceNode,
			NodeViewModel targetNode,
			System.Windows.Point sourcePoint,
			System.Windows.Point targetPoint,
			System.Windows.Style connectorGeometryStyle,
			System.Windows.Style targetDecoratorStyle,
			ObservableCollection<ConnectorViewModelEx> connectorsList = null)
		{
			NodePortViewModel sourcePort = new NodePortViewModel()
			{
				NodeOffsetX = sourcePoint.X,
				NodeOffsetY = sourcePoint.Y,
			};

			if (sourceNode.Ports == null)
				sourceNode.Ports = new PortCollection();
			(sourceNode.Ports as PortCollection).Add(sourcePort);

			NodePortViewModel targetPort = new NodePortViewModel()
			{
				NodeOffsetX = targetPoint.X,
				NodeOffsetY = targetPoint.Y,
			};

			if (targetNode.Ports == null)
				targetNode.Ports = new PortCollection();
			(targetNode.Ports as PortCollection).Add(targetPort);

			ConnectorViewModelEx connector = new ConnectorViewModelEx()
			{
				SourceNode = sourceNode,
				TargetNode = targetNode,

				SourcePort = sourcePort,
				TargetPort = targetPort,



				ConnectorGeometryStyle = connectorGeometryStyle,
				TargetDecoratorStyle = targetDecoratorStyle,
			};

			if(connectorsList != null)
				connectorsList.Add(connector);
			else
				ConnectorsList.Add(connector);
		}

		#endregion Connectors

		private void CreateScript(IScript script)
		{
			NodeViewModelEx node = new NodeViewModelEx()
			{
				Pivot = new System.Windows.Point(0, 0),
				Content = script,
				OffsetX = 50,
				OffsetY = 15,
				UnitHeight = _height + 15,
				UnitWidth = _width,
				//EndPoint = 15 + _height + _diffVertical,
				ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_Script"] as DataTemplate,
			};


			NodesList.Add(node);
		}

		private SingleScriptDiagramService GetFirstActualScript()
		{
			if (_isFirst == false)
				return null;

			if(NodesList == null || NodesList.Count < 2)
				return null;

			return NodesList[1].Content as SingleScriptDiagramService;
		}


		public void CreateNode(
			IScriptItem item,
			int itemIndex = -1,
			bool isFromDrawScript = true)
		{

			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.CreateNode(item, itemIndex, isFromDrawScript);
				return;
			}

			if(item is ISubScript subScript)
			{
				AddSubScriptNode(
					subScript,
					subScript.Script,
					_scriptItemToNode,
					itemIndex);
			}
			else
			{
				CreateNode_RegularNode(item, itemIndex);
			}
			

			if(!isFromDrawScript)
			{
				if (itemIndex > -1)
				{
					IScriptItem newItem = NodesList[itemIndex + 1].Content as IScriptItem;
					if (NodesList[itemIndex + 1].Content is SingleScriptDiagramService singleScriptDiagram)
						newItem = singleScriptDiagram.SubScript;
					AddPassFialToNode(newItem);

					if ((itemIndex + 2) < (NodesList.Count - 1))
					{
						IScriptItem nextItem = NodesList[itemIndex + 2].Content as IScriptItem;
						if (NodesList[itemIndex + 2].Content is SingleScriptDiagramService singleScriptDiagram1)
							nextItem = singleScriptDiagram1.SubScript;
						AddPassFialToNode(nextItem);
					}

					
				}
				else
					AddPassFialToNode();
				SetOffsetY();
			}
		}

		public void AddPassFialToNode(IScriptItem scriptItem = null)
		{

			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.AddPassFialToNode(scriptItem);
				return;
			}

			NodeViewModelEx setToNode = null;
			if (scriptItem != null)
			{
				if (_scriptItemToNode.ContainsKey(scriptItem) == false)
					return;

				setToNode = _scriptItemToNode[scriptItem];
			}
			else
			{
				setToNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;
				scriptItem = GetScriptItemFromNode(setToNode);
			}

			

			if(setToNode.Content is SingleScriptDiagramService singleScript &&
				singleScript.ScriptHeight == 0)
			{
				singleScript.ScriptHeight = _height + _diffVertical;
			}

			foreach (NodeViewModel node in NodesList)
			{
				IScriptItem item = GetScriptItemFromNode(node);
				if (item == null)
					continue;

				if (item.PassNext == scriptItem)
				{
					RemovePassFail(node, true);
					DrawPassFialNextConnector(true, node, setToNode);
				}

				if (item.FailNext == scriptItem)
				{
					RemovePassFail(node, false);
					DrawPassFialNextConnector(false, node, setToNode);
				}

				//if (item.FailNext == scriptItem)
				//{
				//	RemovePassFail(node, false);
				//	DrawPassFialNextConnector(false, node, setToNode);
				//}
			}
		}

		public void DeletePassFialFromNode(
			IScriptItem scriptItem,
			bool isPass)
		{

			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.DeletePassFialFromNode(scriptItem, isPass);
				return;
			}

			NodeViewModelEx setToNode = null;
			if (scriptItem != null)
			{
				if (_scriptItemToNode.ContainsKey(scriptItem) == false)
					return;

				setToNode = _scriptItemToNode[scriptItem];
			}
			else
				setToNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;

			

			RemovePassFail(setToNode, isPass);

		}


		public void MoveNode(
			IScriptItem item,
			IScriptItem replaceItem)
		{
			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.MoveNode(item, replaceItem);

				return;
			}

			if (item == null || _scriptItemToNode.ContainsKey(item) == false)
			{
				return;
			}

			NodeViewModelEx itemNode = _scriptItemToNode[item];

			NodeViewModelEx replaceItemNode = null;
			if(replaceItem != null)
				replaceItemNode = _scriptItemToNode[replaceItem];

			int itemIndex = NodesList.IndexOf(itemNode);

			int replaceItemIndex = -1;
			if(replaceItemNode != null)
				replaceItemIndex = NodesList.IndexOf(replaceItemNode);

			NodeViewModel tempNode = NodesList[itemIndex];
			NodesList.RemoveAt(itemIndex);

			if (replaceItemIndex > -1)
				NodesList.Insert(replaceItemIndex, tempNode);
			else
				NodesList.Add(tempNode);



			if (_script != null)
			{
				ConnectorsList.Clear();
				AddConnectors(_script, _scriptItemToNode);
			}

			SetOffsetY();
		}

		


		public void DeleteNode(IScriptItem item)
		{
			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.DeleteNode(item);

				return;
			}

			if (item == null || _scriptItemToNode.ContainsKey(item) == false)
				return;
			

			NodeViewModelEx itemNode = _scriptItemToNode[item];
			NodesList.Remove(itemNode);			
			SetOffsetY();
		}

		private IScriptItem GetScriptItemFromNode(NodeViewModel node)
		{
			IScriptItem item = null;
			if (!(node.Content is IScriptItem))
			{
				if (!(node.Content is SingleScriptDiagramService singleScript))
					return null;
				else
				{
					item = singleScript.SubScript;
				}
			}
			else
			{
				item = node.Content as IScriptItem;
			}

			return item;
		}

		private void CreateNode_RegularNode(
			IScriptItem item,
			int itemIndex = -1)
		{
			double offsetY = 0;
			if (NodesList.Count > 0)
			{
				NodeViewModelEx prevNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;
				offsetY = prevNode.OffsetY + _height + _diffVertical;
			}

			NodeViewModelEx node = new NodeViewModelEx()
			{
				Pivot = new System.Windows.Point(0, 0),
				Content = item,
				OffsetX = _offsetHorizontal,
				OffsetY = offsetY,
				UnitHeight = _height,
				UnitWidth = _width,
			};

			if (item is ISubScript)
			{
				node.ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_Step_SubScript"] as DataTemplate;
			}
			else if (item is ScriptStepCANMessage)
			{
				node.ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_Step_CANMessage"] as DataTemplate;
			}
			else
			{
				node.ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_Step"] as DataTemplate;
			}


			if (itemIndex == -1)
				NodesList.Add(node);
			else
				NodesList.Insert(itemIndex + 1, node);

			if (_scriptItemToNode.ContainsKey(item) == false)
				_scriptItemToNode.Add(item, node);
		}

		private void RemovePassFail(
			NodeViewModel node,
			bool isPass)
		{
			foreach (ConnectorViewModel connector in ConnectorsList)
			{
				if (!(connector is ConnectorViewModelEx connectorEx))
					continue;

				if (connector.SourceNode == node)
				{
					if (isPass && connector.SourcePoint.X == node.OffsetX)
					{
						ConnectorsList.Remove(connectorEx);
						break;
					}
					else if (!isPass && connector.SourcePoint.X > node.OffsetX)
					{
						ConnectorsList.Remove(connectorEx);
						break;
					}
				}
			}
		}

		private void SetOffsetY(/*bool handleConnectors = false*/)
		{
			if (_isFirst)
			{
				SingleScriptDiagramService actualScript = GetFirstActualScript();
				if (actualScript != null)
					actualScript.SetOffsetY();//handleConnectors);
				return;
			}

			double offsetY = 0;
			for (int i = 0; i < NodesList.Count; i++)
			{

				NodeViewModelEx node = NodesList[i] as NodeViewModelEx;
				node.OffsetY = offsetY;

				if (node.Content is SingleScriptDiagramService singleScriptDiagram)
				{
					offsetY += singleScriptDiagram.ScriptHeight + _diffVertical;
					if (singleScriptDiagram.SubScript != null)
						offsetY += _height + _diffVertical;

				}
				else
					offsetY += _height + _diffVertical;
			}
			
			ScriptHeight = offsetY;
			if (ParentNode != null)
			{
				ParentNode.UnitHeight = offsetY + 10;
				ParentNode.UnitWidth = ScriptWidth + 50;
			}

			//if (!handleConnectors)
			//	return;

			try
			{
				ConnectorsList.Clear();
			}
			catch { }

			for (int i = 1; i < NodesList.Count; i++)
			{
				AddPassFialToNode(GetScriptItemFromNode(NodesList[i]));
			}
		}


		#endregion Methods
	}
}
