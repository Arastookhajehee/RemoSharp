using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;

using System.Net.NetworkInformation;

namespace RemoSharp
{
    public class GH_CanvasBoundsLocalWSaddress : GHCustomComponent
    {

        PushButton pushButton1;
        PushButton pushButton2;

        public bool makeWSclient = false;

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
            pushButton1 = new PushButton("Wi-Fi IP Panel", "Adding a panel with wi-fi server address information");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            pushButton2 = new PushButton("WS Cliet Template", "Adding the neccessary components for starting a WS Client");
            pushButton2.OnValueChanged += PushButton2_OnValueChanged;
            AddCustomControl(pushButton1);
            AddCustomControl(pushButton2);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentVal = Convert.ToBoolean(e.Value);
            if (currentVal) 
            {
                this.OnPingDocument().ScheduleSolution(0, CreatePanel);

            }
        }

        private void PushButton2_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentVal = Convert.ToBoolean(e.Value);
            if (currentVal)
            {
                var pivot = this.Attributes.Pivot;
                int pivotX = Convert.ToInt32(pivot.X) + 50;
                int pivotY = Convert.ToInt32(pivot.Y) + 40;

                RecognizeAndMake("Grasshopper.Kernel.Special.GH_ButtonObject", pivotX - 50, pivotY + 150);
                RecognizeAndMake("Bengesht.WsClientCat.WsClientStart", pivotX + 150, pivotY + 142);
                RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 295, pivotY + 155);
                RecognizeAndMake("Bengesht.WsClientCat.WsClientRecv", pivotX + 295, pivotY + 100);
            }
        }

        void CreatePanel(GH_Document doc)
        {
            string ipAddress = "127.0.0.1";

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    string wifi_name = ni.Name;

                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipAddress = ip.Address.ToString();
                        }
                    }
                }
            }

            var pivot = this.Attributes.Pivot;
            var newPivot = new System.Drawing.PointF(pivot.X - 20, pivot.Y + 85);

            var panel = new Grasshopper.Kernel.Special.GH_Panel();
            panel.CreateAttributes();
            panel.Attributes.Pivot = newPivot;
            panel.SetUserText("ws://" + ipAddress + ":PORT/RemoSharp");
            this.OnPingDocument().AddObject((IGH_DocumentObject)panel, true);
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
            if (makeWSclient) 
            { 
                
            }
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
                return RemoSharp.Properties.Resources.CanvasBoundsIPAddress.ToBitmap(); ;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("ac5e1ecf-0201-4b39-8efb-e1ee02037982"); }
        }

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY)
        {
            var thisDoc = this.OnPingDocument();
            // converting the string format of the closest component to an actual type
            var type = Type.GetType(typeName);
            // most probable the type is going to return null
            // for that we search through all the loaded dlls in Grasshopper and Rhino's application
            // to find out which one matches that of the closest component
            if (type == null)
            {
                // going through the loaded components
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // trying for all dll types unless one would return an actual type
                    // since almost all of them give us null we check for this condition
                    if (type == null)
                    {
                        type = a.GetType(typeName);
                    }
                }
            }
            // we can instantiate a class with this line based on the type we found in string format
            // we have to cast it into an (IGH_DocumentObject) format so that we can access the methods
            // that we need to add it to the grasshopper document
            // also in order to add any object into the GH canvas it has to be cast into (IGH_DocumentObject)
            var myObject = (IGH_DocumentObject)Activator.CreateInstance(type);
            // creating atts to create the pivot point
            // this pivot point can be anywhere
            myObject.CreateAttributes();
            //        myObject.Attributes.Pivot = new System.Drawing.PointF(200, 600);
            var currentPivot = new System.Drawing.PointF(pivotX, pivotY);

            myObject.Attributes.Pivot = currentPivot;
            // making sure the update argument is false to prevent GH crashes
            thisDoc.AddObject(myObject, false);
        }
    }
}