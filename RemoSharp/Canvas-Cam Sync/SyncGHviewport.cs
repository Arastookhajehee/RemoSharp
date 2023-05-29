using System;
using Grasshopper.Kernel;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp.Canvas_Cam_Sync
{
    public class SyncGHviewport : GHCustomComponent
    {
        PushButton pushButton1;

        /// <summary>
        /// Initializes a new instance of the SyncCanvasBounds class.
        /// </summary>
        public SyncGHviewport()
          : base("SyncGHviewport", "SyncView",
              "Syncronizes the position and the zoom level of the canvas based on an input from a stream.",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pushButton1 = new PushButton("WS_Client",
            //"Creates The Required WS Client Components To Broadcast Canvas Screen.", "WS_Client");
            //pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            //AddCustomControl(pushButton1);

            pManager.AddTextParameter("BoundsStram", "Bounds",
                "Simplified Bounds information from the ClientCanvasBounds Component (bnds4XML).",
                GH_ParamAccess.item, "");
        }
        /*
        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 673, pivot.Y - 39);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 504, pivot.Y + 9);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 293, pivot.Y + 0);
                System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X - 148, pivot.Y + 10);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.SetUserText("");
                panel.Name = "RemoSharp";
                panel.NickName = "RemoSharp";
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.Name = "RemoSharp";
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                wsRecv.CreateAttributes();
                wsRecv.Attributes.Pivot = wsRecvPivot;

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsRecv, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    this.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                });

            }
        }
        */
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
            string str = "";
            DA.GetData(0,ref str);

            if (str == null || str == "" || str == " " || str == "Hello World") return;
            
            string[] parts = str.Split(',');
            Single x = Convert.ToSingle(parts[4]);
            Single y = Convert.ToSingle(parts[5]);
            float zoom = (float)Convert.ToDouble(parts[6]);
            Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint = new System.Drawing.PointF(x, y);
            Grasshopper.Instances.ActiveCanvas.Viewport.Zoom = zoom;
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
                return RemoSharp.Properties.Resources.SyncGHviewport.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("CA67C49A-5D53-4DD1-AA46-52CFE5A11DDB"); }
        }
    }
}