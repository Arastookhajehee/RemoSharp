using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using GHCustomControls;
using Grasshopper.Kernel;
using Rhino.Geometry;
using WebSocketSharp;

using WPFNumericUpDown;
using System.Data.SqlTypes;

namespace RemoSharp.WebSocketClient
{
    public class WebSocketClient : GHCustomComponent
    {
        public List<string> messages = new List<string>();
        public WebSocket client;
        bool connect = false;
        bool keepAlive = false;
        bool needsRestart = false;

        public ToggleSwitch autoUpdateSwitch;
        ToggleSwitch listenSwitch;
        ToggleSwitch keepRecordSwitch;

        public bool autoUpdate = true;
        bool listen = true;
        bool keepRecord = false;

        /// <summary>
        /// Initializes a new instance of the WebSocketClient class.
        /// </summary>
        public WebSocketClient()
          : base("WebSocketClient", "WebSocketClient",
              "Description",
              "RemoSharp", "WSC_Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            autoUpdateSwitch = new ToggleSwitch("Auto Update", "If turned off, a trigger should be used for the listen component.", true);
            autoUpdateSwitch.OnValueChanged += AutoUPdate_OnValueChanged;
            listenSwitch = new ToggleSwitch("Listen", "If enabled the client listens for messages.", true);
            listenSwitch.OnValueChanged += ListenSwitch_OnValueChanged;
            keepRecordSwitch = new ToggleSwitch("Keep Record", "Keeps all the messages coming from the server", false);
            keepRecordSwitch.OnValueChanged += KeepRecordSwitch_OnValueChanged;

            AddCustomControl(autoUpdateSwitch);
            AddCustomControl(listenSwitch);
            AddCustomControl(keepRecordSwitch);

            pManager.AddTextParameter("url", "url", "", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("connect", "connect", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("KeepAlive", "keepAlive", "", GH_ParamAccess.item);
        }

        private void AutoUPdate_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            autoUpdate = Convert.ToBoolean(e.Value);
        }

        private void ListenSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            listen = Convert.ToBoolean(e.Value);

            if (client == null) return;

            if (listen) client.OnMessage += Client_OnMessage;
            else client.OnMessage -= Client_OnMessage;       

        }

        private void KeepRecordSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            keepRecord = Convert.ToBoolean(e.Value);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("WSClient", "wsc", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            string url = "";
            DA.GetData(0,ref url);
            DA.GetData(1, ref connect);
            DA.GetData(2, ref keepAlive);

            if (client != null && !url.Equals(client.Url.AbsoluteUri))
            {
                needsRestart= true;
                if (client != null)
                {
                    client.OnMessage -= Client_OnMessage;
                    client.OnOpen -= Client_OnOpen;
                    client.OnClose -= Client_OnClose;
                    client.Close();
                    client= null;
                }
            }

            if (needsRestart && !connect) 
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please Restart Connection");
                return;
            }

            if (connect)
            {
                if (client == null || needsRestart) client = new WebSocket(url);

                if (!client.IsAlive)
                {

                    client.Connect();

                    client.OnOpen += Client_OnOpen;
                    client.OnClose += Client_OnClose;
                    client.OnMessage += Client_OnMessage;

                    needsRestart = false;
                }
            }
           
            DA.SetData(0, client);
        }

        private void Client_OnClose(object sender, CloseEventArgs e)
        {
            while (!client.IsAlive && keepAlive)
            {
                client.Connect();
            }
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {
           
            messages.Add(e.Data);
            if (!keepRecord)
            {
                
                while (messages.Count > 1)
                {
                    messages.RemoveAt(0);
                }
            }

            

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                var recepients = this.Params.Output[0].Recipients;
                foreach (var item in recepients)
                {
                    if (item is Grasshopper.Kernel.Parameters.Param_GenericObject)
                    {
                        if (item.Attributes.Parent.DocObject is RemoSharp.WebSocketClient.WSClientListen)
                        {
                            if (this.autoUpdate)
                            {
                                item.Attributes.Parent.DocObject.ExpireSolution(true);
                            }
                            else
                            {
                                item.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Please Attach a Trigger to this component\nto read incoming messages");
                            }
                        }
                    }
                }
            });

        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                this.ExpireSolution(false);
            });
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
                return RemoSharp.Properties.Resources.WSC.ToBitmap();;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("7FADA9A3-FE26-48C9-A6C7-CDFF00D03225"); }
        }
    }
}