

using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCompareBit : ScriptNodeBase, IScriptStepWithParameter
	{
		#region Properties and Fields

		public DeviceParameterData Parameter { get; set; }

		public int BitIndex { get; set; }

		private int _bitSelectedIndex;
		public int BitSelectedIndex 
		{
			get => _bitSelectedIndex;
			set
			{
				if (!(Parameter is IParamWithDropDown dropDown))
					return;

				if (dropDown.DropDown == null)
					return;

				_bitSelectedIndex = value;

				BitIndex = value;
			}
		}

		public override string Description 
		{
			get
			{
				string desc = "Compare BIT ";
				if (Parameter != null)
					desc += $"\"{Parameter.Name}\" ";
				desc += "- ID:" + ID;
				return desc;
			}
		}

		#endregion Properties and Fields

		#region Constructor

		public ScriptNodeCompareBit()
		{
			Name = "Compare Bit";
		}

		#endregion Constructor

		#region Methods

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}

		#endregion Methods
	}

}
