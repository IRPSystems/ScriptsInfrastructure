﻿
using DeviceCommunicators.EvvaDevice;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Models.ScriptSteps;

namespace ScriptHandler.Services
{
	public class ScriptStepsFactoryService
	{
		public static ScriptStepBase Factory(ScriptNodeBase node)
		{
			if (node.GetType().Name == "ScriptNodeCompare")
				return new ScriptStepCompare();
			else if (node.GetType().Name == "ScriptNodeDelay")
				return new ScriptStepDelay();
			else if (node.GetType().Name == "ScriptNodeDynamicControl")
				return new ScriptStepDynamicControl();
			else if (node.GetType().Name == "ScriptNodeSweep")
				return new ScriptStepSweep();
			else if (node.GetType().Name == "ScriptNodeSetParameter")
			{
				if(node is ScriptNodeSetParameter setParameter &&
					setParameter.Parameter is Evva_ParamData)
				{
					var step = new ScriptStepStartStopSaftyOfficer()
					{
						SafetyOfficerErrorLevel = setParameter.SafetyOfficerErrorLevel
					};
					return step;
				}
				else
					return new ScriptStepSetParameter();
			}
			else if (node.GetType().Name == "ScriptNodeSetSaveParameter")
				return new ScriptStepSetSaveParameter();
            else if (node.GetType().Name == "ScriptNodeSaveParameter")
                return new ScriptStepSaveParameter();
            else if (node.GetType().Name == "ScriptNodeNotification")
				return new ScriptStepNotification();
			else if (node.GetType().Name == "ScriptNodeSubScript")
				return new ScriptStepSubScript();
			else if (node.GetType().Name == "ScriptNodeSelectMotorType")
				return new ScriptStepSelectMotorType();
			else if (node.GetType().Name == "ScriptNodeIncrementValue")
				return new ScriptStepIncrementValue();
            else if (node.GetType().Name == "ScriptNodeLoopIncrement")
                return new ScriptStepLoopIncrement();
            else if (node.GetType().Name == "ScriptNodeConverge")
				return new ScriptStepConverge();
			else if (node.GetType().Name == "ScriptNodeCompareRange")
				return new ScriptStepCompareRange();
			else if (node.GetType().Name == "ScriptNodeCompareWithTolerance")
				return new ScriptStepCompareWithTolerance();
			else if (node.GetType().Name == "ScriptNodeCANMessage")
				return new ScriptStepCANMessage();
			else if (node.GetType().Name == "ScriptNodeCANMessageUpdate")
				return new ScriptStepCANMessageUpdate();
			else if (node.GetType().Name == "ScriptNodeCANMessageStop")
				return new ScriptStepCANMessageStop();
			else if (node.GetType().Name == "ScriptNodeStopContinuous")
				return new ScriptStepStopContinuous();
			else if (node.GetType().Name == "ScriptNodeEOLFlash")
				return new ScriptStepEOLFlash();
			else if (node.GetType().Name == "ScriptNodeEOLCalibrate")
				return new ScriptStepEOLCalibrate();
			else if (node.GetType().Name == "ScriptNodeEOLSendSN")
				return new ScriptStepEOLSendSN();
			else if (node.GetType().Name == "ScriptNodeResetParentSweep")
				return new ScriptStepResetParentSweep();
			else if (node.GetType().Name == "ScriptNodeScopeSave")
				return new ScriptStepScopeSave();
			else if (node.GetType().Name == "ScriptNodeEOLPrint")
				return new ScriptStepEOLPrint();
			else if (node.GetType().Name == "ScriptNodeCompareBit")
				return new ScriptStepCompareBit();
            else if (node.GetType().Name == "ScriptNodeGetRegisterValues")
                return new ScriptStepGetRegisterValues();

            return null;

		}
	}
}
