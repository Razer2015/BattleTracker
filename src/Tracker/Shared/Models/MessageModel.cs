using Shared.Enums;
using System;

namespace Shared.Models
{
    public class MessageModel
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
        public LogType LogType { get; set; }
    }
}
