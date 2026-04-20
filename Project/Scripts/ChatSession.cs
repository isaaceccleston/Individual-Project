using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Godot;

public class ChatSession
{
    public string modelID;
    public string name;
    private List<ChatMessage> messages = new();
    private int conversationBudget;
    public string messagesSummary = "";
    public List<ChatMessage> trimmedMessages = new();

    // LLama rough averages
    private float charsPerToken = 3.5f;
    // private int replyBuffer = 512;
    private int replyBuffer = 150;
    public int maxTokens;

    public ChatSession(string modelID, string name, string systemPrompt, int modelContextWindow, int maxTokens)
    {
        this.modelID = modelID;
        this.name = name;
        this.maxTokens = maxTokens;
        //add system prompt as first message
        messages.Add(new ChatMessage("system", systemPrompt));

        int systemPromptCost = EstimateTokens(systemPrompt);
        //remove space in budget for system prompt and reply buffer
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
        
        if(target == "All")
            framing = $"{sender} to everyone: {message}";
        else if(target == name)
        {
            framing = $"{sender} to you: {message}";
        }
        else
        {
            framing = $"{sender} to {target}: {message}";
        }

        AddMessage("user", framing);
    }

    private int EstimateTokens(string message)
    {
        //estimate tokens based on character count
        return (int)(message.Length / charsPerToken);
    }

    private void TrimMessages()
    {
        //skips system prompt
        int total = messages.Skip(1).Sum(m => EstimateTokens(m.content));

        while (total > conversationBudget && messages.Count > 2)
        {
            GD.Print($"Trimming messages. Total tokens: {total}, Budget: {conversationBudget}"); // Debug log
            trimmedMessages.Add(messages[1]);
            total -= EstimateTokens(messages[1].content);
            messages.RemoveAt(1);
        }
    }

    public List<ChatMessage> GetMessages()
    {
        return new List<ChatMessage>(messages);
    }
}