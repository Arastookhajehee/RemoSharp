using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using RemoSharp.Distributors;
using Rhino.Geometry;

namespace RemoSharp.WebSocketClient
{
    public class WSClientListen : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WSClientListen class.
        /// </summary>
        public WSClientListen()
          : base("WSListen", "WSListen",
              "Description",
               "RemoSharp", "Com_Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("WSClient", "wsc", "Websocket client component", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("message", "msg", "", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var inputComponent = this.Params.Input[0].Sources[0].Attributes.Parent.DocObject;

            if (inputComponent is RemoSharp.WebSocketClient.WebSocketClient)
            {
                WebSocketClient client = (WebSocketClient)inputComponent;
                if (!client.autoUpdate)
                {
                    string[] lastMessage = { client.messages[client.messages.Count - 1] };
                    DA.SetDataList(0, lastMessage.ToList());
                    client.messages.Clear();
                    client.messages.AddRange(lastMessage);
                }
                else
                {
                    DA.SetDataList(0, client.messages);
                }
            }
            else if (inputComponent is RemoSharp.RemoSetupClient) 
            {
                RemoSetupClient client = (RemoSetupClient)inputComponent;
                DA.SetDataList(0, client.messages);
            }
            else if (inputComponent is RemoSetupClientV3)
            {
                RemoSetupClientV3 client = (RemoSetupClientV3)inputComponent;
                DA.SetDataList(0, client.messages);
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
                return RemoSharp.Properties.Resources.receive.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("71CCCAC5-2071-493C-AD02-347781A1ED08"); }
        }
    }
}