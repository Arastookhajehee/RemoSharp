using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemoSharp.Utilities
{
    public partial class LoginDialouge : Form
    {
        public string username { get; private set; }
        public string password { get; private set; }
        public string sessionID { get; private set; }

        public LoginDialouge()
        {
            InitializeComponent();

           // get the credentials from the file
            Tuple<string, string, string> credentials = GetCredentials();

            // set the textboxes to the credentials
            usernameBox.Text = credentials.Item1;
            passwordBox.Text = credentials.Item2;
            sessionIDBox.Text = credentials.Item3;


            this.KeyUp += new KeyEventHandler(okButton_KeyUp);

            this.usernameBox.KeyDown += new KeyEventHandler(okButton_KeyUp);
            this.passwordBox.KeyDown += new KeyEventHandler(okButton_KeyUp);
            this.sessionIDBox.KeyDown += new KeyEventHandler(okButton_KeyUp);

            this.okButton.KeyDown += new KeyEventHandler(okButton_KeyUp);

        }

        private void okButton_Click(object sender, EventArgs e)
        {
            // find the remosetupclient v3 component
            var setupComp = Grasshopper.Instances.ActiveCanvas.Document
                .Objects.FirstOrDefault(x => x is RemoSharp.Distributors.RemoSetupClientV3)
                as RemoSharp.Distributors.RemoSetupClientV3;

            this.username = usernameBox.Text;
            this.password = passwordBox.Text;
            this.sessionID = sessionIDBox.Text;

            setupComp.username = usernameBox.Text;
            setupComp.password = passwordBox.Text;
            setupComp.sessionID = sessionIDBox.Text;

            // if the save checkbox is checked
            if (saveCheck.Checked)
            {
                string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\credentials.json";
                // save the credentials to the file
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.WriteLine(usernameBox.Text);
                    sw.WriteLine(passwordBox.Text);
                    sw.WriteLine(sessionIDBox.Text);
                }
            }

            this.Close();
        }

        // a static method that checks if there is a json file with the credentials
        // in the same directory as the assembly
        // if there is, it reads the credentials from the file and returns them
        // if there isn't, it saves the credentials to a file and returns them
        public static Tuple<string, string, string> GetCredentials()
        {
            // file path

            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\credentials.json";

            // if the file exists
            if (System.IO.File.Exists(path))
            {
                // read the credentials from the file
                string[] lines = System.IO.File.ReadAllLines(path);
                return new Tuple<string, string, string>(lines[0], lines[1], lines[2]);
            }
            // if the file doesn't exist
            else
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    // write the credentials to the file
                    sw.WriteLine("username");
                    sw.WriteLine("password");
                    sw.WriteLine("sessionID");

                }
                // return the credentials
                return new Tuple<string, string, string>("username", "password", "sessionID");                   
            }   
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.passwordBox.PasswordChar = showPassCheck.Checked ? '\0' : '*';
        }

        private void KeyPressUP(object sender, KeyPressEventArgs e)
        {

        }

        private void okButton_KeyUp(object sender, KeyEventArgs e)
        {
            // if key is enter
            if (e.KeyCode == Keys.Enter)
            {
                this.username = usernameBox.Text;
                this.password = passwordBox.Text;
                this.sessionID = sessionIDBox.Text;

                this.Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
