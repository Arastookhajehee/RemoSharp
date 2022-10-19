using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class RemoToggle : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        /// <summary>
        /// Initializes a new instance of the RemoToggle class.
        /// </summary>
        public RemoToggle()
          : base("RemoToggle", "RemoTgl",
              "Remote Boolean Toggle",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Toggle", "Tgl", "Toggle a Boolean Toggle on the main remote GH_Canvas", GH_ParamAccess.item, true);
            pManager.AddTextParameter("Username_ID", "user", "The name of this canvas", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ToggleCommand", "TglCmd", "Command to trigger switching a toggle on the main remote GH_Canvas", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            string username = "";
            bool ToggleSwitch = false;
            DA.GetData(0, ref ToggleSwitch);
            DA.GetData(1, ref username);

            int coordX = Convert.ToInt32(this.Component.Attributes.Pivot.X) -30;
            int coordY = Convert.ToInt32(this.Component.Attributes.Pivot.Y) - 27;

            Guid toggleGuid = this.Params.Input[0].Sources[0].InstanceGuid;

            string outputData = "ID_" + username + ",RemoParam,"
                + coordX + ","
                + coordY + ","
                + "ToggleBooleanToggle," + ToggleSwitch + "," + toggleGuid.ToString();
            DA.SetData(0, outputData);
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
                return RemoSharp.Properties.Resources.RemoToggle.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("00132dbb-12b9-4abc-a3fd-c46fe219ff43"); }
        }
    }
}