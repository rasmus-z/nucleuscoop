﻿using Nucleus.Gaming;
using Nucleus.Gaming.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SlimDX.DirectInput;
using SlimDX.XInput;

namespace Nucleus.Coop
{
    public partial class PositionsControl : UserInputControl
    {
        private bool canProceed;

        // array of users's screens
        private UserScreen[] screens;

        // the factor to scale all screens to fit them inside the edit area
        private float scale;

        // the total bounds of all the connected monitors together
        private Rectangle totalBounds;

        private Font playerFont;
        private Font smallTextFont;
        private Font playerTextFont;

        private RectangleF playersArea;

        private bool dragging = false;
        private int draggingIndex = -1;
        private Point draggingOffset;
        private Point mousePos;
        private int draggingScreen = -1;
        private Rectangle draggingScreenRec;
        private Rectangle draggingScreenBounds;

        private Image gamepadImg;
        private Image genericImg;

        public override bool CanProceed
        {
            get { return canProceed; }
        }
        public override string Title
        {
            get { return "Position Players"; }
        }
        public override bool CanPlay
        {
            get { return false; }
        }

        private DirectInput dinput;
        private List<Controller> controllers;

        private Timer gamepadTimer;

        public PositionsControl()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            dinput = new DirectInput();
            controllers = new List<Controller>();
            for (int i = 0; i < 4; i++)
            {
                controllers.Add(new Controller((UserIndex)i));
            }

            gamepadTimer = new Timer();
            gamepadTimer.Interval = 100;
            gamepadTimer.Tick += GamepadTimer_Tick;

            playerFont = new Font("Segoe UI", 40);
            playerTextFont = new Font("Segoe UI", 18);
            smallTextFont = new Font("Segoe UI", 12);

            gamepadImg = Resources.gamepad;
            genericImg = Resources.generic;

            RemoveFlicker();
        }

        public override void Ended()
        {
            base.Ended();

            gamepadTimer.Enabled = false;
        }

        private void GamepadTimer_Tick(object sender, EventArgs e)
        {
            List<PlayerInfo> data = profile.PlayerData;
            bool changed = false;

            if (game.Game.SupportsDirectInput)
            {
                IList<DeviceInstance> devices = dinput.GetDevices(SlimDX.DirectInput.DeviceType.Gamepad, DeviceEnumerationFlags.AttachedOnly);

                // first search for disconnected gamepads
                for (int j = 0; j < data.Count; j++)
                {
                    PlayerInfo p = data[j];
                    if (p.IsXInput)
                    {
                        continue;
                    }

                    bool foundGamepad = false;
                    for (int i = 0; i < devices.Count; i++)
                    {
                        DeviceInstance device = devices[i];
                        if (device.InstanceGuid == p.GamepadGuid)
                        {
                            foundGamepad = true;
                            break;
                        }
                    }

                    if (!foundGamepad)
                    {
                        changed = true;
                        data.RemoveAt(j);
                        j--;
                    }
                }

                for (int i = 0; i < devices.Count; i++)
                {
                    DeviceInstance device = devices[i];
                    bool already = false;

                    // see if this gamepad is already on a player
                    for (int j = 0; j < data.Count; j++)
                    {
                        PlayerInfo p = data[j];
                        if (p.GamepadGuid == device.InstanceGuid)
                        {
                            already = true;
                            break;
                        }
                    }

                    if (already)
                    {
                        continue;
                    }

                    changed = true;

                    // new gamepad
                    PlayerInfo player = new PlayerInfo();
                    player.GamepadGuid = device.InstanceGuid;
                    player.GamepadName = device.InstanceName;
                    data.Add(player);
                }
            }
            if (game.Game.SupportsXInput)
            {
                for (int j = 0; j < data.Count; j++)
                {
                    PlayerInfo p = data[j];
                    if (p.IsXInput)
                    {
                        Controller c = controllers[p.GamepadId];
                        if (!c.IsConnected)
                        {
                            changed = true;
                            data.RemoveAt(j);
                            j--;
                        }
                    }
                }

                for (int i = 0; i < 4; i++)
                {
                    Controller c = controllers[i];
                    bool already = false;

                    if (c.IsConnected)
                    {
                        // see if this gamepad is already on a player
                        for (int j = 0; j < data.Count; j++)
                        {
                            PlayerInfo p = data[j];
                            if (p.IsXInput && p.GamepadId == i)
                            {
                                State s = c.GetState();
                                int newmask = (int)s.Gamepad.Buttons;
                                if (p.GamepadMask != newmask)
                                {
                                    changed = true;
                                    p.GamepadMask = newmask;
                                }

                                already = true;
                                break;
                            }
                        }
                        if (already)
                        {
                            continue;
                        }

                        changed = true;

                        // new gamepad
                        PlayerInfo player = new PlayerInfo();
                        player.IsXInput = true;
                        player.GamepadId = i;
                        data.Add(player);
                    }
                }
            }

            if (changed)
            {
                UpdatePlayers();
                Refresh();
            }
        }

