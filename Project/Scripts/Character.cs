using Godot;

public partial class Character : Node2D
{
    public string name { get; set; }
    public string modelID { get; set; }
    public string systemPrompt { get; set; }
    public int contextWindow { get; set; }
    public int maxTokens { get; set; }

    /* Currently not in use, but more intuitive for initial class structure */
    public Character(string name, string modelID, string systemPrompt, int contextWindow, int maxTokens)
    {
        this.name = name;
        this.modelID = modelID;
        this.systemPrompt = systemPrompt;
        this.contextWindow = contextWindow;
        this.maxTokens = maxTokens;
    }
}