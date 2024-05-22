using GHCustomControls;
using Grasshopper.Kernel;
using RemoSharp.RemoCommandTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Data.SQLite;
using Grasshopper.Kernel.Special;
using System.Threading;
namespace RemoSharp.Utilities
{
    public class SerializeToFile : GHCustomComponent
    {
        string databasePath = "";
        GHCustomControls.PushButton saveButton;
        PushButton setupButton;
        PushButton deleteButton;
        StackPanel stackPanel2;
        PushButton loadButton;
        StackPanel stackPanel;
        public GHCustomControls.Label label;

        /// <summary>
        /// Initializes a new instance of the SerializeToFile class.
        /// </summary>
        public SerializeToFile()
          : base("SerializeToFile", "Nickname",
              "Description",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            setupButton = new PushButton("setup", "setup Description");
            setupButton.OnValueChanged += SetupButton_OnValueChanged;
            deleteButton = new PushButton("delete", "delete Description");
            deleteButton.OnValueChanged += DeleteButton_OnValueChanged;

            stackPanel2 = new StackPanel("stack2", GHCustomControls.Orientation.Horizontal);
            stackPanel2.Add(setupButton);
            stackPanel2.Add(deleteButton);

            label = new GHCustomControls.Label("lable", "label", "username");

            saveButton = new PushButton("save", "save Description");
            saveButton.OnValueChanged += saveButton_OnValueChanged;

            loadButton = new PushButton("load", "load Description");
            loadButton.OnValueChanged += LoadButton_OnValueChanged;
            stackPanel = new StackPanel("stack", GHCustomControls.Orientation.Horizontal);
            stackPanel.Add(saveButton);
            stackPanel.Add(loadButton);


            AddCustomControl(label);
            AddCustomControl(stackPanel2);
            AddCustomControl(stackPanel);
        }

        private void DeleteButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool val = Convert.ToBoolean(e.Value);
            if (!val) return;

            // a timer that waits for 700ms before executing the code in deleteDBEntry
            this.OnPingDocument().ScheduleSolution(700, (doc) => DeleteDBEntry());

        }

        private void DeleteDBEntry()
        {
            if (string.IsNullOrEmpty((string)this.label.CurrentValue) || ((string)this.label.CurrentValue).Equals("username"))
            {
                return;
            }

            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            if (string.IsNullOrEmpty((string)this.label.CurrentValue) || ((string)this.label.CurrentValue).Equals("username")) return;

            try
            {
                // connect to the database
                string fileName = "";
                Credentials credents = null;
                bool fileExists = GetDBPath(out fileName, out credents);
                string connectionString = $"Data Source={fileName};Version=3;";

                // get a list of purposes from the database column
                // if the database does not exist, return
                // if the database exists, get the list of purposes
                // if the list of purposes is empty, return
                // if the list of purposes is not empty, show a window that asks the user to select a purpose
                // if the user cancels the window, return
                // if the user selects a purpose, get the partial doc from the database and deserialize it
                bool dbFileExists = File.Exists(fileName);
                if (!dbFileExists) return;


                var selectedObjects = thisDoc.SelectedObjects().Where(o => !(o is RemoSharp.Utilities.SerializeToFile)).ToList();

                if (selectedObjects.Count == 0)
                {

                    List<string> purposes = new List<string>();
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        // sorted by timestamp
                        string selectQuery = "SELECT Purpose FROM Snippets ORDER BY Timestamp DESC";
                        using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                        {
                            using (SQLiteDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    purposes.Add(reader.GetString(0));
                                }
                            }
                        }
                    }
                    // use rhino's built-in list box
                    if (purposes.Count == 0) return;
                    string purpose = (string)Rhino.UI.Dialogs.ShowListBox("Select a purpose to delete", "Purpose", purposes.ToArray());

                    if (string.IsNullOrEmpty(purpose)) return;

                    // ask the user to confirm the deletion with a message box yes/no
                    DialogResult dialogResult = MessageBox.Show($"Are you sure you want to delete the purpose {purpose}?", "Delete", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.No) return;

                    // remove the selected purpose entry from the database
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();

                        string deleteQuery = "DELETE FROM Snippets WHERE Purpose = @Purpose";
                        using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                        {
                            command.Parameters.AddWithValue("@Purpose", purpose);
                            command.ExecuteNonQuery();
                        }
                    }

