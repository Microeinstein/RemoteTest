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
            NetLib.Core.debugStart();
            Thread t1, t2, t3;
            t1 = new Thread(new ThreadStart(() => Application.Run(new FormBase("Server", "8080"))));
            t2 = new Thread(new ThreadStart(() => Application.Run(new FormBase("UserA", "localhost:8080"))));
            //t3 = new Thread(new ThreadStart(() => Application.Run(new FormBase("UserB", "localhost:8080"))));
            t1.Start();
            t2.Start();
            //t3.Start();
#else
            Application.Run(new FormBase());
#endif
        }
    }

    public class CueTextBox : TextBox {
        [Localizable(true)]
        public string Cue {
            get => _cue;
            set { _cue = value; updateCue(); }
        }
        private string _cue;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, string lp);

        private void updateCue() {
            if (IsHandleCreated && _cue != null)
                SendMessage(Handle, 0x1501, (IntPtr)1, _cue);
        }
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            updateCue();
        }
    }
}
