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
        HorizontalSliderInteger slider;
        //PushButton setupButton;
        //PushButton pushButton2;
        int serverIndex = 1;

        public WS_Server_Samples()
          : base("WS_Server_Samples", "WS_S_Samples",
              "3 different public Servers",
              "RemoSharp", "Com_Tools")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddIntegerParameter("Select Server", "Srv_Sel", "The index of the public server to use. (1 - 10)", GH_ParamAccess.item, 1);
            slider = new HorizontalSliderInteger("Internet-Based Public Server", "Please select the " +
                "internet-based public server that you would like to use.", 1, 1, 4);

            slider.OnValueChanged += Slider_OnValueChanged;

            AddCustomControl(slider);
            //setupButton = new PushButton("Wi-Fi IP Panel", "Adding a panel with wi-fi server address information");
            //setupButton.OnValueChanged += seupButton_OnValueChanged;
            //pushButton2 = new PushButton("WS Cliet Template", "Adding the neccessary components for starting a WS Client");
            //pushButton2.OnValueChanged += HideButton_OnValueChanged;
            //AddCustomControl(setupButton);
            //AddCustomControl(pushButton2);
        }

        private void Slider_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            serverIndex = Convert.ToInt32(e.Value);
            this.ExpireSolution(true);

        }

        //private void seupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool currentVal = Convert.ToBoolean(e.Value);
        //    if (currentVal)
        //    {
        //        this.OnPingDocument().ScheduleSolution(0, CreatePanel);

        //    }
        //}

        //private void HideButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
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
            //pManager.AddTextParameter("Server Template Info", "Server Template Info", "Github Repo WebSocket Echo Server template address" + "\r" + "This address can be used to make free WebSocket Echo Servers on Glitch.com", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            int index = serverIndex;

            // server list
            var serverList = new List<string>();

            for (int i = 1; i < 4; i++)
            {
                string addressIndex = i.ToString("00");
                serverList.Add("wss://remosharp-public-server" + addressIndex + ".glitch.me/");
            }
            serverList.Add("ws://133.247.128.84:18580");

            //if (index < 1 || index > 16)
            //{
            //    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please input a number form 1 to 10");
            //    return;
            //}
            string serverAddress = serverList[index - 1];
            //string serverTemplate = "https://github.com/Arastookhajehee/RemoSharp_Public_WS_Glitch_Server_Template.git";
            DA.SetData(0, serverAddress);
            //DA.SetData(1, serverTemplate);

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