﻿
using Newtonsoft.Json.Linq;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Models.ScriptNodes.ReleaseTasks;
using System.Windows;
using System.Windows.Controls;

namespace ScriptHandler.Selectors
{
	public class DesignerScriptNodeTemplateSelector: DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			FrameworkElement element = container as FrameworkElement;

			if (element is ScriptNodeReleaseTasks)
			{
				object ret = GetReleaseTasks(element);
				return ret as DataTemplate;
			}

			if (item is ScriptNodeSetParameter)
				return element.FindResource("Design_SetParameterTemplate") as DataTemplate;
			if (item is ScriptNodeSetSaveParameter)
				return element.FindResource("Design_SetSaveParameterTemplate") as DataTemplate;
            if (item is ScriptNodeSaveParameter)
                return element.FindResource("Design_SaveParameterTemplate") as DataTemplate;
            if (item is ScriptNodeDelay)
				return element.FindResource("Design_DelayTemplate") as DataTemplate;
			if (item is ScriptNodeScopeSave)
				return element.FindResource("Design_ScopeSaveTemplate") as DataTemplate;
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
            if (item is ScriptNodeLoopIncrement)
                return element.FindResource("Design_LoopIncrementTemplate") as DataTemplate;
            if (item is ScriptNodeConverge)
				return element.FindResource("Design_ConvergeTemplate") as DataTemplate;
			if (item is ScriptNodeCompareRange)
				return element.FindResource("Design_CompareRangeTemplate") as DataTemplate;
			if (item is ScriptNodeCompareWithTolerance)
				return element.FindResource("Design_CompareWithToleranceTemplate") as DataTemplate;
			if (item is ScriptNodeCANMessageStop)
				return element.FindResource("Design_StopCANMessagTemplate") as DataTemplate;
			if (item is ScriptNodeCANMessageUpdate)
				return element.FindResource("Design_CANMessageUpdateTemplate") as DataTemplate;
			if (item is ScriptNodeCANMessage)
				return element.FindResource("Design_CANMessageTemplate") as DataTemplate;
			if (item is ScriptNodeEOLFlash)
				return element.FindResource("Design_EOLFlashTemplate") as DataTemplate;
			if (item is ScriptNodeEOLCalibrate)
				return element.FindResource("Design_EOLCalibrateTemplate") as DataTemplate;
			if (item is ScriptNodeEOLSendSN)
				return element.FindResource("Design_EOLSendSNTemplate") as DataTemplate;
			if (item is ScriptNodeEOLPrint)
				return element.FindResource("Design_EOLPrintTemplate") as DataTemplate;
			if (item is ScriptNodeCompareBit)
				return element.FindResource("Design_CompareBitTemplate") as DataTemplate;
			if (item is ScriptNodeStopContinuous)
				return element.FindResource("Design_StopContinuousTemplate") as DataTemplate;

			return null;
		}

		private object GetReleaseTasks(object value)
		{
			if (value is ScriptNodeBuildVersion)
				return null;
			if (value is ScriptNodeCopyFiles)
				return null;
			if (value is ScriptNodeCreateReleaseNotes)
				return null;
			if (value is ScriptNodeGitCommands)
				return null;
			if (value is ScriptNodeLinkToJira)
				return null;
			if (value is ScriptNodeLogicConstrains)
				return null;
			if (value is ScriptNodeMailToOutlook)
				return null;
			if (value is ScriptNodeOpenUrls)
				return null;
			if (value is ScriptNodeReleaseTasks)
				return null;
			if (value is ScriptNodeRenameFiles)
				return null;
			if (value is ScriptNodeUploadToGithub)
				return null;
			if (value is ScriptNodeUseExeFiles)
				return null;
			if (value is ScriptNodeZipFiles)
				return null;
			return null;
		}
	}
}
