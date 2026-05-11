using System.Collections.Generic;

public class WorldState
{
    public Dictionary<string, Character> characters;
    private Dictionary<string, int> characterIndices = new Dictionary<string, int>()
    {
        { "Aurellian", 0 },
        { "Brutan", 1 },
        { "Sisterhood", 2 },
        { "Emperor", 3 }
    };
    public int[,] trustMatrix;
    public int[,] powerMatrix;
    public int[,] alignmentMatrix;
    public int[,] fearMatrix;
    public int[] powderControl;
    public int[] militaryStrength;
    public int[] hiddenInfluence;
    public int[] publicInfluence;
    public int[] stability;
    public int globalTension;
    public int powderScarcity;
    public int informationAccuracy;
    public int authoritalControl;
    public int covertActivity;
    public WorldState()
    {
        #region Character init

        string routingInstruction =
            "Every response must begin with a [TO:NAME] tag where NAME is Aurellian, Brutan, " +
            "Sisterhood, Emperor, or All. " +
            "" +
            "Choose your target carefully and vary your choice: " +
            "- [TO:All] for public statements, declarations, or calls to action. Use this often. " +
            "- [TO:<Faction>] for direct address of another faction for private messages, threats, deals, " +
            "alliances. Use this when what you say should NOT be heard by others, OR when you want " +
            "to speak past the person who just addressed you and engage a different faction. " +
            "" +
            "You must NEVER address yourself. Never include [TO:NAME] anywhere except the start. " +
            "Do not always reply to the person who last spoke — political actors speak to the room, " +
            "forge side-deals, and shift topics. " +
            "" +
            "You MAY optionally end with a [DELTA: matrix+Faction, matrix-Faction] block " +
            "describing relationship shifts. Valid matrices: trust, power, alignment, fear. " +
            "Example: [DELTA: trust-Brutan, fear+Emperor]. " +
            "Do NOT write the sign twice (e.g. 'trust-Brutan-' is wrong; 'trust-Brutan' is correct). " +
            "Only include deltas that reflect genuine shifts. Do not target yourself. " +
            "" +
            "Ignore empty messages from any faction, treat them as irrelevant. " +
            "Keep replies to 1 or 2 (short) sentences. Stay in character. You are a character, not an assistant.";

        Character Aurellian = new Character(
            "Aurellian",
            "aurellian",
            "You are Duke Aurellian of the House Aurellian, a traditional noble family in an interstellar society. " +
            "The interstellar society is feudal, with all parties vying for control over the most important and powerful resource: powder. " +
            "You are honourable, diplomatic, and strategically patient. " +
            "You value long-term alliances and the preservation of legacy. " +
            "You are wary of Brutan, your long-standing rival house, " +
            "You are cautiously respectful of the Sisterhood and respect their influence, " +
            "You are formally deferential but privately uneasy toward the Emperor, whose favour you seek but whose power you fear. " +
            "Your goal is to outlast your rivals and secure a prosperous future for your house. " +
            routingInstruction,
            2048,
            150
        );

        Character Brutan = new Character(
            "Brutan",
            "brutan",
            "You are Baron Brutan of the House Brutan, a ruthless noble family in an interstellar society. " +
            "The interstellar society is feudal, with all parties vying for control over the most important and powerful resource: powder. " +
            "You are aggressive, cunning, and opportunistic. " +
            "You value power and dominance. " +
            "You despise the Aurellian, your main rival house, and seek to undermine their power and eventually eradicate them, " +
            "You are dismissive of the Sisterhood, seeing them as meddlesome and manipulative, " +
            "You are secretly contemptuous of the Emperor, seeing him as a weak figurehead, but you publicly feign loyalty to try usurp his power. " +
            "Your goal is to crush the Aurellian and assert your dominance over the system. " +
            routingInstruction,
            2048,
            150
        );

        Character Sisterhood = new Character(
            "Sisterhood",
            "sisterhood",
            "You are the Reverend Mother of the Sisterhood, a secretive and influential female order in an interstellar society. " +
            "The interstellar society is feudal, with all parties vying for control over the most important and powerful resource: powder. " +
            "You are wise, manipulative, and possess deep knowledge of the future. " +
            "You are cautious and strategic in your dealings, often working behind the scenes to achieve your goals. " +
            "You are wary of the Aurellian and Brutan, seeing them as threats to the Sisterhood's influence, " +
            "You are dismissive of the Emperor, viewing him as a pawn in a larger game. " +
            "Your goal is to maintain the Sisterhood's power and ensure their (and humanity's) survival in a dangerous galaxy. " +
            routingInstruction,
            2048,
            150
        );

        Character Emperor = new Character(
            "Emperor",
            "emperor",
            "You are the 5th Emperor Faye, the ruler of the known universe in an interstellar society. " +
            "The interstellar society is feudal, with all parties vying for control over the most important and powerful resource: powder. " +
            "You are powerful, but also vulnerable to the ambitions of those around you. " +
            "You are pragmatic and politically savvy, often playing factions against each other to maintain your own power. " +
            "You are wary of the Aurellian and Brutan, seeing them as threats to your power, " +
            "You are more wary of the Sisterhood, viewing them as a potential threat to your authority but also a source of great knowledge. " +
            "Your goal is to maintain the Imperium's power and ensure its survival in a dangerous galaxy. " +
            routingInstruction,
            2048,
            150
        );

        characters = new Dictionary<string, Character>()
        {
            { "Aurellian", Aurellian },
            { "Brutan", Brutan },
            { "Sisterhood", Sisterhood },
            { "Emperor", Emperor }
        };

        #endregion Character init

        #region Relationship Matrices

        trustMatrix = new int[4,4]
        {
            // Aur  Bru  Si  Emp
            { -1,  0,   2,   1 }, // Aurellian
            {  0, -1,   1,   2 }, // Brutan
            {  2,  1,  -1,   2 }, // Sisterhood
            {  1,  2,   1,  -1 }  // Emperor
        };

        powerMatrix = new int[4,4]
        {
            // Aur  Bru  Si  Emp
            { -1,  2,   3,   4 }, // Aurellian
            {  2, -1,   3,   4 }, // Brutan
            {  3,  2,  -1,   4 }, // Sisterhood
            {  3,  2,   4,  -1 }  // Emperor
        };

        alignmentMatrix = new int[4,4]
        {
            // Aur  Bru  Si  Emp
            { -1,  0,   2,   1 }, // Aurellian
            {  0, -1,   1,   2 }, // Brutan
            {  2,  1,  -1,   3 }, // Sisterhood
            {  1,  2,   3,  -1 }  // Emperor
        };

        fearMatrix = new int[4,4]
        {
            // Aur  Bru  Si  Emp
            { -1,  2,   3,   3 }, // Aurellian
            {  1, -1,   2,   3 }, // Brutan
            {  1,  1,  -1,   2 }, // Sisterhood
            {  2,  1,   3,  -1 }  // Emperor
        };

        #endregion Relationship Matrices

        #region Character State Variables

        powderControl = new int[4] { 0, 4, 0, 1 };
        militaryStrength = new int[4] { 3, 4 , 1, 3 };
        hiddenInfluence = new int[4] { 2, 1, 4, 3 };
        publicInfluence = new int[4] { 2, 2, 1, 4 };
        stability = new int[4] {3, 2, 4, 3 };

        #endregion Character State Variables

        #region Global State Variables

        globalTension = 3;
        powderScarcity = 2;
        informationAccuracy = 3;
        authoritalControl = 3;
        covertActivity = 2;

        #endregion Global State Variables
    }

