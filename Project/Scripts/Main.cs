using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
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
    private Queue<string> agentTurnQueue = new();
    private bool isPlayerTurn = true;

    public override void _Ready()
    {
        //
        startUI = GetNode<StartUI>("StartUI");
        ui = GetNode<UI>("UI");
        ui.Visible = false;

        startUI.StartGameSignal += OnStartGame;

        //
        ui.MessageSubmitted += OnUserMessage;
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
        ui.SetPlayerFaction(playerFaction);

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
        if (!isPlayerTurn)
        {
            GD.Print("It's not the player's turn, message ignored.");
            return;
        }

        string target = ui.GetCurentReciever();
        if(string.IsNullOrEmpty(target))
        {
            GD.PrintErr("No target selected, defaulted to ALL.");
            target = "All";
        }
        
        isPlayerTurn = false;

        BroadcastMessage(playerFaction, target, message);

        ollamaInterface.logger.LogMessage(
            playerFaction, 
            target, 
            "user", 
            message, 
            (int)(message.Length / 3.5f)); // Rough token estimate

        ui.OnModelReply(playerFaction, target, message);

        agentTurnQueue.Clear();
        foreach(string name in worldState.characters.Keys)
        {
            if(name != playerFaction)
            {
                agentTurnQueue.Enqueue(name);
            }
        }

        StartNextAgentTurn();
    }

    private void OnModelReply(string senderName, string target, string message)
    {
        if(target != "All" && !worldState.characters.ContainsKey(target))
        {
            GD.PrintErr($"Received message for unknown target '{target}', sending to ALL.");
            target = "All";
        }

        ui.OnModelReply(senderName, target, message);

        BroadcastMessage(senderName, target, message);

        StartNextAgentTurn();
    }

    private void BroadcastMessage(string sender, string target, string message)
    {
        foreach (var kvp in worldState.characters)
        {
            string factionName = kvp.Key;
        
            if(factionName == sender)
            {
                continue; // Don't send the message to the sender's own model session
            }
            if(factionName == playerFaction)
            {
                continue; // Don't send the message to the player's model session
            }

            bool hears = target == "All" || target == factionName;
            if (!hears)
            {
                continue;
            }

            var session = ollamaInterface.chatManager.GetOrCreateSession(kvp.Value);
            session.ObserveMessage(sender, target, message);
        }
    }

    private void StartNextAgentTurn()
    {
        if (agentTurnQueue.Count == 0)
        {
            isPlayerTurn = true;
            GD.Print("All agents have taken their turn. It's now the player's turn.");
            return;
        }

        string nextAgent = agentTurnQueue.Dequeue();
        GD.Print($"It's now {nextAgent}'s turn to respond.");

        ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[nextAgent]);

        var timer = GetTree().CreateTimer(0.5f);
        timer.Timeout += () =>
        {
            ollamaInterface.PromptCurrentAgent();
        };
    }
}
