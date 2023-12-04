
using DeviceCommunicators.General;
using DeviceCommunicators.MCU;
using DeviceHandler.Models;
using Entities.Enums;
using Entities.Models;
using ScriptHandler.Interfaces;
using ScriptHandler.Models;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.ViewModels;
using Services.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ScriptHandler.Services
{
    public class GenerateProjectService
    {
        public GeneratedProjectData Generate(
            ProjectData projectData,
			InvalidScriptData invalidScriptData,
			DevicesContainer devicesContainer)
        {
            GeneratedProjectData generatedProject = new GeneratedProjectData()
            {
                Name = projectData.Name,
                TestsList = new ObservableCollection<GeneratedScriptData>(),
                //StopScriptStep = new StopScriptStepService(),
            };

			List<DeviceCommunicator> usedCommunicatorsList = new List<DeviceCommunicator>();

            IsTestValidService isTestValidService = new IsTestValidService();
			
			foreach (DesignScriptViewModel testVM in projectData.ScriptsList)
            {
                if (!(testVM.CurrentScript is TestData testData))
                    continue;

                

                isTestValidService.IsValid(testData, invalidScriptData, devicesContainer);

				GeneratedTestData generatedScript = GenerateScript(
                    testVM.CurrentScript.ScriptPath,
                    testVM.CurrentScript,
                    devicesContainer,
                    ref usedCommunicatorsList) as GeneratedTestData;

                testVM.IsChanged = false;
				generatedProject.TestsList.Add(generatedScript);
			}

            return generatedProject;
        }




        #region Methods

        public GeneratedTestData GenerateScript(
            string scriptPath,
            IScript scriptData,
            DevicesContainer devicesContainer,
			ref List<DeviceCommunicator> usedCommunicatorsList)
        {

			GeneratedTestData runnerScript = new GeneratedTestData()
            {
                Name = scriptData.Name,
                IsPass = null,
                State = Enums.SciptStateEnum.None,
                ScriptPath = scriptPath,
            };


            runnerScript.ScriptItemsList = new ObservableCollection<IScriptItem>();
            Dictionary<int, ScriptStepBase> stepNameToObject = new Dictionary<int, ScriptStepBase>();
            Dictionary<int, ScriptNodeBase> nodeNameToObject = new Dictionary<int, ScriptNodeBase>();
            foreach (ScriptNodeBase node in scriptData.ScriptItemsList)
            {
                
                ScriptStepBase scriptStep = ScriptStepsFactoryService.Factory(node);
                if (scriptStep == null)
                    continue;

                scriptStep.Name = node.Name;

                if (scriptStep is ScriptStepSubScript)
                {
                    HandleSubScript(
                        scriptPath,
                        node as ScriptNodeSubScript,
                        scriptStep as ScriptStepSubScript,
						devicesContainer,
						ref usedCommunicatorsList);
                }

                if (node is IScriptNodeWithParam withParam &&
                    withParam.Parameter != null &&
					devicesContainer.TypeToDevicesFullData.ContainsKey(withParam.Parameter.DeviceType))
                {
                    DeviceData deviceData =
                        devicesContainer.TypeToDevicesFullData[withParam.Parameter.DeviceType].Device;
                    if (deviceData != null)
                    {
                        if (withParam.Parameter.Device == null)
                            withParam.Parameter.Device = deviceData;
                        DeviceParameterData data = deviceData.ParemetersList.ToList().Find((p) => p.Name == withParam.Parameter.Name);
                        if (data != null)
                            withParam.Parameter = data;
                    }
                }

				scriptStep.Description = node.Description;
				scriptStep.ID = node.ID;

				runnerScript.ScriptItemsList.Add(scriptStep);
                stepNameToObject.Add(node.ID, scriptStep);
                nodeNameToObject.Add(node.ID, node);
            }


			BuildSteps(
                stepNameToObject,
                nodeNameToObject,
                ref usedCommunicatorsList,
                devicesContainer);

            return runnerScript;
        }

        private void BuildSteps(
            Dictionary<int, ScriptStepBase> stepNameToObject,
            Dictionary<int, ScriptNodeBase> nodeNameToObject,
            ref List<DeviceCommunicator> usedCommunicatorsList,
            DevicesContainer devicesContainer)
        {
            
            foreach (int id in stepNameToObject.Keys)
            {
                ScriptStepBase scriptStep = stepNameToObject[id];
                ScriptNodeBase scriptNode = nodeNameToObject[id];


                if (scriptNode.PassNextId >= 0)
                {
                    if (stepNameToObject.ContainsKey(scriptNode.PassNextId))
                    {
                        scriptStep.PassNext = stepNameToObject[scriptNode.PassNextId];
                        scriptStep.PassNextDescription = scriptStep.PassNext.Description;

					}
                }

                if (scriptNode.FailNextId >= 0)
                {
                    if (stepNameToObject.ContainsKey(scriptNode.FailNextId))
                    {
                        scriptStep.FailNext = stepNameToObject[scriptNode.FailNextId];
                        scriptStep.FailNextDescription = scriptStep.FailNext.Description;
                    }
                }

                scriptStep.Generate(
                    scriptNode,
					stepNameToObject,
					ref usedCommunicatorsList,
			        this,
			        devicesContainer);

				SetCommunicator(
                    scriptStep,
                    devicesContainer,
                    usedCommunicatorsList);
            }

		}



        private void SetCommunicator(
            ScriptStepBase scriptStep,
            DevicesContainer devicesContainer,
            List<DeviceCommunicator> usedCommunicatorsList)
        {
            DeviceCommunicator communicator = null;
            if (scriptStep is IScriptStepWithParameter withParameter)
            {
                if (withParameter.Parameter != null && 
                    devicesContainer.TypeToDevicesFullData.ContainsKey(withParameter.Parameter.DeviceType))
                {
                    DeviceFullData deviceFullData =
                        devicesContainer.TypeToDevicesFullData[withParameter.Parameter.DeviceType];
                    communicator = deviceFullData.DeviceCommunicator;
                }
            }

            if (communicator != null && usedCommunicatorsList.IndexOf(communicator) < 0)
                usedCommunicatorsList.Add(communicator);

            if (scriptStep is IScriptStepWithCommunicator withCommunicator && communicator != null)
                withCommunicator.Communicator = communicator;

        }

        private void HandleSubScript(
            string scriptPath,
            ScriptNodeSubScript subScriptNode,
            ScriptStepSubScript subScript,
            DevicesContainer devicesContainer,
			ref List<DeviceCommunicator> usedCommunicatorsList)
        {
            bool isNodeSet = subScriptNode.IsNotSet(
                devicesContainer, 
                null);
            if (isNodeSet) 
            {
                return;
            }


			subScript.Script = GenerateScript(
                scriptPath,
                subScriptNode.Script,
                devicesContainer,
				ref usedCommunicatorsList);
        }

        #endregion Methods
    }
}
