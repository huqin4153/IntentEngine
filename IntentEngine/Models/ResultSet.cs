using System.Data;

namespace IntentEngine.Models
{
    public class ResultSet
    {
        public int SqlIndex { get; set; }
        public string Type { get; set; }
        public DataTable TableData { get; set; }
        public object ScalarValue { get; set; }
        public int RowsAffected { get; set; }
        public long ElapsedMs { get; set; }
    }
}
