
using System.ComponentModel;

namespace ScriptHandler.Enums
{
	public enum ComparationTypesEnum
	{
		[Description("=")]
		Equal,
		[Description("!=")]
		NotEqual,
		[Description(">")]
		Larger,
		[Description(">=")]
		LargerEqual,
		[Description("<")]
		Smaller,
		[Description("<=")]
		SmallerEqual,
	}
}
