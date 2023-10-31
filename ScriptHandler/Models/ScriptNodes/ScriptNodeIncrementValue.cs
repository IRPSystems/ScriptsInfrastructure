


using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeIncrementValue : ScriptNodeBase, IScriptNodeWithParam
	{
		private DeviceParameterData _parameter;
		public DeviceParameterData Parameter
		{
			get => _parameter;
			set
			{
				_parameter = value;
				OnPropertyChanged("Parameter");
			}
		}

		public double IncrementValue { get; set; }


		public ScriptNodeIncrementValue()
		{
			Name = "Increment Value";
		}

		

		public override string Description
		{
			get
			{
				string stepDescription = "Increment ";
				if (_parameter is DeviceParameterData deviceParameter)
				{
					stepDescription += " \"" + deviceParameter + "\"";
				}

				stepDescription += " By " + IncrementValue;

				stepDescription += " - ID:" + ID;
				return stepDescription;
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;			

			return false;
		}
	}
}