        private void AddPlayer(int i, float playerWidth, float playerHeight, float offset)
        {
            Rectangle r = RectangleUtil.Float(50 + ((playerWidth + offset) * i), 100, playerWidth, playerHeight);
            PlayerInfo player = new PlayerInfo();
            player.EditBounds = r;
            profile.PlayerData.Add(player);
        }

        private void UpdateScreens()
        {
            if (screens == null)
            {
                screens = ScreensUtil.AllScreens();
                totalBounds = RectangleUtil.Union(screens);
            }
            else
            {
                UserScreen[] newScreens = ScreensUtil.AllScreens();
                Rectangle newBounds = RectangleUtil.Union(newScreens);
                if (newBounds.Equals(totalBounds))
                {
                    return;
                }

                // screens got updated, need to reflect in our window
                screens = newScreens;
                totalBounds = newBounds;

                // remove all players screens
                List<PlayerInfo> playerData = profile.PlayerData;
                if (playerData != null)
                {
                    for (int i = 0; i < playerData.Count; i++)
                    {
                        PlayerInfo player = playerData[i];
                        player.EditBounds = GetDefaultBounds(draggingIndex);
                        player.ScreenIndex = -1;
                    }
                }
            }

            if (totalBounds.Width > totalBounds.Height)
            {
                // horizontal monitor setup
                scale = (this.Width * 0.9f) / (float)totalBounds.Width;
                if (totalBounds.Height * scale > this.Height * 0.4f)
                {
                    scale = (this.Height * 0.4f) / (float)totalBounds.Height;
                }
            }
            else
            {
                // vertical monitor setup
                scale = (this.Height * 0.4f) / (float)totalBounds.Height;
                if (totalBounds.Width * scale > this.Width * 0.9f)
                {
                    scale = (this.Width * 0.9f) / (float)totalBounds.Width;
                }
            }

            Rectangle scaledBounds = RectangleUtil.Scale(totalBounds, scale);
            scaledBounds = RectangleUtil.Center(scaledBounds, RectangleUtil.Float(0, this.Height * 0.25f, this.Width, this.Height * 0.7f));

            int minY = 0;
            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen screen = screens[i];

                Rectangle bounds = RectangleUtil.Scale(screen.MonitorBounds, scale);
                Rectangle uiBounds = new Rectangle(bounds.X + scaledBounds.X, bounds.Y + scaledBounds.Y, bounds.Width, bounds.Height);
                screen.UIBounds = uiBounds;

                minY = Math.Min(minY, uiBounds.X);
            }

            // remove negative monitors
            minY = -minY;
            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen screen = screens[i];

