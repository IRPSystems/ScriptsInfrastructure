
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using ScriptHandler.DesignDiagram.ViewModels;
using ScriptHandler.Models.ScriptNodes;
using ScriptHandler.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ScriptHandler.Models
{
    public class ProjectData : ObservableObject
    {
        #region Properties

        public string Name { get; set; }
        [JsonIgnore]
		[Obsolete]
		public ObservableCollection<DesignDiagramViewModel> TestsList { get; set; }

		[JsonIgnore]
		public ObservableCollection<DesignDiagramViewModel> ScriptsList { get; set; }
		[JsonIgnore]
		public IEnumerable<ScriptData> ScriptsOnlyList 
		{
			get => ScriptsList
				.ToList().Where((vm) => !(vm.DesignDiagram is TestData))
				.Select((vm) => vm.DesignDiagram);
		}

		[Obsolete]
        public List<string> TestPathsList { get; set; }
		public List<string> ScriptsPathsList { get; set; }

		[JsonIgnore]
		public ObservableCollection<ScriptNodeCANMessage> CanMessagesList { get; set; }

		public string RecordParametersFile { get; set; }

		[JsonIgnore]
		public string ProjectPath { get; set; }

		public bool IsChangesExist { get; set; }

        #endregion Properties

        #region Constructor

        public ProjectData()
        {
			CanMessagesList = new ObservableCollection<ScriptNodeCANMessage>();
			ScriptsList = new ObservableCollection<DesignDiagramViewModel>();
			ScriptsPathsList = new List<string>();

			ScriptsList.CollectionChanged += (sender, args) => { OnPropertyChanged(nameof(ScriptsOnlyList)); };
		}

        #endregion Constructor
    }
}
