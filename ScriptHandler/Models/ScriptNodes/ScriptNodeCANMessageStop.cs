

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeCANMessageStop : ScriptNodeBase
	{
		public uint CANID { get; set; }

		public override string Description
		{
			get
			{
				return $"Stop message - 0x {CANID.ToString("X")} - ID: {ID}";
			}
		}

		public ScriptNodeCANMessageStop()
		{
			Name = "CAN Message Stop";
		}
	}
}
