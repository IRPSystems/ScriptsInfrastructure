
using System.Collections.ObjectModel;
using System.Windows;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using Syncfusion.UI.Xaml.Diagram;
using ScriptHandler.Interfaces;
using ScriptHandler.Services;
using ScriptHandler.ViewModel;
using System.Windows.Input;
using System.Diagnostics.Metrics;
using System.Windows.Controls;
using System.Linq;
using ScriptHandler.Models;

namespace ScriptHandler.ViewModels
{
	public class ScriptDiagramViewModel : SfDiagram
	{
		#region Properties

		public ObservableCollection<NodeViewModel> NodesList
		{
			get => Nodes as ObservableCollection<NodeViewModel>;
		}

		public ObservableCollection<ConnectorViewModelEx> ConnectorsList
		{
			get => Connectors as ObservableCollection<ConnectorViewModelEx>;
		}

		#endregion Properties

		#region Fields

		private ExportDiagramService _exportDiagram;

		private IScript _script;

		private const double _height = 25;
		private const double _width = 240;
		private const double _diff = 10;

		private double _maxLeft;

		#endregion Fields


		#region Constructor

		public ScriptDiagramViewModel()
		{

			Constraints = Constraints & ~GraphConstraints.PageEditing;

			Nodes = new ObservableCollection<NodeViewModel>();
			Connectors = new ObservableCollection<ConnectorViewModelEx>();


			DefaultConnectorType = ConnectorType.Orthogonal;

			_exportDiagram = new ExportDiagramService();

			ChangeBackground(
				Application.Current.MainWindow.FindResource("MahApps.Brushes.Control.Background") as
					System.Windows.Media.SolidColorBrush);

			_maxLeft = 0;

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

		#region Create a diagram

		public void DrawScript(IScript script)
		{
			if (script == null)
				return;

			if (Nodes == null)
				return;

			//Mouse.OverrideCursor = Cursors.Wait;

			NodesList.Clear();
			ConnectorsList.Clear();

			double left = 50;
			_maxLeft = 50;

			CreateScript(script);

			if (script.ScriptItemsList.Count == 0)
			{
				//Mouse.OverrideCursor = null;
				return;
			}

			_script = script;


			Dictionary<string, NodeViewModelEx> contentToNode = new Dictionary<string, NodeViewModelEx>();
			CreateDiagram(
				"",
				script,
				left);

			AddScriptToFirstStepConnector(
				NodesList[0],
				NodesList[1]);

			SetOffsetY(true);

			//Mouse.OverrideCursor = null;
		}



		private void CreateDiagram(
			string parentDescription,
			IScript script,
			double left)
		{

			left += 50;
			if(left > _maxLeft)
				_maxLeft = left;

			foreach (IScriptItem step in script.ScriptItemsList)
			{

				string stepDescription =
					GetStepDescription(
						step.Description,
						parentDescription);


				if (step is ISubScript subScript)
				{

					CreateNode(step, left);

					IScript subScriptScript = subScript.Script;

					if (subScriptScript == null)
						continue;

					NodeViewModelEx subSubScriptNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;
					CreateDiagram(
						stepDescription,
						subScriptScript,
						left);

					//if (subScriptScript.ScriptItemsList != null &&
					//	subScriptScript.ScriptItemsList.Count > 0)
					//{
					//	AddScriptToFirstStepConnector(
					//		subSubScriptNode,
					//		subScriptScript.ScriptItemsList[0]);
					//}

					continue;
				}
				else
				{

					
					CreateNode(step, left);
				}
			}

			AddConnectors(_script);
		}


		private void AddConnectors(IScript script)
		{
			if (script == null || script.ScriptItemsList == null)
				return;

			foreach (IScriptItem step in script.ScriptItemsList)
			{
				if (step is ISubScript subScript)
				{
					AddConnectors(subScript.Script);
				}


				AddSingleNextConnector(step, true);
				AddSingleNextConnector(step, false);
			}
		}

		private void AddSingleNextConnector(
			IScriptItem step,
			bool isPass)
		{
			

			NodeViewModel sourceNode = GetItemNode(step);
			if (sourceNode == null)
				return;

			NodeViewModel targetNode = null;
			if (isPass && step.PassNext != null)
			{
				targetNode = GetItemNode(step.PassNext);
			}
			if (!isPass && step.FailNext != null)
			{
				targetNode = GetItemNode(step.FailNext);
			}

			if (targetNode == null)
				return;
			
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


			AddSingleConnector(
				sourceNode,
				targetNode,
				new System.Windows.Point(x, 0.75),
				new System.Windows.Point(x, 0.25),
				connectorGeometryStyle,
				targetDecoratorStyle,
				isPass);
		}

		private void AddSingleConnector(
			NodeViewModel sourceNode,
			NodeViewModel targetNode,
			System.Windows.Point sourcePoint,
			System.Windows.Point targetPoint,
			System.Windows.Style connectorGeometryStyle,
			System.Windows.Style targetDecoratorStyle,
			bool isPass)
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

			double length = 30;
			OrthogonalDirection direction = OrthogonalDirection.Left;
			if (!isPass)
			{
				length *= (_maxLeft / 50);
				direction = OrthogonalDirection.Right;
			}

			int sourceIndex = NodesList.IndexOf(sourceNode);
			int targetIndex = NodesList.IndexOf(targetNode);
			if (sourceIndex > targetIndex) // The arrow go up
				length += 20;

			connector.Segments = new ConnectorSegments()
			{
				new OrthogonalSegment()
				{
					Length = length,
					Direction = direction,
				},
			};

			

			ConnectorsList.Add(connector);
		}

