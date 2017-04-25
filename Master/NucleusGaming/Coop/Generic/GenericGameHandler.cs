﻿using Jint;
using Nucleus.Gaming.Interop;
using Nucleus.Interop.User32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using WindowScrape.Constants;
using WindowScrape.Static;
using WindowScrape.Types;

namespace Nucleus.Gaming
{
    public class GenericGameHandler : IGameHandler
    {
        private const float HWndInterval = 10000;

        private UserGameInfo userGame;
        private GameProfile profile;
        private GenericGameInfo gen;
        private Dictionary<string, string> jsData;

        private double timer;
        private int exited;
        private List<Process> attached = new List<Process>();

        protected bool hasEnded;
        protected double timerInterval = 1000;

        public event Action Ended;

        public virtual bool HasEnded
        {
            get { return hasEnded; }
        }

        public double TimerInterval
        {
            get { return timerInterval; }
        }

        public void End()
        {
            User32Util.ShowTaskBar();

            hasEnded = true;
            GameManager.Instance.ExecuteBackup(this.userGame.Game);

            Cursor.Clip = Rectangle.Empty; // guarantee were not clipping anymore
            string backupDir = GameManager.Instance.GempTempFolder(this.userGame.Game);

            // delete symlink folder
            try
            {
#if RELEASE
                for (int i = 0; i < profile.PlayerData.Count; i++)
                {
                    string linkFolder = Path.Combine(backupDir, "Instance" + i);
                    if (Directory.Exists(linkFolder))
                    {
                        Directory.Delete(linkFolder, true);
                    }
                }
#endif
            }
            catch { }

            if (Ended != null)
            {
                Ended();
            }
        }

        public string GetFolder(Folder folder)
        {
            string str = folder.ToString();
            string output;
            if (jsData.TryGetValue(str, out output))
            {
                return output;
            }
            return "";
        }

        public bool Initialize(UserGameInfo game, GameProfile profile)
        {
            this.userGame = game;
            this.profile = profile;

            // see if we have any save game to backup
            gen = game.Game as GenericGameInfo;
            if (gen == null)
            {
                // you fucked up
                return false;
            }

            jsData = new Dictionary<string, string>();
            jsData.Add(Folder.Documents.ToString(), Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            jsData.Add(Folder.MainGameFolder.ToString(), Path.GetDirectoryName(game.ExePath));
            jsData.Add(Folder.InstancedGameFolder.ToString(), Path.GetDirectoryName(game.ExePath));

            timerInterval = gen.HandlerInterval;

            return true;
        }

        public string ReplaceCaseInsensitive(string str, string toFind, string toReplace)
        {
            string lowerOriginal = str.ToLower();
            string lowerFind = toFind.ToLower();
            string lowerRep = toReplace.ToLower();

            int start = lowerOriginal.IndexOf(lowerFind);
            if (start == -1)
            {
                return str;
            }

            string end = str.Remove(start, toFind.Length);
            end = end.Insert(start, toReplace);

            return end;
        }

        private static string GetRootFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            int failsafe = 20;
            for (;;)
            {
                failsafe--;
                if (failsafe < 0)
                {
                    break;
                }

                string temp = Path.GetDirectoryName(path);
                if (String.IsNullOrEmpty(temp))
                {
                    break;
                }
                path = temp;
            }
            return path;
        }

