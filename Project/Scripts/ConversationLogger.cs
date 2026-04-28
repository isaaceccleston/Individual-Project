using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class ConversationLogger
{
    private List<LogEntry> log = new();
    private int turnCount = 0;

    public void LogMessage(string sender, string receiver, string role,
            string content, int estimatedTokens, long latencyMs = 0,
            List<string> appliedDeltas = null, int sessionContextTokens = 0,
            int sessionContextBudget = 0, int sessionTrimmedCount = 0,
            List<string> rejectedDeltas = null)
    {
        log.Add(new LogEntry
        {
            Turn = turnCount++,
            Sender = sender,
            Receiver = receiver,
            Role = role,
            Content = content,
            EstimatedTokens = estimatedTokens,
            LatencyMs = latencyMs,
            AppliedDeltas = appliedDeltas ?? new List<string>(),
            RejectedDeltas = rejectedDeltas ?? new List<string>(),
            SessionContextTokens = sessionContextTokens,
            SessionContextBudget = sessionContextBudget,
            SessionTrimmedCount = sessionTrimmedCount
        });
    }

    public void Export(string path)
    {
        string json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<LogEntry> GetLog(){return log;}
}
