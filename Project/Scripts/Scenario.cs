using System.Collections.Generic;

// Represents one evaluation scenario: a scripted sequence of player messages,
// a set of initial world-state overrides, and a turn cap at which the scenario ends.
// The Description is included in the log and passed to the judge LLM for scoring.
public class Scenario
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }           // shown to judge LLM, not to agents
    public Dictionary<string, int> StateOverrides { get; set; } = new();
    public List<PlayerMessage> PlayerScript { get; set; } = new();
    public int TurnCap { get; set; } = 8;
}