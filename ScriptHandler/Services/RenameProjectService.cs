
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
				CopyFile(
					file,
					newDir,
					newProjectName,
					projectName);
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
			string newProjectName,
			string projectName)
		{
			string fileName = Path.GetFileName(file);
			string newPath = Path.Combine(newDir, fileName);

			string extension = Path.GetExtension(file);
			if (extension.ToLower() == ".prj")
				newPath = newPath.Replace(fileName, newProjectName + ".prj");
			else if (extension.ToLower() == ".gprj")
				newPath = newPath.Replace(fileName, newProjectName + ".gprj");

			File.Copy(file, newPath);

			if (extension.ToLower() == ".prj" || extension.ToLower() == ".gprj")
			{
				HandleProjectFile(
					newPath,
					newProjectName,
					projectName);
			}
		}

		private void HandleProjectFile(
			string newPath,
			string newProjectName,
			string projectName)
		{
			string fileData = null;
			using (StreamReader sr = new StreamReader(newPath))
			{
				fileData = sr.ReadToEnd();
			}

			if (string.IsNullOrEmpty(fileData))
				return;

			fileData = fileData.Replace(projectName, newProjectName);

			using (StreamWriter sw = new StreamWriter(newPath))
			{
				sw.Write(fileData);
			}
		}
	}
}
