namespace IntentEngine.Models
{
    public class DataSourceConfig
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ProviderType { get; set; }
        public string ConnectionString { get; set; }
        public bool IsDefault { get; set; }
    }
}
