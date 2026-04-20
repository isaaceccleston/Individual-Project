// One pre-planned message the scripted player will fire on their turn.
// Target is "All" for public, or a faction name for private.
public class PlayerMessage
{
    public string Target { get; set; }
    public string Content { get; set; }

    public PlayerMessage(string target, string content)
    {
        Target = target;
        Content = content;
    }
}