using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.Utilities
{
    public class GHL : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_Log class.
        /// </summary>
        public GHL()
          : base("GHL", "GHL",
              "GHL",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Run", "R", "GHL", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        int running = 0;
        int currentListLength = 0;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (running == 0) ConnectToRemoSharpTrigger();
            var nowObj = System.DateTime.Now;
            var now = nowObj.Year + "-" + nowObj.Month + "-" + nowObj.Day;
            //    if (!SaveLog) return;

            // https://gurukultree.com/articles/20/how-to-get-a-path-to-the-desktop-for-current-user-in-cshrp/
            string filePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var undoLog = this.OnPingDocument().UndoServer.UndoNames;
            if (undoLog.Count != currentListLength)
            {
                currentListLength = undoLog.Count;
                var thisInstance = System.DateTime.Now.ToShortDateString() + "," + System.DateTime.Now.ToShortTimeString();
                thisInstance += ":" + System.DateTime.Now.Second;
                using (StreamWriter outputFile = new StreamWriter(Path.Combine(filePath, "RemoSharpLog " + now + ".rms")))
                {
                    foreach (string line in undoLog)
                        outputFile.WriteLine(line + "," + thisInstance);
                }
            }
            running++;
            if (running > 5)
            {
                DA.SetData(0, "Running");
            }
        }

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
            get { return new Guid("0B4848DD-C620-4E0E-99A2-6F27748EDC1E"); }
        }
    }
}