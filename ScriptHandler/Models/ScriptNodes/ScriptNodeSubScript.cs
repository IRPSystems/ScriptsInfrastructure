

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using Newtonsoft.Json;
using ScriptHandler.DesignDiagram.ViewModels;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using Syncfusion.DocIO.DLS;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeSubScript : ScriptNodeBase, ISubScript
	{
		#region Properties

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

		public bool IsInfinity { get; set; }



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

		private IScript _script;
		[JsonIgnore]
		public IScript Script
		{
			get => _script;
			set
			{
				_prevScript = _script;
				if (value == null)
				{
					_script = value;
					return;
				}

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

				DesignDiagram = new DesignDiagramViewModel(null, null, null, isSubScript: true);
				DesignDiagram.DesignDiagram = _script as ScriptData;
				DesignDiagram.DrawNodes();
				DesignDiagram.OffsetY = 0;
				Height = GetHeight();


				OnPropertyChanged(nameof(Script));
				OnPropertyChanged(nameof(Description));
			}
		}

		public string SelectedScriptName { get; set; }

		[JsonIgnore]
		public DesignDiagramViewModel DesignDiagram { get; set; }
		public double DesignDiagramHeight { get; set; }
		public double DesignDiagramWidth { get; set; }

		#endregion Properties

		#region Constructor

		public ScriptNodeSubScript()
		{
			Name = "Sub Script";
			Repeats = 1;
			Timeout = 0;
			IsStopOnFail = true;
			IsStopOnPass = false;
			ContinueUntilType = SubScriptContinueUntilTypeEnum.Repeats;

			Script_SelectionChangedCommand = new RelayCommand(Script_SelectionChanged);
		}

		#endregion Constructor

		#region Methods

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

		private IScript _prevScript;
		private void Script_SelectionChanged()
		{
			if (_prevScript == _script || _script == null)
				return;
						
			ScriptChangedEvent?.Invoke(this);

			_prevScript = _script;
		}

		public double GetHeight()
		{
			if (Script == null)
				return DesignDiagramViewModel.ToolHeight;

			double height = DesignDiagramViewModel.ToolHeight;

			DesignDiagramHeight = 0;
			foreach (IScriptItem item in Script.ScriptItemsList)
			{
				if (item is ScriptNodeSubScript subScript)
					DesignDiagramHeight += subScript.GetHeight();
				else
					DesignDiagramHeight += DesignDiagramViewModel.BetweenTools;
			}

			DesignDiagramHeight += 20;

			height += DesignDiagramHeight;

			return height + 10;
		}

		public double GetWidth()
		{
			if (Script == null)
				return DesignDiagramViewModel.ToolWidth;


			double width = DesignDiagramViewModel.ToolWidth +
				DesignDiagramViewModel.ToolWidth * 0.33;
			foreach(IScriptItem item in Script.ScriptItemsList)
			{
				if(item is ScriptNodeSubScript subScript)
					width += subScript.GetWidth() * 0.33;
			}

			//width += 50;
			DesignDiagramWidth = width;

			return width;
		}

		public void InitNextArrows()
		{
			DesignDiagram.InitNextArrows();

			if(Script == null) 
				return;

			foreach (IScriptItem item in Script.ScriptItemsList)
			{
				if (item is ScriptNodeSubScript subScript)
					subScript.InitNextArrows();
			}
		}

		#endregion Methods

		#region Commands

		public RelayCommand Script_SelectionChangedCommand { get; private set; }

		#endregion Commands

		#region Events

		public event Action<ScriptNodeSubScript> ScriptChangedEvent;

		#endregion Events

	}
}
