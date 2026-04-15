using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public partial class OllamaInterface : Node2D
{
    private string API_KEY = "sk-244be4b5b3e34bffb2b5220114334042";
    private string targetURL = "https://h.langbein.org:11003/api/chat/completions";
    //
    public ChatManager chatManager = new();
    public ChatSession currentSession;
    //
    public ConversationLogger logger = new();
    //
    public WorldState worldState;
    //
    private HttpRequest httpRequest;
    private HttpRequest summaryHttpRequest;
    private int summaryThreshold = 2;
    private bool waitingForResponse = false;
    private string pendingSender;
    private string pendingMessage;
    private bool hasPendingMessage = false;
    //
    [Signal]
    public delegate void ModelReplyEventHandler(string sender, string target, string message);

    public override void _Ready()
    {
        httpRequest = new HttpRequest();
        AddChild(httpRequest);
        httpRequest.RequestCompleted += OnReply;

        summaryHttpRequest = new HttpRequest();
        AddChild(summaryHttpRequest);
        summaryHttpRequest.RequestCompleted += OnSummaryReply;
    }

    private void Send(string sender, string message)
    {
        waitingForResponse = true;

        string fMessage = $"{sender} says: {message}";
        currentSession.AddMessage("user", fMessage);

        logger.LogMessage(
            sender,
            currentSession.name,
            "user",
            message,
            (int)(message.Length / 3.5f),
            currentSession.GetMessages().Count - 1);

        List<ChatMessage> messageList = currentSession.GetMessages();

        if (!string.IsNullOrEmpty(currentSession.messagesSummary))
        {
            messageList.Insert(1, new ChatMessage("system",
                $"[Summary of earlier conversation: {currentSession.messagesSummary}]"));
        }

        string context = worldState?.GetContext(currentSession.name);
        if (!string.IsNullOrEmpty(context))
            messageList.Insert(2, new ChatMessage("system", context));

        var body = new {
            model = currentSession.modelID,
            messages = messageList,
            max_tokens = currentSession.maxTokens
        };

        string json = JsonSerializer.Serialize(body);
        string[] headers = { "Content-Type: application/json", $"Authorization: Bearer {API_KEY}" };

        Error err = httpRequest.Request(targetURL, headers, Godot.HttpClient.Method.Post, json);
        if (err != Error.Ok)
            GD.PrintErr("HTTP Request failed: ", err);
        
        GD.Print($"Sent message from {sender} to {currentSession.name}"); // Debug log
    }

    public void SendMessage(string sender, string message)
    {   
        if(waitingForResponse)
        {
            GD.Print("Still waiting for response, wait...");
            return;
        }

        if(currentSession.trimmedMessages.Count > summaryThreshold)
        {
            GD.Print("Summary threshold reached, summarizing conversation...");
            pendingSender = sender;
            pendingMessage = message;
            hasPendingMessage = true;
            waitingForResponse = true;
            RequestSummary();
            return;
        }

        Send(sender, message);
    }

    public void RequestSummary()
    {
        List<ChatMessage> summaryMessages =
        [
            new ChatMessage("system", "You are a concise summarizer. The following messages are from a political " +
                "dialogue simulation. Summarize them in 2-3 sentences, preserving key facts, " +
                "positions taken, and any tension or decisions between the parties. " +
                "Respond with only the summary text, no preamble."),
            .. currentSession.trimmedMessages,
            new ChatMessage("user", "Summarize the above conversation."),
        ];

        var body = new { model = currentSession.modelID, messages = summaryMessages, max_tokens = 120 };
        string json = JsonSerializer.Serialize(body);
        string[] headers = { "Content-Type: application/json", $"Authorization: Bearer {API_KEY}" };

        Error err = summaryHttpRequest.Request(targetURL, headers, Godot.HttpClient.Method.Post, json);
        if (err != Error.Ok)
            GD.PrintErr("Summary request failed: ", err);
        
        GD.Print("Summary request sent."); // Debug log
    }

    private void OnReply(long result, long responseCode, string[] headers, byte[] body)
    {
        waitingForResponse = false;

        string responseText = Encoding.UTF8.GetString(body);

        if (responseCode != 200)
        {
            GD.PrintErr($"HTTP Error {responseCode}: {responseText}");
            return;
        }

        try
        {
            using JsonDocument doc = JsonDocument.Parse(responseText);
            string rawResponse = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            
            var (target, cleanResponse) = ParseRouting(rawResponse);
            currentSession.AddMessage("assistant", cleanResponse);

            logger.LogMessage(
                currentSession.name, 
                target, 
                "assistant", 
                cleanResponse,
                (int)(cleanResponse.Length / 3.5f), 
                currentSession.GetMessages().Count-1);

            EmitSignal(SignalName.ModelReply, currentSession.name, target, cleanResponse);
        }
        catch (Exception e)
        {
            GD.PrintErr("JSON parse error: " + e.Message);
        }
        
        GD.Print("Received response for " + currentSession.name); // Debug log
    }

    private void OnSummaryReply(long result, long responseCode, string[] headers, byte[] body)
    {
        if (responseCode == 200)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(Encoding.UTF8.GetString(body));
                string summary = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                currentSession.messagesSummary = summary.Trim();
                GD.Print($"Summary stored for {currentSession.name}: {currentSession.messagesSummary}");
            }
            catch (Exception e)
            {
                GD.PrintErr("Summary parse error: " + e.Message);
            }
        }
        else
        {
            GD.PrintErr($"Summary HTTP error {responseCode}");
        }

        currentSession.trimmedMessages.Clear();
        waitingForResponse = false;

        if (hasPendingMessage)
        {
            hasPendingMessage = false;
            Send(pendingSender, pendingMessage);
        }

        GD.Print("Summary response received for " + currentSession.name); // Debug log
    }

    private (string target, string clean) ParseRouting(string raw)
    {
        var match = Regex.Match(raw.TrimStart(), @"^\[TO:(\w+)\]\s*", RegexOptions.IgnoreCase);
        return match.Success
            ? (match.Groups[1].Value, raw[match.Length..].Trim())
            : ("User", raw.Trim());
    }
}
