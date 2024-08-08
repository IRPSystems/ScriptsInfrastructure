
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScriptHandler.Models
{
	public class EOLStepSummeryData: ObservableObject
	{
		public string StepDescription { get; set; }
		public string Description { get; set; }
		public string Value { get; set; }
		public bool IsPass { get; set; }
		public string ErrorDescription { get; set; }

		public EOLStepSummeryData() { }

		public EOLStepSummeryData(
			string stepDescription, 
			string description, 
			string value = "", 
			bool isPass = true, 
			string errorDescription = "")
		{
			StepDescription = stepDescription;
			Description = description;
			Value = value;
			IsPass = isPass;
			ErrorDescription = errorDescription;
		}
	}							  
}								  
								  
								  