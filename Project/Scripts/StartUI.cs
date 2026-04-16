using Godot;

public partial class StartUI : Control
{
    public bool mode; // false = baseline, true = LLM
    public int faction; // 0 = Aurellian, 1 = Brutan, 2 = Sisterhood, 3 = Emperor
    //
    private VBoxContainer mainVBox;
    private Button LLMButton;
    private Button baselineButton;
    private Button quitButton;
    //
    private VBoxContainer factionVBox;
    private Button aurellianButton;
    private Button brutanButton;
    private Button sisterhoodButton;
    private Button emperorButton;
    private Button backButton;
    //
    private string thisPath;
    //
    [Signal]
    public delegate void StartGameSignalEventHandler(bool mode, int faction);

    public override void _Ready()
    {
        //
        thisPath = this.GetPath();

        mainVBox = GetNode<VBoxContainer>(thisPath + "/MainVBox");
        LLMButton = mainVBox.GetNode<Button>(thisPath + "/MainVBox/ModeHBox/ColorRect/LLMButton");
        baselineButton = mainVBox.GetNode<Button>(thisPath + "/MainVBox/ModeHBox/ColorRect2/BaselineButton");
        quitButton = mainVBox.GetNode<Button>(thisPath + "/MainVBox/QuitPanel/QuitButton");

        factionVBox = GetNode<VBoxContainer>(thisPath + "/FactionVBox");
        aurellianButton = factionVBox.GetNode<Button>(thisPath + "/FactionVBox/AurellianBox/ColorRect/AurellianButton");
        brutanButton = factionVBox.GetNode<Button>(thisPath + "/FactionVBox/BrutanBox/ColorRect/BrutanButton");
        sisterhoodButton = factionVBox.GetNode<Button>(thisPath + "/FactionVBox/SisterhoodBox/ColorRect/SisterhoodButton");
        emperorButton = factionVBox.GetNode<Button>(thisPath + "/FactionVBox/EmperorBox/ColorRect/EmperorButton");
        backButton = factionVBox.GetNode<Button>(thisPath + "/FactionVBox/BackPanel/BackButton");

        LLMButton.Pressed += LLMModeSelected;
        baselineButton.Pressed += BaselineModeSelected;
        quitButton.Pressed += Quit;

        aurellianButton.Pressed += AurellianChosen;
        brutanButton.Pressed += BrutanChosen;
        sisterhoodButton.Pressed += SisterhoodChosen;
        emperorButton.Pressed += EmperorChosen;
        backButton.Pressed += BackToMain;

        //
        mainVBox.Visible = true;
        factionVBox.Visible = false;
    }

    //
    private void LLMModeSelected()
    {
        mode = true;
        mainVBox.Visible = false;
        factionVBox.Visible = true;
    }

    private void BaselineModeSelected()
    {
        mode = false;
        mainVBox.Visible = false;
        factionVBox.Visible = true;
    }

    private void Quit()
    {
        GetTree().Quit();
    }

    //
    private void AurellianChosen()
    {
        faction = 0;
        EmitSignal(SignalName.StartGameSignal, mode, faction);
    }

    private void BrutanChosen()
    {
        faction = 1;
        EmitSignal(SignalName.StartGameSignal, mode, faction);
    }

    private void SisterhoodChosen()
    {
        faction = 2;
        EmitSignal(SignalName.StartGameSignal, mode, faction);
    }

    private void EmperorChosen()
    {
        faction = 3;
        EmitSignal(SignalName.StartGameSignal, mode, faction);
    }

    private void BackToMain()
    {
        mainVBox.Visible = true;
        factionVBox.Visible = false;
    }

}
