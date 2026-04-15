using System.Collections.Generic;

public class ChatManager
{
    private Dictionary<string, ChatSession> characterSessions = new Dictionary<string, ChatSession>();

    public ChatSession GetOrCreateSession(Character character)
    {
        if (!characterSessions.ContainsKey(character.name))
        {
            characterSessions[character.name] = new ChatSession(
                character.modelID, 
                character.name, 
                character.systemPrompt, 
                character.contextWindow,
                character.maxTokens
                );
        }
        return characterSessions[character.name];
    }
}
