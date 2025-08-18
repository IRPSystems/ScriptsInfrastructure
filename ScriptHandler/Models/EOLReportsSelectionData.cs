

using CommunityToolkit.Mvvm.ComponentModel;

namespace ScriptHandler.Models
{
	public class EOLReportsSelectionData: ObservableObject
	{
        public bool IsSaveToReport { get; set; }
        public bool IsSaveToPdfExecTable { get; set; }
        public bool IsSaveToPdfDynTable { get; set; }
        public bool IsSaveToCustomerVer { get; set; }
        public bool IsSaveToWats { get; set; }

        public EOLReportsSelectionData() 
        {
			IsSaveToReport = true;
			IsSaveToPdfExecTable = true;
			IsSaveToPdfDynTable = true;
            IsSaveToCustomerVer = false;
            IsSaveToWats = true;

        }
    }
}
