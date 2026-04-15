using Godot;
using System;
using System.Collections.Generic;

public partial class Main : Node2D
{
    OllamaInterface ollamaInterface;
    UI ui;
    string sender = "User";
    private int agentHopCount;
    private int maxAgentHops = 6;
    WorldState worldState = new WorldState();
    public override void _Ready()
    {
        // Initialize the Ollama interface and chat manager
        ollamaInterface = GetNode<OllamaInterface>("OllamaInterface");
        ollamaInterface.worldState = worldState;
        ui = GetNode<UI>("UI");

        ui.MessageSubmitted += OnUserMessage;
        ui.RecieverChanged += ChangeModelSession;
        ui.SenderChanged += ChangeSender;
        ui.ExportLogRequested += ExportLog;
        ui.SummariseRequested += RequestSummary;

        ollamaInterface.ModelReply += OnModelReply;
    }

    public void RequestSummary()
    {
        ollamaInterface.RequestSummary();
    }

    public void ExportLog()
    {
        ollamaInterface.logger.Export("conversation_log.json");
    }

    public void OnUserMessage(string message)
    {
        agentHopCount = 0;
        ollamaInterface.SendMessage(sender, message);
    }

    private void OnModelReply(string senderName, string target, string message)
    {
        ui.OnModelReply(senderName, target, message);

        bool isTargetAgent = target != "User" && worldState.characters.ContainsKey(target);

        if (isTargetAgent && agentHopCount < maxAgentHops)
        {
            agentHopCount++;
            GD.Print($"Agent hop {agentHopCount}: {senderName} -> {target}");

            var timer = GetTree().CreateTimer(1.0f); // 1 second delay before next message
            timer.Timeout += () => {
                ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[target]);
                ollamaInterface.SendMessage(senderName, message);
            };
        }
        else
        {
            if(agentHopCount >= maxAgentHops)
            {
                GD.Print("Max agent hops reached, stopping chain.");
            }
            agentHopCount = 0;
        }
    }

    public void ChangeModelSession(string characterName)
    {
        switch(characterName)
        {
            case "Aurellian":
                ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters["Aurellian"]);
                break;
            case "Brutan":
                ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters["Brutan"]);
                break;
            default:
                GD.PrintErr($"Unknown character: {characterName}");
                break;
        }
    }

    public void ChangeSender(string sender)
    {
        this.sender = sender;
    }
}
