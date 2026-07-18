using System.Collections.Generic;

namespace IntentEngine.Models
{
    public class Function
    {
        public int Id { get; set; }
        public int IntentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int SortOrder { get; set; }
        public string DataSource { get; set; }

        public List<FlowStep> Steps { get; set; }
        public List<FunctionParameter> Parameters { get; set; }
    }
}