                    return;
                }
            }
            catch (Exception error)
            {
                // text box to show the error
                MessageBox.Show(error.Message, "Error");
            }
        }

        private void SetupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool val = Convert.ToBoolean(e.Value);
            if (!val) return;

            // a timer that waits for 700ms before executing the code in SetUpSaveToFile
            this.OnPingDocument().ScheduleSolution(700, (doc) => SetUpSaveToFile());

        }

        private void SetUpSaveToFile()
        {
            string dbPath = "";
            Credentials credents = null;
            bool dbExists = GetDBPath(out dbPath, out credents);


            if (this.label.CurrentValue.Equals("username") || credents.username.Equals("username"))
            {
                LoginDialouge loginDialouge = new LoginDialouge();
                loginDialouge.ShowDialog();

                if (credents.username.Equals("username")) return;

            }
            // Snipates.sqlite is the database that will store the partial docs. The columns are: username, purpose, tags, partialDoc
            // username is a string, purpose is a string, tags is a string, partialDoc is a string
            // if the file does not exist, create it and create the table Snipates
            // the purpose is the primary column
            // use the Microsoft.Data.Sqlite library

            try
            {


                credents = new Credentials();
                credents.dbPath = dbPath;

                // create the database
                string connectionString = $"Data Source={credents.dbPath};Version=3;";

                // Create the SQLite database file
                bool dbFileExists = File.Exists(credents.dbPath);
                if (!dbFileExists) SQLiteConnection.CreateFile(credents.dbPath);

                // Connect to the database
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Create a table in the database called Snippets, with the columns: Purpose, Nicknames, PartialDoc, Username
                    // Purpose is the primary key
                    // if the table does not exist, create it

                    string createTableQuery = "CREATE TABLE IF NOT EXISTS Snippets (Purpose TEXT PRIMARY KEY, Nicknames TEXT, PartialDoc TEXT, Username TEXT, Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP)";

                    using (SQLiteCommand command = new SQLiteCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }

                credents.WriteToFile();
            }
            catch (Exception error)
            {
                // text box to show the error
                MessageBox.Show(error.Message, "Error");
            }

        }

        private static bool GetDBPath(out string path, out Credentials credents)
        {
            Credentials credentials = Credentials.ReadFromFile();

            bool dbPathIsDefault = credentials.dbPath.Equals("dbPath");
            bool missingDB = !File.Exists(credentials.dbPath);

            if (dbPathIsDefault || missingDB)
            {
                // open a file path dialouge to select a folder to save the database
                FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                folderBrowserDialog.Description = "Select the folder to save the database";
                folderBrowserDialog.ShowDialog();

                // get the path of the database and join it with snipates.sqlite
                path = Path.Combine(folderBrowserDialog.SelectedPath, "RemoSnippets.rmdb");
                credentials.dbPath = path;
                credents = credentials;

                bool fileExists = File.Exists(credents.dbPath);

                return fileExists;
            }
            else
            {
                path = credentials.dbPath;
                credents = credentials;
                bool fileExists = File.Exists(credents.dbPath);
                return fileExists;
            }
        }

        private void LoadButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            var val = Convert.ToBoolean(e.Value);
            if (!val) return;

            // create a timer that waits for 700ms before executing the code in load snippet
            this.OnPingDocument().ScheduleSolution(700, (doc) => LoadSnippet());
            
        }

        private void LoadSnippet()
        {
            //if (string.IsNullOrEmpty((string)this.label.CurrentValue) || ((string)this.label.CurrentValue).Equals("username"))
            //{
            //    return;
            //}


            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            //if (string.IsNullOrEmpty((string)this.label.CurrentValue) || ((string)this.label.CurrentValue).Equals("username")) return;

            try
            {


                // connect to the database
                string fileName = "";
                Credentials credents = null;
                bool fileExists = GetDBPath(out fileName, out credents);
                string connectionString = $"Data Source={fileName};Version=3;";

                // get a list of purposes from the database column
                // if the database does not exist, return
                // if the database exists, get the list of purposes
                // if the list of purposes is empty, return
                // if the list of purposes is not empty, show a window that asks the user to select a purpose
                // if the user cancels the window, return
                // if the user selects a purpose, get the partial doc from the database and deserialize it
                bool dbFileExists = File.Exists(fileName);
                if (!dbFileExists) return;


                var selectedObjects = thisDoc.SelectedObjects().Where(o => !(o is RemoSharp.Utilities.SerializeToFile)).ToList();

                if (selectedObjects.Count == 0)
                {

                    MergeSnippetByPurposeSelection(thisDoc, connectionString);
                    return;
                }

                Dictionary<string, List<string>> possiblePurposes = new Dictionary<string, List<string>>();
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();
                    // sorted by timestamp
                    string selectQuery = "SELECT Purpose, Nicknames FROM Snippets ORDER BY Timestamp DESC";
                    using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string purpose = reader.GetString(0);
                                List<string> tags = reader.GetString(1).Split(',').ToList();
                                possiblePurposes.Add(purpose, tags);
                            }
                        }
                    }
                }

                if (possiblePurposes.Count == 0) return;
                // find the purpose keys in the dictionary where the nicknames of the selected objects are a subset of the nicknames of the purpose
                List<string> selectedNicknames = selectedObjects.Where(o => !(o is GH_Group)).Select(o => o.NickName).ToList();
                List<string> matchingPurposes = new List<string>();
                foreach (var kvp in possiblePurposes)
                {
                    if (selectedNicknames.All(n => kvp.Value.Contains(n)))
                    {
                        matchingPurposes.Add(kvp.Key);
                    }
                }

                if (matchingPurposes.Count == 0)
                {
                    MessageBox.Show("No matching purpose found for the selected component", "No Match");
                    return;
                }
                string purposeOptions = (string)Rhino.UI.Dialogs.ShowListBox("Select a purpose", "Purpose", matchingPurposes.ToArray());
                if (string.IsNullOrEmpty(purposeOptions)) return;
                // get the partial doc from the database
                RemoPartialDoc remoPartialDoc = null;
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string selectQuery = "SELECT PartialDoc FROM Snippets WHERE Purpose = @Purpose";
                    using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Purpose", purposeOptions);
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string partialDoc = reader.GetString(0);
                                remoPartialDoc = (RemoPartialDoc)RemoCommand.DeserializeFromJson(partialDoc);
                            }
                        }
                    }
                }

                RemoSharp.CommandExecutor.ExecuteRemoPartialDoc(thisDoc, remoPartialDoc, true);

                this.loadButton.CurrentValue = false;
                this.saveButton.CurrentValue = false;
                this.setupButton.CurrentValue = false;

            }
            catch (Exception error)
            {
                // text box to show the error
                MessageBox.Show(error.Message, "Error");
            }
        }

        private void MergeSnippetByPurposeSelection(GH_Document thisDoc, string connectionString)
        {

            List<string> purposes = new List<string>();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // sorted by timestamp
                string selectQuery = "SELECT Purpose FROM Snippets ORDER BY Timestamp DESC";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            purposes.Add(reader.GetString(0));
                        }
                    }
                }
            }
            // use rhino's built-in list box
            if (purposes.Count == 0) return;
            string purpose = (string)Rhino.UI.Dialogs.ShowListBox("Select a purpose", "Purpose", purposes.ToArray());

            if (string.IsNullOrEmpty(purpose)) return;

            // get the partial doc from the database
            RemoPartialDoc remoPartialDoc = null;
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT PartialDoc FROM Snippets WHERE Purpose = @Purpose";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Purpose", purpose);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string partialDoc = reader.GetString(0);
                            remoPartialDoc = (RemoPartialDoc)RemoCommand.DeserializeFromJson(partialDoc);
                        }
                    }
                }
            }

            RemoSharp.CommandExecutor.ExecuteRemoPartialDoc(thisDoc, remoPartialDoc, true);
        }

        // a function that opens a window to get the 9 keywords from the user using the windows built-in dialog
        private List<string> GetTagsFromUser()
        {

            return null;
        }


        private void saveButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            var val = Convert.ToBoolean(e.Value);
            if (!val) return;

            // create a timer that waits for 700ms before executing the code in save snippet
            OnPingDocument().ScheduleSolution(700, (doc) => SaveSnippet()); 
        }

        private void SaveSnippet()
        {
            //var thisDoc = this.OnPingDocument();
            //if (thisDoc == null) return;

            //if (string.IsNullOrEmpty((string)this.label.CurrentValue) || ((string)this.label.CurrentValue).Equals("username"))
            //{
            //    return;
            //}

            var thisDoc = this.OnPingDocument();
            var selection = this.OnPingDocument().SelectedObjects();
            if (selection.Count == 0) return;
            RemoPartialDoc partialDoc = new RemoPartialDoc((string)this.label.CurrentValue, "", selection, thisDoc, true);

            string serialized = RemoCommand.SerializeToJson(partialDoc);

            string[] tags = selection.Where(o => !(o is GH_Group)).Select(o => o.NickName).ToArray();
            string tagCSV = string.Join(",", tags);


            //sort points by coordinates
            // create a dialouge to ask for the purpose of the partial doc use visual basic inputbox
            // if the user cancels the input box, return
            // if the returned text is empty, return
            // if the returned text is not empty, save the partial doc with the purpose as the primary key

            string purpose = Microsoft.VisualBasic.Interaction.InputBox("Please Enter the purpose of the Snippet", "Snippet Purpose");
            if (string.IsNullOrEmpty(purpose)) return;

            // save the partial doc to the file
            // connect to the database
            string fileName = "";
            Credentials credents = null;
            bool fileExists = GetDBPath(out fileName, out credents);
            string connectionString = $"Data Source={fileName};Version=3;";
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // insert the partial doc into the database
                string insertQuery = "INSERT INTO Snippets (Purpose, Nicknames, PartialDoc, Username) VALUES (@Purpose, @Nicknames, @PartialDoc, @Username)";

                // if the same purpose exists, get the user to confirm if they want to overwrite the existing partial doc
                // get the existing nicknames. If the existing nicknames are different from the new nicknames, get the user to confirm if they want to overwrite the existing partial doc
                // ask the user with a dialog box if they want to overwrite the existing partial doc
                // the message of the dialogue should include existing nicknames and new nicknames
                bool purposeExists = false;
                string existingNicknames = "";
                // just get the purpose and the nicknames
                string selectQuery = "SELECT Purpose, Nicknames FROM Snippets WHERE Purpose = @Purpose";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@Purpose", purpose);
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            purposeExists = true;
                            existingNicknames = reader.GetString(1);
                        }
                    }
                }

                if (purposeExists)
                {
                    DialogResult dialogResult = MessageBox.Show
                        ($"The purpose {purpose} already exists with the nicknames:\n{existingNicknames}.\n\n" +
                        $"Current nicknames:\n{tagCSV}\n\nDo you want to overwrite it?", "Overwrite", MessageBoxButtons.YesNo);
                    if (dialogResult == DialogResult.No) return;

                    // delete the existing partial doc
                    string deleteQuery = "DELETE FROM Snippets WHERE Purpose = @Purpose";
                    using (SQLiteCommand command = new SQLiteCommand(deleteQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Purpose", purpose);
                        command.ExecuteNonQuery();
                    }
                    // add the new entry
                    string addQuery = "INSERT INTO Snippets (Purpose, Nicknames, PartialDoc, Username) VALUES (@Purpose, @Nicknames, @PartialDoc, @Username)";
                    using (SQLiteCommand command = new SQLiteCommand(addQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Purpose", purpose);
                        command.Parameters.AddWithValue("@Nicknames", tagCSV);
                        command.Parameters.AddWithValue("@PartialDoc", serialized);
                        command.Parameters.AddWithValue("@Username", (string)this.label.CurrentValue);
                        command.ExecuteNonQuery();
                    }
                    return;
                }
                else
                {
                    // add the new entry
                    using (SQLiteCommand command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Purpose", purpose);
                        command.Parameters.AddWithValue("@Nicknames", tagCSV);
                        command.Parameters.AddWithValue("@PartialDoc", serialized);
                        command.Parameters.AddWithValue("@Username", (string)this.label.CurrentValue);
                        command.ExecuteNonQuery();
                    }
                }






            }
            this.loadButton.CurrentValue = false;
            this.saveButton.CurrentValue = false;
            this.setupButton.CurrentValue = false;
        }



        private void TabPanel_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            var va = e.Value;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("AD3A5883-0EE0-49E3-A2CF-7A97C880F58C"); }
        }
    }
}