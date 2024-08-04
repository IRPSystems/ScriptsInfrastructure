
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using DeviceHandler.ViewModel;
using Entities.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.ViewModels;
using Services.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScriptHandler.ViewModel
{
	public class GenerateErrorsViewModel : ObservableObject
	{
		#region Properties

		public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }
		public ProjectData Project { get; set; }
		public InvalidScriptItemData SelectedErrorItem { get; set; }

		public string ErrorType { get; set; }
		public string Resulution { get; set; }

		public ParametersViewModel ParametersList { get; set; }
		public DeviceParameterData SelectedParameter { get; set; }
		public Visibility ParameterReplacingVisibility { get; set; }

		#endregion Properties
		
		#region Constructor

		public GenerateErrorsViewModel(DevicesContainer devicesContainer)
		{
			ParametersList = new ParametersViewModel(new DragDropData(), devicesContainer, false);

			ChangeParamCommand = new RelayCommand(ChangeParam);

			ParameterReplacingVisibility = Visibility.Collapsed;
		}

		#endregion Constructor

		#region Methods

		private void ErrorList_MouseDown(MouseButtonEventArgs e)
		{
			if (!(e.Source is TextBlock textBlock))
				return;

			if (!(textBlock.DataContext is InvalidScriptItemData invalidScriptItem))
				return;

			SelectedErrorItem = invalidScriptItem;

			ParameterReplacingVisibility = Visibility.Collapsed;


			if (SelectedErrorItem is InvalidScriptItemData_DeviceNotFound deviceNotFound)
			{
				ErrorType = "Device not found";
				Resulution =
					"The device \"" + deviceNotFound.DeviceType + " does not exist in the setup.\r\n" +
					"Please add it and try to generate the script again.";
			}
			else if (SelectedErrorItem is InvalidScriptItemData_DataIsNotSet)
			{
				ErrorType = "Script item data is not set";
				Resulution =
					"Part of the script item data is not set.\r\n" +
					"Script: " + invalidScriptItem.Parent.Script.Name + "\r\n" +
					"Script item: " + invalidScriptItem.ScirptItem.Description + "\r\n" +
					"Please go to the script item at the script and correct it.";
			}
			else if (SelectedErrorItem is InvalidScriptItemData_ParamDontExist paramDontExist)
			{
				ParameterReplacingVisibility = Visibility.Visible;
				ErrorType = "Parameter not found";
				Resulution =
					"The parameter \"" + paramDontExist.Parameter.Name + "\" was not found in it's device parameter list.\r\n" +
					"The device is: \"" + paramDontExist.Parameter.DeviceType;
			}

		}

		#region Drop

		private void ListScript_Drop(DragEventArgs e)
		{
			LoggerService.Inforamtion(this, "Object is dropped");

			try
			{



				if (e.Data.GetDataPresent(ParametersViewModel.DragDropFormat))
				{
					string tbName = "";
					if (!(e.OriginalSource is TextBlock textBlock))
						return;

					tbName = textBlock.Name;
					if (!tbName.StartsWith("tbParam"))
						return;

					DeviceParameterData param = e.Data.GetData(ParametersViewModel.DragDropFormat) as DeviceParameterData;
					textBlock.DataContext = param;
					SelectedParameter = param;
				}
				

			}
			catch (Exception ex)
			{
				LoggerService.Error(this, "Failed to drop", "Design Error", ex);
			}

		}
		

		private void ListScript_DragEnter(DragEventArgs e)
		{
			if (!e.Data.GetDataPresent(ParametersViewModel.DragDropFormat))
			{
				e.Effects = DragDropEffects.None;
			}
		}

		#endregion Drop

		private void ChangeParam()
		{
			if(SelectedParameter == null)
				return;

			if (!(SelectedErrorItem is InvalidScriptItemData_ParamDontExist paramDontExist))
				return;

			if (paramDontExist.Parameter == null)
				return;

			MCU_ParamData mcuParamDontExist = paramDontExist.Parameter as MCU_ParamData;

			foreach (DesignScriptViewModel script in Project.ScriptsList)
			{
				foreach(ScriptNodeBase node in script.CurrentScript.ScriptItemsList)
				{
					if(node is IScriptStepWithParameter withParam)
					{
						if(withParam.Parameter is MCU_ParamData mcuParam && mcuParamDontExist != null)
						{
							if (mcuParamDontExist.Cmd == mcuParam.Cmd)
							{
								withParam.Parameter = SelectedParameter;
							}
						}
						else if(paramDontExist.Parameter.Name == withParam.Parameter.Name)
						{
							withParam.Parameter = SelectedParameter;
						}
					}
				}

				script.Save();
			}

			ReloadEvent?.Invoke();
		}

		#endregion Methods

		#region Commands

		public RelayCommand ChangeParamCommand { get; private set; }

		private RelayCommand<MouseButtonEventArgs> _ErrorList_MouseDownCommand;
		public RelayCommand<MouseButtonEventArgs> ErrorList_MouseDownCommand
		{
			get
			{
				return _ErrorList_MouseDownCommand ?? (_ErrorList_MouseDownCommand =
					new RelayCommand<MouseButtonEventArgs>(ErrorList_MouseDown));
			}
		}



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

		#region Events

		public event Action ReloadEvent;

		#endregion Events
	}
}
