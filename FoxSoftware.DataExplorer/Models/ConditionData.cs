namespace FoxSoftware.DataExplorer.Models
{
	public class ConditionData
	{
		public bool Negate { get; set; }
		public string Column { get; set; }
		public Condition Condition { get; set; }
		public object Value { get; set; }
	}
}
