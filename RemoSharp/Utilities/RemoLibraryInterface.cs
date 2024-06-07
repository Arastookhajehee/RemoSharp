using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows.Forms;

namespace RemoSharp.Utilities
{
    // public class to store the database entries.
    // properties are the same as the database columns
    public class Snippet
    {
        public string user { get; set; }    
        public string Purpose { get; set; }
        public string Nicknames { get; set; }
        //public string SnippetData { get; set; }
        public string Timestamp { get; set; }
        public int loadCount { get; set; }

        public Snippet(string user, string purpose, string nicknames, string timestamp, int loadCount)
        {
            this.user = user;
            this.Purpose = purpose;
            this.Nicknames = nicknames;
            this.Timestamp = timestamp;
            this.loadCount = loadCount;
        }
        public Snippet() { }

        public static List<Snippet> GetSnippetsFromDatabase(string connectionString)
        {
            List<Snippet> snippets = new List<Snippet>();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                string selectQuery = "SELECT Purpose, Nicknames, Timestamp, LoadCount FROM Snippets";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Snippet snippet = new Snippet();
                            snippet.user = reader.GetString(0);
                            snippet.Purpose = reader.GetString(1);
                            snippet.Nicknames = reader.GetString(2);
                            snippet.Timestamp = reader.GetString(3);
                            snippet.loadCount = reader.GetInt32(4);
                            snippets.Add(snippet);
                        }
                    }
                }
            }
            return snippets;
        }

    }
    


    public partial class RemoLibraryInterface : Form
    {
        public GH_Document gh_Document { get; set; }
        public Dictionary<string, List<string>> mainPurposeList { get; set; }

        internal Credentials credentials { get; private set; }

        public RemoLibraryInterface()
        {
            InitializeComponent();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            //get the selected purpose from the purposeBox

            var purpose = this.purposeList.SelectedItem.ToString();
            if (string.IsNullOrEmpty(purpose)) return;


            gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
            try
            {
                RemoLibrary.LoadSnippet(purpose, gh_Document);
            }
            catch (Exception)
            {

            }
            gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
            
        }

        private void RemoLibraryInterface_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
            UpdatePurposeList();

            if (this.gh_Document == null) this.gh_Document = Grasshopper.Instances.ActiveCanvas.Document;

            this.gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;

        }

        private void UpdatePurposeList()
        {
            // create the general purpose list

            string fileName = "";
            Credentials credents = null;
            bool fileExists = RemoLibrary.GetDBPath(out fileName, out credents);
            string connectionString = $"Data Source={fileName};Version=3;";

            

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

            this.mainPurposeList = possiblePurposes;
            this.purposeList.Items.Clear();
            this.purposeList.Items.AddRange(possiblePurposes.Keys.ToArray());
        }

        private void ActiveCanvas_DocumentChanged(Grasshopper.GUI.Canvas.GH_Canvas sender, Grasshopper.GUI.Canvas.GH_CanvasDocumentChangedEventArgs e)
        {
            try
            {
                if (this.gh_Document != null) this.gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
                this.gh_Document = e.NewDocument;
                if( this.gh_Document != null) this.gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
            }
            catch (Exception error)
            {
                Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
                MessageBox.Show(error.Message);
            }
        }

        private void Gh_Document_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {

            try
            {
                if (e.ObjectCount > 1)
                {
                    return;
                }

                string fileName = "";
                Credentials credents = null;
                bool fileExists = RemoLibrary.GetDBPath(out fileName, out credents);
                string connectionString = $"Data Source={fileName};Version=3;";

                var selectedObjects = e.Objects;
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


                // update the matching purposes list
                foreach (var item in selectedNicknames)
                {
                    this.latestNicknames.Items.Add(item);
                }


                while (this.latestNicknames.Items.Count > 2)
                {
                    this.latestNicknames.Items.RemoveAt(0);
                }


                var tempList = this.latestNicknames.Items.Cast<string>().ToList();

                selectedNicknames = tempList;


                // ignnore case when comparing nicknames iequalitycomparer
                IEqualityComparer<string> comparer = StringComparer.OrdinalIgnoreCase;

                List<string> matchingPurposes = new List<string>();
                foreach (var kvp in possiblePurposes)
                {
                    if (selectedNicknames.All(n => kvp.Value.Contains(n, comparer)))
                    {
                        matchingPurposes.Add(kvp.Key);
                    }
                }

                // update the purpose list
                // if there are no matching purposes reset the list
                this.purposeList.Items.Clear();
                if (matchingPurposes.Count == 0)
                {
                    this.purposeList.Items.AddRange(this.mainPurposeList.Keys.ToArray());
                }
                else
                {
                    this.purposeList.Items.AddRange(matchingPurposes.ToArray());
                }

            }
            catch (Exception error)
            {

                gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
                MessageBox.Show(error.Message);

            }
        }

        private void inputBox_KeyUp(object sender, KeyEventArgs e)
        {
            // filter the main purpose list with the input text and update the purposeList
            string input = this.inputBox.Text.ToLower();
            if (string.IsNullOrEmpty(input))
            {
                this.purposeList.Items.Clear();
                this.purposeList.Items.AddRange(this.mainPurposeList.Keys.ToArray());
                return;
            }

            // find the keys in the main purpose list that contain the input text and update the purpose list
            // ignore case sensitivity when comparing and use the fuzzySharp library to find the best matches with 70% similarity
            FilterPurposeListBySearchBar(input);

        }

        private void FilterPurposeListBySearchBar(string input)
        {
            var filteredList = this.mainPurposeList.Keys.ToList();
            //var ratios = filteredList.Select(k => FuzzySharp.Fuzz.Ratio(k.ToLower(), input));
            filteredList = filteredList.Where(item => FuzzySharp.Fuzz.PartialRatio(item.ToLower(), input.ToLower()) > 70 || item.ToLower().Contains(input.ToLower())).ToList();

            this.purposeList.Items.Clear();
            this.purposeList.Items.AddRange(filteredList.ToArray());
        }

        private void inputBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (string.IsNullOrEmpty(this.inputBox.Text))
            {
                this.purposeList.Items.Clear();
                this.purposeList.Items.AddRange(this.mainPurposeList.Keys.ToArray());
            }
            else
            {
                FilterPurposeListBySearchBar(this.inputBox.Text);
            }
        }

        private void loginButton_Click(object sender, EventArgs e)
        {
            // open a logindialouge
            // if the login dialouge username password were not empty enable all the buttons
            LoginDialouge loginDialouge = new LoginDialouge();
            loginDialouge.ShowDialog();

            if (!string.IsNullOrEmpty(loginDialouge.credentials.username) && !string.IsNullOrEmpty(loginDialouge.credentials.password))
            {
                this.loadButton.Enabled = true;
                this.saveButton.Enabled = true;
                this.deleteButton.Enabled = true;
                this.purposeList.Enabled = true;
                this.latestNicknames.Enabled = true;

                this.credentials = loginDialouge.credentials;
                this.gh_Document = Grasshopper.Instances.ActiveCanvas.Document;
                this.UpdatePurposeList();

            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            this.TopMost = false;
            gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
            try
            {
                RemoLibrary.SaveSnippet(this.gh_Document, this.credentials);
                this.UpdatePurposeList();
            }
            catch (Exception)
            {
            }
            gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;
            this.TopMost = true;

        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            // get the selected purpose from the purposeList
            // if none is selected return otherwise run the deleteentry method from the remolibrary
            var purpose = this.purposeList.SelectedItem.ToString();
            if (string.IsNullOrEmpty(purpose)) return;

            this.TopMost = false;
            RemoLibrary.DeleteDBEntry(this.gh_Document, purpose);
            this.UpdatePurposeList();
            this.TopMost = true;
        }
    }
}
