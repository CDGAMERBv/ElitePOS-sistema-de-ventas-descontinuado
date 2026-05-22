using System;

namespace ElitePOS.Shared.Models
{
    public class ChatMessage
    {
        public string Text { get; set; } = "";
        public bool IsUser { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
