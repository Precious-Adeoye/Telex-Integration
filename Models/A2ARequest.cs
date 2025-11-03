namespace Telex_Integration.Models
{
    public class A2ARequest
    {
        public string Message { get; set; } = string.Empty;
        public A2AUser? User { get; set; }
        public string? ChannelId { get; set; }
        public Dictionary<string, object>? Context { get; set; }
    }
}
