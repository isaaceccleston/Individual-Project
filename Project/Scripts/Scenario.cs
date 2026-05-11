using System.Collections.Generic;

public class Scenario
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Dictionary<string, int> StateOverrides { get; set; } = new();
    public List<PlayerMessage> PlayerScript { get; set; } = new();
    public int TurnCap { get; set; } = 6;
}