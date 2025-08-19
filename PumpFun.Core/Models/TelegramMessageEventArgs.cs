namespace PumpFun.Core.Models
{
    public class TelegramMessageEventArgs : EventArgs
    {
        public string Message { get; set; } = string.Empty;
        public string FormattedMessage { get; set; } = string.Empty;
        public long ChatId { get; set; }
        public int MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public List<string> Images { get; set; } = new List<string>();
    }
}
