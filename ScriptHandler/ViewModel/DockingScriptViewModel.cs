
using CommunityToolkit.Mvvm.Messaging;
using Controls.ViewModels;
using DeviceHandler.ViewModel;
using DeviceHandler.Views;
using ScriptHandler.Interfaces;
using ScriptHandler.Messanger;
using ScriptHandler.Models;
using ScriptHandler.Views;
using Services.Services;
using Syncfusion.Windows.Tools.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.ViewModels
{
	public class DockingScriptViewModel: DocingBaseViewModel
	{
		#region Properties

		public ObservableCollection<DesignScriptViewModel> DesignScriptsList { get; set; }

		#endregion Properties

		#region Fields

		private Dictionary<DesignScriptViewModel, ContentControl> _vmToControl;

		private ContentControl _designTools;
		private ContentControl _explorer;
		private ContentControl _designParameters;

		#endregion Fields

		#region Constructor

		public DockingScriptViewModel(
			DesignToolsViewModel designTools,
			ParametersViewModel designParameters,
			ExplorerViewModel explorer) :
			base("DockingScripts")
		{

			DesignScriptsList = new ObservableCollection<DesignScriptViewModel>();
			IsSelectedDocument += DocumentSelectedChangedHandler;
			//DockStateChanged += DockStateChangedEventHandler;
			CloseButtonClick += CloseButtonClickEvent;
			CloseAllTabs += OnCloseTabsEventHandler;
			CloseOtherTabs += OnCloseTabsEventHandler;

			_vmToControl = new Dictionary<DesignScriptViewModel, ContentControl>();

			InitSubWindows(
				designTools,
				designParameters,
				explorer);
		}

		#endregion Constructor

		#region Methods

		private void InitSubWindows(
			DesignToolsViewModel designTools,
			ParametersViewModel designParameters,
			ExplorerViewModel explorer)
		{
			if (explorer != null)
			{
				_explorer = new ContentControl() { Name = "Explorer" };
				ExplorerView explorerView = new ExplorerView() { DataContext = explorer };
				_explorer.Content = explorerView;
				SetHeader(_explorer, "Explorer");
				SetSideInDockedMode(_explorer, DockSide.Left);
				AddSideWindow(_explorer);
			}


			_designTools = new ContentControl() { Name = "Tools" };
			DesignToolsView designToolsView = new DesignToolsView() { DataContext = designTools };
			_designTools.Content = designToolsView;
			SetHeader(_designTools, "Tools");
			if (explorer == null)
			{
				SetSideInDockedMode(_designTools, DockSide.Left);
			}
			else
			{
				SetTargetNameInDockedMode(_designTools, "Explorer");
				SetSideInDockedMode(_designTools, DockSide.Tabbed);
			}
			AddSideWindow(_designTools);

			if (designParameters != null)
			{
				_designParameters = new ContentControl();
				ParametersView designParametersView = new ParametersView() { DataContext = designParameters };
				_designParameters.Content = designParametersView;
				SetHeader(_designParameters, "Parameters"); ;
				SetSideInDockedMode(_designParameters, DockSide.Right);
				AddSideWindow(_designParameters);
			}
		}

		private void AddSideWindow(ContentControl contentControl)
		{
			SetState(contentControl, DockState.Dock);
			SetSizetoContentInDock(contentControl, true);
			//SetCanFloat(contentControl, false);
			SetCanClose(contentControl, false);
			Children.Add(contentControl);
		}


		public void OpenScript(DesignScriptViewModel scriptVM)
		{
			LoggerService.Inforamtion(this, "Open script");
			if (scriptVM.CurrentScript == null)
				return;

			LoggerService.Inforamtion(this, "Open script " + scriptVM.CurrentScript.Name);

			if (_vmToControl.ContainsKey(scriptVM))
			{
				ContentControl contentControl = _vmToControl[scriptVM];
				this.SelectItem(contentControl);
				return;
			}

			//scriptVM.ScriptIsChangedEvent += ScriptIsChangedEventHandler;
			DesignScriptsList.Add(scriptVM);

			DesignScriptView designScriptView = new DesignScriptView()
			{
				DataContext = scriptVM
			};

			ContentControl window = new ContentControl();
			window.Content = designScriptView;
			SetState(window, DockState.Document);
			if (_vmToControl.ContainsKey(scriptVM) == false)
			{
				Children.Add(window);
				_vmToControl.Add(scriptVM, window);
			}

			SetHeader(window, scriptVM.CurrentScript.Name);

			//ScriptIsChangedEventHandler(scriptVM, false);
		}

		private void DocumentSelectedChangedHandler(FrameworkElement sender, IsSelectedChangedEventArgs e)
		{
			if (e.NewValue)
			{
				if (!(e.TargetElement is ContentControl contentControl))
					return;

				if(!(contentControl.Content is DesignScriptView designScriptView))
					return;

				if(!(designScriptView.DataContext is DesignScriptViewModel designScriptViewModel))
					return;

				WeakReferenceMessenger.Default.Send(new SCRIPT_SELECTION_CHANGED() { DesignScriptVM = designScriptViewModel });
			}
		}

		private void CloseButtonClickEvent(object sender, CloseButtonEventArgs e)
		{
			if (!(e.TargetItem is ContentControl window))
				return;

			e.Cancel = CloseWindow(window);
		}

		private bool CloseWindow(ContentControl window)
		{ 		
						
			if (!(window.Content is DesignScriptView scriptV))
				return false;

			if (!(scriptV.DataContext is DesignScriptViewModel scriptVM))
				return false;

			bool isCancel = scriptVM.SaveIfNeeded();
			if (isCancel)
			{
				return true;
			}

			Children.Remove(window);
			_vmToControl.Remove(scriptVM);
			DesignScriptsList.Remove(scriptVM);

			return false;
		}

		private void OnCloseTabsEventHandler(object sender, CloseTabEventArgs e)
		{
			foreach(var tab in e.ClosingTabItems)
			{
				if (!(tab is TabItemExt tabItem))
					continue;

				if(!(tabItem.Header is string header))
					continue;

				DesignScriptViewModel scriptVM = DesignScriptsList.ToList().Find((s) => s.CurrentScript.Name == header);
				if (scriptVM == null)
					continue;

				if (!_vmToControl.ContainsKey(scriptVM))
					continue;

				ContentControl window = _vmToControl[scriptVM];
				e.Cancel = CloseWindow(window);
				if (e.Cancel == true)
					return;
			}

		}

		public void CloseScript(ScriptData script)
		{
			foreach (ContentControl control in Children)
			{
				if (!(control.Content is DesignScriptView view))
					continue;

				if (!(view.DataContext is DesignScriptViewModel viewModel))
					continue;

				if (viewModel.CurrentScript == script)
				{
					Children.Remove(control);
					_vmToControl.Remove(viewModel);
					DesignScriptsList.Remove(viewModel);
					break;
				}
			}
		}

		public void CloseDesignScript(
			DesignScriptViewModel viewModel,
			bool isRemoveFromDesignScriptsList = true)
		{
			if (_vmToControl.ContainsKey(viewModel) == false)
				return;

			if(_vmToControl[viewModel] is ContentControl control)
				Children.Remove(control);

			_vmToControl.Remove(viewModel);
			if(isRemoveFromDesignScriptsList)
				DesignScriptsList.Remove(viewModel);
		}


		public void CloseAllScripts()
		{
			foreach (DesignScriptViewModel viewModel in DesignScriptsList)
				CloseDesignScript(viewModel, false);

			DesignScriptsList.Clear();
		}

		public bool IsScriptOpen(ScriptData script)
		{
			foreach(ContentControl control in Children) 
			{
				if (!(control.Content is DesignScriptView view))
					continue;

				if(!(view.DataContext is DesignScriptViewModel viewModel))
					continue;

				if(viewModel.CurrentScript == script)
					return true;
			}

			return false;
		}

		#endregion Methods
	}
}
