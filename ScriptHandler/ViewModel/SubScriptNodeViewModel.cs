
using Syncfusion.UI.Xaml.Diagram;
using System.Collections.Generic;

namespace ScriptHandler.ViewModel
{
	public class SubScriptNodeViewModel: NodeViewModelEx
	{
		public List<NodeViewModel> SubNodesList { get; set; }
		public List<ConnectorViewModelEx> SubConnectorsList { get; set; }

		public double SubScriptHeight { get; set; }
	}
}