		private NodeViewModel GetItemNode(IScriptItem item)
		{
			NodeViewModel node = NodesList.ToList().Find((n) => n.Content == item);
			return node;
		}

		private void AddScriptToFirstStepConnector(
			NodeViewModel scriptNode,
			NodeViewModel stepFirstStep)
		{
			System.Windows.Style connectorGeometryStyle =
				Application.Current.MainWindow.Resources["ConnectorGeometryStyle_ScriptToFirstStep"] as System.Windows.Style;
			System.Windows.Style targetDecoratorStyle =
				Application.Current.MainWindow.Resources["TargetDecoratorStyle_ScriptToFirstStep"] as System.Windows.Style;

			AddSingleConnector(
				scriptNode,
				stepFirstStep,
				new System.Windows.Point(0.5, 1),
				new System.Windows.Point(0.5, 0),
				connectorGeometryStyle,
				null,
				true);
		}

		#endregion Create a diagram


		#region Expand/Collapse

		private void SubScriptExpand(IScriptItem e)
		{
			if (!(e is ISubScript subScript))
				return;

			ExpandSubScript(subScript);
		}

		private void ExpandSubScript(ISubScript subScript)
		{

			SubScriptNodeViewModel subScriptNode = null;
			foreach (NodeViewModel node in NodesList)
			{
				if (node.Content == subScript)
				{
					subScriptNode = node as SubScriptNodeViewModel;
					break;
				}
			}



			foreach (NodeViewModel node in NodesList)
			{
				if (node.OffsetY < subScriptNode.OffsetY)
					continue;

				if (node == subScriptNode)
					continue;

				node.OffsetY += subScriptNode.SubScriptHeight;
			}

			foreach (NodeViewModel subNode in subScriptNode.SubNodesList)
			{
				if (!(subNode is NodeViewModelEx exNode))
					continue;
				exNode.Visibility = Visibility.Visible;


				if (subNode != subScriptNode.SubNodesList[subScriptNode.SubNodesList.Count - 1])
					exNode.OffsetY += subScriptNode.SubScriptHeight;
			}

			foreach (ConnectorViewModelEx connector in subScriptNode.SubConnectorsList)
			{
				if (!(connector is ConnectorViewModelEx exConnector))
					continue;
				exConnector.Visibility = Visibility.Visible;
			}

			foreach (NodeViewModel subNode in subScriptNode.SubNodesList)
			{
				if (!(subNode.Content is ISubScript subSubScript))
					continue;

				ExpandSubScript(subSubScript);
			}
		}

