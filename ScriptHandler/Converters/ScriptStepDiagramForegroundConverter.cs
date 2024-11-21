
using System.Globalization;
using System.Windows.Data;
using System;
using System.Windows.Media;
using System.Windows;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Models.ScriptSteps;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes.ReleaseTasks;
using ScriptHandler.Models.ScriptStep.ReleaseTasks;

namespace ScriptHandler.Converter
{
	public class ScriptStepDiagramForegroundConverter : IValueConverter
	{

		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if(value is ScriptNodeReleaseTasks)
			{
				object ret = GetReleaseTasks(value);
				return ret;
			}

			if (value is ScriptNodeSetParameter || value is ScriptStepSetParameter)
				return Brushes.White;
            if (value is ScriptNodeSaveParameter || value is ScriptStepSaveParameter)
                return Brushes.White;
            if (value is ScriptStepStartStopSaftyOfficer)
				return Brushes.White;
			if (value is ScriptNodeSetSaveParameter || value is ScriptStepSetSaveParameter)
				return Brushes.White;
			if (value is ScriptNodeScopeSave || value is ScriptStepScopeSave)
				return Brushes.Black;
			if (value is ScriptNodeDelay || value is ScriptStepDelay)
				return Brushes.Black;
			if (value is ScriptNodeDynamicControl || value is ScriptStepDynamicControl)
				return Brushes.Black;
			if (value is ScriptNodeCANMessage || value is ScriptStepCANMessage)
				return Brushes.White;
			if (value is ScriptNodeSweep || value is ScriptStepSweep)
				return Brushes.White;
			if (value is ScriptNodeCompareRange || value is ScriptStepCompareRange)
				return Brushes.White;
			if(value is ScriptNodeCompareWithTolerance || value is ScriptStepCompareWithTolerance)
				return Brushes.Black;
			if (value is ScriptNodeCompare || value is ScriptStepCompare)
				return Brushes.Black;
			if (value is ScriptNodeResetParentSweep || value is ScriptStepResetParentSweep)
				return Brushes.Black;

			if (value is ScriptNodeStopContinuous || value is ScriptStepStopContinuous)
				return Brushes.Black;
			if (value is ScriptNodeCANMessageStop || value is ScriptStepCANMessageStop)
				return Brushes.Black;

			if (value is ScriptNodeCANMessageUpdate || value is ScriptStepCANMessageUpdate)
				return Brushes.White;

			if (value is ScriptNodeNotification || value is ScriptStepNotification)
				return Brushes.Black;
			if (value is ISubScript subScript)
			{
				if (subScript.Script == null)
					return Brushes.White;
				else
					return Brushes.Black;
			}
			if (value is ScriptNodeIncrementValue || value is ScriptStepIncrementValue)
				return Brushes.White;
            if (value is ScriptNodeLoopIncrement || value is ScriptStepLoopIncrement)
                return Brushes.White;
            if (value is ScriptNodeConverge || value is ScriptStepConverge)
				return Brushes.White;
			if (value is ScriptNodeEOLFlash || value is ScriptStepEOLFlash)
				return Brushes.Black;
			if (value is ScriptNodeEOLCalibrate || value is ScriptStepEOLCalibrate)
				return Brushes.Black;
			if (value is ScriptNodeEOLSendSN || value is ScriptStepEOLSendSN)
				return Brushes.Black;
			if (value is ScriptNodeEOLPrint || value is ScriptStepEOLPrint)
				return Brushes.White;
			if (value is ScriptNodeCompareBit || value is ScriptStepCompareBit)
				return Brushes.White;

			return Brushes.White;

		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}

		private object GetReleaseTasks(object value)
		{
			if(value is ScriptNodeBuildVersion || value is ScriptStepBuildVersion)
				return Brushes.White;
			if (value is ScriptNodeCopyFiles || value is ScriptStepCopyFiles)
				return Brushes.White;
			if (value is ScriptNodeCreateReleaseNotes || value is ScriptStepCreateReleaseNotes)
				return Brushes.Black;
			if (value is ScriptNodeGitCommands || value is ScriptStepGitCommands)
				return Brushes.Black;
			if (value is ScriptNodeLinkToJira || value is ScriptStepLinkToJira)
				return Brushes.Black;
			if (value is ScriptNodeLogicConstrains || value is ScriptStepLogicConstrains)
				return Brushes.Black;
			if (value is ScriptNodeMailToOutlook || value is ScriptStepMailToOutlook)
				return Brushes.Black;
			if (value is ScriptNodeOpenUrls || value is ScriptStepOpenUrls)
				return Brushes.Black;
			if (value is ScriptNodeRenameFiles || value is ScriptStepRenameFiles)
				return Brushes.White;
			if (value is ScriptNodeUploadToGithub || value is ScriptStepUploadToGithub)
				return Brushes.White;
			if (value is ScriptNodeUseExeFiles || value is ScriptStepUseExeFiles)
				return Brushes.White;
			if (value is ScriptNodeZipFiles || value is ScriptStepZipFiles)
				return Brushes.White;
			return null;
		}
	}
}
