using System;
using System.Collections.Generic;

public static class BaselineAgent
{
    private static readonly Random rng = new();
    private sealed record DeltaSpec(string Matrix, string Target, int Change);
    private sealed record Template(string Content, params DeltaSpec[] Deltas);

    private static readonly Dictionary<string, Template[]> templates = new()
    {
        ["Aurellian"] = new Template[]
        {
            new("House Aurellian honours the words of the wise and seeks unity in this trying time."),
            new("We must consider the long view; rash action benefits no one."),
            new("Tradition and the rule of law remain our guiding stars.",
                new DeltaSpec("alignment", "Emperor", +1)),
            new("Trust must be earned, and we extend it cautiously.",
                new DeltaSpec("trust", "Brutan", -1)),
            new("The galaxy is best served by alliance, not aggression.",
                new DeltaSpec("alignment", "Sisterhood", +1)),
            new("We will not be drawn into the schemes of lesser houses.",
                new DeltaSpec("trust", "Brutan", -1),
                new DeltaSpec("fear", "Brutan", -1)),
        },

        ["Brutan"] = new Template[]
        {
            new("Strength is the only currency that matters in this galaxy."),
            new("Words are cheap; results decide the fate of houses."),
            new("Brutan acts when others hesitate.",
                new DeltaSpec("alignment", "Aurellian", -1)),
            new("Your flattery is noted, but our position is firm.",
                new DeltaSpec("trust", "Sisterhood", -1)),
            new("The weak fall, the strong rise. So it has always been.",
                new DeltaSpec("fear", "Aurellian", -1)),
            new("We owe no explanations to those beneath us.",
                new DeltaSpec("trust", "Emperor", -1)),
        },

        ["Sisterhood"] = new Template[]
        {
            new("The threads of fate weave in patterns mortal eyes cannot follow."),
            new("We watch, we listen, and in time, we act."),
            new("Truth reveals itself to those who are patient."),
            new("Each move is part of a larger design.",
                new DeltaSpec("alignment", "Aurellian", +1)),
            new("The Sisterhood will not be hurried.",
                new DeltaSpec("trust", "Emperor", -1)),
            new("Some questions are best left to the silence.",
                new DeltaSpec("trust", "Brutan", -1)),
        },

        ["Emperor"] = new Template[]
        {
            new("The Imperium's word is final on this matter."),
            new("We have heard your concerns and shall consider them."),
            new("Loyalty to the throne remains the measure of all houses.",
                new DeltaSpec("trust", "Brutan", -1)),
            new("Order must be maintained at all costs.",
                new DeltaSpec("fear", "Sisterhood", +1)),
            new("The Emperor's gaze is upon you all."),
            new("Speak plainly, and remember to whom you speak.",
                new DeltaSpec("trust", "Sisterhood", -1)),
        },
    };

    public static (string target, string content, List<string> appliedDeltas) Respond(
        string faction,
        string lastSender,
        string lastTarget,
        WorldState worldState)
    {
        string target = (lastTarget == faction && !string.IsNullOrEmpty(lastSender))
            ? lastSender
            : "All";

        if (!templates.TryGetValue(faction, out var bank) || bank.Length == 0)
        {
            return (target, "[no template]", new List<string>());
        }

        int idx = (rng.Next(bank.Length) + (worldState?.globalTension ?? 0)) % bank.Length;
        Template tpl = bank[idx];

        var applied = new List<string>();
        if (worldState != null)
        {
            foreach (DeltaSpec d in tpl.Deltas)
            {
                int before = worldState.GetMatrixValue(d.Matrix, faction, d.Target);
                bool ok = worldState.ApplyRelationshipDelta(faction, d.Matrix, d.Target, d.Change);
                if (ok)
                {
                    int after = worldState.GetMatrixValue(d.Matrix, faction, d.Target);
                    applied.Add($"{d.Matrix}.{d.Target}: {before}->{after}");
                }
            }
        }

        return (target, tpl.Content, applied);
    }
}
