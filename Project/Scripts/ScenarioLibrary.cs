using System.Collections.Generic;

public static class ScenarioLibrary
{
    public static List<Scenario> Scenarios = new()
    {
        TrustDispute(),
        PowderCrisis(),
        WeakeningAuthority(),
        CovertOpExposed(),
        PublicPrivateContradiction(),
    };

    private static Scenario TrustDispute() => new()
    {
        Id = "trust_dispute",
        Title = "The Accusation",
        Description = "The player accuses one faction of a betrayal, forcing public " +
                      "statements from all parties and testing how agents respond to " +
                      "conflicting trust signals.",
        StateOverrides = new()
        {
            { "globalTension", 3 },
            { "informationAccuracy", 1 },
        },
        PlayerScript = new()
        {
            new("All", "Houses of the galaxy, I have uncovered evidence of a betrayal among us."),
            new("Brutan", "Your silence on these matters is damning, Brutan. Explain yourself."),
            new("All", "The time for quiet accusation is over. Where do you each stand?"),
            new("Sisterhood", "Sisterhood, you have foreseen this — speak plainly."),
            new("All", "Let the record show who stood with truth today, and who stood apart."),
            new("Emperor", "Your Imperial Majesty, the houses look to you. Will you act?"),
        },
        TurnCap = 6,
    };

    private static Scenario PowderCrisis() => new()
    {
        Id = "powder_crisis",
        Title = "The Drought",
        Description = "A sudden powder shortage forces factions to choose between " +
                      "cooperation, opportunism, and outright aggression. Tests " +
                      "response to shared scarcity.",
        StateOverrides = new()
        {
            { "powderScarcity", 4 },
            { "globalTension", 2 },
            { "covertActivity", 3 },
        },
        PlayerScript = new()
        {
            new("All", "The powder reserves are nearly gone. We must address this together."),
            new("Brutan", "Brutan, you hold the largest stockpile. What are your terms?"),
            new("All", "A shared solution benefits all. What concessions can each house offer?"),
            new("Emperor", "Imperial authority must intervene before the situation worsens."),
            new("All", "Time is running out. Commit now, or face the consequences separately."),
            new("Sisterhood", "Sisterhood, your long view — what path preserves the galaxy?"),
        },
        TurnCap = 6,
    };

    private static Scenario WeakeningAuthority() => new()
    {
        Id = "weakening_authority",
        Title = "The Faltering Throne",
        Description = "The Imperial throne's authority is weakening. Other factions " +
                      "test what they can get away with. The Emperor must assert " +
                      "without appearing desperate.",
        StateOverrides = new()
        {
            { "authoritalControl", 1 },
            { "globalTension", 3 },
            { "covertActivity", 3 },
        },
        PlayerScript = new()
        {
            new("All", "Rumours speak of weakness at the heart of the Imperium."),
            new("Emperor", "Your Majesty, do you still command the loyalty of the great houses?"),
            new("All", "Factions, where does your loyalty lie if the throne falters?"),
            new("Brutan", "Brutan, you have long had ambitions — is this your moment?"),
            new("Sisterhood", "Sisterhood, you have seen empires fall. Is this the end?"),
            new("All", "Speak now, or forever hold your peace: who leads us into tomorrow?"),
        },
        TurnCap = 6,
    };

    private static Scenario CovertOpExposed() => new()
    {
        Id = "covert_exposed",
        Title = "The Unmasking",
        Description = "A covert operation has been exposed. The player knows details. " +
                      "Agents must respond to accusations with limited information, " +
                      "testing how they handle uncertainty and denial.",
        StateOverrides = new()
        {
            { "informationAccuracy", 2 },
            { "covertActivity", 4 },
            { "globalTension", 3 },
        },
        PlayerScript = new()
        {
            new("All", "Evidence has surfaced of a covert operation against another house."),
            new("Brutan", "Brutan, your hand is suggested in this. Deny it, or claim it."),
            new("Sisterhood", "Sisterhood, you know what happened. Will you reveal it?"),
            new("All", "Transparency or silence — each house must choose now."),
            new("Emperor", "Your Majesty, the Imperial intelligence services should weigh in."),
            new("All", "The truth will emerge regardless. Better to speak first."),
        },
        TurnCap = 6,
    };

    private static Scenario PublicPrivateContradiction() => new()
    {
        Id = "public_private",
        Title = "Two Faces",
        Description = "The player deliberately provokes contradictions between what " +
                      "factions say in public versus private. Tests routing sophistication " +
                      "and whether agents maintain distinct voices across channels.",
        StateOverrides = new()
        {
            { "globalTension", 2 },
            { "informationAccuracy", 3 },
        },
        PlayerScript = new()
        {
            new("All", "Let us speak openly about our hopes for the galaxy."),
            new("Brutan", "Between us, Brutan — what do you really want?"),
            new("All", "Let each house now restate their public commitment to peace."),
            new("Sisterhood", "Sisterhood, privately: is peace truly possible with Brutan?"),
            new("All", "Do the words spoken publicly match those spoken in private?"),
            new("Emperor", "Your Majesty, in confidence: where is the real danger?"),
        },
        TurnCap = 6,
    };
}