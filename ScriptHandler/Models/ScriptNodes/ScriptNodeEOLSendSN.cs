
using System.IO;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLSendSN : ScriptNodeBase
	{

		public override string Description 
		{ 
			get
			{
				string description = "Send SN";
				return description + " - ID:" + ID;
			}
		}

		public ScriptNodeEOLSendSN() 
		{
			Name = "EOL Send SN";
		}
	}
}
