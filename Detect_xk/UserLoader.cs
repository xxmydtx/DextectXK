using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Detect_xk
{
    public partial class UserLoader : Form
    {
        public string PassWord;
        public string UserName;
        public bool IsIn = false;
        public UserLoader()
        {
            InitializeComponent();
        }

        private void btn_SignIn_Click(object sender, EventArgs e)
        {
            if(IsUser())
            {
                
                PassWord = textUserMima.Text;
                UserName = textUserName.Text;
                IsIn = true;
                this.DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }
        bool IsUser()
        {
            return true;
        }

    }
}
