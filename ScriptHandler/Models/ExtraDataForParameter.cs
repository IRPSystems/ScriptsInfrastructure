
using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.MCU;
using DeviceCommunicators.Models;
using DeviceCommunicators.NumatoGPIO;
using System.Reflection.Metadata;

namespace ScriptHandler.Models
{
	public class ExtraDataForParameter: ObservableObject
	{


		public DeviceParameterData Parameter { get; set; }


		public int Ni6002_IOPort { get; set; }
		public int Ni6002_Line { get; set; }


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
	}
}
