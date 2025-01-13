
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.Enums;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.NumatoGPIO;
using DeviceCommunicators.ZimmerPowerMeter;
using Entities.Models;
using Syncfusion.Windows.Shared;
using System;

namespace ScriptHandler.Models
{
	public class ExtraDataForParameter: ObservableObject, ICloneable
	{


		public DeviceParameterData Parameter { get; set; }

        public int Ni6002_ExpectedRPM { get; set; }
        public int Ni6002_IOPort { get; set; }
		public int Ni6002_Line { get; set; }
		public int NI6002_NumofCounts { get; set; }
		public double NIDAQShuntResistor { get; set; }
		public int AteCommand { get; set; }
		public eThermistorType NIThermistorIndex { get; set; }


		private int _ateCommandDropDwonIndex;
		public int AteCommandDropDwonIndex
		{
			get => _ateCommandDropDwonIndex;
			set
			{
				_ateCommandDropDwonIndex = value;

				if (!(Parameter is ATE_ParamData ate))
					return;

				if (ate.ATECommand == null || (value < 0 || value >= ate.ATECommand.Count))
				{
                    AteCommand = -1;
					return;
                }

                DropDownParamData dd = ate.ATECommand[value];

				int intVal;
				bool ret = int.TryParse(dd.Value, out intVal);
				AteCommand = intVal;
			}
		}

		public int Zimmer_Channel { get; set; }
		
		public int NumatoGPIOPort { get; set; }

		private int _numatoGPIODropDwonIndex;
		public int NumatoGPIODropDwonIndex
		{
			get => _numatoGPIODropDwonIndex;
			set
			{
				_numatoGPIODropDwonIndex = value;

				if (!(Parameter is NumatoGPIO_ParamData numato))
					return;

				if (numato.DropDown == null)
					return;

				if (value < 0 || value >= numato.DropDown.Count)
					NumatoGPIOPort = -1;

				DropDownParamData dd = numato.DropDown[value];

				int intVal;
				bool ret = int.TryParse(dd.Value, out intVal);
				NumatoGPIOPort = intVal;
			}
		}

		public ExtraDataForParameter() { }
		public ExtraDataForParameter(ExtraDataForParameter source) 
		{
			Ni6002_IOPort = source.Ni6002_IOPort;
			Ni6002_Line = source.Ni6002_Line;
			NIDAQShuntResistor = source.NIDAQShuntResistor;
			AteCommand = source.AteCommand;
			Zimmer_Channel = source.Zimmer_Channel;
			NumatoGPIOPort = source.NumatoGPIOPort;
            NIThermistorIndex = source.NIThermistorIndex;
			NI6002_NumofCounts = source.NI6002_NumofCounts;
			Ni6002_ExpectedRPM = source.Ni6002_ExpectedRPM;
        }

		public void SetToParameter(DeviceParameterData parameter)
		{
			if (parameter is NI6002_ParamData ni)
			{
				ni.Io_port = Ni6002_IOPort;
				ni.portLine = Ni6002_Line;
				ni.shunt_resistor = NIDAQShuntResistor;
				ni.ThermistorType = NIThermistorIndex;
				ni.numofcounts = NI6002_NumofCounts;
				ni.ExpectedRPM = NI6002_ExpectedRPM;

            }
			else if (parameter is NumatoGPIO_ParamData numato)
			{
				numato.Io_port = NumatoGPIOPort;
			}
			else if (parameter is ZimmerPowerMeter_ParamData zimmer)
			{
				zimmer.Channel = Zimmer_Channel;
			}
			else if (parameter is ATE_ParamData ate)
			{
				ate.Value = AteCommand;
			}
		}

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
