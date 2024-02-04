
using System.IO;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLCalibrate : ScriptNodeBase
	{

		public override string Description 
		{ 
			get
			{
				string description = "Calibrate";
				return description + " - ID:" + ID;
			}
		}

		public ScriptNodeEOLCalibrate() 
		{
			Name = "EOL Clibrate";
		}
	}
}
