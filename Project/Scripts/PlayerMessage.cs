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