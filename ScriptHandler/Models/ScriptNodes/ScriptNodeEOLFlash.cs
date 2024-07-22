
using CommunityToolkit.Mvvm.Input;
using FlashingToolLib.FlashingTools;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Controls;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLFlash: ScriptNodeBase
	{
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
		public UdsSequence UdsSequence { get; set; }

		[JsonIgnore]
		public string FileExtension { get; set; }

		public string Name1 { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Flash - " + Path.GetFileName(FlashFilePath);
				return description + " - ID:" + ID;
			}
		}

		public ScriptNodeEOLFlash() 
		{
			Init();
		}

		public void Init()
		{
			Name = "EOL Flash";

			FlashFilePathOpenCommand = new RelayCommand(FlashFilePathOpen);
			UdsSequence_SelectionChangedCommand = new RelayCommand(UdsSequence_SelectionChanged);

			FileExtension = "";
			UdsSequence = UdsSequence.generic;
			UdsSequence_SelectionChanged();
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

		private void UdsSequence_SelectionChanged()
		{
			

			switch (UdsSequence)
			{
				case UdsSequence.bootloader:
				case UdsSequence.silence:
					RXId = "3FE";
					TXId = "3FF";
					break;
				case UdsSequence.generic:
				default:
					RXId = "1CFFF9FE";
					TXId = "1CFFFEF9";
					break;
			}
		}

		public override object Clone()
		{
			ScriptNodeEOLFlash flash = base.Clone() as ScriptNodeEOLFlash;
			flash.Init();

			return flash;
		}

		[JsonIgnore]
		public RelayCommand FlashFilePathOpenCommand { get; private set; }
		[JsonIgnore]
		public RelayCommand UdsSequence_SelectionChangedCommand { get; private set; }
	}
}
