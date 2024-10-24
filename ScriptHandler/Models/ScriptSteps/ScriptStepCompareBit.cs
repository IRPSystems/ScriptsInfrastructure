﻿
using DeviceCommunicators.General;
using DeviceCommunicators.Models;
using DeviceHandler.Interfaces;
using DeviceHandler.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Interfaces;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.Services;
using Services.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Windows;

namespace ScriptHandler.Models
{
	public class ScriptStepCompareBit : ScriptStepGetParamValue
	{
		
		public int BitIndex { get; set; }



		public ScriptStepCompareBit()
		{
			if (Application.Current != null)
				Template = Application.Current.MainWindow.FindResource("AutoRunTemplate") as DataTemplate;
		}

		public override void Execute()
		{
			try
			{
				IsPass = false;

				ErrorMessage = Description;

				EOLStepSummeryData eolStepSummeryData;
				bool isOK = SendAndReceive(Parameter, out eolStepSummeryData);
				if (!isOK)
				{
					return;
				}

				int value = 0;
				if (Parameter.Value is string str)
				{
					bool res = int.TryParse(str, out value);
					if (res == false)
					{
						IsPass = false;
						ErrorMessage += "Recived value is not an integer value";
						return;
					}
				}
				else
				{
					bool res = int.TryParse(Parameter.Value.ToString(), out value);
					if (res == false)
					{
						IsPass = false;
						ErrorMessage += "Recived value is not an integer value";
						return;
					}
				}

				uint bit = (uint)((value >> BitIndex) & 1);
				if (bit == 0)
				{
					IsPass = false;

					string bitName = BitIndex.ToString();
					if (Parameter is IParamWithDropDown dropDown)
						bitName = $"\"{dropDown.DropDown[BitIndex].Name}\"";
					ErrorMessage += $"Bit {bitName} is not set";
					return;
				}

				IsPass = true;
			}
			catch(Exception ex)
			{
				LoggerService.Error(this, "Failed to execute CompareBit", ex);
				IsPass = false;
				ErrorMessage += ex.Message;
			}
        }

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Parameter == null)
				return true;

			return false;
		}

		protected override void Generate(
			ScriptNodeBase sourceNode,
			Dictionary<int, ScriptStepBase> stepNameToObject,
			ref List<DeviceCommunicator> usedCommunicatorsList,
			GenerateProjectService generateService,
			DevicesContainer devicesContainer)
		{
			Parameter = (sourceNode as ScriptNodeCompareBit).Parameter;
			BitIndex = (sourceNode as ScriptNodeCompareBit).BitIndex;
		}

		public override void GetRealParamAfterLoad(
			DevicesContainer devicesContainer)
		{
			if (Parameter is ICalculatedParamete)
				return;

			DeviceParameterData parameter = GetRealParam(
				Parameter,
				devicesContainer);
		}
	}
}
