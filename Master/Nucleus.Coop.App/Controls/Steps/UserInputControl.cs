﻿using Nucleus.Gaming.Coop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nucleus.Gaming.Coop {
    public class UserInputControl : UserControl {
        protected GameProfile profile;
        protected UserGameInfo game;
        protected HandlerData handlerData;

        public virtual bool CanProceed { get { throw new NotImplementedException(); } }
        public virtual bool CanPlay { get { throw new NotImplementedException(); } }

        public virtual string Title { get { throw new NotImplementedException(); } }

        public GameProfile Profile { get { return profile; } }

        public Image Image { get; set; }

        public event Action<UserControl, bool, bool> OnCanPlayUpdated;

        protected virtual void RemoveFlicker() {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer,
                true);
        }

        public virtual void Initialize(HandlerData handlerData, UserGameInfo game, GameProfile profile) {
            this.handlerData = handlerData;
            this.profile = profile;
            this.game = game;
        }

        public virtual void Ended() {

        }

        protected virtual void CanPlayUpdated(bool canPlay, bool autoProceed) {
            if (OnCanPlayUpdated != null) {
                OnCanPlayUpdated(this, canPlay, autoProceed);
            }
        }

        public UserInputControl() {
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Color.FromArgb(54, 57, 63);
            ForeColor = Color.FromArgb(240, 240, 240);
            Margin = new Padding(8, 8, 8, 8);

            // create it here, else the designer will show the default windows font
            Font = new Font("Segoe UI", 12, GraphicsUnit.Point);
        }
    }
}
