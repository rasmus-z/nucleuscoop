﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Nucleus.Gaming;
using System.Collections;
using System.Reflection;
using Nucleus.Gaming.Coop;
using Nucleus.Gaming.Platform.Windows.Controls;
using Nucleus.Gaming.Windows.Controls;
using Nucleus.Gaming.Platform.Windows;

namespace Nucleus.Gaming.Coop.Controls
{
    public class PlayerOptionsControl : UserInputControl
    {
        private ControlListBox list;
        private Font nameFont;
        private Font detailsFont;

        public override bool CanProceed
        {
            get { return true; }
        }

        public override bool CanPlay
        {
            get { return true; }
        }

        public override string Title
        {
            get { return "Player Options"; }
        }

        public PlayerOptionsControl()
        {
            nameFont = new Font("Segoe UI", 18);
            detailsFont = new Font("Segoe UI", 12);
        }

        public override void Initialize(HandlerData handlerData, UserGameInfo game, GameProfile profile) {
            base.Initialize(handlerData, game, profile);

            this.Controls.Clear();

            int wid = 200;

            Image = FormGraphicsUtil.BuildCharToBitmap(new Size(40, 40), 30, Color.FromArgb(240, 240, 240), "⚙");
            list = new ControlListBox();
            list.Size = this.Size;

            List<GameOption> options = handlerData.Options;
            Dictionary<string, object> vals = profile.Options;
            for (int j = 0; j < options.Count; j++)
            {
                GameOption opt = options[j];
                if (opt.Hidden)
                {
                    continue;
                }

                object val;
                if (!vals.TryGetValue(opt.Key, out val))
                {
                    continue;
                }

                CoolListControl cool = new CoolListControl(false);
                cool.Title = opt.Name;
                cool.Details = opt.Description;
                cool.Width = list.Width;
                cool.TitleFont = nameFont;
                cool.DetailsFont = detailsFont;
                cool.EnableClicking = false;

                list.Controls.Add(cool);

                // Check the value type and add a control for it
                if (opt.Value is Enum || opt.IsCollection())
                {
                    ComboBox box = new ComboBox();
                    int border = 10;

                    object value;
                    object defaultValue;
                    IList values;
                    if (opt.Value is Enum)
                    {
                        value = (Enum)val;
                        values = Enum.GetValues(value.GetType());
                        defaultValue = opt.DefaultValue;
                    }
                    else
                    {
                        values = opt.GetCollection();
                        value = values[0];
                        defaultValue = opt.DefaultValue;
                    }

                    for (int i = 0; i < values.Count; i++)
                    {
                        box.Items.Add(values[i]);
                    }

                    if (defaultValue != null)
                    {
                        box.SelectedIndex = box.Items.IndexOf(defaultValue);
                        if (box.SelectedIndex == -1)
                            box.SelectedIndex = box.Items.IndexOf(value);
                    }
                    else
                        box.SelectedIndex = box.Items.IndexOf(value);

                    box.Width = wid;
                    box.Height = 40;
                    box.Left = cool.Width - box.Width - border;
                    box.Top = (cool.Height / 2) - (box.Height / 2);
                    box.Anchor = AnchorStyles.Right;
                    box.DropDownStyle = ComboBoxStyle.DropDownList;
                    cool.Controls.Add(box);

                    box.Tag = opt;
                    box.SelectedValueChanged += box_SelectedValueChanged;
                    ChangeOption(box.Tag, box.SelectedItem);
                }
                else if (opt.Value is bool)
                {
                    SizeableCheckbox box = new SizeableCheckbox();
                    int border = 10;

                    box.Checked = (bool)val;
                    box.Width = 40;
                    box.Height = 40;
                    box.Left = cool.Width - box.Width - border;
                    box.Top = (cool.Height / 2) - (box.Height / 2);
                    box.Anchor = AnchorStyles.Right;
                    cool.Controls.Add(box);

                    box.Tag = opt;
                    box.CheckedChanged += box_CheckedChanged;
                    ChangeOption(box.Tag, box.Checked);
                }
                else if (opt.Value is int || opt.Value is double)
                {
                    NumericUpDown num = new NumericUpDown();
                    int border = 10;

                    int value = int.Parse(val.ToString());
                    if (value < num.Minimum)
                    {
                        num.Minimum = value;
                    }

                    num.Value = value;

                    num.Width = wid;
                    num.Height = 40;
                    num.Left = cool.Width - num.Width - border;
                    num.Top = (cool.Height / 2) - (num.Height / 2);
                    num.Anchor = AnchorStyles.Right;
                    cool.Controls.Add(num);

                    num.Tag = opt;
                    num.ValueChanged += num_ValueChanged;
                    ChangeOption(num.Tag, num.Value);
                }
                else if (opt.Value is GameOptionValue)
                {
                    ComboBox box = new ComboBox();
                    int border = 10;

                    GameOptionValue value = (GameOptionValue)val;
                    PropertyInfo[] props = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Static);

                    for (int i = 0; i < props.Length; i++)
                    {
                        PropertyInfo prop = props[i];
                        box.Items.Add(prop.GetValue(null, null));
                    }
                    box.SelectedIndex = box.Items.IndexOf(value);

                    box.Width = wid;
                    box.Height = 40;
                    box.Left = cool.Width - box.Width - border;
                    box.Top = (cool.Height / 2) - (box.Height / 2);
                    box.Anchor = AnchorStyles.Right;
                    //box.DropDownStyle = ComboBoxStyle.DropDownList;
                    cool.Controls.Add(box);

                    box.Tag = opt;
                    box.SelectedValueChanged += box_SelectedValueChanged;
                    ChangeOption(box.Tag, box.SelectedItem);
                }

                UserControl bottomLine = new UserControl();
                bottomLine.Width = cool.Width;
                bottomLine.Height = 1;
                bottomLine.Top = cool.Height - 1;
                bottomLine.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
                bottomLine.BackColor = Color.FromArgb(35, 39, 42);
                cool.Controls.Add(bottomLine);
            }

            this.Controls.Add(list);
            list.UpdateSizes();
            CanPlayUpdated(true, false);
        }

        private void ChangeOption(object tag, object value)
        {
            // boxing but wahtever
            GameOption option = (GameOption)tag;
            profile.Options[option.Key] = value;
        }

        private void box_SelectedValueChanged(object sender, EventArgs e)
        {
            ComboBox check = (ComboBox)sender;
            if (check.SelectedItem == null)
            {
                return;
            }
            ChangeOption(check.Tag, check.SelectedItem);
        }

        private void num_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown check = (NumericUpDown)sender;
            ChangeOption(check.Tag, check.Value);
        }

        private void box_CheckedChanged(object sender, EventArgs e)
        {
            SizeableCheckbox check = (SizeableCheckbox)sender;
            ChangeOption(check.Tag, check.Checked);
        }
    }
}
