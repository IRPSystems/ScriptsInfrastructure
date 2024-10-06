
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.NumatoGPIO;
using DeviceCommunicators.ZimmerPowerMeter;

namespace ScriptHandler.Models
{
	public class ExtraDataForParameter: ObservableObject
	{


		public DeviceParameterData Parameter { get; set; }


		public int Ni6002_IOPort { get; set; }
		public int Ni6002_Line { get; set; }
		public double NIDAQShuntResistor { get; set; }


		private int _ateCommandDropDwonIndex;
		public int AteCommandDropDwonIndex
		{
			get => _ateCommandDropDwonIndex;
			set
			{
				if (!(Parameter is ATE_ParamData ate))
					return;

				_ateCommandDropDwonIndex = value;
			}
		}

		public int Zimmer_Channel { get; set; }

		private int _numatoGPIODropDwonIndex;
		public int NumatoGPIODropDwonIndex
		{
			get => _numatoGPIODropDwonIndex;
			set
			{
				if (!(Parameter is NumatoGPIO_ParamData numato))
					return;

				if (numato.DropDown == null)
					return;

				_numatoGPIODropDwonIndex = value;
			}
		}

		public ExtraDataForParameter() { }
		public ExtraDataForParameter(ExtraDataForParameter source) 
		{
			Ni6002_IOPort = source.Ni6002_IOPort;
			Ni6002_Line = source.Ni6002_Line;
			NIDAQShuntResistor = source.NIDAQShuntResistor;
			AteCommandDropDwonIndex = source.AteCommandDropDwonIndex;
			Zimmer_Channel = source.Zimmer_Channel;
			NumatoGPIODropDwonIndex = source.NumatoGPIODropDwonIndex;
		}

		public void SetToParameter(DeviceParameterData parameter)
		{
			if (parameter is NI6002_ParamData ni)
			{
				ni.Io_port = Ni6002_IOPort;
				ni.portLine = Ni6002_Line;
				ni.shunt_resistor = NIDAQShuntResistor;
			}
			else if (parameter is NumatoGPIO_ParamData numato)
			{
				numato.Io_port = NumatoGPIODropDwonIndex;
			}
			else if (parameter is ZimmerPowerMeter_ParamData zimmer)
			{
				zimmer.Channel = Zimmer_Channel;
			}
			else if (parameter is ATE_ParamData ate)
			{
				ate.Value = AteCommandDropDwonIndex;
			}
		}
	}
}