                Rectangle uiBounds = screen.UIBounds;
                uiBounds.X += minY;
                screen.UIBounds = uiBounds;
                screen.SwapTypeBounds = RectangleUtil.Float(uiBounds.X, uiBounds.Y, uiBounds.Width * 0.1f, uiBounds.Width * 0.1f);
            }
        }
        public override void Initialize(UserGameInfo game, GameProfile profile)
        {
            base.Initialize(game, profile);

            gamepadTimer.Enabled = true;
            canProceed = false;
            UpdatePlayers();
        }

        private void UpdatePlayers()
        {
            float playersWidth = this.Width * 0.5f;

            List<PlayerInfo> playerData = profile.PlayerData;
            int playerCount = playerData.Count;
            float playerWidth = (playersWidth * 0.9f) / (float)playerCount;
            float playerHeight = Math.Min(this.Height * 0.07f, playerWidth * 0.5625f);
            float offset = (playersWidth * 0.1f) / (float)playerCount;
            playersArea = new RectangleF(50, 100, playersWidth, playerHeight);

            UpdateScreens();

            for (int i = 0; i < playerData.Count; i++)
            {
                PlayerInfo info = playerData[i];

                if (info.ScreenIndex == -1)
                {
                    info.EditBounds = GetDefaultBounds(i);
                }
            }

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            mousePos = e.Location;

            if (dragging)
            {
                var players = profile.PlayerData;

                PlayerInfo player = players[draggingIndex];
                Rectangle p = player.EditBounds;
                if (draggingScreen == -1)
                {
                    for (int i = 0; i < screens.Length; i++)
                    {
                        UserScreen screen = screens[i];
                        Rectangle s = screen.UIBounds;
                        float pc = RectangleUtil.PcInside(p, s);

                        // bigger than 60% = major part inside this screen
                        if (pc > 0.6f)
                        {
                            float offset = s.Width * 0.05f;

                            // check if there's space available on this screen
                            var playas = profile.PlayerData;
                            Rectangle? editor;
                            Rectangle? monitor;
                            GetFreeSpace(i, out editor, out monitor);

                            if (editor != null)
                            {
                                draggingScreenRec = editor.Value;
                                draggingScreenBounds = monitor.Value;
                                draggingScreen = i;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    Rectangle s = screens[draggingScreen].UIBounds;
                    float pc = RectangleUtil.PcInside(p, s);
                    if (pc < 0.6f)
                    {
                        draggingScreen = -1;
                    }
                }

                p = new Rectangle(mousePos.X + draggingOffset.X, mousePos.Y + draggingOffset.Y, p.Width, p.Height);
                players[draggingIndex].EditBounds = p;

                Invalidate();
            }
        }

        private void GetFreeSpace(int screenIndex, out Rectangle? editorBounds, out Rectangle? monitorBounds)
        {
            editorBounds = null;
            monitorBounds = null;

            var players = profile.PlayerData;
            UserScreen screen = screens[screenIndex];
            Rectangle bounds = screen.MonitorBounds;
            Rectangle ebounds = screen.UIBounds;

            switch (screen.Type)
            {
                case UserScreenType.FullScreen:
                    for (int i = 0; i < players.Count; i++)
                    {
                        PlayerInfo p = players[i];
                        if (p.ScreenIndex == screenIndex)
                        {
                            return;
                        }
                    }

                    monitorBounds = screen.MonitorBounds;
                    editorBounds = screen.UIBounds;
                    break;
                case UserScreenType.DualHorizontal:
                    {
                        int playersUsing = 0;
                        Rectangle areaUsed = new Rectangle();

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                                areaUsed = Rectangle.Union(areaUsed, p.MonitorBounds);
                            }
                        }

                        if (playersUsing == 2)
                        {
                            return;
                        }

                        int half = (int)(bounds.Height / 2.0f);

                        for (int i = 0; i < 2; i++)
                        {
                            Rectangle area = new Rectangle(bounds.X, bounds.Y + (half * i), bounds.Width, half);
                            if (!areaUsed.Contains(area))
                            {
                                monitorBounds = area;

                                int halfe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X, ebounds.Y + (halfe * i), ebounds.Width, halfe);
                                return;
                            }
                        }
                    }
                    break;
                case UserScreenType.DualVertical:
                    {
                        int playersUsing = 0;
                        Rectangle areaUsed = new Rectangle();

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;

                                if (i == 0)
                                {
                                    // this check needs to exist, because if the coordinates in the monitor
                                    // are negative the Union method will extend from 0 to the negative and we'll end up messing everything up
                                    areaUsed = p.MonitorBounds;
                                }
                                else
                                {
                                    areaUsed = Rectangle.Union(areaUsed, p.MonitorBounds);
                                }
                            }
                        }

                        if (playersUsing == 2)
                        {
                            return;
                        }

                        int half = (int)(bounds.Width / 2.0f);

                        for (int i = 0; i < 2; i++)
                        {
                            Rectangle area = new Rectangle(bounds.X + (half * i), bounds.Y, half, bounds.Height);
                            if (!areaUsed.Contains(area))
                            {
                                monitorBounds = area;
                                int halfe = (int)(ebounds.Width / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfe * i), ebounds.Y, halfe, ebounds.Height);
                                return;
                            }
                        }
                    }
                    break;
                case UserScreenType.FourPlayers:
                    {
                        int playersUsing = 0;
                        Rectangle areaUsed = new Rectangle();

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfo p = players[i];
                            if (p.ScreenIndex == screenIndex)
                            {
                                playersUsing++;
                                areaUsed = Rectangle.Union(areaUsed, p.MonitorBounds);
                            }
                        }

                        if (playersUsing == 4)
                        {
                            return;
                        }

                        int halfw = (int)(bounds.Width / 2.0f);
                        int halfh = (int)(bounds.Height / 2.0f);

                        for (int x = 0; x < 2; x++)
                        {
                            for (int y = 0; y < 2; y++)
                            {
                                Rectangle area = new Rectangle(bounds.X + (halfw * x), bounds.Y + (halfh * y), halfw, halfh);

                                bool goNext = false;
                                // check if there's any player with the area's x,y coord
                                for (int i = 0; i < players.Count; i++)
                                {
                                    PlayerInfo p = players[i];
                                    if (p.ScreenIndex == screenIndex)
                                    {
                                        //if (p.MonitorBounds.X == area.X &&
                                        //    p.MonitorBounds.Y == area.Y)
                                        if (p.MonitorBounds.IntersectsWith(area))
                                        {
                                            goNext = true;
                                            break;
                                        }
                                    }
                                }

                                if (goNext)
                                {
                                    continue;
                                }
                                monitorBounds = area;
                                int halfwe = (int)(ebounds.Width / 2.0f);
                                int halfhe = (int)(ebounds.Height / 2.0f);
                                editorBounds = new Rectangle(ebounds.X + (halfwe * x), ebounds.Y + (halfhe * y), halfwe, halfhe);
                                return;
                            }
                        }
                    }
                    break;
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            var players = profile.PlayerData;

            if (dragging)
            {
                return;
            }

            if (e.Button == MouseButtons.Left)
            {
                for (int i = 0; i < screens.Length; i++)
                {
                    UserScreen screen = screens[i];
                    if (screen.SwapTypeBounds.Contains(e.Location))
                    {
                        if (screen.Type == UserScreenType.FourPlayers)
                        {
                            screen.Type = 0;
                        }
                        else
                        {
                            screen.Type++;
                        }

                        // invalidate all players inside screen
                        for (int j = 0; j < players.Count; j++)
                        {
                            // return to default position
                            PlayerInfo p = players[j];
                            if (p.ScreenIndex == i)
                            {
                                p.EditBounds = GetDefaultBounds(j);
                                p.ScreenIndex = -1;
                            }
                        }

                        Invalidate();
                        return;
                    }
                }

                for (int i = 0; i < players.Count; i++)
                {
                    Rectangle r = players[i].EditBounds;
                    if (r.Contains(e.Location))
                    {
                        dragging = true;
                        draggingIndex = i;
                        draggingOffset = new Point(r.X - e.X, r.Y - e.Y);
                        Rectangle newBounds = GetDefaultBounds(draggingIndex);
                        profile.PlayerData[draggingIndex].EditBounds = newBounds;

                        if (draggingOffset.X < -newBounds.Width ||
                            draggingOffset.Y < -newBounds.Height)
                        {
                            draggingOffset = new Point(0, 0);
                        }

                        break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right ||
                     e.Button == MouseButtons.Middle)
            {
                // if over a player on a screen, change the type
                for (int i = 0; i < players.Count; i++)
                {
                    PlayerInfo p = players[i];
                    Rectangle r = p.EditBounds;
                    if (r.Contains(e.Location))
                    {
                        if (p.ScreenIndex != -1)
                        {
                            UserScreen screen = screens[p.ScreenIndex];
                            int halfWidth = screen.MonitorBounds.Width / 2;
                            int halfHeight = screen.MonitorBounds.Height / 2;

                            Rectangle bounds = p.MonitorBounds;
                            if (screen.Type == UserScreenType.FourPlayers)
                            {
                                // check if the size is 1/4th of screen
                                if (bounds.Width == halfWidth &&
                                    bounds.Height == halfHeight)
                                {
                                    bool hasLeftRightSpace = true;
                                    bool hasTopBottomSpace = true;

                                    // check if we have something left/right or top/bottom
                                    for (int j = 0; j < players.Count; j++)
                                    {
                                        if (i == j)
                                        {
                                            continue;
                                        }

                                        PlayerInfo other = players[j];
                                        if (other.ScreenIndex != p.ScreenIndex)
                                        {
                                            continue;
                                        }

                                        if (other.MonitorBounds.Y == p.MonitorBounds.Y)
                                        {
                                            hasLeftRightSpace = false;
                                        }
                                        if (other.MonitorBounds.X == p.MonitorBounds.X)
                                        {
                                            hasTopBottomSpace = false;
                                        }

                                        if (other.MonitorBounds.X == screen.MonitorBounds.X + halfWidth &&
                                            other.MonitorBounds.Height == screen.MonitorBounds.Height)
                                        {
                                            hasLeftRightSpace = false;
                                        }
                                        if (other.MonitorBounds.X == screen.MonitorBounds.X &&
                                            other.MonitorBounds.Width == screen.MonitorBounds.Width)
                                        {
                                            hasTopBottomSpace = false;
                                        }
                                    }

                                    if (hasLeftRightSpace)
                                    {
                                        Rectangle edit = p.EditBounds;
                                        if (bounds.X == screen.MonitorBounds.X + bounds.Width)
                                        {
                                            bounds.X -= bounds.Width;
                                            edit.X -= edit.Width;
                                        }

                                        bounds.Width *= 2;
                                        edit.Width *= 2;

                                        p.EditBounds = edit;
                                        p.MonitorBounds = bounds;

                                        Invalidate();
                                    }
                                    else if (hasTopBottomSpace)
                                    {
                                        bounds.Height *= 2;
                                        p.MonitorBounds = bounds;
                                        Rectangle edit = p.EditBounds;
                                        edit.Height *= 2;
                                        p.EditBounds = edit;

                                        Invalidate();
                                    }
                                }
                                else
                                {
                                    bounds.Width = screen.MonitorBounds.Width / 2;
                                    bounds.Height = screen.MonitorBounds.Height / 2;
                                    p.MonitorBounds = bounds;

                                    Rectangle edit = p.EditBounds;
                                    edit.Width = screen.UIBounds.Width / 2;
                                    edit.Height = screen.UIBounds.Height / 2;
                                    p.EditBounds = edit;

                                    Invalidate();
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
            {
                if (dragging)
                {
                    PlayerInfo p = profile.PlayerData[draggingIndex];
                    dragging = false;

                    if (draggingScreen != -1)
                    {
                        p.ScreenIndex = draggingScreen;
                        p.MonitorBounds = draggingScreenBounds;
                        p.EditBounds = draggingScreenRec;

                        draggingScreen = -1;
                    }
                    else
                    {
                        // return to default position
                        p.EditBounds = GetDefaultBounds(draggingIndex);
                        p.ScreenIndex = -1;
                    }

                    if (profile.PlayerData.Count > 1)
                    {
                        canProceed = true;
                        for (int i = 0; i < profile.PlayerData.Count; i++)
                        {
                            PlayerInfo player = profile.PlayerData[i];
                            if (player.ScreenIndex == -1)
                            {
                                canProceed = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        canProceed = false;
                    }
                    CanPlayUpdated(canProceed, false);

                    Invalidate();
                }
            }
        }

        private Rectangle GetDefaultBounds(int index)
        {
            float playersWidth = this.Width * 0.5f;
            float playerWidth = Math.Min(this.Width * 0.23f, (playersWidth * 0.9f) / (float)profile.PlayerData.Count);
            float playerHeight = playerWidth * 0.5625f;
            float offset = (playersWidth * 0.1f) / (float)profile.PlayerData.Count;
            return new Rectangle((int)(50 + ((playerWidth + offset) * index)), 100, (int)playerWidth, (int)playerHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            UpdateScreens();

            Graphics g = e.Graphics;
            for (int i = 0; i < screens.Length; i++)
            {
                UserScreen s = screens[i];
                g.DrawRectangle(Pens.White, s.UIBounds);
                g.DrawRectangle(Pens.White, s.SwapTypeBounds);

                switch (s.Type)
                {
                    case UserScreenType.FullScreen:
                        g.DrawImage(Resources.fullscreen, s.SwapTypeBounds);
                        break;
                    case UserScreenType.DualHorizontal:
                        g.DrawImage(Resources.horizontal, s.SwapTypeBounds);
                        break;
                    case UserScreenType.DualVertical:
                        g.DrawImage(Resources.vertical, s.SwapTypeBounds);
                        break;
                    case UserScreenType.FourPlayers:
                        g.DrawImage(Resources._4players, s.SwapTypeBounds);
                        break;
                }
            }

            var players = profile.PlayerData;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInfo info = players[i];
                Rectangle s = info.EditBounds;
                g.Clip = new Region(new RectangleF(s.X, s.Y, s.Width + 1, s.Height + 1));

                Rectangle gamepadRect = RectangleUtil.ScaleAndCenter(gamepadImg.Size, s);

                string str = (i + 1).ToString();
                SizeF size = g.MeasureString(str, playerFont);
                PointF loc = RectangleUtil.Center(size, s);
                if (info.IsXInput)
                {
                    loc.Y -= gamepadRect.Height * 0.1f;
                    GamepadButtonFlags flags = (GamepadButtonFlags)info.GamepadMask;
                    g.DrawString(flags.ToString(), smallTextFont, Brushes.White, new PointF(loc.X, loc.Y + gamepadRect.Height * 0.01f));

                    g.DrawString((info.GamepadId + 1).ToString(), playerFont, Brushes.White, loc);
                    g.DrawImage(gamepadImg, gamepadRect);
                }
                else
                {
                    loc.X = s.X;
                    g.DrawString(info.GamepadName, playerTextFont, Brushes.White, loc);
                    g.DrawImage(genericImg, gamepadRect);
                }

                if (info.ScreenIndex != -1)
                {
                    g.DrawRectangle(Pens.Green, s);
                }
            }
            g.ResetClip();

            if (dragging && draggingScreen != -1)
            {
                g.DrawRectangle(Pens.Red, draggingScreenRec);
            }

            g.DrawString("Drag each player to\ntheir respective screen", playerTextFont, Brushes.White, new PointF(470, 100));
            g.DrawString("Players", playerTextFont, Brushes.White, new PointF(50, 50));

            g.DrawString("Click on screen's top-left corner to change players on that screen", playerTextFont, Brushes.White, new PointF(20, 500));
            g.DrawString("(4-player only) Right click player to change size", playerTextFont, Brushes.White, new PointF(20, 530));
        }
    }
}