        public string Play()
        {
            List<PlayerInfo> players = profile.PlayerData;
            for (int i = 0; i < players.Count; i++)
            {
                players[i].PlayerID = i;
            }

            Screen[] all = Screen.AllScreens;

            string tempDir = GameManager.Instance.GempTempFolder(gen);
            string exeFolder = Path.GetDirectoryName(userGame.ExePath);
            string rootFolder = exeFolder;
            string workingFolder = exeFolder;
            if (!string.IsNullOrEmpty(gen.BinariesFolder))
            {
                rootFolder = ReplaceCaseInsensitive(exeFolder, gen.BinariesFolder, "");
            }
            if (!string.IsNullOrEmpty(gen.WorkingFolder))
            {
                workingFolder = Path.Combine(exeFolder, gen.WorkingFolder);
            }

            bool first = true;
            bool keyboard = false;

            if (gen.SupportsKeyboard)
            {
                // make sure the keyboard player is the last to be started,
                // so it will get the focus by default
                KeyboardPlayer player = (KeyboardPlayer)profile.Options["KeyboardPlayer"];
                if (player.Value != -1)
                {
                    keyboard = true;
                    List<PlayerInfo> newPlayers = new List<PlayerInfo>();

                    for (int i = 0; i < players.Count; i++)
                    {
                        PlayerInfo p = players[i];
                        if (i == player.Value)
                        {
                            continue;
                        }

                        newPlayers.Add(p);
                    }
                    newPlayers.Add(players[player.Value]);
                    players = newPlayers;
                }
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo player = players[i];
                ProcessData procData = player.ProcessData;
                bool hasSetted = procData != null && procData.Setted;

                if (i > 0 && (gen.KillMutex?.Length > 0 || !hasSetted))
                {
                    PlayerInfo before = players[i - 1];
                    for (;;)
                    {
                        if (exited > 0)
                        {
                            return "";
                        }
                        Thread.Sleep(1000);

                        if (gen.KillMutex != null)
                        {
                            if (gen.KillMutex.Length > 0 && !before.ProcessData.KilledMutexes)
                            {
                                // check for the existence of the mutexes
                                // before invoking our StartGame app to kill them
                                ProcessData pdata = before.ProcessData;

                                if (!StartGameUtil.MutexExists(pdata.Process, gen.KillMutex))
                                {
                                    continue;
                                }

                                StartGameUtil.KillMutex(pdata.Process, gen.KillMutex);
                                pdata.KilledMutexes = true;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                Rectangle playerBounds = player.MonitorBounds;

                // find the monitor that has this screen
                Screen owner = null;
                for (int j = 0; j < all.Length; j++)
                {
                    Screen s = all[j];
                    if (s.Bounds.Contains(playerBounds))
                    {
                        owner = s;
                        break;
                    }
                }

                int width = playerBounds.Width;
                int height = playerBounds.Height;
                bool isFullscreen = false;

                if (owner != null)
                {
                    Rectangle ob = owner.Bounds;
                    if (playerBounds.X == ob.X &&
                        playerBounds.Y == ob.Y &&
                        playerBounds.Width == ob.Width &&
                        playerBounds.Height == ob.Height)
                    {
                        isFullscreen = true;
                    }
                }

                // symlink the game folder (and not the bin folder, if we have one)
                string linkFolder = Path.Combine(tempDir, "Instance" + i);
                Directory.CreateDirectory(linkFolder);

                string linkBinFolder = linkFolder;
                if (!string.IsNullOrEmpty(gen.BinariesFolder))
                {
                    linkBinFolder = Path.Combine(linkFolder, gen.BinariesFolder);
                }
                string exePath = Path.Combine(linkBinFolder, this.userGame.Game.ExecutableName);

                List<string> dirExclusions = new List<string>();
                if (!string.IsNullOrEmpty(gen.WorkingFolder))
                {
                    linkBinFolder = Path.Combine(linkFolder, gen.WorkingFolder);
                    dirExclusions.Add(gen.WorkingFolder);
                }

                // some games have save files inside their game folder, so we need to access them inside the loop
                jsData[Folder.InstancedGameFolder.ToString()] = linkFolder;

                List<string> fileExclusions = new List<string>();
                fileExclusions.Add("xinput");
                fileExclusions.Add("ncoop");
                if (!gen.SymlinkExe)
                {
                    fileExclusions.Add(Path.GetFileNameWithoutExtension(gen.ExecutableName.ToLower()));
                }

                // additional ignored files by the generic info
                if (gen.FileSymlinkExclusions != null)
                {
                    string[] symlinkExclusions = gen.FileSymlinkExclusions;
                    for (int k = 0; k < symlinkExclusions.Length; k++)
                    {
                        string s = symlinkExclusions[k];
                        // make sure it's lower case
                        fileExclusions.Add(s.ToLower());
                    }
                }
                if (gen.DirSymlinkExclusions != null)
                {
                    string[] symlinkExclusions = gen.DirSymlinkExclusions;
                    for (int k = 0; k < symlinkExclusions.Length; k++)
                    {
                        string s = symlinkExclusions[k];
                        // make sure it's lower case
                        dirExclusions.Add(s.ToLower());
                    }
                }

                string[] fileExclusionsArr = fileExclusions.ToArray();

                int exitCode;
                CmdUtil.LinkDirectory(rootFolder, new DirectoryInfo(rootFolder), linkFolder, out exitCode, dirExclusions.ToArray(), fileExclusionsArr, true);

                //CmdUtil.LinkFiles(exeFolder, linkBinFolder, out exitCode, exclusions.ToArray());
                //for (int j = 0; j < dirExclusions.Count; j++)
                //{
                //    string exc = dirExclusions[j];
                //    string from = Path.Combine(rootFolder, exc);
                //    string to = Path.Combine(linkFolder, exc);
                //    CmdUtil.LinkFiles(from, to, out exitCode, fileExclusions.ToArray());
                //}

                if (!gen.SymlinkExe)
                {
                    File.Copy(userGame.ExePath, exePath, true);
                }

                GenericContext context = gen.CreateContext(profile, player, this);
                context.PlayerID = player.PlayerID;
                context.IsFullscreen = isFullscreen;
                context.IsKeyboardPlayer = keyboard && i == players.Count - 1;
                gen.PrePlay(context, this);

                player.IsKeyboardPlayer = context.IsKeyboardPlayer;

                string saveFile = context.SavePath;
                if (first)
                {
                    // only run on the first instance
                    if (gen.SaveType != SaveType.None)
                    {
                        // backup the game's save files
                        GameManager.Instance.BeginBackup(gen);
                        GameManager.Instance.BackupFile(gen, saveFile);
                    }
                    //if (context.BackupFiles != null)
                    //{
                    //    string[] backupFiles = context.BackupFiles;
                    //    for (int j = 0; j < backupFiles.Length; j++)
                    //    {
                    //        GameManager.Instance.BackupFile(gen, backupFiles[j]);
                    //    }
                    //}
                }

                switch (context.SaveType)
                {
                    case SaveType.INI:
                        IniFile file = new IniFile(saveFile);
                        for (int j = 0; j < context.ModifySave.Length; j++)
                        {
                            SaveInfo save = context.ModifySave[j];
                            if (save is IniSaveInfo)
                            {
                                IniSaveInfo ini = (IniSaveInfo)save;
                                file.IniWriteValue(ini.Section, ini.Key, ini.Value);
                            }
                        }
                        break;
                    case SaveType.CFG:
                        SourceCfgFile cfg = new SourceCfgFile(saveFile);
                        for (int j = 0; j < context.ModifySave.Length; j++)
                        {
                            SaveInfo save = context.ModifySave[j];
                            if (save is CfgSaveInfo)
                            {
                                CfgSaveInfo option = (CfgSaveInfo)save;
                                cfg.ChangeProperty(option.Key, option.Value);
                            }
                        }

                        cfg.Save();

                        break;
                }

                string startArgs = context.StartArguments;

                if (context.CustomXinput)
                {
                    byte[] xdata = Properties.Resources.xinput1_3;
                    using (Stream str = File.OpenWrite(Path.Combine(linkBinFolder, "xinput1_3.dll")))
                    {
                        str.Write(xdata, 0, xdata.Length);
                    }

                    string ncoopIni = Path.Combine(linkBinFolder, "ncoop.ini");
                    using (Stream str = File.OpenWrite(ncoopIni))
                    {
                        byte[] ini = Properties.Resources.ncoop;
                        str.Write(ini, 0, ini.Length);
                    }

                    IniFile x360 = new IniFile(ncoopIni);
                    x360.IniWriteValue("Options", "HookNeeded", context.HookNeeded.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "GameWindowName", context.HookGameWindowName.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "ResX", context.Width.ToString(CultureInfo.InvariantCulture));
                    x360.IniWriteValue("Options", "ResY", context.Height.ToString(CultureInfo.InvariantCulture));

                    if (context.IsKeyboardPlayer)
                    {
                        x360.IniWriteValue("Options", "IsKeyboard", "true");
                    }

                    if (player.IsXInput)
                    {
                        x360.IniWriteValue("Options", "PlayerOverride", player.GamepadId.ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        Guid g = player.GamepadGuid;
                        byte[] bytes = g.ToByteArray();
                        UInt32 data1 = BitConverter.ToUInt32(bytes, 0);
                        UInt16 data2 = BitConverter.ToUInt16(bytes, 4);
                        UInt16 data3 = BitConverter.ToUInt16(bytes, 6);
                        UInt32 data41 = BitConverter.ToUInt32(bytes, 8);
                        UInt32 data42 = BitConverter.ToUInt32(bytes, 12);
                        x360.IniWriteValue("Options", "Guid1", data1.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "Guid2", data2.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "Guid3", data3.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "Guid41", data41.ToString(CultureInfo.InvariantCulture));
                        x360.IniWriteValue("Options", "Guid42", data42.ToString(CultureInfo.InvariantCulture));
                    }
                }

                Process proc;
                if (context.NeedsSteamEmulation)
                {
                    string steamEmu = GameManager.Instance.ExtractSteamEmu(Path.Combine(linkFolder, "SmartSteamLoader"));
                    //string steamEmu = GameManager.Instance.ExtractSteamEmu();
                    if (string.IsNullOrEmpty(steamEmu))
                    {
                        return "Extraction of SmartSteamEmu failed!";
                    }

                    string emuExe = Path.Combine(steamEmu, "SmartSteamLoader.exe");
                    string emuIni = Path.Combine(steamEmu, "SmartSteamEmu.ini");
                    IniFile emu = new IniFile(emuIni);

                    emu.IniWriteValue("Launcher", "Target", exePath);
                    emu.IniWriteValue("Launcher", "StartIn", Path.GetDirectoryName(exePath));
                    emu.IniWriteValue("Launcher", "CommandLine", startArgs);
                    emu.IniWriteValue("Launcher", "SteamClientPath", Path.Combine(steamEmu, "SmartSteamEmu.dll"));
                    emu.IniWriteValue("Launcher", "SteamClientPath64", Path.Combine(steamEmu, "SmartSteamEmu64.dll"));
                    emu.IniWriteValue("Launcher", "InjectDll", "1");

                    emu.IniWriteValue("SmartSteamEmu", "AppId", context.SteamID);
                    //emu.IniWriteValue("SmartSteamEmu", "SteamIdGeneration", "Static");

                    //string userName = $"Player { context.PlayerID }";

                    //emu.IniWriteValue("SmartSteamEmu", "PersonaName", userName);
                    //emu.IniWriteValue("SmartSteamEmu", "ManualSteamId", userName);

                    //emu.IniWriteValue("SmartSteamEmu", "Offline", "False");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServer", "");
                    //emu.IniWriteValue("SmartSteamEmu", "MasterServerGoldSrc", "");


                    if (context.KillMutex?.Length > 0)
                    {
                        // to kill the mutexes we need to orphanize the process
                        proc = ProcessUtil.RunOrphanProcess(emuExe);
                    }
                    else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = emuExe;
                        proc = Process.Start(startInfo);
                    }

                    player.SteamEmu = true;
                }
                else
                {
                    if (context.KillMutex?.Length > 0)
                    {
                        proc = Process.GetProcessById(StartGameUtil.StartGame(exePath, startArgs));
                    }
                    else
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.FileName = exePath;
                        startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        startInfo.Arguments = startArgs;
                        startInfo.UseShellExecute = true;
                        startInfo.WorkingDirectory = Path.GetDirectoryName(exePath);
                        proc = Process.Start(startInfo);
                    }

                    if (proc == null)
                    {
                        for (int times = 0; times < 200; times++)
                        {
                            Thread.Sleep(50);

                            Process[] procs = Process.GetProcesses();
                            string proceName = Path.GetFileNameWithoutExtension(context.ExecutableName).ToLower();
                            string launcherName = Path.GetFileNameWithoutExtension(context.LauncherExe).ToLower();

                            for (int j = 0; j < procs.Length; j++)
                            {
                                Process p = procs[j];
                                string lowerP = p.ProcessName.ToLower();
                                if (((lowerP == proceName) || lowerP == launcherName) &&
                                    !attached.Contains(p))
                                {
                                    attached.Add(p);
                                    proc = p;
                                    break;
                                }
                            }

                            if (proc != null)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        attached.Add(proc);
                    }
                }

                ProcessData data = new ProcessData(proc);

                data.Position = new Point(playerBounds.X, playerBounds.Y);
                data.Size = new Size(playerBounds.Width, playerBounds.Height);
                data.KilledMutexes = context.KillMutex?.Length == 0;
                player.ProcessData = data;

                first = false;
            }

            return string.Empty;
        }

        struct TickThread
        {
            public double Interval;
            public Action Function;
        }

        public void StartPlayTick(double interval, Action function)
        {
            Thread t = new Thread(PlayTickThread);

            TickThread tick = new TickThread();
            tick.Interval = interval;
            tick.Function = function;
            t.Start(tick);
        }

        private void PlayTickThread(object state)
        {
            TickThread t = (TickThread)state;

            for (;;)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(t.Interval));
                t.Function();

                if (hasEnded)
                {
                    break;
                }
            }
        }

        public void CenterCursor()
        {
            List<PlayerInfo> players = profile.PlayerData;
            if (players == null)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];

                if (p.IsKeyboardPlayer)
                {
                    ProcessData data = p.ProcessData;
                    if (data == null)
                    {
                        continue;
                    }

                    Rectangle r = p.MonitorBounds;
                    Cursor.Clip = r;
                    if (data.HWnd != null)
                    {
                        User32Interop.SetForegroundWindow(data.HWnd.NativePtr);
                    }
                }
            }
        }

