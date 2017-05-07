using Micro;
using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Micro.NetLib;

namespace Micro.RemoteTest {
    public partial class FormBase : Form {
        bool clientMode = false;
        Regex regAddress = new Regex(@"[\w.]+:\d{1,5}");
        Connection connect;

        public FormBase() {
            InitializeComponent();
            connect = new Connection();
            connect.startResult += startResult;
            connect.stoppedClient += (a, b) => stoppedClient(a, b);
            connect.stoppedServer += stoppedServer;
            connect.updateUserList += updateList;
            connect.updateForm += updateForm;
            connect.incomingMessage += incomingMessage;
            connect.incomingUser += incomingUser;
            connect.leavingUser += (a, b, c) => stoppedClient(b, c, a);
            _updateForm();
        }
        public FormBase(string nick, string addr) : this() {
            txtNick.Text = nick;
            txtAddress.Text = addr;
#if DEBUG
            Size = MinimumSize;
#endif
        }
        private void FormBase_Load(object sender, EventArgs e) {
            Activate();
        }
        private void FormBase_FormClosing(object sender, FormClosingEventArgs e) {
            btnStop.PerformClick();
        }
        
        private void txtNickAddress_TextChanged(object sender, EventArgs e) {
            _updateForm();
        }
        private void btnClientServer_Click(object sender, EventArgs e) {
            if (clientMode = (sender == btnClient)) {
                _writeChat("Connecting...");
                var addr = txtAddress.Text.Split(':');
                connect.startClient(txtNick.Text, addr[0], int.Parse(addr[1]));
            } else if (sender == btnServer) {
                connect.startServer(txtNick.Text, int.Parse(txtAddress.Text));
            }
        }
        private void btnStop_Click(object sender, EventArgs e) {
            connect.stop();
        }
        private void btnSend_Click(object sender, EventArgs e) {
            sendText();
        }
        private void btnKick_Click(object sender, EventArgs e) {
            kickClients();
        }

        private void txtChat_KeyDown(object sender, KeyEventArgs e) {
            if (e.KeyData == Keys.Enter) {
                if (e.Shift)
                    txtChat.Text += "\n";
                else
                    sendText();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
        private void txtChat_TextChanged(object sender, EventArgs e) {
            int nc = Regex.Matches(txtChat.Text, @"\n").Count;
            int he = 22 + (txtChat.Font.Height - 1) * Math.Min(nc, 8);
            tableLayout.RowStyles[1].Height = he + 4;
        }
        private void listUsers_KeyUp(object sender, KeyEventArgs e) {
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
            bool none = connect.isNone;
            txtNick.Enabled = txtAddress.Enabled = none;
            txtChat.Enabled = btnSend.Enabled = btnStop.Enabled = !none;
            btnKick.Enabled = connect.isServer;
            btnClient.Enabled = (none && !string.IsNullOrWhiteSpace(txtAddress.Text) && 
                                 regAddress.Match(txtAddress.Text).Value == txtAddress.Text);
            btnServer.Enabled = (none && !string.IsNullOrWhiteSpace(txtAddress.Text) &&
                                 Regex.Match(txtAddress.Text, @"\d{1,5}").Value == txtAddress.Text);
        }
        void _updateList() {
            listUsers.Items.Clear();
            foreach (var u in connect.Users) {
                listUsers.Items.Add(new UserItem(u));
            }
        }
        void _writeChat(string text) {
            textLog.SuspendLayout();
            textLog.Text += string.Format("[{0}] {1}\r\n", getTime(), text);
            textLog.SelectionStart = textLog.Text.Length - 1;
            textLog.ScrollToCaret();
            textLog.ResumeLayout();
            txtChat.Focus();
        }

        void sendText() {
            writeChat(txtNick.Text + ": " + txtChat.Text);
            connect.sendAll(txtChat.Text);
            txtChat.Text = "";
        }
        void kickClients() {
            if (connect.isServer && listUsers.SelectedIndices.Count > 0) {
                var tup = FormDialog.AskInput("Kick clients", "Type reason...");
                if (tup.Item1 == DialogResult.OK) {
                    UserItem[] users = new UserItem[listUsers.SelectedItems.Count];
                    listUsers.SelectedItems.CopyTo(users, 0);
                    foreach (UserItem item in users) {
                        connect.kick(item.user.id, tup.Item2);
                    }
                }
            }
        }
        void startResult(bool success) {
            if (success) {
                writeChat(connect.isClient ? "Connected" : "Listening");
                connect.startProcessing();
            } else
                writeChat("Unable to " + (clientMode ? "connect" : "listen"));
        }
        void stoppedClient(stopReason reason, string additional, User user = null) {
            writeChat(connect.getLeaveReason(reason, additional, user));
        }
        void stoppedServer() {
            writeChat("Server closed");
        }
        void incomingMessage(User user, string[] text) {
            writeChat(string.Format("{0}: {1}", user.nickname, text.Length > 0 ? text[0] : ""));
        }
        void incomingUser(User user) {
            writeChat(string.Format("{0} connected", user.nickname));
        }
        
        string getTime() {
            var now = DateTime.Now;
            string h = now.Hour + "",
                   m = now.Minute + "",
                   s = now.Second + "";
            h = h.Length < 2 ? "0" + h : h;
            m = m.Length < 2 ? "0" + m : m;
            s = s.Length < 2 ? "0" + s : s;
            return string.Format("{0}:{1}:{2}", h, m, s);
        }
    }

    class UserItem {
        public readonly User user;

        public UserItem(User u) {
            user = u;
        }
        public override string ToString() {
            return user.nickname;
        }
    }
}
