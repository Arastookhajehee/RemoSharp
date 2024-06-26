using GHCustomControls;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using WebSocketSharp;

namespace RemoSharp.WebSocketClient
{
    public class WebSocketClient : GHCustomComponent
    {
        public List<string> messages = new List<string>();
        public WebSocket client;
        int connectionAttempts = 0;

        public ToggleSwitch autoUpdateSwitch;
        public ToggleSwitch keepRecordSwitch;
        public ToggleSwitch listenSwitch;
        public ToggleSwitch KeepAliveSwitch;

        bool keepAlive = true;

        public bool autoUpdate = true;
        public bool keepRecord = false;
        public bool listen = true;

        System.Timers.Timer timer = new System.Timers.Timer(60000);

        /// <summary>
        /// Initializes a new instance of the WebSocketClient class.
        /// </summary>
        public WebSocketClient()
          : base("WebSocketClient", "WebSocketClient",
              "Description",
               "RemoSharp", "Com_Tools")
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
            KeepAliveSwitch = new ToggleSwitch("Keep Alive", "Keeps the connection alive", true);
            KeepAliveSwitch.OnValueChanged += KeepAliveSwitch_OnValueChanged;


            AddCustomControl(autoUpdateSwitch);
            AddCustomControl(listenSwitch);
            AddCustomControl(keepRecordSwitch);
            AddCustomControl(KeepAliveSwitch);

            pManager.AddTextParameter("url", "url", "", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("connect", "connect", "", GH_ParamAccess.item);
        }

        private void KeepAliveSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            this.keepAlive = Convert.ToBoolean(e.Value);

            if (!keepAlive)
            {
                timer.Elapsed -= Timer_Elapsed;
                timer.AutoReset = false;
                timer.Stop();
                timer.Dispose();
            }
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
            this.keepRecord = Convert.ToBoolean(e.Value);
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

            bool connect = false;

            string url = "";
            DA.GetData(0, ref url);
            DA.GetData(1, ref connect);

            //if (client != null && !url.Equals(client.Url.AbsoluteUri))
            //{
            //    if (client != null)
            //    {
            //        client.OnMessage -= Client_OnMessage;
            //        client.OnOpen -= Client_OnOpen;
            //        client.OnClose -= Client_OnClose;
            //        client.Close();
            //        client= null;
            //    }
            //}


            if (connect)
            {
                if (client != null)
                {
                    if (client.IsAlive)
                    {
                        DA.SetData(0, client);
                        return;
                    }
                }

                client = new WebSocket(url);
                client.Connect();

                if (client.IsAlive)
                {
                    client.OnOpen += Client_OnOpen;
                    client.OnClose += Client_OnClose;
                    client.OnMessage += Client_OnMessage;

                    timer = new System.Timers.Timer(60000);
                    timer.Elapsed += Timer_Elapsed;
                    timer.AutoReset = true;
                    timer.Start();
                }


            }
            else
            {
                if (client != null)
                {
                    client.OnOpen -= Client_OnOpen;
                    client.OnClose -= Client_OnClose;
                    client.OnMessage -= Client_OnMessage;
                    client.Close();
                    client = null;
                }
            }

            DA.SetData(0, client);


        }

        private void Client_OnClose(object sender, CloseEventArgs e)
        {
            while (!client.IsAlive && this.connectionAttempts < 2)
            {
                client.Connect();
                connectionAttempts++;
                if (client.IsAlive) connectionAttempts = 0;
            }

            timer.Elapsed -= Timer_Elapsed;
            timer.AutoReset = false;
            timer.Stop();
            timer.Dispose();
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {

            if (e.Data == "pong") return;
            messages.Add(e.Data);
            if (!this.keepRecord)
            {

                while (messages.Count > 1)
                {
                    messages.RemoveAt(0);
                }
            }


            try
            {
                Grasshopper.Kernel.GH_Document thisGH_Doc = this.OnPingDocument();
                if (thisGH_Doc != null)
                {
                    thisGH_Doc.ScheduleSolution(1, doc =>
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
            }
            catch
            {
            }
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            this.Message = "Connected";
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                this.Attributes.ExpireLayout();
            });

            if (keepAlive)
            {
                timer.Elapsed += Timer_Elapsed;
                timer.AutoReset = true;
                timer.Start();
            }
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                if (client != null)
                {
                    client.Send("ping");
                }
            }
            catch (Exception)
            {
                timer.Elapsed -= Timer_Elapsed;
                timer.AutoReset = false;
                timer.Stop();
                timer.Dispose();
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
                return RemoSharp.Properties.Resources.WSC.ToBitmap(); ;
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