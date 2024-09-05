using DeviceHandler.Models;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLPrint : ScriptNodeBase
	{
		public ScriptNodeEOLPrint()
		{
			Name = "EOL Print";
		}

		public string PartNumber { get; set; }
		public string CustomerPartNumber { get; set; }
		public string Spec { get; set; }
		public string HW_Version { get; set; }
		public string MCU_Version { get; set; }


		public override string Description
		{
			get
			{
				return "EOL Print - ID:" + ID;
			}
		}
		public override void PostLoad(
			DevicesContainer devicesContainer,
			IScript currentScript)
		{
			
		}


		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			

			return false;
		}
	}
}
