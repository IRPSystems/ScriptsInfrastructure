
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeStopContinuous : ScriptNodeBase
	{
		#region Properties

		private IScriptItem _stepToStop;
		[JsonIgnore]
		public IScriptItem StepToStop
		{
			get => _stepToStop;
			set
			{
				if (value == null)
					_stepToStop = value;

				if (!(value is ScriptNodeCANMessage canMessage))
					return;

				//SetCanMessage(canMessage);

				_stepToStop = value;
				if (_stepToStop != null)
					StepToStopID = canMessage.IDInProject;
				else
					StepToStopID = -1;




				OnPropertyChanged(nameof(StepToStopID));
			}
		}

		private int _stepToStopId;
		public int StepToStopID
		{
			get
			{
				if (StepToStop == null)
					return _stepToStopId;
				else
				{
					if (!(StepToStop is ScriptNodeCANMessage canMessage))
						return -1;

					_stepToStopId = canMessage.IDInProject;
					return canMessage.IDInProject;
				}

			}
			set
			{
				_stepToStopId = value;
				OnPropertyChanged(nameof(StepToStopID));
			}
		}

		public ProjectData ParentProject { get; set; }

		public override string Description
		{
			get
			{
				if(StepToStop != null)
					return "Stop \"" + StepToStop.Description + "\" - ID:" + ID;
				else
					return "Stop \"\" - ID:" + ID;
			}
		}

		#endregion Properties

		#region Constructor

		public ScriptNodeStopContinuous()
		{
			Name = "Stop CAN Message";

			DeleteStepToStopCommand = new RelayCommand(DeleteStepToStop);
		}

		#endregion Constructor

		#region Methods

		private void DeleteStepToStop()
		{
			StepToStop = null;
		}

		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			if (StepToStopID >= 0)
			{
				foreach (ScriptNodeBase node in currentScript.ScriptItemsList)
				{
					if (node.ID == StepToStopID)
					{
						StepToStop = node;
						break;
					}
				}
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (StepToStop == null)
				return true;

			return false;
		}

		#endregion Methods


		#region Commands

		public RelayCommand DeleteStepToStopCommand { get; private set; }

		#endregion Commands
	}
}
