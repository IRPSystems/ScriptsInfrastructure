
using CommunityToolkit.Mvvm.ComponentModel;
using ScriptHandler.Models;
using Services.Services;
using System.Collections.Generic;
using System;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using Entities.Models;
using ScriptHandler.Models.ScriptNodes;

namespace ScriptHandler.ViewModels
{
	public class DesignToolsViewModel: ObservableObject
	{
		#region Properties

		public ObservableCollection<ScriptNodeBase> ScriptNodeToolList { get; set; }

		#endregion Properties

		#region Fields

		private DragDropData _designDragDropData;

		#endregion Fields

		#region Constructor

		public DesignToolsViewModel(
			DragDropData designDragDropData)
		{
			_designDragDropData = designDragDropData;
			InitScriptNodeToolList();
		}

		#endregion Constructor

		#region Methods

		private void InitScriptNodeToolList()
		{
			LoggerService.Inforamtion(this, "Initiating the tools list");

			Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().
				SingleOrDefault(assembly => assembly.GetName().Name == "ScriptHandler");

			List<Type> typesList = assembly.GetTypes().ToList();
			typesList = typesList.Where((t) => t.Namespace == "ScriptHandler.Models.ScriptNodes").ToList();

			ScriptNodeToolList = new ObservableCollection<ScriptNodeBase>();
			foreach (Type type in typesList)
			{
				if (!IsNodeBase(type))
					continue;


				//if (type.Name == typeof(ScriptNodeEOLFlash).Name ||
				//	type.Name == typeof(ScriptNodeEOLCalibrate).Name ||
				//	type.Name == typeof(ScriptNodeEOLSendSN).Name)
				//	continue;

				var c = Activator.CreateInstance(type);
				ScriptNodeToolList.Add(c as ScriptNodeBase);
			}
		}

		private bool IsNodeBase(Type type)
		{
			while(type.BaseType.Name != "ScriptNodeBase")
			{
				if (type.BaseType.Name == "Object")
					return false;

				type = type.BaseType;
			}

			return true;
		}


		#region Drag

		private void ListTools_MouseEnter(MouseEventArgs e)
		{
			if (_designDragDropData.IsIgnor)
				return;

			if (e.MouseDevice.LeftButton == MouseButtonState.Pressed)
				_designDragDropData.IsMouseDown = true;
			else
				_designDragDropData.IsMouseDown = false;
		}

		private void ListTools_PreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (_designDragDropData.IsIgnor)
				return;

			_designDragDropData.IsMouseDown = true;
			_designDragDropData.StartPoint = e.GetPosition(null);
		}

		private void ListTools_PreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			_designDragDropData.IsMouseDown = false;
		}

		private void ListTools_MouseMove(MouseEventArgs e)
		{
			if (_designDragDropData.IsMouseDown == false)
				return;

			DragObject(e);
		}

		private void DragObject(MouseEventArgs e)
		{
			LoggerService.Inforamtion(this, "Object is draged");

			Point mousePos = e.GetPosition(null);
			Vector diff = _designDragDropData.StartPoint - mousePos;

			if (e.LeftButton == MouseButtonState.Pressed &&
				Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
				Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
			{
				string formate = "ScriptNodeTool";
				

				// Get the dragged ListViewItem
				ListView listView =
					FindAncestorService.FindAncestor<ListView>((DependencyObject)e.OriginalSource);
				ListViewItem listViewItem =
					FindAncestorService.FindAncestor<ListViewItem>((DependencyObject)e.OriginalSource);

				DependencyObject sourceObject = listViewItem;

				object item = null;
				if (listView != null && listViewItem != null)
				{
					// Find the data behind the ListViewItem
					item = listView.ItemContainerGenerator.
						ItemFromContainer(listViewItem);
				}

				if (item == null)
					return;
				

				DataObject dragData = new DataObject(formate, item);
				DragDrop.DoDragDrop(sourceObject, dragData, DragDropEffects.Move);
			}
		}

		

		#endregion Drag

		private void ListTools_MouseDoubleClick(MouseButtonEventArgs e)
		{
			if (!(e.Source is ListView listView))
				return;

			if (listView.SelectedItem == null)
				return;

			if (!(listView.SelectedItem is ScriptNodeBase scriptNode))
				return;

			AddNodeEvent?.Invoke(scriptNode);
		}

		#endregion Methods

		#region Commands

		#region Drag

		private RelayCommand<MouseEventArgs> _ListTools_MouseEnterCommand;
		public RelayCommand<MouseEventArgs> ListTools_MouseEnterCommand
		{
			get
			{
				return _ListTools_MouseEnterCommand ?? (_ListTools_MouseEnterCommand =
					new RelayCommand<MouseEventArgs>(ListTools_MouseEnter));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _ListTools_PreviewMouseLeftButtonDownCommant;
		public RelayCommand<MouseButtonEventArgs> ListTools_PreviewMouseLeftButtonDownCommant
		{
			get
			{
				return _ListTools_PreviewMouseLeftButtonDownCommant ?? (_ListTools_PreviewMouseLeftButtonDownCommant =
					new RelayCommand<MouseButtonEventArgs>(ListTools_PreviewMouseLeftButtonDown));
			}
		}

		private RelayCommand<MouseButtonEventArgs> _ListTools_PreviewMouseLeftButtonUpCommant;
		public RelayCommand<MouseButtonEventArgs> ListTools_PreviewMouseLeftButtonUpCommant
		{
			get
			{
				return _ListTools_PreviewMouseLeftButtonUpCommant ?? (_ListTools_PreviewMouseLeftButtonUpCommant =
					new RelayCommand<MouseButtonEventArgs>(ListTools_PreviewMouseLeftButtonUp));
			}
		}

		private RelayCommand<MouseEventArgs> _ListTools_MouseMoveCommand;
		public RelayCommand<MouseEventArgs> ListTools_MouseMoveCommand
		{
			get
			{
				return _ListTools_MouseMoveCommand ?? (_ListTools_MouseMoveCommand =
					new RelayCommand<MouseEventArgs>(ListTools_MouseMove));
			}
		}

		#endregion Drag


		private RelayCommand<MouseButtonEventArgs> _ListTools_MouseDoubleClickCommand;
		public RelayCommand<MouseButtonEventArgs> ListTools_MouseDoubleClickCommand
		{
			get
			{
				return _ListTools_MouseDoubleClickCommand ?? (_ListTools_MouseDoubleClickCommand =
					new RelayCommand<MouseButtonEventArgs>(ListTools_MouseDoubleClick));
			}
		}

		#endregion Commands

		#region Events

		public event Action<ScriptNodeBase> AddNodeEvent;

		#endregion Events
	}
}