		private void SubScriptCollapse(IScriptItem e)
		{
			if (!(e is ISubScript subScript))
				return;

			CollapseSubScript(subScript);
		}

		private void CollapseSubScript(ISubScript subScript)
		{

			SubScriptNodeViewModel subScriptNode = null;
			foreach (NodeViewModel node in NodesList)
			{
				if (node.Content == subScript)
				{
					subScriptNode = node as SubScriptNodeViewModel;
					break;
				}
			}

			if (subScriptNode == null)
				return;


			double subScriptNodeYOffset = subScriptNode.OffsetY;
			subScriptNode.SubScriptHeight =
				subScriptNode.SubNodesList[subScriptNode.SubNodesList.Count - 1].OffsetY -
				subScriptNodeYOffset;


			foreach (NodeViewModel subNode in subScriptNode.SubNodesList)
			{
				if (!(subNode.Content is ISubScript subSubScript))
					continue;

				CollapseSubScript(subSubScript);
			}

			foreach (NodeViewModel subNode in subScriptNode.SubNodesList)
			{
				if (!(subNode is NodeViewModelEx exNode))
					continue;
				exNode.Visibility = Visibility.Collapsed;
			}



			foreach (ConnectorViewModel connector in subScriptNode.SubConnectorsList)
			{
				if (!(connector is ConnectorViewModelEx exConnector))
					continue;
				exConnector.Visibility = Visibility.Collapsed;
			}


			foreach (NodeViewModel node in NodesList)
			{
				if (node.OffsetY < subScriptNodeYOffset)
					continue;

				if (node == subScriptNode)
					continue;

				node.OffsetY -= subScriptNode.SubScriptHeight;
			}
		}

		#endregion Expand/Collapse




		public void CreateScript(IScript script)
		{
			NodeViewModelEx node = new NodeViewModelEx()
			{
				Pivot = new System.Windows.Point(0, 0),
				Content = script,
				OffsetX = 50,
				OffsetY = 15,
				UnitHeight = _height,
				UnitWidth = _width,
				EndPoint = 15 + _height + _diff,
				ContentTemplate = Application.Current.MainWindow.Resources["ScriptLogDiagramTemplate_Script"] as DataTemplate,
			};


			NodesList.Add(node);

			_script = script;
		}

