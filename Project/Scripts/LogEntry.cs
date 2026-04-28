using System.Collections.Generic;

public class LogEntry
{
    public int Turn { get; set; }
    public string Sender { get; set; }
    public string Receiver { get; set; }
    public string Role { get; set; } //e.g.: "user" | "assistant" | "system"
    public string Content { get; set; }
    public int EstimatedTokens { get; set; } // this msg estimate
    //
    public int SessionContextTokens { get; set; }
    public int SessionContextBudget { get; set; }
    public int SessionTrimmedCount { get; set; }
    //
    public long LatencyMs { get; set; } // time taken for the model to respond in milliseconds
    public List<string> AppliedDeltas { get; set; } = new();
    public List<string> RejectedDeltas { get; set; } = new();
}
