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
    // eval stuff
    private Scenario currentScenario;
    private int scenarioTurnCount = 0;
    private bool isAutomatedRun = false;
    private string currentMode = "LLM";     // "LLM", "Scripted", or "Baseline"
    private int currentRunIndex = 0;
    private bool isBaselineMode = false;    // overridden per-scenario by F-key
    //
    // last inbound message — baseline agents need this to decide reply target
    private string lastInboundSender = "";
    private string lastInboundTarget = "All";

    public override void _Ready()
    {
        //
        SetProcessInput(true);
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

    // F1–F5 → LLM scenarios 1–5, F6–F10 → baseline (non-LLM) scenarios 1–5.
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed) return;

        int scenarioIdx = keyEvent.Keycode switch
        {
            Key.F1 => 0, Key.F2 => 1, Key.F3 => 2, Key.F4 => 3, Key.F5 => 4,
            Key.F6 => 0, Key.F7 => 1, Key.F8 => 2, Key.F9 => 3, Key.F10 => 4,
            _ => -1
        };

        if (scenarioIdx < 0) return;
        if (scenarioIdx >= ScenarioLibrary.Scenarios.Count) return;
        if (isAutomatedRun)
        {
            GD.Print("Already running a scenario.");
            return;
        }

        // playerFaction must be set by having started the game normally first
        if (string.IsNullOrEmpty(playerFaction))
        {
            GD.PrintErr("Start the game first (pick a faction), then press F1-F10.");
            return;
        }

        bool baseline = keyEvent.Keycode >= Key.F6 && keyEvent.Keycode <= Key.F10;
        string mode = baseline ? "Baseline" : "LLM";

        StartScenario(ScenarioLibrary.Scenarios[scenarioIdx], mode, currentRunIndex + 1, baseline);
    }

    private void OnStartGame(bool mode, int faction)
    {
        GD.Print($"Starting game with mode: {(mode ? "LLM" : "Baseline")}, faction: {faction}"); // Debug log
        currentMode = mode ? "LLM" : "Scripted";

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

    // Called from UI signal
    public void OnUserMessage(string message)
    {
        if (!isPlayerTurn)
        {
            GD.Print("It's not the player's turn, message ignored.");
            return;
        }
        if (isAutomatedRun)
        {
            GD.Print("Scenario is driving input — manual messages ignored.");
            return;
        }

        string target = ui.GetCurentReciever();
        if (string.IsNullOrEmpty(target))
        {
            GD.PrintErr("No target selected, defaulted to ALL.");
            target = "All";
        }

        SendPlayerMessage(target, message);
    }

    //used by both UI and scripted player.
    private void SendPlayerMessage(string target, string message)
    {
        isPlayerTurn = false;

        BroadcastMessage(playerFaction, target, message);

        ollamaInterface.logger.LogMessage(
            playerFaction,
            target,
            "user",
            message,
            (int)(message.Length / 3.5f));

        ui.OnModelReply(playerFaction, target, message);

        lastInboundSender = playerFaction;
        lastInboundTarget = target;

        agentTurnQueue.Clear();
        foreach (string name in worldState.characters.Keys)
        {
            if (name != playerFaction)
                agentTurnQueue.Enqueue(name);
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

    //
    private void StartNextAgentTurn()
    {
        if (agentTurnQueue.Count == 0)
        {
            isPlayerTurn = true;
            GD.Print("All agents have taken their turn. It's now the player's turn.");

            // If we're in an automated run, fire the next scripted message after a short pause
            if (isAutomatedRun)
            {
                var scenarioTimer = GetTree().CreateTimer(1.0f);
                scenarioTimer.Timeout += () => FireNextScriptedMessage();
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
            ollamaInterface.currentSession =
                ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[nextAgent]);

            var timer = GetTree().CreateTimer(0.5f);
            timer.Timeout += () => ollamaInterface.PromptCurrentAgent();
        }
    }

    // Fire one templated baseline reply for the named agent.
    // Logs and routes through OnModelReply so the rest of the pipeline (UI,
    // BroadcastMessage, scenario advance) is identical to the LLM path.
    private void RunBaselineAgentTurn(string agentName)
    {
        var (target, content, appliedDeltas) = BaselineAgent.Respond(
            agentName, lastInboundSender, lastInboundTarget, worldState);

        ollamaInterface.logger.LogMessage(
            agentName,
            target,
            "assistant",
            content,
            (int)(content.Length / 3.5f),
            0, // baseline has no real latency
            appliedDeltas);

        OnModelReply(agentName, target, content);
    }

    //
    public void StartScenario(Scenario scenario, string mode, int runIndex, bool baseline = false)
    {
        GD.Print($"STARTING SCENARIO: {scenario.Id} | mode={mode} | run={runIndex}");

        currentScenario = scenario;
        currentMode = mode;
        currentRunIndex = runIndex;
        scenarioTurnCount = 0;
        isAutomatedRun = true;
        isBaselineMode = baseline;

        // Fresh sessions so no history leaks between runs
        ollamaInterface.chatManager = new ChatManager();
        ollamaInterface.logger = new ConversationLogger();

        // Rebuild world state from scratch + apply scenario's overrides
        worldState = new WorldState();
        worldState.ApplyOverrides(scenario.StateOverrides);
        ollamaInterface.worldState = worldState;

        isPlayerTurn = true;
        FireNextScriptedMessage();
    }

    // Fire the next player script message, or end the scenario if exhausted.
    private void FireNextScriptedMessage()
    {
        if (scenarioTurnCount >= currentScenario.TurnCap ||
            scenarioTurnCount >= currentScenario.PlayerScript.Count)
        {
            EndScenario();
            return;
        }

        PlayerMessage next = currentScenario.PlayerScript[scenarioTurnCount];
        scenarioTurnCount++;

        // if the script targets the player's own faction, broadcast instead
        // (scripts are written generically and may name any of the four)
        string target = (next.Target == playerFaction) ? "All" : next.Target;

        GD.Print($"[SCENARIO] Player turn {scenarioTurnCount}: [{target}] {next.Content}");
        SendPlayerMessage(target, next.Content);
    }

    // Called when the player script is exhausted or the turn cap is hit.
    private void EndScenario()
    {
        GD.Print($"=== SCENARIO COMPLETE: {currentScenario.Id} ===");

        string filename = $"{currentMode.ToLower()}_{currentScenario.Id}_run{currentRunIndex}.json";
        ollamaInterface.logger.Export(filename);
        GD.Print($"Log exported: {filename}");

        isAutomatedRun = false;
        isBaselineMode = false;
        currentScenario = null;
    }
}
