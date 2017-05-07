using System;
using System.Windows.Forms;

namespace Micro.RemoteTest {
    public partial class FormDialog : Form {
        public FormDialog() {
            InitializeComponent();
        }
        public static Tuple<DialogResult, string> AskInput(string title, string question) {
            var dialog = new FormDialog();
            dialog.Text = title;
            dialog.txtInput.Cue = question;
            return new Tuple<DialogResult, string>(dialog.ShowDialog(), dialog.txtInput.Text);
        }

        private void btnOK_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.OK;
            Close();
        }
        private void btnCancel_Click(object sender, EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
