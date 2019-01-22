Game.AddOption("Keyboard Player",
    "The player that will be playing on keyboard and mouse (if any)",
    "KeyboardPlayer",
    Nucleus.KeyboardPlayer.NoKeyboardPlayer);
Game.ExecutableContext = [
    "binkw32.dll"
];
Game.KillMutex = [
    "SR3"
];
Game.FileSymlinkExclusions = [
    "display.ini",
];

Game.NeedsSteamEmulation = true;
Game.HandlerInterval = 100;
Game.SymlinkExe = false;
Game.SymlinkGame = true;
Game.SupportsKeyboard = true;
Game.ExecutableName = "saintsrowthethird_dx11.exe";
Game.SteamID = "5523011";
Game.GUID = "5523011";
Game.GameName = "Saints Row 3 (DX11)";
Game.MaxPlayers = 4;
Game.MaxPlayersOneMonitor = 4;
Game.BinariesFolder = "";
Game.LauncherTitle = "";
Game.SaveType = Nucleus.SaveType.None;
Game.SupportsPositioning = true;
Game.HideTaskbar = false;
Game.StartArguments = "";
Game.LauncherExe = "game_launcher.exe";
Game.Hook.ForceFocus = true;
Game.Hook.ForceFocusWindowRegex = "Saints Row: The Third";
Game.Hook.DInputEnabled = false;
Game.Hook.XInputEnabled = true;
Game.Hook.XInputReroute = false;

Game.Play = function () {
    var savePath = Context.SavePath = Context.GetFolder(Nucleus.Folder.InstancedGameFolder) + "\\display.ini";
    Context.ModifySaveFile(savePath, savePath, Nucleus.SaveType.INI, [
       new Nucleus.IniSaveInfo("", "ResolutionWidth", Context.Width),
       new Nucleus.IniSaveInfo("", "ResolutionHeight", Context.Height),
       new Nucleus.IniSaveInfo("", "Fullscreen", false),
       new Nucleus.IniSaveInfo("", "VerifyResolution", false),
       new Nucleus.IniSaveInfo("", "SkipIntroVideo", true),
    ]);
}