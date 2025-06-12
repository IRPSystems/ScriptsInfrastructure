
using CommunityToolkit.Mvvm.Input;
using DeviceHandler.Models;
using FlashingToolLib.FlashingTools;
using Microsoft.Win32;
using Newtonsoft.Json;
using Syncfusion.UI.Xaml.Diagram;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Controls;
using static iso15765.CUdsClient;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLFlash: ScriptNodeBase
	{
		#region Fields and Properties

		private string _filePath;
		public string FlashFilePath 
		{
			get => _filePath;
			set
			{
				_filePath = value;
				if (string.IsNullOrEmpty(_filePath))
					return;
				
				FileExtension = Path.GetExtension(value);
				if (FileExtension.ToLower() == ".hex")
				{
					if (_filePath.ToLower().EndsWith(".brn.hex"))
						FileExtension = ".brn.hex";
				}
			}
		}

		public string RXId { get; set; }
		public string TXId { get; set; }
		public ECustomer Customer { get; set; }



		public int NumOfFlashFile { get; set; }

		public string SourceModeGroupName { get => $"EOLSourceMode_{Description}"; }
		public bool IsEolSource { get; set; }
		public bool IsToolSource { get; set; }
        public bool MCU_Used { get; set; }
        public bool MCU2_Used { get; set; }

        [JsonIgnore]
		public string FileExtension { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Flash - " + Path.GetFileName(FlashFilePath);
				return description + " - ID:" + ID;
			}
		}

		#endregion Fields and Properties

		#region Constructor

		public ScriptNodeEOLFlash() 
		{
			Init();
		}

		#endregion Constructor

		#region Methods

		public void Init()
		{
			Name = "EOL Flash";

			FlashFilePathOpenCommand = new RelayCommand(FlashFilePathOpen);

			FileExtension = "";
            Customer = ECustomer.GENERIC;
		}

		private void FlashFilePathOpen()
		{
			OpenFileDialog openFileDlg = new OpenFileDialog();
			//!todo
			openFileDlg.DefaultExt = ".irphex|.irpcycad|.cycad|.bin|.irpbin|.hex|.txt|";
			openFileDlg.Filter = "irphex files (*.irphex)|*.irphex|" +
								 "cyacd files (*.cyacd)|*.cyacd|" +
								 "irpcyacd files (*.irpcyacd)|*.irpcyacd|" +
								 "bin files (*.bin)|*.bin|" +
								 "irpbin files (*.irpbin)|*.irpbin|" +
								 "hex files (*.hex)|*.hex";

			var result = openFileDlg.ShowDialog();
			if (result != true)
				return;

			FlashFilePath = openFileDlg.FileName;
		}

		public override object Clone()
		{
			ScriptNodeEOLFlash flash = base.Clone() as ScriptNodeEOLFlash;
			flash.Init();

			return flash;
		}

		public override bool IsNotSet(
			DevicesContainer devicesContainer,
			ObservableCollection<InvalidScriptItemData> errorsList)
		{
			if (string.IsNullOrEmpty(FlashFilePath))
				return true;

			return false;
		}

		#endregion Methods

		#region Commeands

		[JsonIgnore]
		public RelayCommand FlashFilePathOpenCommand { get; private set; }

		#endregion Commeands
	}
}
