using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class WebSocket_BFF : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        /// <summary>
        /// Initializes a new instance of the WebSocket_BFF class.
        /// </summary>
        public WebSocket_BFF()
          : base("WebSocket_BFF", "WS_BFF",
              "Tries to keep a connection live with a WebSocket Server Hosted on the web (for example glitch.com servers)",
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
            pManager.AddTextParameter("WS_BFF_Message", "BFF_Msg","Message to be send to the WS server to keep it alive", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            if (messageAlertIndex == 0)
            {
                this.Component.Message = "Need a Trigger :(";
            }
            else {
                this.Component.Message = "Got my Trigger :D";
            }
            
            string BFF_Msg = "YAY! Still Friends!";
            DA.SetData(0, BFF_Msg);

            messageAlertIndex++;
        }

        public int messageAlertIndex = 0;

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return RemoSharp.Properties.Resources.WS_BFF.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("070d3911-ed8a-44bf-9f3c-64efe68f7ff0"); }
        }
    }
}