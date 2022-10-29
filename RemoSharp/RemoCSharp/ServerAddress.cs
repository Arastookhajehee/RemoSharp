using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using GHCustomControls;
using WPFNumericUpDown;
using Grasshopper.GUI.Base;

namespace RemoSharp.RemoCSharp
{
    public class ServerAddress : GHCustomComponent
    {
        /// <summary>
        /// Initializes a new instance of the ServerAddress class.
        /// </summary>
        public ServerAddress()
          : base("ServerAddress", "Addresses",
              "A component to generate server address text based on RemoSharp's server setting convention" + Environment.NewLine +
                "Port 18580 -> Canvas Server" + Environment.NewLine +
                "Port 18581 -> Bounds Server" + Environment.NewLine +
                "Port 18582 -> Commands Server" + Environment.NewLine +
                "Port 18583 -> Camera Server",
              "RemoSharp", "Com_Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("UseFullAddress", "UseFullAddress", "Whether to generate address based on IP or WebSocket Server Complete Address", GH_ParamAccess.item, false);
            pManager.AddTextParameter("FullAddress", "FullAddress", "WebSocket Server Complete Address", GH_ParamAccess.item, "");
            pManager.AddTextParameter("IP_01", "IP_01", "IP First Number", GH_ParamAccess.item, "");
            pManager.AddTextParameter("IP_02", "IP_02", "IP Second Number", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Address_03", "Address_03", "Address Number from RemoSharp's Server Manager (IP Third Number)", GH_ParamAccess.item, "");
            pManager.AddTextParameter("PC_ID_04", "PC_ID_04", "ID Number from RemoSharp's Server Manager (IP Forth Number)", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("address01", "address01", "Canvas Document Server Based on RemoSharp's Convention", GH_ParamAccess.item);
            pManager.AddTextParameter("address02", "address02", "Bounds Document Server Based on RemoSharp's Convention", GH_ParamAccess.item);
            pManager.AddTextParameter("address03", "address03", "Bounds Document Server Based on RemoSharp's Convention", GH_ParamAccess.item);
            pManager.AddTextParameter("address04", "address04", "Bounds Document Server Based on RemoSharp's Convention", GH_ParamAccess.item);
            pManager.AddTextParameter("address05", "address05", "ID Number from RemoSharp's Server Manager (IP Forth Number)", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool useFullAddress = false;
            string fullAddress = "";
            string IP_01 = "";
            string IP_02 = "";
            string Address_03 = "";
            string PC_ID_04 = "";

            DA.GetData(0, ref useFullAddress);
            DA.GetData(1, ref fullAddress);
            DA.GetData(2, ref IP_01);
            DA.GetData(3, ref IP_02);
            DA.GetData(4, ref Address_03);
            DA.GetData(5, ref PC_ID_04);

            if (useFullAddress)
            {
                DA.SetData(0, fullAddress);
                return;
            }

            string canvasServerAddress = string.Format("ws://{0}.{1}.{2}.{3}:{4}/RemoSharp", IP_01, IP_02, Address_03, PC_ID_04, 18580);
            string boundsServerAddress = string.Format("ws://{0}.{1}.{2}.{3}:{4}/RemoSharp", IP_01, IP_02, Address_03, PC_ID_04, 18581);
            string commandsServerAddress = string.Format("ws://{0}.{1}.{2}.{3}:{4}/RemoSharp", IP_01, IP_02, Address_03, PC_ID_04, 18582);
            string cameraServerAddress = string.Format("ws://{0}.{1}.{2}.{3}:{4}/RemoSharp", IP_01, IP_02, Address_03, PC_ID_04, 18583);
            
            bool Address_IP_Is_Null = string.IsNullOrEmpty(Address_03) || string.IsNullOrEmpty(PC_ID_04);
            if (Address_IP_Is_Null)
            {
                DA.SetData(0, "");
                DA.SetData(1, "");
                DA.SetData(2, "");
                DA.SetData(3, "");
                DA.SetData(4, PC_ID_04);
            }
            else
            {
            DA.SetData(0,canvasServerAddress);
            DA.SetData(1, boundsServerAddress);
            DA.SetData(2, commandsServerAddress);
            DA.SetData(3, cameraServerAddress);
            DA.SetData(4, PC_ID_04);
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
                return RemoSharp.Properties.Resources.Server_Addresses.ToBitmap(); ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("A446BCD9-50C6-4156-B75E-6B980A7D63BB"); }
        }
    }
}