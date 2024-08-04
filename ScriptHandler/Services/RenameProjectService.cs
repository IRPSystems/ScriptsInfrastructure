
using ScriptHandler.Views;
using Services.Services;
using System.IO;

namespace ScriptHandler.Services
{
	public class RenameProjectService
	{
		public string ProjectRename(
			string projectPath,
			string projectName)
		{
			LoggerService.Inforamtion(this, $"Renaming project \"{projectName}\"");
			string newProjectName = GetNewName(projectName);
			if(string.IsNullOrEmpty(newProjectName)) 
				return null;

			string originalDir = Path.GetDirectoryName(projectPath);
			int index = originalDir.LastIndexOf('\\');
			string newDir = originalDir.Substring(0, index);


			newDir = Path.Combine(newDir, newProjectName);

			if (Directory.Exists(newDir))
			{
				newDir = newDir.Replace(newProjectName, string.Empty);
				LoggerService.Error(
					this,
					newProjectName + " already exists in " + newDir,
					"Rename Project Error");

				return null;
			}

			Directory.CreateDirectory(newDir);

			string[] filesList = Directory.GetFiles(originalDir);
			foreach (string file in filesList)
			{
				LoggerService.Inforamtion(this, $"Copying file \"{file}\"");
				CopyFile(
					file,
					newDir,
					newProjectName);
			}

			projectPath = Path.Combine(newDir, newProjectName + ".prj");

			return projectPath;
		}

		private string GetNewName(string projectName)
		{
			ScriptNameView scriptNameView = new ScriptNameView();

			scriptNameView.Title = "Change Project Name";
			scriptNameView.SubTitle = "New project name";
			scriptNameView.ButtonTitle = "Change";
			scriptNameView.ScriptName = projectName;

			bool? result = scriptNameView.ShowDialog();
			if (result != true)
			{
				return null;
			}

			projectName = scriptNameView.ScriptName;
			return projectName;
		}

		private void CopyFile(
			string file,
			string newDir,
			string newProjectName)
		{
			string fileName = Path.GetFileName(file);
			string newPath = Path.Combine(newDir, fileName);

			string extension = Path.GetExtension(file);
			if (extension.ToLower() == ".prj")
				newPath = Path.Combine(newDir, newProjectName + ".prj");
			else if (extension.ToLower() == ".gprj")
				newPath = Path.Combine(newDir, newProjectName + ".gprj");

			if (File.Exists(newPath))
				return;

			File.Copy(file, newPath);

			
		}
	}
}
