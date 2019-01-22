Game.ExecutableContext = [ // need to add these or it might conflict with Tales of the Borderlands
    "PhysXLocal",
    "binkw32.dll"
];

// temporarily disabled keyboard support
Game.SupportsKeyboard = false;
Game.HandlerInterval = 100;
Game.SymlinkExe = false;
Game.SymlinkGame = true;
Game.SteamID = "8980";
Game.GUID = "8980";
Game.GameName = "Borderlands";
Game.MaxPlayers = 4;
Game.MaxPlayersOneMonitor = 4;
Game.NeedsSteamEmulation = true;
Game.LauncherTitle = "splashscreen";
Game.SaveType = Nucleus.SaveType.INI;
Game.SupportsPositioning = true;
Game.StartArguments = "-windowed -NoLauncher -nostartupmovies";
Game.ExecutableName = "borderlands.exe";
Game.BinariesFolder = "binaries";
Game.Hook.ForceFocus = true;
Game.Hook.ForceFocusWindowRegex = "Borderlands";
Game.Hook.DInputEnabled = false;
Game.Hook.XInputEnabled = true;
Game.Hook.XInputReroute = false;//true; // this is beta
Game.Hook.XInputNames = ["xinput1_3.dll"];

// this game will multiply the values on the creators Update
// ... but is it only in the creators update?
Game.DPIHandling = Nucleus.DPIHandling.InvScaled; 

Game.Play = function () {
    // block all mouse and keyboard input for the player that
    // isnt the keyboard one
    // (Borderlands 1 NEEDS this, else it will lose focus)
    var isKeyboard = Player.IsKeyboardPlayer;
    Context.Hook.BlockMouseEvents = !isKeyboard;
    Context.Hook.BlockKeyboardEvents = !isKeyboard;

    var savePath = Context.GetFolder(Nucleus.Folder.Documents) + "\\My Games\\Borderlands\\WillowGame\\Config\\WillowEngine.ini";
    Context.ModifySaveFile(savePath, savePath, Nucleus.SaveType.INI, [
        new Nucleus.IniSaveInfo("SystemSettings", "WindowedFullscreen", Context.IsFullscreen),
        new Nucleus.IniSaveInfo("SystemSettings", "ResX", Context.Width),
        new Nucleus.IniSaveInfo("SystemSettings", "ResY", Context.Height),
        new Nucleus.IniSaveInfo("SystemSettings", "Fullscreen", false),
        new Nucleus.IniSaveInfo("Engine.Engine", "bPauseOnLossOfFocus", false),
        new Nucleus.IniSaveInfo("WillowGame.WillowGameEngine", "bPauseLostFocusWindowed", false)
    ]);
}