		public void CreateNode(
			IScriptItem item,
			double left,
			bool isFromDrawScript = true)
		{
			NodeViewModelEx prevNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;

			NodeViewModelEx node = new NodeViewModelEx()
			{
				Pivot = new System.Windows.Point(0, 0),
				Content = item,
				OffsetX = left,
				OffsetY = prevNode.EndPoint,
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

			NodesList.Add(node);

			if(!isFromDrawScript)
				SetOffsetY(true);
		}

		public void AddPassFialToNode(IScriptItem scriptItem = null)
		{


			NodeViewModelEx setToNode = null;
			if (scriptItem != null)
			{
				setToNode = GetItemNode(scriptItem) as NodeViewModelEx;
			}
			else
				setToNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;

			if (setToNode == null)
				return;

			if (!(setToNode.Content is IScriptItem lastItem))
				return;

			foreach (NodeViewModel node in NodesList)
			{
				if (!(node.Content is IScriptItem item))
					continue;

				if (item.PassNext == lastItem)
				{
					RemovePass(node, true);
					DrawPassFialNextConnector(true, node, setToNode);
				}

				if (item.FailNext == lastItem)
				{
					RemovePass(node, false);
					DrawPassFialNextConnector(false, node, setToNode);
				}
			}
		}

		public void RemovePass(
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

		public void SetOffsetY(bool handleConnectors = false)
		{
			NodeViewModelEx scriptNode = NodesList[0] as NodeViewModelEx;

			for (int i = 1; i < NodesList.Count; i++)
			{
				NodeViewModelEx prevNode = null;
				if (i == 1)
					prevNode = scriptNode;
				else
					prevNode = NodesList[i - 1] as NodeViewModelEx;

				NodeViewModelEx node = NodesList[i] as NodeViewModelEx;

				node.OffsetY = prevNode.EndPoint;
				node.EndPoint = prevNode.EndPoint + _height + _diff;
			}

			if (!handleConnectors)
				return;

			ConnectorsList.Clear();

			AddConnectors(_script);
			AddAllFirstConnectors();
		}

		private void AddAllFirstConnectors()
		{
			if (NodesList.Count == 1)
				return;

			AddScriptToFirstStepConnector(
				NodesList[0],
				NodesList[1]);

			for (int i = 1; i < NodesList.Count; i++)
			{
				if (!(NodesList[i].Content is ISubScript subScript))
					continue;

				if (subScript.Script == null || subScript.Script.ScriptItemsList == null || subScript.Script.ScriptItemsList.Count == 0)
					continue;

				if ((i + 1) >= NodesList.Count)
					continue;


				AddScriptToFirstStepConnector(
					NodesList[i],
					NodesList[i + 1]);
			}
		}

		#region Move node

		public void MoveNode(
			IScriptItem item,
			IScriptItem replaceItem)
		{
			
			if (item == null)
				return;
			

			NodeViewModelEx itemNode =  GetItemNode(item) as NodeViewModelEx;
			if (itemNode == null)
				return;

			NodeViewModelEx replaceItemNode = null;
			if (replaceItem != null)
				replaceItemNode = GetItemNode(replaceItem) as NodeViewModelEx;

			int itemIndex = NodesList.IndexOf(itemNode);

			int replaceItemIndex = -1;
			if (replaceItemNode != null)
				replaceItemIndex = NodesList.IndexOf(replaceItemNode);

			bool isLastItem = (replaceItemIndex == -1 || replaceItemIndex == (NodesList.Count - 1));

			bool isUp = replaceItemIndex < itemIndex;

			NodeViewModel tempNode = NodesList[itemIndex];
			NodesList.Remove(itemNode);

			replaceItemIndex = -1;
			if (replaceItemNode != null)
				replaceItemIndex = NodesList.IndexOf(replaceItemNode);

			if (replaceItem is ISubScript replaceSubScript)
			{
				if (!isUp)
				{
					int counter = 0;
					GetSubScriptNodesNum(
						ref counter,
						replaceSubScript.Script);

					replaceItemIndex += counter;
				}
			}

			if (isUp && isLastItem)
				replaceItemIndex--;

			if (replaceItemIndex < 0)
				NodesList.Add(tempNode);
			else
				NodesList.Insert(replaceItemIndex, tempNode);
			
			if(item is ISubScript subScript &&
				subScript.Script != null &&
				subScript.Script.ScriptItemsList != null &&
				subScript.Script.ScriptItemsList.Count != 0)
			{
				MoveSubScript(item, replaceItemNode, isUp);				
			}

			

			SetOffsetY(true);
		}



		private void MoveSubScript(
			IScriptItem subScriptItem,
			NodeViewModel replacedNode,
			bool isUp)
		{

			if (!(subScriptItem is ISubScript subScript))
				return;


			

			if (!isUp)
			{
				NodeViewModel sbuScriptNode = GetItemNode(subScript);
				int placementIndex = NodesList.IndexOf(sbuScriptNode);
				MoveSubScriptItemsUp(
					subScript,
					placementIndex);
			}
			else
			{
				MoveSubScriptItemsDown(
					subScript,
					replacedNode);
			}
		}

		private void MoveSubScriptItemsDown(
			ISubScript subScript,
			NodeViewModel replacedNode)
		{
			foreach (IScriptItem subItem in subScript.Script.ScriptItemsList)
			{				

				int placementIndex = NodesList.IndexOf(replacedNode);

				NodeViewModel tempNode = GetItemNode(subItem);
				NodesList.Remove(tempNode);

				if (placementIndex < NodesList.Count && placementIndex >= 0)
					NodesList.Insert(placementIndex, tempNode);
				else
					NodesList.Add(tempNode);


				if (subItem is ISubScript subSubScript)
				{
					MoveSubScriptItemsDown(subSubScript, replacedNode);
				}
			}
		}

		private void MoveSubScriptItemsUp(
			ISubScript subScript,
			int placementIndex)
		{
			foreach (IScriptItem subItem in subScript.Script.ScriptItemsList)
			{

				NodeViewModel tempNode = GetItemNode(subItem);
				NodesList.Remove(tempNode);

				if (placementIndex < NodesList.Count)
					NodesList.Insert(placementIndex, tempNode);
				else
					NodesList.Add(tempNode);


				if (subItem is ISubScript subSubScript)
				{
					MoveSubScriptItemsUp(subSubScript, placementIndex);
				}
			}
		}

		#endregion Move node

		#region Delete node

		public void DeleteNode(IScriptItem item)
		{
			

			if (item == null)
				return;


			NodeViewModelEx itemNode = GetItemNode(item) as NodeViewModelEx;
			if (itemNode == null)
				return;

			if (item is ISubScript subScript &&
				subScript.Script != null &&
				subScript.Script.ScriptItemsList != null &&
				subScript.Script.ScriptItemsList.Count != 0)
			{
				DeleteISubtemsList(
					subScript,
					itemNode);
			}
			else
			{
				NodesList.Remove(itemNode);
			}

			SetOffsetY(true);
		}

		private void DeleteISubtemsList(
			ISubScript subScript,
			NodeViewModelEx itemNode)
		{
			NodesList.Remove(itemNode);


			foreach (IScriptItem subItem in subScript.Script.ScriptItemsList)
			{
				NodeViewModelEx subItemNode = GetItemNode(subItem) as NodeViewModelEx;
				if (subItemNode == null)
					continue;

				NodesList.Remove(subItemNode);

				if (subItem is ISubScript subSubScript)
				{
					DeleteISubtemsList(
						subSubScript,
						subItemNode);
				}

			}
		}

		#endregion Delete node

		private string GetStepDescription(
			string stepDescription,
			string parentDescription)
		{
			if(string.IsNullOrEmpty(parentDescription)) 
				return stepDescription;

			string description =
				stepDescription +
				"; " +
				parentDescription;

			return description;
		}

		private void GetSubScriptNodesNum(
			ref int counter,
			IScript script)
		{
			if (script == null)
				return;

			for(int i = 0; i < script.ScriptItemsList.Count; i++) 
			{
				counter++;

				if (script.ScriptItemsList[i] is ISubScript subScript)
				{
					GetSubScriptNodesNum(
						ref counter,
						subScript.Script);
				}
			}
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

		public void DeletePassFialFromNode(
			IScriptItem scriptItem,
			bool isPass)
		{

			
			NodeViewModelEx setToNode = null;
			if (scriptItem != null)
			{
				setToNode = GetItemNode(scriptItem) as NodeViewModelEx;
				if (setToNode == null)
					return;
			}
			else
				setToNode = NodesList[NodesList.Count - 1] as NodeViewModelEx;



			RemovePassFail(setToNode, isPass);

		}

		#endregion Methods

		#region Commands

		private RelayCommand<IScriptItem> _SubScriptExpandCommand;
		public RelayCommand<IScriptItem> SubScriptExpandCommand
		{
			get
			{
				return _SubScriptExpandCommand ?? (_SubScriptExpandCommand =
					new RelayCommand<IScriptItem>(SubScriptExpand));
			}
		}

		private RelayCommand<IScriptItem> _SubScriptCollapseCommand;
		public RelayCommand<IScriptItem> SubScriptCollapseCommand
		{
			get
			{
				return _SubScriptCollapseCommand ?? (_SubScriptCollapseCommand =
					new RelayCommand<IScriptItem>(SubScriptCollapse));
			}
		}

		#endregion Commands
	}
}
