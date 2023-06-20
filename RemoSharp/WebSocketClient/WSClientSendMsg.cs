using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;

namespace RemoSharp.WebSocketClient
{
    public class WSClientSendMsg : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WSClientSendMsg class.
        /// </summary>
        public WSClientSendMsg()
          : base("WSClientSendMsg", "WSClientSendMsg",
              "Description",
               "RemoSharp", "Com_Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("WSClient", "wsc", "WebSocket Client", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Send", "send", "Sends the message. Connect a true state toggle to send\nmessages with text input update.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("Message", "msg", "Message to send", GH_ParamAccess.item);
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
            WebSocket client = null;
            bool sendMessage = false;
            string message = "";
            
            DA.GetData(0, ref client);
            DA.GetData(1, ref sendMessage);
            DA.GetData(2, ref message);
            if(client != null && sendMessage) client.Send(message);
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
                return RemoSharp.Properties.Resources.send.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("122C3796-B398-4728-9F22-885036FAC142"); }
        }
    }
}