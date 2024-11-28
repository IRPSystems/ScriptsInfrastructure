using DeviceCommunicators.Models;
using DeviceHandler.Models;
using Entities.Models;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;

namespace ScriptHandler.Models.ScriptNodes
{
    public class ScriptNodeLoopIncrement : ScriptNodeBase, IScriptStepWithParameter
    {
        private DeviceParameterData _parameter;
        public DeviceParameterData Parameter
        {
            get => _parameter;
            set
            {
                _parameter = value;
                ExtraData.Parameter = _parameter;
                OnPropertyChanged("Parameter");
            }
        }

        public double IncrementValue { get; set; }
        public double SetFirstValue { get; set; }
        public int LoopsAmount { get; set; }
        public int Interval { get; set; }
        public TimeUnitsEnum IntervalUnite { get; set; }

		public ExtraDataForParameter ExtraData { get; set; }


		public ScriptNodeLoopIncrement()
        {
            Name = "Loop Increment";
			ExtraData = new ExtraDataForParameter();
		}



        public override string Description
        {
            get
            {
                string stepDescription = "Loop Increment ";
                if (_parameter is DeviceParameterData deviceParameter)
                {
                    stepDescription += " \"" + deviceParameter + "\"";
                }

                stepDescription += " By " + IncrementValue;
                stepDescription += " For " + LoopsAmount + " Times";

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

		public override object Clone()
		{
			ScriptNodeCompareWithTolerance compare = MemberwiseClone() as
				ScriptNodeCompareWithTolerance;

			compare.CompareValue_ExtraData = this.ExtraData.Clone()
				as ExtraDataForParameter;

			return compare;
		}
	}
}
