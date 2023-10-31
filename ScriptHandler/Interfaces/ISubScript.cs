
namespace ScriptHandler.Interfaces
{
	public interface ISubScript: IScriptItem
	{
		IScript Script { get; set; }
		string SelectedScriptName { get; set; }
	}
}