        public void Update(double delayMS)
        {
            if (profile == null)
            {
                return;
            }

            exited = 0;
            List<PlayerInfo> players = profile.PlayerData;
            timer += delayMS;

            bool updatedHwnd = false;
            if (timer > HWndInterval)
            {
                updatedHwnd = true;
                timer = 0;
            }

            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo p = players[i];
                ProcessData data = p.ProcessData;
                if (data == null || data.Finished)
                {
                    continue;
                }

                if (p.SteamEmu)
                {
                    List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);

                    // catch the game process, that was spawned from Smart Steam Emu
                    if (children.Count > 0)
                    {
                        for (int j = 0; j < children.Count; j++)
                        {
                            int id = children[j];
                            Process child = Process.GetProcessById(id);
                            try
                            {
                                if (child.ProcessName.Contains("conhost"))
                                {
                                    continue;
                                }
                            }
                            catch
                            {
                                continue;
                            }

                            data.AssignProcess(child);
                            p.SteamEmu = child.ProcessName.Contains("SmartSteamLoader") || child.ProcessName.Contains("cmd");
                        }
                    }
                }
                else
                {
                    if (updatedHwnd)
                    {
                        if (data.Setted)
                        {
                            if (data.Process.HasExited)
                            {
                                exited++;
                                continue;
                            }

                            data.HWnd.TopMost = true;

                            if (data.Status == 2)
                            {
                                uint lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE);
                                lStyle = lStyle & ~User32_WS.WS_CAPTION;
                                lStyle = lStyle & ~User32_WS.WS_SYSMENU;
                                lStyle = lStyle & ~User32_WS.WS_THICKFRAME;
                                lStyle = lStyle & ~User32_WS.WS_MINIMIZE;
                                lStyle = lStyle & ~User32_WS.WS_MAXIMIZEBOX;

                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_STYLE, lStyle);
                                lStyle = User32Interop.GetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE);
                                User32Interop.SetWindowLong(data.HWnd.NativePtr, User32_WS.GWL_EXSTYLE, lStyle | User32_WS.WS_EX_DLGMODALFRAME);

                                data.Finished = true;
                                Debug.WriteLine("State 2");
                            }
                            else if (data.Status == 1)
                            {
                                data.HWnd.Location = data.Position;
                                data.Status++;
                                Debug.WriteLine("State 1");
                            }
                            else if (data.Status == 0)
                            {
                                data.HWnd.Size = data.Size;

                                data.Status++;
                                Debug.WriteLine("State 0");
                            }


