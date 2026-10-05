using System;
using System.Windows.Forms;

namespace BeanModManager
{
    public partial class Main : Form
    {
        private void UpdateStatus(string message)
        {
            if (InvokeRequired)
            {
                _ = Invoke(new Action<string>(UpdateStatus), message);
                return;
            }
            lblStatus.Text = message;
        }

        private void SafeInvoke(Action action)
        {
            if (InvokeRequired)
            {
                _ = Invoke(action);
            }
            else
            {
                action();
            }
        }

        private T SafeInvoke<T>(Func<T> func)
        {
            return InvokeRequired ? (T)Invoke(func) : func();
        }
    }
}


