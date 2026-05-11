using System.Collections.Generic;

public class LogEntry
{
    public int Turn { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
    public int EstimatedTokens { get; set; }
    public int SessionContextTokens { get; set; }
    public int SessionContextBudget { get; set; }
    public int SessionTrimmedCount { get; set; }
    public long LatencyMs { get; set; }
    public List<string> AppliedDeltas { get; set; } = new();
    public List<string> RejectedDeltas { get; set; } = new();
}
