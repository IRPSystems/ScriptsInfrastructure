
using System.IO;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeFlash: ScriptNodeBase
	{
		public string FilePath { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Flash - " + Path.GetFileName(FilePath);
				return description + " - ID:" + ID;
			}
		}

		public ScriptNodeFlash() 
		{
			Name = "Flash";
		}
	}
}
