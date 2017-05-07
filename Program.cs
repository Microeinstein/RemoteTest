using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Micro.RemoteTest {
    static class Program {
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

#if DEBUG
            Thread t1, t2;
            t1 = new Thread(new ThreadStart(() => Application.Run(new FormBase("Server", "8080"))));
            t2 = new Thread(new ThreadStart(() => Application.Run(new FormBase("Micro", "localhost:8080"))));
            t1.Start();
            t2.Start();
#else
            Application.Run(new FormBase());
#endif
        }
    }

    public class CueTextBox : TextBox {
        [Localizable(true)]
        public string Cue {
            get { return mCue; }
            set { mCue = value; updateCue(); }
        }

        private void updateCue() {
            if (this.IsHandleCreated && mCue != null) {
                SendMessage(this.Handle, 0x1501, (IntPtr)1, mCue);
            }
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            updateCue();
        }
        private string mCue;

        // PInvoke
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);
    }
}
