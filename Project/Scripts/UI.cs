using Godot;
using System;
using System.Diagnostics;

public partial class UI : Control
{
    TextEdit userTextEdit;
    RichTextLabel modelLabel1;
    RichTextLabel modelLabel2;
    Button sendMessageButton;
    Button exportButton;
    OptionButton senderDropdown;
    OptionButton recieverDropdown;
    [Signal]
    public delegate void MessageSubmittedEventHandler(string message);
    [Signal]
    public delegate void SenderChangedEventHandler(string sender);
    [Signal]
    public delegate void RecieverChangedEventHandler(string reciever);
    [Signal]
    public delegate void ExportLogRequestedEventHandler();
    OllamaInterface ollama;

    public override void _Ready()
    {
        userTextEdit = GetNode<TextEdit>("UIHBox/UserPanel/ColorRect2/UserTextedit");
        modelLabel1 = GetNode<RichTextLabel>("UIHBox/ModelsPanel/ModelsVBox/ModelPanel1/ModelLabel1");
        modelLabel2 = GetNode<RichTextLabel>("UIHBox/ModelsPanel/ModelsVBox/ModelPanel2/ModelLabel2");
        sendMessageButton = GetNode<Button>("UIHBox/UserPanel/SendButton");
        exportButton = GetNode<Button>("UIHBox/UserPanel/ExportButton");
        senderDropdown = GetNode<OptionButton>("UIHBox/UserPanel//SenderDropdown");
        recieverDropdown = GetNode<OptionButton>("UIHBox/UserPanel//RecieverDropdown");
        ollama = GetNode<OllamaInterface>("/root/Main/OllamaInterface");

        sendMessageButton.Pressed += OnSendMessagePressed;
        recieverDropdown.ItemSelected += OnRecieverChanged;
        senderDropdown.ItemSelected += OnSenderChanged;
        exportButton.Pressed += OnExportPressed;

        ollama.ModelReply += OnModelReply;
    }

    public void OnSendMessagePressed()
    {
        string userMessage = userTextEdit.Text.Trim();    
        if (string.IsNullOrEmpty(userMessage)){return;}

        //ui bits
        userTextEdit.PlaceholderText = userTextEdit.PlaceholderText + '\n' + userMessage;
        userTextEdit.Text = string.Empty;

        //emit signal stuff to send message to ollama interface
        EmitSignal(SignalName.MessageSubmitted, userMessage);
    }

    public void OnSenderChanged(long index)
    {
        string sender = senderDropdown.GetItemText((int)index);
        EmitSignal(SignalName.SenderChanged, sender);
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
        switch(sender)
        {
            case "Aurellian": modelLabel1.Text = $"{sender}: {message}"; break;
            case "Brutan": modelLabel2.Text = $"{sender}: {message}"; break;
            default: GD.PrintErr("Received message for unknown character: ", sender); break;
        }
    }
}
