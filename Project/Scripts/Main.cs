using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Main : Node2D
{
    //
    OllamaInterface ollamaInterface;
    UI ui;
    StartUI startUI;
    WorldState worldState = new WorldState();

    //
    string playerFaction = "";
    string sender = "User";
    private int agentHopCount;
    private int maxAgentHops = 6;

    public override void _Ready()
    {
        //
        startUI = GetNode<StartUI>("StartUI");
        ui = GetNode<UI>("UI");
        ui.Visible = false;

        startUI.StartGameSignal += OnStartGame;

        //
        ui.MessageSubmitted += OnUserMessage;
        ui.RecieverChanged += ChangeModelSession;
        ui.ExportLogRequested += ExportLog;
        ui.SummariseRequested += RequestSummary;

        // Initialize the Ollama interface and chat manager
        ollamaInterface = GetNode<OllamaInterface>("OllamaInterface");
        ollamaInterface.worldState = worldState;

        ollamaInterface.ModelReply += OnModelReply;
    }

    private void OnStartGame(bool mode, int faction)
    {
        GD.Print($"Starting game with mode: {(mode ? "LLM" : "Baseline")}, faction: {faction}"); // Debug log
        
        startUI.Visible = false;
        ui.Visible = true;
        
        playerFaction = faction switch
        {
            0 => "Aurellian",
            1 => "Brutan",
            2 => "Sisterhood",
            3 => "Emperor",
            _ => throw new ArgumentException("Invalid faction index")
        };

        // ui stuff
        List<string> recieverOptions = worldState.characters.Keys.ToList();
        recieverOptions.Remove(playerFaction);
        ui.SetCharacterLabels(recieverOptions.ToArray());

        //
        string firstReceiver = recieverOptions[0];
        ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[firstReceiver]);
        sender = playerFaction;

        //

        // if (!mode)
        // {
        //     return;
        // }
        // else
        // {
        //     return;
        // }
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
        if (worldState.characters.ContainsKey(characterName))
        {
            ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[characterName]);
        }
        else
        {
            GD.PrintErr($"Unknown character: {characterName}");
        }
    }
}
