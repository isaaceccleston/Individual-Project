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
    //
    // scenario runner state
    private Scenario activeScenario;
    private int scenarioTurn;
    private bool isBaselineMode;            // overridden by which F-key was pressed
    private bool gameStarted;
    //
    // last inbound for baseline routing
    private string lastInboundSender = "";
    private string lastInboundTarget = "All";

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

        // free-play mode chosen on the start screen; F-keys override per-run.
        isBaselineMode = !mode;
        gameStarted = true;
    }

    // F1–F5 → LLM scenarios 1–5, F6–F10 → baseline (non-LLM) scenarios 1–5.
    // Game must be started first (faction selected) before scenarios can run.
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!gameStarted) return;
        if (@event is not InputEventKey key || !key.Pressed || key.Echo) return;

        switch (key.Keycode)
        {
            case Key.F1: RunScenario(0, false); break;
            case Key.F2: RunScenario(1, false); break;
            case Key.F3: RunScenario(2, false); break;
            case Key.F4: RunScenario(3, false); break;
            case Key.F5: RunScenario(4, false); break;
            case Key.F6: RunScenario(0, true); break;
            case Key.F7: RunScenario(1, true); break;
            case Key.F8: RunScenario(2, true); break;
            case Key.F9: RunScenario(3, true); break;
            case Key.F10: RunScenario(4, true); break;
        }
    }

    private void RunScenario(int index, bool baseline)
    {
        if (index < 0 || index >= ScenarioLibrary.Scenarios.Count)
        {
            GD.PrintErr($"Scenario index {index} out of range.");
            return;
        }
        if (activeScenario != null)
        {
            GD.PrintErr($"Scenario {activeScenario.Id} already running — ignoring request.");
            return;
        }
        if (!isPlayerTurn)
        {
            GD.PrintErr("Cannot start scenario mid-turn; wait for current exchange to finish.");
            return;
        }

        activeScenario = ScenarioLibrary.Scenarios[index];
        scenarioTurn = 0;
        isBaselineMode = baseline;

        worldState.ApplyOverrides(activeScenario.StateOverrides);

        GD.Print($"=== Running scenario '{activeScenario.Id}' ({(baseline ? "baseline" : "LLM")} mode), {activeScenario.TurnCap} turns ===");

        FireNextScriptedPlayerMessage();
    }

    private void FireNextScriptedPlayerMessage()
    {
        if (activeScenario == null) return;

        if (scenarioTurn >= activeScenario.TurnCap || scenarioTurn >= activeScenario.PlayerScript.Count)
        {
            string fname = $"scenario_{activeScenario.Id}_{(isBaselineMode ? "baseline" : "llm")}.json";
            ollamaInterface.logger.Export(fname);
            GD.Print($"=== Scenario '{activeScenario.Id}' complete — log written to {fname} ===");
            activeScenario = null;
            return;
        }

        var pm = activeScenario.PlayerScript[scenarioTurn];
        scenarioTurn++;

        // if the script targets the player's own faction, broadcast instead
        // (scripts are written generically and may name any of the four)
        string target = (pm.Target == playerFaction) ? "All" : pm.Target;

        SendPlayerMessage(target, pm.Content);
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
        if (activeScenario != null)
        {
            GD.Print("Scenario is driving input — manual messages ignored.");
            return;
        }

        string target = ui.GetCurentReciever();
        if(string.IsNullOrEmpty(target))
        {
            GD.PrintErr("No target selected, defaulted to ALL.");
            target = "All";
        }

        SendPlayerMessage(target, message);
    }

    // Shared by manual input and the scripted scenario player.
    private void SendPlayerMessage(string target, string message)
    {
        isPlayerTurn = false;

        BroadcastMessage(playerFaction, target, message);

        ollamaInterface.logger.LogMessage(
            playerFaction,
            target,
            "user",
            message,
            (int)(message.Length / 3.5f)); // Rough token estimate

        ui.OnModelReply(playerFaction, target, message);

        lastInboundSender = playerFaction;
        lastInboundTarget = target;

        agentTurnQueue.Clear();
        foreach (string name in worldState.characters.Keys)
        {
            if (name != playerFaction)
            {
                agentTurnQueue.Enqueue(name);
            }
        }

        StartNextAgentTurn();
    }

    private void OnModelReply(string senderName, string target, string message)
    {
        if(target != "All" && !worldState.characters.ContainsKey(target) && target != playerFaction && target != "User")
        {
            GD.PrintErr($"Received message for unknown target '{target}', sending to ALL.");
            target = "All";
        }

        ui.OnModelReply(senderName, target, message);

        BroadcastMessage(senderName, target, message);

        lastInboundSender = senderName;
        lastInboundTarget = target;

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

            // if a scenario is active, advance the scripted player after a short pause
            if (activeScenario != null)
            {
                var t = GetTree().CreateTimer(0.5f);
                t.Timeout += FireNextScriptedPlayerMessage;
            }
            return;
        }

        string nextAgent = agentTurnQueue.Dequeue();
        GD.Print($"It's now {nextAgent}'s turn to respond.");

        if (isBaselineMode)
        {
            // baseline path: deterministic, no HTTP, near-instant
            var t = GetTree().CreateTimer(0.2f);
            t.Timeout += () => RunBaselineAgentTurn(nextAgent);
        }
        else
        {
            ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[nextAgent]);
            var timer = GetTree().CreateTimer(0.5f);
            timer.Timeout += () => ollamaInterface.PromptCurrentAgent();
        }
    }

    private void RunBaselineAgentTurn(string agentName)
    {
        var (target, content) = BaselineAgent.Respond(
            agentName, lastInboundSender, lastInboundTarget, worldState);

        // log it the same way LLM replies are logged so logs are comparable
        ollamaInterface.logger.LogMessage(
            agentName,
            target,
            "assistant",
            content,
            (int)(content.Length / 3.5f),
            0); // baseline has no real latency

        OnModelReply(agentName, target, content);
    }
}
