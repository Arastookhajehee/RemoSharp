using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.RemoCommandTypes
{
    public class GHL : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Log class.
        /// </summary>
        public GHL()
          : base("Grasshopper Logger", "GH Log",
              "A Tool To Log Interactions with Grasshopper. Developed for as a tool for research in Grasshopper",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("ID", "ID", "A Single ID number to be saved into the Log file", GH_ParamAccess.item, "1");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("Run", "R", "GHL", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        int running = 0;
        int currentListLength = 0;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string id = "1";
            DA.GetData(0, ref id);
            if (running == 0) currentPos = Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint;
            if (!IDAdded)
            {
                viewChangeRecord.Add(id);
                IDAdded = true;
            }

            ConnectToRemoSharpTrigger();
            System.Drawing.PointF nextPos = Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint;
            var thisInstance = System.DateTime.Now.ToShortDateString() + "," + System.DateTime.Now.ToShortTimeString() + ":" + System.DateTime.Now.Second;

            bool enoughTime = EnoughTimePassed(prevTime, thisInstance);
            bool viewChange = LargeEnoughMovement(enoughTime, currentPos, nextPos);

            if (viewChange)
            {
                if (running == 0) viewChangeRecord.Add("View Change");

                viewChangeRecord.Add("View Change" + "," + thisInstance);
                double currentZoom = (double)Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
                if (currentZoom > prevZoom)
                {
                    prevZoom = currentZoom;
                    viewChangeRecord.Add("Zoom in" + "," + thisInstance);
                }
                else if (currentZoom < prevZoom)
                {
                    prevZoom = currentZoom;
                    viewChangeRecord.Add("Zoom out" + "," + thisInstance);
                }

                currentPos = nextPos;
                prevTime = thisInstance;
            }
            var nowObj = System.DateTime.Now;
            var now = nowObj.Year + "-" + nowObj.Month + "-" + nowObj.Day;
            //    if (!SaveLog) return;

            // https://gurukultree.com/articles/20/how-to-get-a-path-to-the-desktop-for-current-user-in-cshrp/
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var undoLog = this.OnPingDocument().UndoServer.UndoNames;



            if (undoLog.Count != currentListLength || viewChange)
            {
                if (undoLog.Count > currentListLength)
                {
                    undoLogTimeList.Add(undoLog[0] + "," + thisInstance);
                }
                currentListLength = undoLog.Count;
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(filePath, "GHL " + now + ".rmsp")))
                {

                    foreach (string line in undoLogTimeList) outputFile.WriteLine(line);
                    foreach (string line in viewChangeRecord) outputFile.WriteLine(line);
                    viewChange = false;
                }
            }

            running++;
        }

        bool IDAdded = false;

        System.Drawing.PointF currentPos = new System.Drawing.PointF(0, 0);
        double prevZoom = 1;
        List<string> viewChangeRecord = new List<string>();
        List<string> undoLogTimeList = new List<string>();
        string prevTime = System.DateTime.Now.ToShortDateString() + "," + System.DateTime.Now.ToShortTimeString() + ":" + System.DateTime.Now.Second;
        void ConnectToRemoSharpTrigger()
        {
            foreach (var obj in this.OnPingDocument().Objects)
            {
                if (obj.GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Timer") && obj.NickName.Contains("RemoSharp"))
                {
                    Grasshopper.Kernel.Special.GH_Timer trigger = (Grasshopper.Kernel.Special.GH_Timer)obj;
                    this.OnPingDocument().ScheduleSolution(0, (GH_Document) =>
                    {
                        trigger.AddTarget(this.InstanceGuid);
                    });
                }
            }
        }
        bool LargeEnoughMovement(bool timeChange, System.Drawing.PointF currentPos, System.Drawing.PointF nextPos)
        {
            bool enoughViewChangeX = Math.Abs(currentPos.X - nextPos.X) > 50;
            bool enoughViewChangeY = Math.Abs(currentPos.Y - nextPos.Y) > 50;
            bool enoughMovement = enoughViewChangeX || enoughViewChangeY;

            return enoughMovement && timeChange;
        }
        bool EnoughTimePassed(string prevTime, string currentTime)
        {
            string[] prev = prevTime.Split(':');
            string[] curr = currentTime.Split(':');
            int prevSec = Convert.ToInt16(prev[prev.Length - 1]);
            int currSec = Convert.ToInt16(curr[curr.Length - 1]);

            if (Math.Abs(prevSec - currSec) > 2) return true;
            else return false;
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
                return RemoSharp.Properties.Resources.GHL.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0B4848DD-C620-4E0E-99A2-6F27748EDC1E"); }
        }
    }
}