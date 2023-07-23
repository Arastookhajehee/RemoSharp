using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grasshopper.Kernel;
using RemoSharp.RemoCommandTypes;
using Rhino.Commands;
using Rhino.Geometry;
using WebSocketSharp;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace RemoSharp
{
    public class SyncAllCanvas : GH_Component
    {
        WebSocket client;
        string username = "";
        /// <summary>
        /// Initializes a new instance of the SyncAllCanvas class.
        /// </summary>
        public SyncAllCanvas()
          : base("SyncAllCanvas", "syncCanvas",
              "syncs the whole canvas except setup components and local ones (component with 'local' nickname)",
              "RemoSharp", "RemoSetup")
        {
            this.NickName = "RemoSetup";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("WSClient", "wsc", "RemoSharp's Command Websocket Client", GH_ParamAccess.item);
            pManager.AddBooleanParameter("syncSend", "sync", "Sync this Grasshopper's content for other connected clients", GH_ParamAccess.item, false);

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
            bool run = false;
            // getting the username information
            DA.GetData(0, ref username);
            DA.GetData(1, ref client);
            DA.GetData(2, ref run);

            if (!run) return;
            string savePath = @"C:\temp\RemoSharp\saveTempFile.ghx";
            string openPath = @"C:\temp\RemoSharp\openTempFile" + username + ".ghx";
            string finalPath = @"C:\temp\RemoSharp\finalTempFile" + username + ".ghx";

            CheckForDirectoryAndFileExistance(savePath);
            CheckForDirectoryAndFileExistance(openPath);
            CheckForDirectoryAndFileExistance(finalPath);

            Grasshopper.Kernel.GH_DocumentIO saveDoc = new GH_DocumentIO(this.OnPingDocument());
            bool saveDocR = saveDoc.SaveQuiet(savePath);

            while (true)
            {
                try
                {
                    System.IO.File.Copy(savePath, openPath, true);
                    break;
                }
                catch { }
            }

            Grasshopper.Kernel.GH_DocumentIO openDoc = new GH_DocumentIO();
            openDoc.Open(openPath);

            for (int i = openDoc.Document.ObjectCount - 1; i > -1 ; i--)
            {
                var obj = openDoc.Document.Objects[i];
                if (obj.NickName.ToUpper().Contains("LOCAL") ||
                    obj.NickName.ToUpper().Contains("RemoSetup".ToUpper()))
                {
                    openDoc.Document.RemoveObject(obj, false);
                }
            }
            openDoc.SaveQuiet(savePath);

            try
            {
                string content = "";
                using (StreamReader sr = new StreamReader(savePath))
                {
                    content = sr.ReadToEnd();

                    RemoCanvasSync remoCanvasSync = new RemoCanvasSync(username, content);
                    string cmdJson = RemoCommand.SerializeToJson(remoCanvasSync);
                    client.Send(cmdJson);
                    sr.Close();
                }
            }
            catch { }
        }

        private void CheckForDirectoryAndFileExistance(string path)
        {
            bool directoryExists = Directory.Exists(Path.GetDirectoryName(path));
            bool fileExists = File.Exists(path);

            if (directoryExists && fileExists) return;

            if (!directoryExists) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!fileExists)
            {
                using (var file = File.Create(path))
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes("First Line");
                    file.Write(byteArray, 0, 0);
                    file.Close();
                }
            }
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
            get { return new Guid("4ACD2C9C-A40D-4E9B-B57C-FECC31AF1561"); }
        }
    }
}