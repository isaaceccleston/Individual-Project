using System.Collections.Generic;
using System.Linq;

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
        /*
        Defines all active characters (agents) in the world. Each character represents
        a faction or actor capable of generating dialogue and making decisions.

        Constructor parameters:
            Name: display name used in logs and dialogue.
            ModelID: identifier for selecting a persona or model configuration (ollama).
            System Prompt: defines behaviour, tone, goals, and reply format.
            Context Window: max tokens the model can consider at once.
            Max Tokens: max length of the generated response.  
        */
        //credit cgpt for docs

        //
        string routingInstruction = "Every response must begin with a [TO:NAME] tag " +
                                    "where NAME is either Aurellian, Brutan, Sisterhood, Emperor, or All, indicating the intended recipient(s). " +
                                    "Messages to All can be read by every faction, unlike messages to specific factions. " +
                                    "Do not use the [TO:NAME] tag anywhere else in the message. " +
                                    "Keep replies to 1 sentence max. Stay in character. Do not break the fourth wall. You are a character not an assistant.";

        // House "Aurellian" (Atriedes)
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

        // House "Brutan" (Harkonnen)
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

        // The "Sisterhood" (Bene Gesserit)
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
        
        // The "Emperor" (Shaddam IV / Imperium)
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
        /*
        Defines how each character perceives others.

        Structure:
            matrix[x, y] where x = source character, and y = target character

        Value scale:
            0–4 (none to extreme, -1 for self-reference)

        Matrices:
            trustMatrix: how much X trusts Y (affects openness and cooperation)
            powerMatrix: how powerful X believes Y is (affects caution and deference)
            alignmentMatrix: how aligned X’s goals are with Y (affects cooperation vs conflict)
            fearMatrix: how much X fears Y (affects caution and indirect behaviour)

        Usage:
            Values can be combined to guide behaviour.
            Example: low trust + high fear → cautious or deceptive responses
        */
        //credit to cgpt for docs & 'strong emergent behavior' comment


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
        /*
        Defines shared but faction-specific state. Each value is tracked per character.

        Value scale:
            0–4 (low to high)

        Variables:
            powderControl[x]: how much control x has over the key resource.
            militaryStrength[x]: military capability of x.
            hiddenInfluence[x]: hidden influence within the broader political system.
            publicInfluence[x]: public influence within the broader political system.
            stability[x]: internal stability of the faction.
        */
        //credit to cgpt for docs


        powderControl = new int[4] { 0, 4, 0, 1 }; // pre-powder transfer
        militaryStrength = new int[4] { 3, 4 , 1, 3 };
        hiddenInfluence = new int[4] { 2, 1, 4, 3 }; //aurellian are respected, brutan are blegh, sisterhood are feared, emperor.
        publicInfluence = new int[4] { 2, 2, 1, 4 }; //aurellian are loved, brutan are hated, sisterhood are mysterious, emperor is revered.};
        stability = new int[4] {3, 2, 4, 3 };
        
        #endregion Character State Variables

        #region Global State Variables
        /*
        Represents system-wide “temperature” that shapes how all factions behave.
            These do not belong to any one character, but influence how relationships,
            capabilities, and decisions are interpreted.

        Value scale:
            0–4 (low to high)

        Variables:
            globalTension: overall hostility between factions.
            powderScarcity: availability of the key resource.
            informationAccuracy: reliability of the information environment.
            authoritalControl: strength and legitimacy of central rule (Emperor / Imperium).
            covertActivity: level of espionage and indirect action.
        */
        //credit to cgpt for docs


        globalTension = 3;
        powderScarcity = 2; 
        informationAccuracy = 3;
        authoritalControl = 3;
        covertActivity = 2; // we're in a quiet time

        #endregion Global State Variables
    }

    public string GetContext(string factionName)
{
    if (!characters.ContainsKey(factionName))
    {
        return "";
    }

    int index = characterIndices[factionName];

    // --- Global state: world-wide conditions affecting all factions ---
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

    // --- Own state: this faction's own resources and standing ---
    string ownContext = $"Your current standing: powder control is {LevelOf(powderControl[index])}, " +
        $"military strength is {LevelOf(militaryStrength[index])}, " +
        $"hidden influence is {LevelOf(hiddenInfluence[index])}, " +
        $"public influence is {LevelOf(publicInfluence[index])}, " +
        $"internal stability is {LevelOf(stability[index])}. ";

    // --- Relationships: how this faction perceives each other faction ---
    var relationshipLines = new List<string>();
    foreach (var kvp in characterIndices)
    {
        string otherName = kvp.Key;
        int otherIndex = kvp.Value;
        if (otherIndex == index) continue;  // skip self-reference

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
}