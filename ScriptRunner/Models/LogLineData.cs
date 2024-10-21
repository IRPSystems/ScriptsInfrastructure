

using CommunityToolkit.Mvvm.ComponentModel;
using ScriptRunner.Enums;
using System;
using System.Windows;
using System.Windows.Media;

namespace ScriptRunner.Models
{
    public class LogLineData: ObservableObject
    {
       
        public TimeSpan Time { get; set; }
        public string Data { get; set; }

        public LogTypeEnum LogType { get; set; }

        public Brush Background
        {
            get
            {
                switch (LogType)
                {

                    case LogTypeEnum.ScriptData: return Brushes.Magenta;
                    case LogTypeEnum.StepData: return Brushes.Transparent;
                    case LogTypeEnum.Pass: return Brushes.Green;
                    case LogTypeEnum.Fail: return Brushes.Red;
                    case LogTypeEnum.None: return Brushes.Transparent;
                }

				return Brushes.Transparent;
			}
        }

		public Brush Foreground
		{
			get
			{
				switch (LogType)
				{

					case LogTypeEnum.ScriptData: return Brushes.White;
					case LogTypeEnum.StepData:
						if (Application.Current != null)
							return Application.Current.MainWindow.Foreground;
						else break;
					case LogTypeEnum.Pass: return Brushes.White; 
					case LogTypeEnum.Fail: return Brushes.White;
					case LogTypeEnum.None:
						if (Application.Current != null)
							return Application.Current.MainWindow.Foreground;
						else break;
				}

				return Brushes.Transparent;
			}
		}
	}
}
