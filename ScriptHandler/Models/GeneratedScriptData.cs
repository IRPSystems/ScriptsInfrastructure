
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Xaml.Behaviors.Media;
using Newtonsoft.Json;
using ScriptHandler.Enums;
using ScriptHandler.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ScriptHandler.Models
{
    public class GeneratedScriptData : ObservableObject, IScript
    {
        public string Name { get; set; }
        public ObservableCollection<IScriptItem> ScriptItemsList { get; set; }

        public string ScriptPath { get; set; }

        public bool? IsPass { get; set; }
        [JsonIgnore]
		public SciptStateEnum State { get; set; }

		public ScriptSenderEnum ScriptSender { get; set; }

        [JsonIgnore]
        public bool IsDoRun { get; set; }
		[JsonIgnore]
		public bool IsSelected { get; set; }
		public bool isExecuted { get; set; }
        [JsonIgnore]
		public Brush Background { get; set; }
        [JsonIgnore]
		public Brush Foreground { get; set; }
        [JsonIgnore]
		public Brush BorderBrush { get; set; }


		[JsonIgnore]
		public bool IsContainsSO { get; set; }

		public int TotalRunSteps { get; set; }
		public int PassRunSteps { get; set; }
		public int FailRunSteps { get; set; }

		public ObservableCollection<InvalidScriptItemData> ErrorsList { get; set; }

        public GeneratedScriptData()
        {
			Background = Brushes.Transparent;
			if (Application.Current != null)
				Foreground = Application.Current.MainWindow.Foreground;
            State = SciptStateEnum.None;

            isExecuted = false;
            IsSelected = false;
            IsDoRun = true;
		}
	}
}
