

using CommunityToolkit.Mvvm.ComponentModel;

namespace ScriptHandler.Models
{
	public class EOLReportsSelectionData: ObservableObject
	{
		public bool IsSaveToExcel { get; set; }
		public bool IsSaveToPdf { get; set; }
	}
}
