using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Newtonsoft.Json.Linq;
using ScriptHandler.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptHandler.Models.ScriptNodes
{
    public class ScriptNodeSaveParameter : ScriptNodeBase, IScriptStepWithParameter
    {
        public ScriptNodeSaveParameter()
        {
            Description = Name = "Save Parameter";
        }

        private DeviceParameterData _parameter;
        public DeviceParameterData Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value;
                OnPropertyChanged("Parameter");
            }
        }

        public override string Description
        {
            get
            {
                string stepDescription = "Save ";
                if (_parameter is DeviceParameterData deviceParameter)
                {
                    stepDescription += " \"" + deviceParameter + "\"";
                }

                stepDescription += " - ID:" + ID;
                return stepDescription;

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
    }
}
