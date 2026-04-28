using Godot;
using System;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

public partial class OllamaInterface : Node2D
{
    private string API_KEY;
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
    private int summaryThreshold = 6;
    private bool waitingForResponse = false;
    private bool hasPendingMessage = false;
    //
    private Stopwatch requestStopwatch = new Stopwatch();
    //
    // request timeout + retry state for graceful HTTP failure handling
    private const float RequestTimeoutSec = 90f;
    private const int MaxRetries = 2;
    private const float RetryDelaySec = 2f;
    private int retryCount = 0;
    private string lastRequestJson;
    private string[] lastRequestHeaders;
    //
    [Signal]
    public delegate void ModelReplyEventHandler(string sender, string target, string message);

    public override void _Ready()
    {
        API_KEY = GetAPIKey("/Users/isaaceccleston/Desktop/api-key.txt");

        httpRequest = new HttpRequest();
        httpRequest.Timeout = RequestTimeoutSec;
        AddChild(httpRequest);
        httpRequest.RequestCompleted += OnReply;

        summaryHttpRequest = new HttpRequest();
        summaryHttpRequest.Timeout = RequestTimeoutSec;
        AddChild(summaryHttpRequest);
        summaryHttpRequest.RequestCompleted += OnSummaryReply;
    }

    string GetAPIKey(string filepath)
    {
        try
        {
            GD.Print("Reading API key from: " + filepath); // Debug log
            return System.IO.File.ReadAllText(filepath).Trim();
        }
        catch (Exception e)
        {
            GD.PrintErr("Failed to read API key: " + e.Message);
            return null;
        }
    }

    private void Send()
    {
        waitingForResponse = true;

        List<ChatMessage> messageList = currentSession.GetMessages();

        if(!string.IsNullOrEmpty(currentSession.messagesSummary))
        {
            messageList.Insert(1, new ChatMessage("system", $"[Summary of earlier conversation: {currentSession.messagesSummary}]"));
        }

        string context = worldState?.GetContext(currentSession.name);
        if (!string.IsNullOrEmpty(context))
        {
            messageList.Insert(2, new ChatMessage("system", context));
        }

        var body = new
        {
            model = currentSession.modelID,
            messages = messageList,
            max_tokens = currentSession.maxTokens
        };

        string json = JsonSerializer.Serialize(body);
        string[] headers =
        {
            "Content-Type: application/json",
            $"Authorization: Bearer {API_KEY}"
        };

        // cache for retry
        lastRequestJson = json;
        lastRequestHeaders = headers;

        requestStopwatch.Restart();

        Error err = httpRequest.Request(targetURL, headers, Godot.HttpClient.Method.Post, json);
        if (err != Error.Ok)
        {
            GD.PrintErr("HTTP Request failed: ", err);
            HandleSendFailure($"HTTP send error: {err}", 0);
        }

        GD.Print($"Prompting {currentSession.name} to respond");
    }

    private void RetrySend()
    {
        if (currentSession == null)
        {
            GD.PrintErr("RetrySend with no current session — aborting.");
            return;
        }
        GD.Print($"Retrying request for {currentSession.name} (attempt {retryCount}/{MaxRetries})");
        requestStopwatch.Restart();
        Error err = httpRequest.Request(targetURL, lastRequestHeaders, Godot.HttpClient.Method.Post, lastRequestJson);
        if (err != Error.Ok)
        {
            GD.PrintErr("Retry send failed: ", err);
            HandleSendFailure($"Retry send error: {err}", 0);
        }
    }

    // Common failure path — either retry, or give up and emit a placeholder
    // so the agent turn queue in Main can advance instead of hanging.
    private void HandleSendFailure(string reason, long elapsedMs)
    {
        GD.PrintErr($"Request failure for {currentSession?.name}: {reason}");

        if (retryCount < MaxRetries)
        {
            retryCount++;
            var t = GetTree().CreateTimer(RetryDelaySec);
            t.Timeout += RetrySend;
            return;
        }

        // exhausted retries — log + emit fallback so the runtime continues
        string failMsg = $"[{currentSession?.name} failed to respond: {reason}]";
        logger.LogMessage(
            currentSession?.name ?? "Unknown",
            "All",
            "system",
            failMsg,
            0,
            elapsedMs);

        retryCount = 0;
        waitingForResponse = false;
        EmitSignal(SignalName.ModelReply, currentSession?.name ?? "Unknown", "All", failMsg);
    }

