

using CommunityToolkit.Mvvm.ComponentModel;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeSubScript : ScriptNodeBase, ISubScript
	{
		public string ParentScriptName { get; set; }
		

		public SubScriptContinueUntilTypeEnum ContinueUntilType { get; set; }

		private int _repeats;
		public int Repeats 
		{
			get => _repeats;
			set
			{
				_repeats = value;
				OnPropertyChanged("Repeats");
			}
		}

		private int _timeout;
		public int Timeout
		{
			get => _timeout;
			set
			{
				_timeout = value;
				OnPropertyChanged("Timeout");
			}
		}

		private TimeUnitsEnum _timeoutUnite;
		public TimeUnitsEnum TimeoutUnite
		{
			get => _timeoutUnite;
			set
			{
				_timeoutUnite = value;
				OnPropertyChanged("TimeoutUnite");
				OnPropertyChanged("Description");
				OnPropertyChanged("PassNextDescription");
			}
		}

		public bool IsStopOnFail { get; set; }
		public bool IsStopOnPass { get; set; }



		public override string Description
		{
			get
			{
				string scriptName = "";
				if(Script != null) 
					scriptName = Script.Name;
				string str = "Sub Script = " + scriptName;
				str += " - " + ContinueUntilType + ": ";
				if (ContinueUntilType == SubScriptContinueUntilTypeEnum.Repeats)
					str += Repeats;
				else if (ContinueUntilType == SubScriptContinueUntilTypeEnum.Timeout)
					str += Timeout + "ms";

				str += " - ID:" + ID;

				return str;
			}
		}

		[JsonIgnore]
		public ProjectData Parent { get; set; }

		[JsonIgnore]
		private IScript _script;
		[JsonIgnore]
		public IScript Script
		{
			get => _script;
			set
			{
				if (value == null)
					return;

				if(ParentScriptName != null && value.Name == ParentScriptName) 
				{
					MessageBox.Show("Selecting the parent script as sub script is not allowed", "Select Sub Script Error");
					return;
				}
				_script = value;
				if (_script == null)
					return;

				SelectedScriptName = _script.Name;

				if(_script is ObservableObject obj)
				{
					obj.PropertyChanged += ScriptPropertyChangedEventHandler;
				}

				OnPropertyChanged(nameof(Script));
				OnPropertyChanged(nameof(Description));
			}
		}

		public string SelectedScriptName { get; set; }

		public ScriptNodeSubScript()
		{
			Name = "Sub Script";
			Repeats = 1;
			Timeout = 0;
			IsStopOnFail = true;
			IsStopOnPass = false;
			ContinueUntilType = SubScriptContinueUntilTypeEnum.Repeats;
		}



		private void ScriptPropertyChangedEventHandler(object sender, PropertyChangedEventArgs e)
		{
			if(e.PropertyName == "Name")
			{
				SelectedScriptName = _script.Name;
			}
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (Script == null)
				return true;

			return false;
		}

	}
}
