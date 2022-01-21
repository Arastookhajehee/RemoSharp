using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Types;

namespace RemoSharp
{
    public class WS_Server_Samples : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WS_Server_Samples class.
        /// </summary>
        ToolStripDropDown menu;
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        public WS_Server_Samples()
          : base("WS_Server_Samples", "WS_S_Samples",
              "10 different public Websocket Servers live on Glitch.Com" +
                "You can create your own Glitch.com WS Echo Servers by using this template:" + "/r" +
                "https://github.com/Arastookhajehee/RemoSharp_Public_WS_Glitch_Server_Template.git",
              "RemoSharp", "Com_Tools")
        {
        }

        //public override bool AppendMenuItems(ToolStripDropDown menu)
        //{
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 01");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 02");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 03");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 04");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 05");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 06");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 07");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 08");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 09");
        //    Menu_AppendGenericMenuItem(this.menu, "Glitch.Com Server 10");



        //    return true;
        //}

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("Select Server", "Srv_Sel", "The index of the public server to use. (1 - 10)", GH_ParamAccess.item, 1);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Sever Address", "Srv_Add","The Address of the public WebSocket Echo Server on Glitch.Com", GH_ParamAccess.item);
            pManager.AddTextParameter("Server Template", "Srv_Tmp", "Github Repo WebSocket Echo Server template address" + "\r" + "This address can be used to make free WebSocket Echo Servers on Glitch.com", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            ToolStripDropDown menu = this.menu;

            int index = 1;
            DA.GetData(0, ref index);

            // server list
            var serverList = new List<string>();
            for (int i = 1; i < 10; i++) 
            {
                serverList.Add("wss://remosharp-public-server0"+ i +".glitch.me/");
            }
            serverList.Add("wss://remosharp-public-server10.glitch.me/");

            if (index < 1 || index > 10) { 
                this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please input a number form 1 to 10");
                return;
            }
            string serverAddress = serverList[index - 1];
            string serverTemplate = "https://github.com/Arastookhajehee/RemoSharp_Public_WS_Glitch_Server_Template.git";
            DA.SetData(0, serverAddress);
            DA.SetData(1, serverTemplate);

        }

        private void Menu_MyCustomItemClicked(Object sender, EventArgs e)
        {
            Rhino.RhinoApp.WriteLine("Alcohol doesn't affect me...");
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
                return RemoSharp.Properties.Resources.Server_Samples.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("912d4842-27bc-4605-9912-d5c85dc21b82"); }
        }
    }
}