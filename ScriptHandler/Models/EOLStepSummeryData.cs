
using CommunityToolkit.Mvvm.ComponentModel;

namespace ScriptHandler.Models
{
	public class EOLStepSummeryData: ObservableObject
	{
		public string StepDescription { get; set; }
		public string Description { get; set; }
		public double? TestValue { get; set; }
        public double? ComparisonValue { get; set; }
        public string Method { get; set; }
		public string Units { get; set; }
        public double? MinVal { get; set; }
        public double? MaxVal { get; set; }
        public double? Tolerance { get; set; }
        public string Reference { get; set; }
        public bool IsPass { get; set; }
        public string ErrorDescription { get; set; }

		public EOLStepSummeryData() { }

		public EOLStepSummeryData(
			string stepDescription, 
			string description)
		{
			StepDescription = stepDescription;
			Description = description;
		}
	}							  
}								  
								  
								  