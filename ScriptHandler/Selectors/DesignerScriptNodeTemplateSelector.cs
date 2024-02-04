
using ScriptHandler.Models.ScriptNodes;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.Selectors
{
	public class DesignerScriptNodeTemplateSelector: DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElement element = container as FrameworkElement;

			if(item is ScriptNodeSetParameter)
				return element.FindResource("Design_SetParameterTemplate") as DataTemplate;
			if (item is ScriptNodeSetSaveParameter)
				return element.FindResource("Design_SetSaveParameterTemplate") as DataTemplate;
			if (item is ScriptNodeDelay)
				return element.FindResource("Design_DelayTemplate") as DataTemplate;
			if (item is ScriptNodeResetParentSweep)
				return element.FindResource("Design_ResetParentSweepTemplate") as DataTemplate;
			if (item is ScriptNodeDynamicControl)
				return element.FindResource("Design_DynamicControlTemplate") as DataTemplate;
			if (item is ScriptNodeSweep)
				return element.FindResource("Design_SweepTemplate") as DataTemplate;
			if (item is ScriptNodeCompare)
				return element.FindResource("Design_CompareTemplate") as DataTemplate;
			if (item is ScriptNodeNotification)
				return element.FindResource("Design_NotificationTemplate") as DataTemplate;
			if (item is ScriptNodeSubScript)
				return element.FindResource("Design_SubScriptTemplate") as DataTemplate;
			if (item is ScriptNodeIncrementValue)
				return element.FindResource("Design_IncrementValueTemplate") as DataTemplate;
			if (item is ScriptNodeConverge)
				return element.FindResource("Design_ConvergeTemplate") as DataTemplate;
			if (item is ScriptNodeCompareRange)
				return element.FindResource("Design_CompareRangeTemplate") as DataTemplate;
			if (item is ScriptNodeStopContinuous)
				return element.FindResource("Design_StopContinuousTemplate") as DataTemplate;
			if (item is ScriptNodeCANMessageUpdate)
				return element.FindResource("Design_CANMessageUpdateTemplate") as DataTemplate;
			if (item is ScriptNodeCANMessage)
				return element.FindResource("Design_CANMessageTemplate") as DataTemplate;
			if (item is ScriptNodeEOLFlash)
				return element.FindResource("Design_FlashTemplate") as DataTemplate;

			return null;
		}
	}
}
