using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RemoSharp.Utilities
{
    public partial class RemoLibraryInterface : Form
    {
        public GH_Document gh_Document { get; set; }

        public Dictionary<string, List<string>> mainPurposeList { get; set; }
        public List<RemoLibrary.RemoLibraryEntry> mainEntries { get; set; }

        internal Credentials credentials { get; private set; }

        public RemoLibraryInterface()
        {
            InitializeComponent();
        }

        private void loadButton_Click(object sender, EventArgs e)
        {
            //get the selected purpose from the purposeBox
            var selectedRow = this.dataGrid.SelectedCells;
            var purpose = this.dataGrid.Rows[selectedRow[0].RowIndex].Cells[0].Value.ToString();
            if (purpose == null) return;


            gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
            try
            {
                int loadCount = -1;
                RemoLibrary.LoadSnippet(purpose.ToString(), gh_Document, out loadCount);
                this.mainEntries.Where(mainEntries => mainEntries.purpose == purpose).First().loadCount = loadCount;
                this.dataGrid.Rows[selectedRow[0].RowIndex].Cells[3].Value = loadCount;
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
            this.mainEntries = new List<RemoLibrary.RemoLibraryEntry>();
            this.dataGrid.MultiSelect = false;
            //UpdatePurposeList();

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



            //Dictionary<string, List<string>> possiblePurposes = new Dictionary<string, List<string>>();
            List<RemoLibrary.RemoLibraryEntry> entries = new List<RemoLibrary.RemoLibraryEntry>();
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                // sorted by timestamp
                string selectQuery = "SELECT Purpose, Nicknames, LoadCount, Username, Timestamp FROM Snippets ORDER BY Timestamp DESC";
                using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string purpose = reader.GetString(0);
                            string tags = reader.GetString(1);
                            int loadCount = reader.GetInt32(2);
                            string username = reader.GetString(3);
                            DateTime timestamp = reader.GetDateTime(4);

                            RemoLibrary.RemoLibraryEntry entry = new RemoLibrary.RemoLibraryEntry(purpose, tags, loadCount, username, timestamp);
                            entries.Add(entry);
                            //possiblePurposes.Add(purpose, tags);
                        }
                    }
                }
            }


            this.dataGrid.CellDoubleClick -= DataGrid_DoubleClick;

            UpdateDataGridView(entries);
            this.mainEntries.Clear();
            this.mainEntries = entries;
            // get the file name from the path fileName and set the databaseTitleChangable text to the file name
            string newTitle = Path.GetFileName(fileName);
            // add the file name to the title of the form after a :
            // if the file already has a : then replace the text after the : with the new title
            if (this.Text.Contains(":"))
            {
                this.Text = this.Text.Substring(0, this.Text.IndexOf(":") + 1) + newTitle;
            }
            else
            {
                this.Text += ": " + newTitle;
            }

            //this.dataGrid.CellContentDoubleClick += DataGrid_DoubleClick;
            this.dataGrid.CellDoubleClick += DataGrid_DoubleClick;

        }

        private void UpdateDataGridView(List<RemoLibrary.RemoLibraryEntry> entries)
        {
            // remove all the rows and columns from the datagridview
            this.dataGrid.Rows.Clear();
            this.dataGrid.Columns.Clear();

            // add the columns to the datagridview
            this.dataGrid.Columns.Add("Purpose", "Purpose");
            this.dataGrid.Columns.Add("Timestamp", "Timestamp");
            this.dataGrid.Columns.Add("Username", "Username");
            this.dataGrid.Columns.Add("LoadCount", "LoadCount");

            // add the entries to the datagridview
            foreach (var entry in entries)
            {
                this.dataGrid.Rows.Add(entry.purpose, entry.timestamp, entry.username, entry.loadCount);
            }
        }

        private void DataGrid_DoubleClick(object sender, EventArgs e)
        {

            // get the selected purpose from the gridview
            // if none is selected return otherwise run the loadentry method from the remolibrary
            var selectedRow = this.dataGrid.SelectedCells;

            var purpose = this.dataGrid.Rows[selectedRow[0].RowIndex].Cells[0].Value.ToString();


            var pause = "";


            gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
            try
            {
                int loadCount = -1;
                RemoLibrary.LoadSnippet(purpose.ToString(), gh_Document, out loadCount);
                this.mainEntries.Where(mainEntries => mainEntries.purpose == purpose).First().loadCount = loadCount;
                this.dataGrid.Rows[selectedRow[0].RowIndex].Cells[3].Value = loadCount;
            }
            catch (Exception)
            {

            }
            gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged += ActiveCanvas_DocumentChanged;


        }

        private void ActiveCanvas_DocumentChanged(Grasshopper.GUI.Canvas.GH_Canvas sender, Grasshopper.GUI.Canvas.GH_CanvasDocumentChangedEventArgs e)
        {
            try
            {
                if (this.gh_Document != null) this.gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
                this.gh_Document = e.NewDocument;
                if (this.gh_Document != null) this.gh_Document.ObjectsAdded += Gh_Document_ObjectsAdded;
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


                // check the the interface is open
                var dialouge = this;
                if (dialouge == null) return;
                // check if the dialouge is closed
                bool isDisposed = dialouge.IsDisposed;
                if (isDisposed)
                {
                    this.gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
                    this.gh_Document = null;
                    this.Close();
                    this.Dispose();
                    return;
                }


                //bool fileExists = RemoLibrary.GetDBPath(out fileName, out credents);

                //string pause = "";


                //string connectionString = $"Data Source={fileName};Version=3;";

                var madeObjects = e.Objects;
                //Dictionary<string, List<string>> possiblePurposes = new Dictionary<string, List<string>>();
                //using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                //{
                //    connection.Open();
                //    // sorted by timestamp
                //    string selectQuery = "SELECT Purpose, Nicknames FROM Snippets ORDER BY Timestamp DESC";
                //    using (SQLiteCommand command = new SQLiteCommand(selectQuery, connection))
                //    {
                //        using (SQLiteDataReader reader = command.ExecuteReader())
                //        {
                //            while (reader.Read())
                //            {
                //                string purpose = reader.GetString(0);
                //                List<string> tags = reader.GetString(1).Split(',').ToList();
                //                possiblePurposes.Add(purpose, tags);
                //            }
                //        }
                //    }
                //}

                //if (possiblePurposes.Count == 0) return;
                // find the purpose keys in the dictionary where the nicknames of the selected objects are a subset of the nicknames of the purpose



                List<string> selectedNicknames = madeObjects.Where(o => !(o is GH_Group)).Select(o => o.NickName).ToList();




                List<RemoLibrary.RemoLibraryEntry> filtered = new List<RemoLibrary.RemoLibraryEntry>();


                if (selectedNicknames.Count == 0) return;

                this.latestNicknames.Items.Add(selectedNicknames[0]);
                if (this.latestNicknames.Items.Count > 3)
                {
                    this.latestNicknames.Items.RemoveAt(0);
                }

                foreach (var item in this.mainEntries)
                {
                    bool hasTheUsername = false;
                    foreach (var selItem in selectedNicknames)
                    {
                        if (item.nicknames.Contains(selItem))
                        {
                            hasTheUsername = true;
                            break;
                        }
                    }
                    if (hasTheUsername)
                    {
                        filtered.Add(item);
                    }
                }


                this.dataGrid.Rows.Clear();
                if (filtered.Count == 0)
                {
                    foreach (var item in this.mainEntries)
                    {
                        this.dataGrid.Rows.Add(item.purpose, item.timestamp, item.username, item.loadCount);
                    }
                }
                else
                {
                    foreach (var item in filtered)
                    {
                        this.dataGrid.Rows.Add(item.purpose, item.timestamp, item.username, item.loadCount);
                    }
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
                this.dataGrid.Rows.Clear();
                foreach (var item in this.mainEntries)
                {
                    this.dataGrid.Rows.Add(item.purpose, item.timestamp, item.username, item.loadCount);
                }
                return;
            }

            // find the keys in the main purpose list that contain the input text and update the purpose list
            // ignore case sensitivity when comparing and use the fuzzySharp library to find the best matches with 70% similarity
            FilterPurposeListBySearchBar(input);

        }

        private void FilterPurposeListBySearchBar(string input)
        {
            var filteredList = this.mainEntries;
            //var ratios = filteredList.Select(k => FuzzySharp.Fuzz.Ratio(k.ToLower(), input));
            filteredList = filteredList.Where(item => FuzzySharp.Fuzz.PartialRatio(item.purpose.ToLower(), input.ToLower()) > 70 || item.purpose.ToLower().Contains(input.ToLower())).ToList();

            this.dataGrid.Rows.Clear();
            foreach (var item in filteredList)
            {
                this.dataGrid.Rows.Add(item.purpose, item.timestamp, item.username, item.loadCount);
            }
        }

        private void inputBox_MouseUp(object sender, MouseEventArgs e)
        {
            // filter the main purpose list with the input text and update the purposeList
            string input = this.inputBox.Text.ToLower();
            if (string.IsNullOrEmpty(input))
            {
                this.dataGrid.Rows.Clear();
                foreach (var item in this.mainEntries)
                {
                    this.dataGrid.Rows.Add(item.purpose, item.timestamp, item.username, item.loadCount);
                }
                return;
            }

            // find the keys in the main purpose list that contain the input text and update the purpose list
            // ignore case sensitivity when comparing and use the fuzzySharp library to find the best matches with 70% similarity
            FilterPurposeListBySearchBar(input);
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
                this.latestNicknames.Enabled = true;
                this.inputBox.Enabled = true;
                this.LoadRMDB.Enabled = true;
                this.SaveRMDB.Enabled = true;
                this.dataGrid.Enabled = true;

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
            // get the selected from the gridview
            // if none is selected return otherwise run the deleteentry method from the remolibrary
            var purpose = this.dataGrid.SelectedCells[0].Value.ToString();
            if (string.IsNullOrEmpty(purpose)) return;

            this.TopMost = false;
            RemoLibrary.DeleteDBEntry(this.gh_Document, purpose);
            this.UpdatePurposeList();
            this.TopMost = true;
        }

        private void RemoLibraryInterface_FormClosed(object sender, FormClosedEventArgs e)
        {
            // unsubscribe from the events
            gh_Document.ObjectsAdded -= Gh_Document_ObjectsAdded;
            Grasshopper.Instances.ActiveCanvas.DocumentChanged -= ActiveCanvas_DocumentChanged;
        }

        private void LoadRMDB_Click(object sender, EventArgs e)
        {

            //open a load dialouge to select an existing .rmdb file
            // after loading the file update the purpose list
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "RemoDB files (*.rmdb)|*.rmdb";
            openFileDialog.Title = "Select a RemoDB file";
            // only allow one file
            openFileDialog.Multiselect = false;
            //openFileDialog.ShowDialog();

            bool dialougeResultOK = openFileDialog.ShowDialog() == DialogResult.OK;

            if (!dialougeResultOK) return;

            string filePath = openFileDialog.FileName;

            if (!File.Exists(filePath)) RemoLibrary.CreateDataBase(filePath);

            Credentials credentials = Credentials.ReadFromFile();

            credentials.dbPath = filePath;
            credentials.WriteToFile();

            // update the purpose list
            this.UpdatePurposeList();


        }

        private void SaveRMDB_Click(object sender, EventArgs e)
        {

            //open a load dialouge to select an existing .rmdb file
            // after loading the file update the purpose list
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "RemoDB files (*.rmdb)|*.rmdb";
            saveFileDialog.Title = "Select a RemoDB file";
            saveFileDialog.FileName = "RemoLibrary.rmdb";
            //saveFileDialog.ShowDialog();

            bool dialougeResultOK = saveFileDialog.ShowDialog() == DialogResult.OK;

            if (!dialougeResultOK) return;

            string filePath = saveFileDialog.FileName;

            if (!File.Exists(filePath)) RemoLibrary.CreateDataBase(filePath);

            Credentials credentials = Credentials.ReadFromFile();

            credentials.dbPath = filePath;
            credentials.WriteToFile();

            // update the purpose list
            this.UpdatePurposeList();

        }

        private void inputBox_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
