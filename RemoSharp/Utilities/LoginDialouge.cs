using Grasshopper.Kernel.Special;
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
        public Credentials credentials { get; private set; }

        public LoginDialouge()
        {
            InitializeComponent();

           // get the credentials from the file
            Credentials credentials = Credentials.ReadFromFile();

            // set the textboxes to the credentials
            usernameBox.Text = credentials.username;
            passwordBox.Text = credentials.password;
            sessionIDBox.Text = credentials.sessionID;


            this.KeyUp += new KeyEventHandler(okButton_KeyUp);

            this.usernameBox.KeyDown += new KeyEventHandler(okButton_KeyUp);
            this.passwordBox.KeyDown += new KeyEventHandler(okButton_KeyUp);
            this.sessionIDBox.KeyDown += new KeyEventHandler(okButton_KeyUp);

            this.okButton.KeyDown += new KeyEventHandler(okButton_KeyUp);

        }

        private void okButton_Click(object sender, EventArgs e)
        {
            SetupCredentials();
            this.Close();
        }

        private void SetupCredentials()
        {
            // find the remosetupclient v3 component
            var ghDoc = Grasshopper.Instances.ActiveCanvas.Document;
            var setupComp = ghDoc.Objects.FirstOrDefault(x => x is RemoSharp.Distributors.RemoSetupClientV3)
                as RemoSharp.Distributors.RemoSetupClientV3;
            var serializeToFile = ghDoc.Objects.FirstOrDefault(x => x is RemoSharp.Utilities.SerializeToFile)
                as RemoSharp.Utilities.SerializeToFile;

            Credentials credentials = new Credentials(usernameBox.Text, passwordBox.Text, sessionIDBox.Text, "dbPath");

            
            if (setupComp != null)
            {
                setupComp.username = credentials.username;
                setupComp.password = credentials.password;
                setupComp.sessionID = credentials.sessionID;

                setupComp.usernameLabel.CurrentValue = setupComp.username + "\n" + setupComp.sessionID;
                setupComp.Attributes.ExpireLayout();
            }
            if (serializeToFile != null)
            {
                serializeToFile.label.CurrentValue = credentials.username;
                serializeToFile.Attributes.ExpireLayout();
            }
            

            this.credentials = credentials;
            //ghDoc.ScheduleSolution(1, doc =>
            //{
            //    GH_Panel panel = setupComp.Params.Input[1].Sources[0] as GH_Panel;
            //    panel.SetUserText(setupComp.sessionID);
            //});

            // if the save checkbox is checked
            if (saveCheck.Checked)
            {
                credentials.WriteCredentialsToFile();
            }
        }

        // a static method that checks if there is a json file with the credentials
        // in the same directory as the assembly
        // if there is, it reads the credentials from the file and returns them
        // if there isn't, it saves the credentials to a file and returns them
        

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
                SetupCredentials();
                this.Close();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }

    public class Credentials
    {
        public string username { get; set; }
        public string password { get; set; }
        public string sessionID { get; set; }

        public string dbPath { get; set; }

        public Credentials()
        {
            this.username = "username";
            this.password = "password";
            this.sessionID = "sessionID";
            this.dbPath = "dbPath";
        }
        public Credentials(string username, string password, string sessionID, string dbPath)
        {
            this.username = username;
            this.password = password;
            this.sessionID = sessionID;
            this.dbPath = dbPath;
        }

        public string WriteCredentialsToFile()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\credentials.json";

            // if the file exists, read the dbPath from the file assign it to the dbPath property
            if (System.IO.File.Exists(path))
            {
                Credentials creds = null;
                using (StreamReader sr = new StreamReader(path))
                {
                    creds = DeserializeFromJson(sr.ReadToEnd());
                    if (creds.dbPath == "dbPath") creds.dbPath = this.dbPath;
                    else if (this.dbPath != "dbPath")
                    {
                        creds.dbPath = this.dbPath;
                    }
                }
                using (StreamWriter sw = new StreamWriter(path))
                {
                    creds.username = this.username;
                    creds.password = this.password;
                    creds.sessionID = this.sessionID;


                    sw.Write(creds.SerializeToJson());
                }
            }
            else // if the file doesn't exist, write the dbPath to the file
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(new Credentials().SerializeToJson());
                }
            }


            return path;
        }

        public string WriteToFile()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\credentials.json";
            
            // if the file exists, read the dbPath from the file assign it to the dbPath property
            if (System.IO.File.Exists(path))
            {
                Credentials creds = null;
                using (StreamReader sr = new StreamReader(path))
                {
                    creds = DeserializeFromJson(sr.ReadToEnd());
                    if (creds.dbPath == "dbPath") creds.dbPath = this.dbPath;
                    else if (this.dbPath != "dbPath")
                    {
                        creds.dbPath = this.dbPath;
                    }
                }
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(creds.SerializeToJson());
                }
            }
            else // if the file doesn't exist, write the dbPath to the file
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write(this.SerializeToJson());
                }
            }


            return path;
        }

        public string SerializeToJson()
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(this);
        }

        public static Credentials DeserializeFromJson(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Credentials>(json);
        }

        public static Credentials ReadFromFile()
        {
            string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\credentials.json";
            if (System.IO.File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    return DeserializeFromJson(sr.ReadToEnd());
                }
            }
            else
            {
                var defaultCredentials = new Credentials();
                defaultCredentials.WriteToFile();
                return defaultCredentials;
            }
        }
    }
}
