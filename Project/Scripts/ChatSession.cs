using System.Collections.Generic;
using System.Linq;
using Godot;

public class ChatSession
{
    public string modelID;
    public string name;
    private List<ChatMessage> messages = new();
    private int conversationBudget;
    public string messagesSummary = "";
    public List<ChatMessage> trimmedMessages = new();
    private float charsPerToken = 3.5f;
    private int replyBuffer = 150;
    public int maxTokens;
    public int TotalMessagesTrimmed { get; private set; } = 0;

    public ChatSession(string modelID, string name, string systemPrompt, int modelContextWindow, int maxTokens)
    {
        this.modelID = modelID;
        this.name = name;
        this.maxTokens = maxTokens;

        messages.Add(new ChatMessage("system", systemPrompt));

        int systemPromptCost = EstimateTokens(systemPrompt);
        conversationBudget = modelContextWindow - systemPromptCost - replyBuffer;
    }

    public void AddMessage(string role, string message)
    {
        messages.Add(new ChatMessage(role, message));
        TrimMessages();
    }

    public void ObserveMessage(string sender, string target, string message)
    {
        string framing;

        if (target == "All")
            framing = $"[Public announcement] {sender}: {message}";
        else if (target == name)
            framing = $"[Private message to you from {sender}] {message}";
        else
            framing = $"[Overheard] {sender} to {target}: {message}";

        AddMessage("user", framing);
    }

    private int EstimateTokens(string message)
    {
        return (int)(message.Length / charsPerToken);
    }

    private void TrimMessages()
    {
        int total = messages.Skip(1).Sum(m => EstimateTokens(m.content));

        while (total > conversationBudget && messages.Count > 2)
        {
            GD.Print($"Trimming messages. Total tokens: {total}, Budget: {conversationBudget}");
            trimmedMessages.Add(messages[1]);
            TotalMessagesTrimmed++;
            total -= EstimateTokens(messages[1].content);
            messages.RemoveAt(1);
        }
    }

    public List<ChatMessage> GetMessages()
    {
        return new List<ChatMessage>(messages);
    }

    public int GetCurrentContextTokens()
    {
        return messages.Skip(1).Sum(m => EstimateTokens(m.content));
    }

    public int GetContextBudget()
    {
        return conversationBudget;
    }
}
