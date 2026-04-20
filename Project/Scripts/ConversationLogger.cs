using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class ConversationLogger
{
    private List<LogEntry> log = new();
    private int turnCount = 0;

    private Dictionary<string, int> sessionTokens = new Dictionary<string, int>();

    public void LogMessage(string sender, string receiver, string role, string content, int estimatedTokens, long latencyMs=0)
    {
        if(!sessionTokens.ContainsKey(sender))
        {
            sessionTokens[sender] = 0;
        }
        sessionTokens[sender] += estimatedTokens;

        log.Add(new LogEntry
        {
            Turn = turnCount++,
            Sender = sender,
            Receiver = receiver,
            Role = role,
            Content = content,
            EstimatedTokens = estimatedTokens,
            RunningTokens = sessionTokens[sender],
            LatencyMs = latencyMs
        });
    }

    public void Export(string path)
    {   
        string json = JsonSerializer.Serialize(log, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    public List<LogEntry> GetLog(){return log;}
}