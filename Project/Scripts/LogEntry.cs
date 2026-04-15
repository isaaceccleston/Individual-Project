public class LogEntry
{
    public int Turn { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Role { get; set; } //e.g.: "user" | "assistant" | "system"
    public string Content { get; set; }
    public int EstimatedTokens { get; set; }
    public int RunningTokens { get; set; } // tokens used in the conversation up to this point
    public int SessionMessageCount { get; set; } // how deep is the context at this point
}