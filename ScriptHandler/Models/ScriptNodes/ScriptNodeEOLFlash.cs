
using System.IO;

namespace ScriptHandler.Models.ScriptNodes
{
	public class ScriptNodeEOLFlash: ScriptNodeBase
	{
		private string _filePath;
		public string FilePath 
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

		private string FileExtension { get; set; }

		public override string Description 
		{ 
			get
			{
				string description = "Flash - " + Path.GetFileName(FilePath);
				return description + " - ID:" + ID;
			}
		}

		public ScriptNodeEOLFlash() 
		{
			Name = "EOL Flash";
		}
	}
}
