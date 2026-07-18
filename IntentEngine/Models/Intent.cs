using System;
using System.Collections.Generic;

namespace IntentEngine.Models
{
    public class Intent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string Category { get; set; }
        public string SemanticText { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public float[] Vector { get; set; }
        public List<Function> Functions { get; set; }
    }
}
