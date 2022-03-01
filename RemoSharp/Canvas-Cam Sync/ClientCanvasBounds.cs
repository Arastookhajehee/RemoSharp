using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp
{
    public class ClientCanvasBounds : GHCustomComponent
    {

        PushButton pushButton1;

        /// <summary>
        /// Initializes a new instance of the ClientCanvasBounds class.
        /// </summary>
        public ClientCanvasBounds()
          : base("Canvas Bounds", "Client_Cvs",
              "Retrieves info from the client's GH_Canvas active bounds and broadcasts it as a string.",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pushButton1 = new PushButton("WS_Client",
                "Creates The Required WS Client Components To Broadcast Canvas Bounds Coordinates", "WS_Client");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddIntegerParameter("ShiftX", "X", "Shift the X coordinate of the GH_Canvas", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("ShiftY", "Y", "Shift the Y coordinate of the GH_Canvas", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("HighResolution", "HighRes", "If True, it will map a 1:1 scale GH_Canvas bounds. " +
                "Note that High-Res means that the screen image will take longer to generate and may result in performance and latency issues.",
                GH_ParamAccess.item,
                false);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y - 113);
                System.Drawing.PointF triggerPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y + 2);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 16, pivot.Y - 75);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 196, pivot.Y - 84);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 346, pivot.Y - 14);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                panel.SetUserText("ws://127.0.0.1:6999/RemoSharpCanvasBounds");

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);
                    wss.Params.Input[0].AddSource((IGH_Param)panel);
                });

            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CanvasBounds", "bounds", "A text based representation of the current GH_Canvas active region bounds",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region Checking for GH_Canvas Zoom
            // Checking for the Zoom Level of GH
            var textList = new List<string>();
            var GH_Objects = this.OnPingDocument().Objects;
            for (int i = 0; i < GH_Objects.Count; i++)
            {
                try
                {
                    IGH_Component comp = (IGH_Component)GH_Objects[i];
                    string componentType = comp.GetType().ToString();
                    if (componentType.Equals("RemoSharp.RemoCompSource"))
                    {
                        string zoomOutMessage = "Zoom Out Please";
                        string zoomInMessage = "Zoom in Please";
                        var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
                        if (zoomLevel > 1)
                        {
                            if (comp.Message != "Please Connect a Trigger")
                            {
                                comp.Message = zoomOutMessage;
                                comp.ClearRuntimeMessages();
                                comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomOutMessage);
                            }
                        }
                        else if (zoomLevel < 1)
                        {
                            if (comp.Message != "Please Connect a Trigger")
                            {
                                comp.ClearRuntimeMessages();
                                comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomInMessage);
                                comp.Message = zoomInMessage;
                            }
                        }
                        else
                        {
                            if (comp.Message != "Please Connect a Trigger")
                            {
                                comp.ClearRuntimeMessages();
                                comp.Message = "";
                            }
                            
                        }
                    }
                    textList.Add(componentType);
                }
                catch { }
            }
            #endregion

            this.Message = "Need a Trigger for RT";
            int x = 0;
            int y = 0;
            bool highRes = false;

            DA.GetData(0, ref x);
            DA.GetData(1, ref y);
            DA.GetData(2, ref highRes);


            double scale = 50;
            if (highRes) 
            {
                scale = 100;
            }
            

            var thisCanvas = Grasshopper.Instances.ActiveCanvas;

            // getting the active region of the grasshopper canvas
            var thisCanvasViewPort = thisCanvas.Viewport;
            var visRg = thisCanvasViewPort.VisibleRegion;
            var coords1 = Convert.ToInt32((visRg.X + visRg.Width) * thisCanvasViewPort.Zoom);
            var coords2 = Convert.ToInt32((visRg.Y + visRg.Height) * thisCanvasViewPort.Zoom);

            int visRgX = Convert.ToInt32(visRg.X);
            int visRgY = Convert.ToInt32(visRg.Y);

            visRgY += 0;
            visRgX += 0;

            int xPos = x - 15;
            int yPos = y + 161;

            if (visRgX < 0)
            {
                xPos += -visRgX;
                visRgX = 0;

            }
            if (visRgY < 0)
            {
                yPos += -visRgY;
                visRgY = 0;
            }
            var viewPortCorners = visRgY + "," + coords2 + "," + visRgX + "," + coords1 + "," + thisCanvasViewPort.Zoom;


            viewPortCorners += "," + xPos + "," + yPos + "," + scale;
            // getting where the gh window is and what is its size

            DA.SetData(0, viewPortCorners);
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
                return RemoSharp.Properties.Resources.CanvasBounds.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("da6c5669-3b2c-47aa-9534-b993480adb2b"); }
        }
    }
}