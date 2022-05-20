using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using GHCustomControls;
using WPFNumericUpDown;
using Grasshopper.GUI.Base;

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

            pManager.AddNumberParameter("ManualScale", "Scl", "Manually Change the scale of the final image", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Resolution", "Res", "0 -> setting 1:0.25 scale GH_Canvas bounds." + Environment.NewLine +
                                                              "1 -> setting 1:0.50 scale GH_Canvas bounds." + Environment.NewLine +
                                                              "2 -> setting 1:1.00 scale GH_Canvas bounds.",
                GH_ParamAccess.item,
                1);
            pManager.AddPointParameter("CalibPoint", "CbrPnt", "A point in XY Coordinates to callibrate the position of the regenerated image from the grasshopper extents", GH_ParamAccess.item, new Point3d(-90, 100, 0));
            pManager.AddTextParameter("ImageServer", "ImgSrv", "The Server Address used for the Image reconstruction", GH_ParamAccess.item, "");
            
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                this.Hidden = true;
                
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y - 113);
                System.Drawing.PointF srvAddPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y - 128);
                System.Drawing.PointF triggerPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y + 200);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y - 74);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X -70, pivot.Y - 83);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 50, pivot.Y - 73);
                System.Drawing.PointF calbSlider = new System.Drawing.PointF(pivot.X - 280, pivot.Y - 20);
                System.Drawing.PointF calbPanel = new System.Drawing.PointF(pivot.X - 280, pivot.Y);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 100, 20);
                panel.SetUserText("ws://127.0.0.1:6999/RemoSharpCanvasBounds");

                Grasshopper.Kernel.Special.GH_Panel srvPanel = new Grasshopper.Kernel.Special.GH_Panel();
                srvPanel.CreateAttributes();
                srvPanel.Attributes.Pivot = srvAddPivot;
                srvPanel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 100, 20);
                srvPanel.SetUserText("");

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;

                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;

                decimal minBound = Convert.ToDecimal(0.001);
                decimal maxBound = Convert.ToDecimal(1.5);
                decimal currentVal = Convert.ToDecimal(1);
                int accuracy = 3;
                GH_SliderAccuracy acc = GH_SliderAccuracy.Float;
                var sliderComponent = new Grasshopper.Kernel.Special.GH_NumberSlider();
                sliderComponent.CreateAttributes();
                sliderComponent.Attributes.Pivot = calbSlider;
                sliderComponent.Slider.Minimum = minBound;
                sliderComponent.Slider.Maximum = maxBound;
                sliderComponent.Slider.Value = currentVal;
                sliderComponent.Slider.DecimalPlaces = accuracy;
                sliderComponent.Slider.Type = acc;

                var multiSlider = new Grasshopper.Kernel.Special.GH_MultiDimensionalSlider();
                multiSlider.CreateAttributes();
                multiSlider.Attributes.Pivot = calbPanel;
                multiSlider.XInterval = new Interval(-200, 0);
                multiSlider.YInterval = new Interval(0, 200);
                multiSlider.SliderMode = Grasshopper.Kernel.Special.GH_MDSliderMode._2d;
                multiSlider.VolatileData.Clear();
                multiSlider.Value = new Point3d(0.55, 0.5, 0.0);
                //point.PersistentData.Append(ghPoint);
                multiSlider.ExpireSolution(true);

                //var ghPoint = new Grasshopper.Kernel.Types.GH_Point();
                //ghPoint.CreateFromCoordinate(new Point3d(-90, 100, 0));
                //Grasshopper.Kernel.Parameters.Param_Point point = new Grasshopper.Kernel.Parameters.Param_Point();
                //point.CreateAttributes();
                //point.Attributes.Pivot = pointPivot;
                //point.PersistentData.Clear();
                //point.PersistentData.Append(ghPoint);
                //point.Attributes.Selected = true;
                //point.ExpireSolution(true);

                var guid = this.InstanceGuid;
                Grasshopper.Kernel.Special.GH_Timer trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.Interval = 100;

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(srvPanel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);
                    this.OnPingDocument().AddObject(wsSend, true);
                    this.OnPingDocument().AddObject(multiSlider, true);
                    this.OnPingDocument().AddObject(sliderComponent, true);
                    this.OnPingDocument().AddObject(trigger, true);

                    trigger.AddTarget(guid);
                    this.Params.Input[0].AddSource((IGH_Param)sliderComponent);
                    this.Params.Input[2].AddSource((IGH_Param)multiSlider);
                    this.Params.Input[3].AddSource((IGH_Param)srvPanel);
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
            pManager.AddTextParameter("CanvasBoundsForXML", "bnds4XML", "A text based representation of the current GH_Canvas active region bounds",
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
                    if (componentType.Equals("RemoSharp.RemoCompSource") || componentType.Equals("RemoSharp.RemoCompTarget"))
                    {
                        //string zoomOutMessage = "Zoom Out Please";
                        //string zoomInMessage = "Zoom in Please";
                        //var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
                        //if (zoomLevel > 1)
                        //{
                        //    if (comp.Message != "Please Connect a Trigger")
                        //    {
                        //        comp.Message = zoomOutMessage;
                        //        comp.ClearRuntimeMessages();
                        //        comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomOutMessage);
                        //    }
                        //}
                        //else if (zoomLevel < 1)
                        //{
                        //    if (comp.Message != "Please Connect a Trigger")
                        //    {
                        //        comp.ClearRuntimeMessages();
                        //        comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomInMessage);
                        //        comp.Message = zoomInMessage;
                        //    }
                        //}
                        //else
                        //{
                        if (comp.Message != "Please Connect a Trigger")
                        {
                            comp.ClearRuntimeMessages();
                            comp.Message = "";
                        }

                        //}
                    }
                    textList.Add(componentType);
                }
                catch { }
            }
            #endregion

            this.Message = "Need a Trigger for RT";

            Point3d pnt = new Point3d(8, 7, 0);
            double manRes = 1;
            int resolutionVal = 1;
            bool lowRes = false;
            bool highRes = false;
            string address = "";

            DA.GetData(0, ref manRes);
            DA.GetData(1, ref resolutionVal);
            DA.GetData(2, ref pnt);
            DA.GetData(3, ref address);

            if (resolutionVal == 0) lowRes = true;
            if (resolutionVal == 2) highRes = true;

            int x = Convert.ToInt32(pnt.X);
            int y = Convert.ToInt32(-pnt.Y);

            double scale = 50;
            if (lowRes) scale = 15;
            else if (highRes) scale = 100;
            
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


            viewPortCorners += "," + xPos + "," + yPos + "," + scale + "," + manRes + "," + address;
            // getting where the gh window is and what is its size

            var bounds_for_xml = Grasshopper.Instances.ActiveCanvas.Viewport.VisibleRegion;
            var screenMidPnt = Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint;
            var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
            string bnds4XML = bounds_for_xml.X
                + "," + bounds_for_xml.Y
                + "," + bounds_for_xml.Width
                + "," + bounds_for_xml.Height
                + "," + screenMidPnt.X
                + "," + screenMidPnt.Y
                + "," + zoomLevel;

            DA.SetData(0, viewPortCorners);
            DA.SetData(1, bnds4XML);
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