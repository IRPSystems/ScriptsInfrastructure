
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.DBC;
using DeviceCommunicators.Enums;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NI_6002;
using DeviceCommunicators.NumatoGPIO;
using DeviceCommunicators.RigolM300;
using DeviceCommunicators.ZimmerPowerMeter;
using DeviceCommunicators.MX180TP;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using Syncfusion.Windows.Shared;
using System;

namespace ScriptHandler.Models
{
	public class ExtraDataForParameter: ObservableObject, ICloneable
	{


		[JsonIgnore]
		public DeviceParameterData Parameter { get; set; }
		public int Rigol_Channel { get; set; }
        public int Rigol_Slot { get; set; }
		public int MX180TP_Channel { get; set; }
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



		public int DBCInterval { get; set; }
		public TimeUnitsEnum DBCIntervalUnite { get; set; }

		public ExtraDataForParameter()
		{
			DBCIntervalUnite = TimeUnitsEnum.ms;
		}

		public ExtraDataForParameter(ExtraDataForParameter source) 
		{
			DBCIntervalUnite = TimeUnitsEnum.ms;

			Ni6002_IOPort = source.Ni6002_IOPort;
			Ni6002_Line = source.Ni6002_Line;
			NIDAQShuntResistor = source.NIDAQShuntResistor;
			AteCommand = source.AteCommand;
			Zimmer_Channel = source.Zimmer_Channel;
			NumatoGPIOPort = source.NumatoGPIOPort;
            NIThermistorIndex = source.NIThermistorIndex;
			NI6002_NumofCounts = source.NI6002_NumofCounts;
			Ni6002_ExpectedRPM = source.Ni6002_ExpectedRPM;
			DBCInterval = source.DBCInterval;
			DBCIntervalUnite = source.DBCIntervalUnite;
            Rigol_Channel = source.Rigol_Channel;
            Rigol_Slot = source.Rigol_Slot;
            MX180TP_Channel = source.MX180TP_Channel;
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
				ni.ExpectedRPM = Ni6002_ExpectedRPM;

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
			else if (parameter is DBC_ParamData dbc)
			{
				dbc.Interval = DBCInterval;
				dbc.IntervalUnite = DBCIntervalUnite.ToString();
			}
			else if (parameter is RigolM300_ParamData rigol)
			{
				rigol.Channel = Rigol_Channel;
				rigol.Slot = Rigol_Slot;
			}
            else if (parameter is MX180TP_ParamData mx)
            {
                mx.Channel = MX180TP_Channel;
            }
        }

		public object Clone()
		{
			return MemberwiseClone();
		}
	}
}
