using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class GH_CanvasBoundsLocalWSaddress : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GH_CanvasBoundsLocalWSaddress class.
        /// </summary>
        public GH_CanvasBoundsLocalWSaddress()
          : base("CanvasBoundsWS_IP", "CvsBndIP",
              "The standard IP and port address for syncing the current GH_Canvas bounds coordinates and the background image recreation process.",
              "RemoSharp", "Com_Tools")
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
            pManager.AddTextParameter("CanvasBoundsWSIP", "CvsBndWSIP", 
                "The standard IP and port address for syncing the current GH_Canvas bounds coordinates and the background image recreation process.",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string IP_Address = "ws://127.0.0.1:6999/RemoSharpCanvasBounds";
            DA.SetData(0, IP_Address);
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
            get { return new Guid("ac5e1ecf-0201-4b39-8efb-e1ee02037982"); }
        }
    }
}