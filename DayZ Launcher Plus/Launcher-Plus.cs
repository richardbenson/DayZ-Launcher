using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using DZLP.Objects;

namespace DZLP
{
    public partial class LauncherPlus : Form
    {
        string DefaultCDN = "http://cdn.armafiles.info/latest/";
        string CurrentCDN;

        List<Server> ServerList = new List<Server>();
        
        public LauncherPlus()
        {
            InitializeComponent();
        }

        public void StartUp(object sender, EventArgs e)
        {
            //Fetch the list of servers from Gamespy
            this.ServerList = Util.GetServerList();
        }

        internal void UpdateStatus(string strMessage)
        {
            lblStatus.Invoke((Action)(() => lblStatus.Text = strMessage));
        }
    }
}
