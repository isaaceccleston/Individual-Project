using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Main : Node2D
{
    OllamaInterface ollamaInterface;
    UI ui;
    StartUI startUI;
    WorldState worldState = new WorldState();
    string playerFaction = "";
    string sender = "User";
    private Queue<string> agentTurnQueue = new();
    private bool isPlayerTurn = true;
    private Scenario currentScenario;
    private int scenarioTurnCount = 0;
    private bool isAutomatedRun = false;
    private string currentMode = "LLM";
    private int currentRunIndex = 0;
    private bool isBaselineMode = false;
    private string lastInboundSender = "";
    private string lastInboundTarget = "All";

    public override void _Ready()
    {
        SetProcessInput(true);
        
        startUI = GetNode<StartUI>("StartUI");
        ui = GetNode<UI>("UI");
        ui.Visible = false;

        startUI.StartGameSignal += OnStartGame;

        ui.MessageSubmitted += OnUserMessage;
        ui.ExportLogRequested += ExportLog;
        ui.SummariseRequested += RequestSummary;

        ollamaInterface = GetNode<OllamaInterface>("OllamaInterface");
        ollamaInterface.worldState = worldState;

        ollamaInterface.ModelReply += OnModelReply;
    }

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

        List<string> recieverOptions = worldState.characters.Keys.ToList();
        recieverOptions.Remove(playerFaction);
        ui.SetCharacterLabels(recieverOptions.ToArray());
        ui.SetPlayerFaction(playerFaction);

        string firstReceiver = recieverOptions[0];
        ollamaInterface.currentSession = ollamaInterface.chatManager.GetOrCreateSession(worldState.characters[firstReceiver]);
        sender = playerFaction;
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
                continue;
            }
            if(factionName == playerFaction)
            {
                continue;
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
            0,
            appliedDeltas);

        OnModelReply(agentName, target, content);
    }

    public void StartScenario(Scenario scenario, string mode, int runIndex, bool baseline = false)
    {
        GD.Print($"STARTING SCENARIO: {scenario.Id} | mode={mode} | run={runIndex}");

        currentScenario = scenario;
        currentMode = mode;
        currentRunIndex = runIndex;
        scenarioTurnCount = 0;
        isAutomatedRun = true;
        isBaselineMode = baseline;

        ollamaInterface.chatManager = new ChatManager();
        ollamaInterface.logger = new ConversationLogger();

        worldState = new WorldState();
        worldState.ApplyOverrides(scenario.StateOverrides);
        ollamaInterface.worldState = worldState;

        isPlayerTurn = true;
        FireNextScriptedMessage();
    }

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

        string target = (next.Target == playerFaction) ? "All" : next.Target;

        GD.Print($"[SCENARIO] Player turn {scenarioTurnCount}: [{target}] {next.Content}");
        SendPlayerMessage(target, next.Content);
    }

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