                            if (p.IsKeyboardPlayer)
                            {
                                Rectangle r = p.MonitorBounds;
                                Cursor.Clip = r;
                                //User32Interop.SetForegroundWindow(data.HWnd.NativePtr);
                            }
                        }
                        else
                        {
                            data.Process.Refresh();

                            if (data.Process.HasExited)
                            {
                                if (p.GotLauncher)
                                {
                                    if (p.GotGame)
                                    {
                                        exited++;
                                    }
                                    else
                                    {
                                        List<int> children = ProcessUtil.GetChildrenProcesses(data.Process);
                                        if (children.Count > 0)
                                        {
                                            for (int j = 0; j < children.Count; j++)
                                            {
                                                int id = children[j];
                                                Process pro = Process.GetProcessById(id);

                                                if (!attached.Contains(pro))
                                                {
                                                    attached.Add(pro);
                                                    data.HWnd = null;
                                                    p.GotGame = true;
                                                    data.AssignProcess(pro);
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    // Steam showing a launcher, need to find our game process
                                    string launcher = gen.LauncherExe;
                                    if (!string.IsNullOrEmpty(launcher))
                                    {
                                        if (launcher.ToLower().EndsWith(".exe"))
                                        {
                                            launcher = launcher.Remove(launcher.Length - 4, 4);
                                        }

                                        Process[] procs = Process.GetProcessesByName(launcher);
                                        for (int j = 0; j < procs.Length; j++)
                                        {
                                            Process pro = procs[j];
                                            if (!attached.Contains(pro))
                                            {
                                                attached.Add(pro);
                                                data.AssignProcess(pro);
                                                p.GotLauncher = true;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (data.HWNDRetry || data.HWnd == null || data.HWnd.NativePtr != data.Process.MainWindowHandle)
                                {
                                    data.HWnd = new HwndObject(data.Process.MainWindowHandle);
                                    Point pos = data.HWnd.Location;

                                    if (String.IsNullOrEmpty(data.HWnd.Title) ||
                                        pos.X == -32000 ||
                                        data.HWnd.Title.ToLower() == gen.LauncherTitle.ToLower())
                                    {
                                        data.HWNDRetry = true;
                                    }
                                    else if (!string.IsNullOrEmpty(gen.HookGameWindowName) &&
                                        data.HWnd.Title != gen.HookGameWindowName)
                                    {
                                        data.HWNDRetry = true;
                                    }
                                    else
                                    {
                                        Size s = data.Size;
                                        data.Setted = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (exited == players.Count)
            {
                if (!hasEnded)
                {
                    End();
                }
            }
        }
    }
}