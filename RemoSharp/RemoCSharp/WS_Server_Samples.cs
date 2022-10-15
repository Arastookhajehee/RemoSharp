using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel.Types;

using GHCustomControls;
using WPFNumericUpDown;

using System.Net.NetworkInformation;


namespace RemoSharp
{
    public class WS_Server_Samples : GHCustomComponent
    {
        /// <summary>
        /// Initializes a new instance of the WS_Server_Samples class.
        /// </summary>
        ToolStripDropDown menu;
        GH_Document GrasshopperDocument;
        IGH_Component Component;
        GHCustomControls.HorizontalSliderInteger slider;
        //PushButton pushButton1;
        //PushButton pushButton2;
        int serverIndex = 1;

        public WS_Server_Samples()
          : base("WS_Server_Samples", "WS_S_Samples",
              "10 different public Websocket Servers live on Glitch.Com" +
                "You can create your own Glitch.com WS Echo Servers by using this template:" + "/r" +
                "https://github.com/Arastookhajehee/RemoSharp_Public_WS_Glitch_Server_Template.git",
              "RemoSharp", "Com_Tools")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddIntegerParameter("Select Server", "Srv_Sel", "The index of the public server to use. (1 - 10)", GH_ParamAccess.item, 1);
            slider = new HorizontalSliderInteger("Internet-Based Public Server", "Please select the internet-based public server that you would like to use.", 1, 1, 10);

            slider.OnValueChanged += Slider_OnValueChanged;

            AddCustomControl(slider);
            //pushButton1 = new PushButton("Wi-Fi IP Panel", "Adding a panel with wi-fi server address information");
            //pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            //pushButton2 = new PushButton("WS Cliet Template", "Adding the neccessary components for starting a WS Client");
            //pushButton2.OnValueChanged += PushButton2_OnValueChanged;
            //AddCustomControl(pushButton1);
            //AddCustomControl(pushButton2);
        }

        private void Slider_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            serverIndex = Convert.ToInt32(e.Value);
            this.ExpireSolution(true);

        }

        //private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool currentVal = Convert.ToBoolean(e.Value);
        //    if (currentVal)
        //    {
        //        this.OnPingDocument().ScheduleSolution(0, CreatePanel);

        //    }
        //}

        //private void PushButton2_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool currentVal = Convert.ToBoolean(e.Value);
        //    if (currentVal)
        //    {
        //        var pivot = this.Attributes.Pivot;
        //        int pivotX = Convert.ToInt32(pivot.X) + 50;
        //        int pivotY = Convert.ToInt32(pivot.Y) + 40;

        //        RecognizeAndMake("Grasshopper.Kernel.Special.GH_ButtonObject", pivotX - 50, pivotY + 150);
        //        RecognizeAndMake("Bengesht.WsClientCat.WsClientStart", pivotX + 150, pivotY + 142);
        //        RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 295, pivotY + 155);
        //        RecognizeAndMake("Bengesht.WsClientCat.WsClientRecv", pivotX + 295, pivotY + 100);
        //    }
        //}

        //void CreatePanel(GH_Document doc)
        //{
        //    string ipAddress = "127.0.0.1";

        //    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        //    {
        //        if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
        //        {
        //            string wifi_name = ni.Name;

        //            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
        //            {
        //                if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //                {
        //                    ipAddress = ip.Address.ToString();
        //                }
        //            }
        //        }
        //    }

        //    var pivot = this.Attributes.Pivot;
        //    var newPivot = new System.Drawing.PointF(pivot.X - 20, pivot.Y + 85);

        //    var panel = new Grasshopper.Kernel.Special.GH_Panel();
        //    panel.CreateAttributes();
        //    panel.Attributes.Pivot = newPivot;
        //    panel.SetUserText("ws://" + ipAddress + ":PORT/RemoSharp");
        //    this.OnPingDocument().AddObject((IGH_DocumentObject)panel, true);
        //}

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("           Sever Address", "           Sever Address", "The Address of the public WebSocket Echo Server on Glitch.Com", GH_ParamAccess.item);
            pManager.AddTextParameter("Server Template Info", "Server Template Info", "Github Repo WebSocket Echo Server template address" + "\r" + "This address can be used to make free WebSocket Echo Servers on Glitch.com", GH_ParamAccess.item);
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

            int index = serverIndex;
            //DA.GetData(0, ref index);
            index = serverIndex;
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