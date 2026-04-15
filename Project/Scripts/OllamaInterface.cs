using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

public partial class OllamaInterface : Node2D
{
    private HttpRequest httpRequest;
    private string API_KEY = "sk-244be4b5b3e34bffb2b5220114334042";
    private string targetURL = "https://h.langbein.org:11003/api/chat/completions";
    public ChatManager chatManager = new();
    public ChatSession currentSession;
    public ConversationLogger logger = new();
    public WorldState worldState;
    private bool waitingForResponse = false;
    [Signal]
    public delegate void ModelReplyEventHandler(string sender, string target, string message);

    public override void _Ready()
    {
        httpRequest = GetNode<HttpRequest>("HTTPRequest");
        httpRequest.RequestCompleted += OnReply;
    }

    public void SendMessage(string sender, string message)
    {   
        if(waitingForResponse)
        {
            GD.Print("Still waiting for response, please wait...");
            return;
        }
        waitingForResponse = true;

        string fMessage = $"{sender} says: {message}";
        currentSession.AddMessage("user", fMessage);

        logger.LogMessage(
            sender, 
            currentSession.name, 
            "user", 
            message,
            (int)(message.Length / 3.5f), // rough token estimate (hardcoded float would be a nice change for future)
            currentSession.GetMessages().Count-1); // -1 because of the system propmt maybe?

        var body = new{
            model = currentSession.modelID,
            messages = currentSession.GetMessages(),
            max_tokens = currentSession.maxTokens
        };

        string json = JsonSerializer.Serialize(body);

        string[] headers = {
            "Content-Type: application/json",
            $"Authorization: Bearer {API_KEY}"
        };
        
        Error err = httpRequest.Request(
            targetURL, headers, Godot.HttpClient.Method.Post, json
        );

        if (err != Error.Ok)
        {
            GD.PrintErr("HTTP Request failed: ", err);
        }
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
            string rawResponse = doc
                .RootElement
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
    }

    private (string target, string clean) ParseRouting(string raw)
    {
        var match = Regex.Match(raw.TrimStart(), @"^\[TO:(\w+)\]\s*", RegexOptions.IgnoreCase);
        return match.Success
            ? (match.Groups[1].Value, raw[match.Length..].Trim())
            : ("User", raw.Trim());
    }
}
