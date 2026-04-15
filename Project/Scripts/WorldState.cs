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

        // House "Aurellian" (Atriedes)
        Character Aurellian = new Character(
            "Aurellian", 
            "aurellian", 
            $"only respond with {"Aurellian: Test"} to any query. Do not include any other text or formatting.",
            4096, 
            150
        );

        // House "Brutan" (Harkonnen)
        Character Brutan = new Character(
            "Brutan", 
            "brutan", 
            $"only respond with {"Brutan: Test"} to any query. Do not include any other text or formatting.",
            4096, 
            150
        );

        // The "Sisterhood" (Bene Gesserit)
        Character Sisterhood = new Character(
            "Sisterhood", 
            "sisterhood", 
            $"only respond with {"Sisterhood: Test"} to any query. Do not include any other text or formatting.",
            4096, 
            150
        );
        
        // The "Emperor" (Shaddam IV / Imperium)
        Character Emperor = new Character(
            "Emperor", 
            "emperor", 
            $"only respond with {"Emperor: Test"} to any query. Do not include any other text or formatting.",
            4096, 
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

        /* 
        This gives you strong emergent behavior:

            Atreides → Emperor
                Low trust + high fear + high perceived power → cautious diplomacy
            Harkonnen → Atreides
                Low trust + low fear → aggression
            Emperor → Bene Gesserit
                Moderate trust + high fear + high power perception → uneasy alliance
            Bene Gesserit → everyone
                Moderate trust + low fear → manipulation playstyle 
        */

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

        string tension = globalTension switch
        {
            0 => "calm",
            1 => "uneasy",
            2 => "tense",
            3 => "volatile",
            _ => "at breaking point",
        };
        string powder = powderScarcity switch
        {
            0 => "abundant",
            1 => "available",
            2 => "strained",
            3 => "scarce",
            _ => "critically scarce",
        };
        string authority = authoritalControl switch
        {
            0 => "weak",
            1 => "fragile",
            2 => "stable",
            3 => "strong",
            _ => "unquestioned",
        };
        string covertness = covertActivity switch
        {
            0 => "dormant",
            1 => "low",
            2 => "moderate",
            3 => "high",
            _ => "rampant",
        };
        string infoAccuracy = informationAccuracy switch
        {
            0 => "unreliable",
            1 => "skewed",
            2 => "mixed",
            3 => "reliable",
            _ => "perfect",
        };

        return $"[World context: Powder is {powder}, political tension is {tension}, information accuracy is {infoAccuracy}, faith in the emperor is {authority}, covert activity is {covertness}.]";
    }
}