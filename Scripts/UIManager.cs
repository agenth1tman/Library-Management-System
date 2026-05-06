using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Library_Management_System
{
    public class UIManager
    {
        Color OGColor = Color.FromArgb(240, 197, 57);
        Color ClickedColor = Color.FromArgb(153, 127, 31);

        public async Task TabButtonClicked(Button btn) 
        {
            btn.ForeColor = ClickedColor;

            await Task.Delay(70);

            btn.ForeColor = OGColor;
        }
    }
}
