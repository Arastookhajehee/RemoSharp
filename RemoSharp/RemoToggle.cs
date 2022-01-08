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
            pManager.AddBooleanParameter("Toggle", "Tgl", "Toggle a Boolean Toggle on the main remote GH_Canvas", GH_ParamAccess.item, false);
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

            bool ToggleSwitch = false;
            DA.GetData(0, ref ToggleSwitch);

            string outputData = "RemoParam,"
                + Convert.ToInt32(this.Component.Attributes.Pivot.X) + ","
                + Convert.ToInt32(this.Component.Attributes.Pivot.Y) + ","
                + "ToggleBooleanToggle," + ToggleSwitch;
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