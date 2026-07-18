namespace IntentEngine.Models
{
    public class FunctionParameter
    {
        public int Id { get; set; }
        public int FunctionId { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }
        public string ControlType { get; set; }
        public string DataSource { get; set; }
        public bool IsRequired { get; set; }
        public string DefaultValue { get; set; }
        public int SortOrder { get; set; }
    }
}
