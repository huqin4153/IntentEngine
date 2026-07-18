using System.Data;

namespace IntentEngine.Models
{
    public class DisplayBlock
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public DataTable TableData { get; set; }
        public string TextContent { get; set; }
        public string ConfigJson { get; set; }
    }
}
