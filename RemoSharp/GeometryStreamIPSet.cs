using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoSharp
{
    public partial class GeometryStremIPSet : Form
    {
        public string WS_Server_Address = "";
        public GeometryStremIPSet()
        {
            this.WS_Server_Address = "";
            InitializeComponent();
        }

        private void Set_Full_Address_Click(object sender, EventArgs e)
        {
            string address = this.Full_Address_Box.Text;
            while (address.Contains(" "))
            {
                address = address.Replace(" ", "");
            }
            address = address.Replace(Environment.NewLine, "");
            this.WS_Server_Address = address;
            this.Close();
        }

        private void Set_IP_Address_Click(object sender, EventArgs e)
        {
            string IP_address = this.IP_Address_Box.Text;
            while (IP_address.Contains(" "))
            {
                IP_address = IP_address.Replace(" ", "");
            }
            IP_address = IP_address.Replace(Environment.NewLine, "");

            string Port_Address = this.Port_Box.Text;
            while (Port_Address.Contains(" "))
            {
                Port_Address = Port_Address.Replace(" ", "");
            }
            Port_Address = Port_Address.Replace(Environment.NewLine, "");

            this.WS_Server_Address = "ws://"+ IP_address + ":"+ Port_Address + "/RemoSharp";
            this.Close();
        }
    }
}
