namespace IntentEngine.Models
{
    public class FlowStep
    {
        public int Id { get; set; }
        public int FunctionId { get; set; }
        public int SortOrder { get; set; }
        public string StepType { get; set; }
        public string Label { get; set; }

        public string SqlText { get; set; }
        public string ResultVar { get; set; }
        public string DataSource { get; set; }

        public string ExpectOperator { get; set; }
        public string ExpectValue { get; set; }
        public string ExpectOnFail { get; set; }
        public int? ExpectTarget { get; set; }
        public string ExpectMessage { get; set; }

        public string DisplayTitle { get; set; }
        public string DisplaySource { get; set; }
        public string DisplayConfig { get; set; }

        public bool IsEnd { get; set; }
    }
}
