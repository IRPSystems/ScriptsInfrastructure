

using CommunityToolkit.Mvvm.ComponentModel;

namespace ScriptHandler.Models
{
	public class EOLReportsSelectionData: ObservableObject
	{
		public bool IsSaveToReport { get; set; }
		public bool IsSaveToPdfExecTable { get; set; }
        public bool IsSaveToPdfDynTable { get; set; }
    }
}
