﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Micro.NetLib;
using Micro.NetLib.Abstraction;
using Micro.NetLib.Information;
using static Micro.NetLib.Core;

namespace Micro.RemoteTest {
    public partial class FormBase : Form {
        bool clientMode = false;
        Regex regAddress = new Regex(@"[\w.]+:\d{1,5}");
        Connection connect;
        List<string> chat;
        int capacity = 50;

        public FormBase() {
            InitializeComponent();
            connect = new Connection();
            connect.Started += startResult;
            connect.Disconnected += (a, b) => stoppedClient(a, b);
            connect.StoppedServer += stoppedServer;
            connect.UpdateUserList += updateList;
            connect.UpdateForm += updateForm;
            connect.IncomingMessages += incomingMessage;
            connect.IncomingUser += incomingUser;
            connect.LeavingUser += (a, b, c) => stoppedClient(b, c, a);
            chat = new List<string>(capacity);
            _updateForm();
        }
        public FormBase(string nick, string addr) : this() {
            txtNick.Text = nick;
            txtAddress.Text = addr;
#if DEBUG
            Size = MinimumSize;
#endif
        }

        void FormBase_Load(object sender, EventArgs e) {
            Activate();
        }
        void FormBase_FormClosing(object sender, FormClosingEventArgs e) {
            btnStop.PerformClick();
        }
        void txtNickAddress_TextChanged(object sender, EventArgs e) {
            _updateForm();
        }
        void btnClientServer_Click(object sender, EventArgs e) {
            if (clientMode = (sender == btnClient)) {
                _writeChat("Connecting...");
                var addr = txtAddress.Text.Split(':');
                connect.StartClient(txtNick.Text, addr[0], ushort.Parse(addr[1]));
            } else if (sender == btnServer) {
                connect.StartServer(txtNick.Text, ushort.Parse(txtAddress.Text));
            }
        }
        void btnStop_Click(object sender, EventArgs e) {
            connect.Stop();
        }
        void btnSend_Click(object sender, EventArgs e) {
            sendText();
        }
        void btnKick_Click(object sender, EventArgs e) {
            kickClients();
        }
        void textLog_LinkClicked(object sender, LinkClickedEventArgs e) {
            System.Diagnostics.Process.Start(e.LinkText);
        }
        void txtChat_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Enter) {
                if (e.Shift)
                    txtChat.Text += "\n";
                else
                    sendText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        void txtChat_TextChanged(object sender, EventArgs e) {
            int nc = Regex.Matches(txtChat.Text, @"\n").Count;
            int he = 22 + (txtChat.Font.Height - 1) * Math.Min(nc, 8);
            tableLayout.RowStyles[1].Height = he + 4;
        }
        void listUsers_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Delete)
                kickClients();
        }

        void updateForm() {
            Invoke(new Action(_updateForm));
        }
        void updateList() {
            Invoke(new Action(_updateList));
        }
        void writeChat(string text) {
            Invoke(new Action<string>(_writeChat), text);
        }

        void _updateForm() {
            bool none = connect.IsIdle;
            txtNick.Enabled = txtAddress.Enabled = none;
            txtChat.Enabled = btnSend.Enabled = btnStop.Enabled = !none;
            btnKick.Enabled = connect.IsServer;
            btnClient.Enabled = (none && !string.IsNullOrWhiteSpace(txtAddress.Text) &&
                                 regAddress.Match(txtAddress.Text).Value == txtAddress.Text);
            btnServer.Enabled = (none && !string.IsNullOrWhiteSpace(txtAddress.Text) &&
                                 Regex.Match(txtAddress.Text, @"\d{1,5}").Value == txtAddress.Text);
        }
        void _updateList() {
            listUsers.Items.Clear();
            foreach (var u in connect.Users)
                listUsers.Items.Add(u);
        }
        void _writeChat(string text) {
            lock (chat) {
                if (chat.Count >= capacity - 1)
                    chat.RemoveRange(0, chat.Count - (capacity - 1));
                chat.Add($"[{getTime()}] {text}");
                textLog.Text = chat.Aggregate((a, b) => $"{a}\r\n{b}");
            }
            textLog.SelectionStart = textLog.Text.Length - 1;
            textLog.ScrollToCaret();
            txtChat.Focus();
        }

        void sendText() {
            writeChat(txtNick.Text + ": " + txtChat.Text);
            connect.Broadcast(txtChat.Text);
            txtChat.Text = "";
        }
        void kickClients() {
            if (connect.IsServer && listUsers.SelectedIndices.Count > 0) {
                var tup = FormDialog.AskInput("Kick clients", "Type reason...");
                if (tup.Item1 == DialogResult.OK) {
                    User[] users = new User[listUsers.SelectedItems.Count];
                    listUsers.SelectedItems.CopyTo(users, 0);
                    foreach (User u in users) {
                        connect.Kick(u.ID, tup.Item2);
                    }
                }
            }
        }
        void startResult(bool success) {
            if (success)
                writeChat(connect.IsClient ? "Connected" : "Listening");
            else
                writeChat("Unable to " + (clientMode ? "connect" : "listen"));
        }
        void stoppedClient(LeaveReason reason, string additional, User user = null) {
            writeChat(connect.GetLeaveReason(reason, additional, user));
        }
        void stoppedServer() {
            writeChat("Server closed");
        }
        void incomingMessage(User user, string[] text) {
            writeChat($"{user.Nickname}: {(text.Length > 0 ? text[0] : "")}");
        }
        void incomingUser(User user) {
            writeChat($"{user.Nickname} connected");
        }

        string getTime() {
            var now = Now;
            string h = now.Hour + "",
                   m = now.Minute + "",
                   s = now.Second + "";
            h = h.Length < 2 ? "0" + h : h;
            m = m.Length < 2 ? "0" + m : m;
            s = s.Length < 2 ? "0" + s : s;
            return string.Format("{0}:{1}:{2}", h, m, s);
        }
    }
}