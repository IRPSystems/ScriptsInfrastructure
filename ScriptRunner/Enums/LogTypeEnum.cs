
using System.ComponentModel;

namespace ScriptRunner.Enums
{
	public enum LogTypeEnum
	{
		[Description("Script Data")]
		ScriptData,
		[Description("Step Data")]
		StepData,
		[Description("Pass")]
		Pass,
		[Description("Fail")]
		Fail,
		[Description("None")]
		None
	}
}