    public void PromptCurrentAgent()
    {
        if (waitingForResponse)
        {
            GD.Print("Still waiting for response, wait...");
            return;
        }

        if(currentSession.trimmedMessages.Count > summaryThreshold)
        {
            GD.Print($"Summary threshold reached for {currentSession.name}, summarising...");
            hasPendingMessage = true;
            waitingForResponse = true;
            RequestSummary();
            return;
        }

        Send();
    }

    public void RequestSummary()
    {
        List<ChatMessage> summaryMessages =
        [
            new ChatMessage("system",
                "You are a concise summarizer. The following messages are from a political " +
                "dialogue simulation involving multiple factions who speak both privately and publicly. " +
                "Summarize them in 2-3 sentences, preserving who said what, positions taken, alliances " +
                "or threats made, and any private information separately from public statements. " +
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
        requestStopwatch.Stop();
        long elapsedMs = requestStopwatch.ElapsedMilliseconds;

        // transport-level failure (timeout / connection / DNS / etc.)
        var resultEnum = (HttpRequest.Result)result;
        if (resultEnum != HttpRequest.Result.Success)
        {
            HandleSendFailure($"transport result={resultEnum} after {elapsedMs}ms", elapsedMs);
            return;
        }

        string responseText = Encoding.UTF8.GetString(body);

        GD.Print($"response: {responseText}"); // Debug log

        // HTTP-level failure (non-200)
        if (responseCode != 200)
        {
            HandleSendFailure($"HTTP {responseCode}", elapsedMs);
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

            if (string.IsNullOrWhiteSpace(rawResponse))
            {
                GD.PrintErr($"Empty response from {currentSession.name}, skipping.");
                logger.LogMessage(currentSession.name, "All", "system",
                    "[Empty response from model]", 0, elapsedMs);
                retryCount = 0;
                waitingForResponse = false;
                EmitSignal(SignalName.ModelReply, currentSession.name, "All", "[No response]");
                return;
            }

            var (target, cleanResponse) = ParseRouting(rawResponse);
            currentSession.AddMessage("assistant", cleanResponse);

            logger.LogMessage(
                currentSession.name,
                target,
                "assistant",
                cleanResponse,
                (int)(cleanResponse.Length / 3.5f),
                elapsedMs);

            retryCount = 0;
            waitingForResponse = false;
            EmitSignal(SignalName.ModelReply, currentSession.name, target, cleanResponse);
        }
        catch (Exception e)
        {
            GD.PrintErr("JSON parse error: " + e.Message);
            // treat as a hard failure so the runtime doesn't hang
            HandleSendFailure($"JSON parse error: {e.Message}", elapsedMs);
            return;
        }

        GD.Print("Received response for " + currentSession.name); // Debug log
    }

    private void OnSummaryReply(long result, long responseCode, string[] headers, byte[] body)
    {
        var resultEnum = (HttpRequest.Result)result;

        if (resultEnum != HttpRequest.Result.Success)
        {
            GD.PrintErr($"Summary transport error: {resultEnum}");
            logger.LogMessage(currentSession?.name ?? "Unknown", "All", "system",
                $"[Summary failed: transport {resultEnum}]", 0, 0);
        }
        else if (responseCode != 200)
        {
            GD.PrintErr($"Summary HTTP error {responseCode}");
            logger.LogMessage(currentSession?.name ?? "Unknown", "All", "system",
                $"[Summary failed: HTTP {responseCode}]", 0, 0);
        }
        else
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
                logger.LogMessage(currentSession?.name ?? "Unknown", "All", "system",
                    $"[Summary parse error: {e.Message}]", 0, 0);
            }
        }

        // either way, drop trimmed messages so we don't loop on the same summary
        currentSession?.trimmedMessages.Clear();
        waitingForResponse = false;

        if (hasPendingMessage)
        {
            hasPendingMessage = false;
            Send();
        }

        GD.Print("Summary response received for " + currentSession?.name);
    }

    private (string target, string clean) ParseRouting(string raw)
    {
        var match = Regex.Match(raw.TrimStart(), @"^\[TO:(\w+)\]\s*", RegexOptions.IgnoreCase);
        return match.Success
            ? (match.Groups[1].Value, raw[match.Length..].Trim())
            : ("User", raw.Trim());
    }
}
