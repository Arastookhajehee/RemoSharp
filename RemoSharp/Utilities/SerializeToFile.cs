using GHCustomControls;
using Grasshopper.Kernel;
using RemoSharp.RemoCommandTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RemoSharp.Utilities
{
    public class SerializeToFile : GHCustomComponent
    {
        GHCustomControls.TabPanel tabPanel;
        GHCustomControls.PushButton pushButton;
        PushButton loadButton;
        GHCustomControls.IGHPanel panel;

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
            tabPanel = new TabPanel("tabPanel", "tabPanel Description",0,true);
            tabPanel.OnValueChanged += TabPanel_OnValueChanged;
            pushButton = new PushButton("save", "save Description");
            pushButton.OnValueChanged += saveButton_OnValueChanged;

            loadButton = new PushButton("load", "load Description");
            loadButton.OnValueChanged += LoadButton_OnValueChanged;


            AddCustomControl(tabPanel);
            AddCustomControl(pushButton);
            AddCustomControl(loadButton);
        }

        private void LoadButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            var val = Convert.ToBoolean(e.Value);
            if (!val) return;

            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
            var remoSetupComp = thisDoc.Objects.FirstOrDefault(x => x is RemoSharp.Distributors.RemoSetupClientV3)
                as RemoSharp.Distributors.RemoSetupClientV3;
            if (remoSetupComp == null) return;
            if (string.IsNullOrEmpty(remoSetupComp.username) || remoSetupComp.username.Equals("username")) return;

            // see if the file saved partialDocs.json exists in the current directory of the assembly file
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(path, "partialDocs.json");
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
                return;
            }

            List<ComplexGeometeySerilization> complexGeometeySerilizations = new List<ComplexGeometeySerilization>();
            using (StreamReader r = new StreamReader(filePath))
            {
                string json = r.ReadToEnd();
                if (string.IsNullOrEmpty(json)) return;
                complexGeometeySerilizations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ComplexGeometeySerilization>>(json);
            }

            // show a window that asks the user to enter 9 keywords
            // the result of the window filled by the user will be the extra tags 
            


            List<string> tags = complexGeometeySerilizations.Select(o => o.tags[0]).ToList();

            // create a rhino pop up window to select the partial doc to load
            var selectionObj = Rhino.UI.Dialogs.ShowListBox("Select the partial doc to load", "Select the partial doc to load", tags, 0);
           

            // get the coorespnoding json string from the selectoinObj
            string jsonString = tags.Contains(selectionObj) ? complexGeometeySerilizations[tags.IndexOf((string)selectionObj)].geoms[0] : "";


            if (string.IsNullOrEmpty(jsonString)) return;

            RemoPartialDoc remoPartialDoc = (RemoPartialDoc) RemoCommand.DeserializeFromJson(jsonString);

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

            if (val)
            {
                var thisDoc = this.OnPingDocument();
                if (thisDoc == null) return;
                var remoSetupComp = thisDoc.Objects.FirstOrDefault(x => x is RemoSharp.Distributors.RemoSetupClientV3)
                    as RemoSharp.Distributors.RemoSetupClientV3;
                if (remoSetupComp == null) 
                {
                    MessageBox.Show("RemoSetupClientV3 component not found in the current document", "Setup Error");
                    return;
                }
                if (string.IsNullOrEmpty(remoSetupComp.username) || remoSetupComp.username.Equals("username")) 
                {
                    MessageBox.Show("Username/Password not set correctly", "Setup Error");
                    return;
                }

            

                // see if the file saved partialDocs.json exists in the current directory of the assembly file
                string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string filePath = Path.Combine(path, "partialDocs.json");

                bool fileExists = File.Exists(filePath);
                if (!fileExists)
                {
                    File.Create(filePath).Close();
                }

                List<ComplexGeometeySerilization> complexGeometeySerilizations = new List<ComplexGeometeySerilization>();
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string json = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(json))
                    {
                        complexGeometeySerilizations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ComplexGeometeySerilization>>(json);
                    }
                }


                var selection = this.OnPingDocument().SelectedObjects();
                if (selection.Count == 0) return;   
                RemoPartialDoc partialDoc = new RemoPartialDoc(remoSetupComp.username, "", selection, thisDoc, true);

                string serialized = RemoCommand.SerializeToJson(partialDoc);

                string[] tags = selection.Select(o => o.NickName).ToArray();
                string tagCSV = string.Join(",", tags);
                // a random color for the partial doc seralization
                Random random = new Random();
                int r = random.Next(0, 255);
                int g = random.Next(0, 255);
                int b = random.Next(0, 255);
                System.Drawing.Color color = System.Drawing.Color.FromArgb(r, g, b);
                
                List<string> jsonList = new List<string>() { serialized };
                List<string> tagList = new List<string>() { tagCSV };
                List<System.Drawing.Color> colorList = new List<System.Drawing.Color>() { color };

                complexGeometeySerilizations.Add(new ComplexGeometeySerilization(jsonList, tagList, colorList));

                using (StreamWriter w = new StreamWriter(filePath))
                {
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(complexGeometeySerilizations);
                    w.Write(json);
                }

            }

        }

        private void TabPanel_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            var va  = e.Value;
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