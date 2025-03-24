

using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeGetRegisterValues : ScriptNodeBase, IScriptStepWithParameter
	{
		#region Properties and Fields

		public DeviceParameterData Parameter { get; set; }
		public int ComparedValue { get; set; }

		public override string Description 
		{
			get
			{
				string desc = "Get Register Values ";
				if (Parameter != null)
					desc += $"\"{Parameter.Name}\" ";
				desc += "to Value - ID:" + ID;
				return desc;
			}
		}

		#endregion Properties and Fields

		#region Constructor

		public ScriptNodeGetRegisterValues()
		{
			Name = "Get Register Values";
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
