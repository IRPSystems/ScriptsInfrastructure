
using CommunityToolkit.Mvvm.Input;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCANMessageUpdate : ScriptNodeCANMessage
	{
		#region Properties

		private IScriptItem _stepToUpdate;
		[JsonIgnore]
		public IScriptItem StepToUpdate
		{
			get => _stepToUpdate;
			set
			{
				if(value == null)
					_stepToUpdate = value;

				if (!(value is ScriptNodeCANMessage canMessage))
					return;

				SetCanMessage(canMessage);

				_stepToUpdate = value;
				if (_stepToUpdate != null)
					StepToUpdateID = canMessage.IDInProject;
				else
					StepToUpdateID = -1;

				


				OnPropertyChanged(nameof(StepToUpdateID));
			}
		}

		private int _stepToUpdateId;
		public int StepToUpdateID
		{
			get
			{
				if (StepToUpdate == null)
					return _stepToUpdateId;
				else
				{
					if(!(StepToUpdate is ScriptNodeCANMessage canMessage))
						return -1;
					
					_stepToUpdateId = canMessage.IDInProject;
					return canMessage.IDInProject;
				}

			}
			set
			{
				_stepToUpdateId = value;
				OnPropertyChanged(nameof(StepToUpdateID));
			}
		}


		public ProjectData ParentProject { get; set; }



		public bool IsChangePayload { get; set; }
		public bool IsChangeInterval { get; set; }


		public override string Description
		{
			get
			{
				return "Update message 0x" + CANID.ToString("X") + "-0x" + Payload.NumericValue.ToString("X") + " - ID:" + ID;
			}
		}


		#endregion Properties

		#region Fields



		#endregion Fields


		#region Constructor

		public ScriptNodeCANMessageUpdate()
		{
			Name = "CAN Message Update";

			IsChangePayload = true;

			SetCommands();
		}

		#endregion Constructor

		#region Methods

		private void DeleteStepToUpdate()
		{
			StepToUpdate = null;
			StepToUpdateID = -1;
		}

		
		public void SetCanMessage(ScriptNodeCANMessage canMessage)
		{
			if (canMessage == null) 
				return;

			CANID = canMessage.CANID;
			IsDBCFile = canMessage.IsDBCFile;
			IsFreeStyle = canMessage.IsFreeStyle;
			DBCFilePath = canMessage.DBCFilePath;

			if (Payload == null)
			{
				Payload = new BitwiseNumberDisplayData(is64Bit: true);
				Payload.NumericValue = canMessage.Payload.NumericValue;
				Payload.PropertyChanged += Payload_PropertyChangedEventHandler;
			}
		}

		public override object Clone()
		{
			ScriptNodeCANMessageUpdate canMessage = base.Clone() as ScriptNodeCANMessageUpdate;
			canMessage.SetCommands();

			return canMessage;
		}

		private void SetCommands()
		{
			DeleteStepToUpdateCommand = new RelayCommand(DeleteStepToUpdate);
		}

		#endregion Methods

		#region Commands

		public RelayCommand DeleteStepToUpdateCommand { get; private set; }

		#endregion Commands
	}
}