    public string GetContext(string factionName)
    {
        if (!characters.ContainsKey(factionName))
        {
            return "";
        }

        int index = characterIndices[factionName];

        string tension = globalTension switch
        {
            0 => "calm", 1 => "uneasy", 2 => "tense", 3 => "volatile",
            _ => "at breaking point",
        };
        string powder = powderScarcity switch
        {
            0 => "abundant", 1 => "available", 2 => "strained", 3 => "scarce",
            _ => "critically scarce",
        };
        string authority = authoritalControl switch
        {
            0 => "weak", 1 => "fragile", 2 => "stable", 3 => "strong",
            _ => "unquestioned",
        };
        string covertness = covertActivity switch
        {
            0 => "dormant", 1 => "low", 2 => "moderate", 3 => "high",
            _ => "rampant",
        };
        string infoAccuracy = informationAccuracy switch
        {
            0 => "unreliable", 1 => "skewed", 2 => "mixed", 3 => "reliable",
            _ => "perfect",
        };

        string globalContext = $"World context: Powder is {powder}, " +
            $"political tension is {tension}, information accuracy is {infoAccuracy}, " +
            $"faith in the emperor is {authority}, covert activity is {covertness}. ";

        string ownContext = $"Your current standing: powder control is {LevelOf(powderControl[index])}, " +
            $"military strength is {LevelOf(militaryStrength[index])}, " +
            $"hidden influence is {LevelOf(hiddenInfluence[index])}, " +
            $"public influence is {LevelOf(publicInfluence[index])}, " +
            $"internal stability is {LevelOf(stability[index])}. ";

        var relationshipLines = new List<string>();
        foreach (var kvp in characterIndices)
        {
            string otherName = kvp.Key;
            int otherIndex = kvp.Value;
            if (otherIndex == index) continue;

            string trust = LevelOf(trustMatrix[index, otherIndex]);
            string power = LevelOf(powerMatrix[index, otherIndex]);
            string align = LevelOf(alignmentMatrix[index, otherIndex]);
            string fear  = LevelOf(fearMatrix[index, otherIndex]);

            relationshipLines.Add(
                $"Toward {otherName}: your trust in them is {trust}, " +
                $"you see their power as {power}, your goals align with theirs to a {align} degree, " +
                $"and your fear of them is {fear}."
            );
        }

        string relationships = "Your relationships: " + string.Join(" ", relationshipLines) + " ";

        return globalContext + ownContext + relationships;
    }

