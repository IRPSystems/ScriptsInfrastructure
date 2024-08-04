using CommunityToolkit.Mvvm.ComponentModel;
using DeviceCommunicators.Models;
using Entities.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System;

namespace ScriptHandler.Models
{
	public class SweepItemData : ObservableObject, ICloneable
	{
		public DeviceParameterData Parameter { get; set; }

		public object StartValue { get; set; }
		public object EndValue { get; set; }
		public object StepValue { get; set; }

		public int StepInterval { get; set; }
		public TimeUnitsEnum StepIntervalTimeUnite { get; set; }

		#region SubScript

		private IScript _subScript;
		public IScript SubScript 
		{
			get => _subScript;
			set
			{
				_subScript = value;

				if(_subScript != null ) 
					SubScriptName = _subScript.Name;
			}
		}

		public string SubScriptName { get; set; }

		#endregion SubScript

		

		[JsonIgnore]
		public SweepItemData Next { get; set; }


		

		public SweepItemData()
		{
			StepIntervalTimeUnite = TimeUnitsEnum.sec;
		}



		public object Clone()
		{
			return this.MemberwiseClone();
		}

	}
}
