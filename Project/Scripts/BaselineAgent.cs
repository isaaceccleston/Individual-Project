using System;
using System.Collections.Generic;

// Deterministic, non-LLM stand-in for an agent. Used as the comparison
// baseline when evaluating the LLM agents: same routing format, same
// turn cadence, but responses are drawn from a small per-faction template
// bank instead of generated. No HTTP calls, instant return.
public static class BaselineAgent
{
    private static readonly Random rng = new();

    private static readonly Dictionary<string, string[]> templates = new()
    {
        ["Aurellian"] = new[]
        {
            "House Aurellian honours the words of the wise and seeks unity in this trying time.",
            "We must consider the long view; rash action benefits no one.",
            "Tradition and the rule of law remain our guiding stars.",
            "Trust must be earned, and we extend it cautiously.",
            "The galaxy is best served by alliance, not aggression.",
            "We will not be drawn into the schemes of lesser houses.",
        },
        ["Brutan"] = new[]
        {
            "Strength is the only currency that matters in this galaxy.",
            "Words are cheap; results decide the fate of houses.",
            "Brutan acts when others hesitate.",
            "Your flattery is noted, but our position is firm.",
            "The weak fall, the strong rise. So it has always been.",
            "We owe no explanations to those beneath us.",
        },
        ["Sisterhood"] = new[]
        {
            "The threads of fate weave in patterns mortal eyes cannot follow.",
            "We watch, we listen, and in time, we act.",
            "Truth reveals itself to those who are patient.",
            "Each move is part of a larger design.",
            "The Sisterhood will not be hurried.",
            "Some questions are best left to the silence.",
        },
        ["Emperor"] = new[]
        {
            "The Imperium's word is final on this matter.",
            "We have heard your concerns and shall consider them.",
            "Loyalty to the throne remains the measure of all houses.",
            "Order must be maintained at all costs.",
            "The Emperor's gaze is upon you all.",
            "Speak plainly, and remember to whom you speak.",
        },
    };

    // Returns a (target, content) pair following the same routing convention
    // as the LLM agents: target is "All" for public, or a faction name for
    // private. The reply mirrors the privacy of the most recent inbound
    // message — if the player addressed this faction directly, reply privately.
    public static (string target, string content) Respond(
        string faction,
        string lastSender,
        string lastTarget,
        WorldState worldState)
    {
        string target;
        if (lastTarget == faction && !string.IsNullOrEmpty(lastSender))
        {
            target = lastSender;
        }
        else
        {
            target = "All";
        }

        if (!templates.TryGetValue(faction, out var bank) || bank.Length == 0)
        {
            return (target, "[no template]");
        }

        // pick deterministically-ish: vary by global tension to give a tiny
        // amount of state-sensitivity, but stay reproducible per turn.
        int idx = (rng.Next(bank.Length) + (worldState?.globalTension ?? 0)) % bank.Length;
        return (target, bank[idx]);
    }
}
