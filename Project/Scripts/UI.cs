using Godot;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public partial class UI : Control
{
    //
    TextEdit userTextEdit;
    Label titleA;
    Label titleB;
    Label titleC;
    Label titleAll;
    Label textA;
    Label textB;
    Label textC;
    Label textAll;
    //
    private string playerFaction = "";
    Dictionary<string, Label> agentLabels;
    //
    Button sendMessageButton;
    Button exportButton;
    Button summariseButton;
    OptionButton recieverDropdown;
    //
    [Signal]
    public delegate void MessageSubmittedEventHandler(string message);
    [Signal]
    public delegate void RecieverChangedEventHandler(string reciever);
    [Signal]
    public delegate void ExportLogRequestedEventHandler();
    [Signal]
    public delegate void SummariseRequestedEventHandler();

    public override void _Ready()
    {
        //
        userTextEdit = GetNode<TextEdit>("UIHBox/UIVBox/ColorRect/VBoxContainer/UserHBox/UserInputField");
        titleA = GetNode<Label>("UIHBox/UIVBox2/RectA/VBoxContainer/TitleA");
        titleB = GetNode<Label>("UIHBox/UIVBox2/RectB/VBoxContainer/TitleB");
        titleC = GetNode<Label>("UIHBox/UIVBox2/RectC/VBoxContainer/TitleC");
        titleAll = GetNode<Label>("UIHBox/UIVBox/ColorRect/VBoxContainer/TitleAll");
        textA = GetNode<Label>("UIHBox/UIVBox2/RectA/VBoxContainer/TextA");
        textB = GetNode<Label>("UIHBox/UIVBox2/RectB/VBoxContainer/TextB");
        textC = GetNode<Label>("UIHBox/UIVBox2/RectC/VBoxContainer/TextC");
        textAll = GetNode<Label>("UIHBox/UIVBox/ColorRect/VBoxContainer/TextAll");
        
        //
        sendMessageButton = GetNode<Button>("UIHBox/UIVBox/ColorRect/VBoxContainer/UserHBox/SendButton");
        exportButton = GetNode<Button>("UIHBox/UIVBox/ColorRect/VBoxContainer/ButtonsHBox/ExportButton");
        recieverDropdown = GetNode<OptionButton>("UIHBox/UIVBox/ColorRect/VBoxContainer/UserHBox/RecieverDropdown");
        summariseButton = GetNode<Button>("UIHBox/UIVBox/ColorRect/VBoxContainer/ButtonsHBox/SummariseButton");
        
        //
        sendMessageButton.Pressed += OnSendMessagePressed;
        recieverDropdown.ItemSelected += OnRecieverChanged;
        exportButton.Pressed += OnExportPressed;
        summariseButton.Pressed += OnSummarisePressed;
    }

    public void SetPlayerFaction(string faction)
    {
        playerFaction = faction;
    }

    public void SetCharacterLabels(string[] characters)
    {
        //dropdown
        recieverDropdown.Clear();
        recieverDropdown.AddItem("All");
        foreach (string option in characters)
        {
            recieverDropdown.AddItem(option);
        }

        //title labels
        titleA.Text = $"{characters[0]}:";
        titleB.Text = $"{characters[1]}:";
        titleC.Text = $"{characters[2]}:";
        titleAll.Text = "All:";

        //mapping
        agentLabels = new Dictionary<string, Label>
        {
            {characters[0], textA},
            {characters[1], textB},
            {characters[2], textC},
        };
    }

    public void OnSummarisePressed()
    {
        EmitSignal(SignalName.SummariseRequested);
    }

    public void OnSendMessagePressed()
    {
        string userMessage = userTextEdit.Text.Trim();    
        if (string.IsNullOrEmpty(userMessage)){return;}

        //ui
        userTextEdit.Text = string.Empty;

        //emit signal stuff to send message to ollama interface
        EmitSignal(SignalName.MessageSubmitted, userMessage);
    }

    public string GetCurentReciever()
    {
        int index = recieverDropdown.Selected;
    
        if(index < 0)
        {
            return "All";
    
        }
    
        return recieverDropdown.GetItemText(index);
    }

    public void OnRecieverChanged(long index)
    {
        string reciever = recieverDropdown.GetItemText((int)index);
        EmitSignal(SignalName.RecieverChanged, reciever);
    }

    public void OnExportPressed()
    {
        Debug.WriteLine("Export log button pressed, emitting signal...");
        EmitSignal(SignalName.ExportLogRequested);
    }

    public void OnModelReply(string sender, string target, string message)
    {
        if(target == "All")
        {
            textAll.Text += $"\n{sender}: {message}";
            return;
        }

        string textFieldOwner = (target == playerFaction) ? sender : target;

        if(agentLabels.TryGetValue(textFieldOwner, out Label text))
        {
            text.Text = $"\n{sender}: {message}";
        }
        else
        {
            GD.PrintErr($"No text field for {target}");
        }
    }
}