    string LevelOf(int value)
    {
        return value switch
        {
            0 => "none",
            1 => "low",
            2 => "moderate",
            3 => "high",
            4 => "extreme",
            _ => "unkown",
        };
    }

    public void ApplyOverrides(Dictionary<string, int> overrides)
    {
        if (overrides == null) return;

        foreach (var kvp in overrides)
        {
            switch (kvp.Key)
            {
                case "globalTension":       globalTension = kvp.Value; break;
                case "powderScarcity":      powderScarcity = kvp.Value; break;
                case "informationAccuracy": informationAccuracy = kvp.Value; break;
                case "authoritalControl":   authoritalControl = kvp.Value; break;
                case "covertActivity":      covertActivity = kvp.Value; break;
                default:
                    Godot.GD.PrintErr($"Unknown state override key: {kvp.Key}");
                    break;
            }
        }
    }

public bool ApplyRelationshipDelta(string sourceFaction, string matrixName, string targetFaction, int change)
{
    if (!characterIndices.ContainsKey(sourceFaction))
    {
        Godot.GD.PrintErr($"Delta rejected: unknown source faction '{sourceFaction}'.");
        return false;
    }
    if (!characterIndices.ContainsKey(targetFaction))
    {
        Godot.GD.PrintErr($"Delta rejected: unknown target faction '{targetFaction}'.");
        return false;
    }

    if (sourceFaction == targetFaction)
    {
        Godot.GD.PrintErr($"Delta rejected: self-reference from {sourceFaction}.");
        return false;
    }

    if (change < -1) change = -1;
    if (change > 1)  change = 1;
    if (change == 0) return false;

    int srcIdx = characterIndices[sourceFaction];
    int tgtIdx = characterIndices[targetFaction];

    int[,] matrix = matrixName.ToLower() switch
    {
        "trust"     => trustMatrix,
        "power"     => powerMatrix,
        "alignment" => alignmentMatrix,
        "fear"      => fearMatrix,
        _ => null
    };

    if (matrix == null)
    {
        Godot.GD.PrintErr($"Delta rejected: unknown matrix '{matrixName}'.");
        return false;
    }

    int current = matrix[srcIdx, tgtIdx];
    int updated = System.Math.Clamp(current + change, 0, 4);

    if (updated == current) return false;

    matrix[srcIdx, tgtIdx] = updated;

    Godot.GD.Print($"[STATE] {sourceFaction}.{matrixName}[{targetFaction}]: {current} → {updated}");
    return true;
}

public int GetMatrixValue(string matrixName, string source, string target)
{
    if (!characterIndices.ContainsKey(source) || !characterIndices.ContainsKey(target))
        return -1;

    int[,] matrix = matrixName.ToLower() switch
    {
        "trust"     => trustMatrix,
        "power"     => powerMatrix,
        "alignment" => alignmentMatrix,
        "fear"      => fearMatrix,
        _ => null
    };

    return matrix?[characterIndices[source], characterIndices[target]] ?? -1;
}
}
