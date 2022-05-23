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
            pushButton1 = new PushButton("Set Up Client",
                "Creates The Required WS Client Components To Broadcast Canvas Bounds Coordinates", "Set Up Client");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            //pManager.AddNumberParameter("ManualScale", "Scl", "Manually Change the scale of the final image", GH_ParamAccess.item, 1);
            //pManager.AddIntegerParameter("Resolution", "Res", "0 -> setting 1:0.25 scale GH_Canvas bounds." + Environment.NewLine +
            //                                                  "1 -> setting 1:0.50 scale GH_Canvas bounds." + Environment.NewLine +
            //                                                  "2 -> setting 1:1.00 scale GH_Canvas bounds.",
            //    GH_ParamAccess.item,
            //    1);
            //pManager.AddPointParameter("CalibPoint", "CbrPnt", "A point in XY Coordinates to callibrate the position of the regenerated image from the grasshopper extents", GH_ParamAccess.item, new Point3d(-90, 100, 0));
            //pManager.AddTextParameter("ImageServer", "ImgSrv", "The Server Address used for the Image reconstruction", GH_ParamAccess.item, "");
            
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                this.Hidden = true;
                
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                
                System.Drawing.PointF remoteBoundsPanelPivot = new System.Drawing.PointF(pivot.X - 285, pivot.Y - 66);
                System.Drawing.PointF localBoundsPanelPivot = new System.Drawing.PointF(pivot.X - 285, pivot.Y +33);
                
                System.Drawing.PointF remoteSrvButtonPivot = new System.Drawing.PointF(pivot.X - 285, pivot.Y -18);
                System.Drawing.PointF localSrvButtonPivot = new System.Drawing.PointF(pivot.X - 285, pivot.Y +81);
                
                System.Drawing.PointF remoteWssPivot = new System.Drawing.PointF(pivot.X -87, pivot.Y -27);
                System.Drawing.PointF localWssPivot = new System.Drawing.PointF(pivot.X - 87, pivot.Y + 72);
                
                System.Drawing.PointF remoteWsSendPivot = new System.Drawing.PointF(pivot.X + 173, pivot.Y - 17);
                System.Drawing.PointF localWsSendPivot = new System.Drawing.PointF(pivot.X + 173, pivot.Y + 82);
                
                System.Drawing.PointF triggerPivot = new System.Drawing.PointF(pivot.X - 281, pivot.Y + 130);

                //System.Drawing.PointF sourceCompPivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y + 180);
                //System.Drawing.PointF targetCompPivot = new System.Drawing.PointF(pivot.X + 0, pivot.Y + 180);

                // setup the panel that contains the remote server viewport bounds stream
                //StreamIPSet canvasAddress = new StreamIPSet();
                //canvasAddress.DialougeTitle.Text = "Please Set Your Bounds Sync Server Address";
                //canvasAddress.ShowDialog();
                string address = "";
                Grasshopper.Kernel.Special.GH_Panel remoteServAddPanel = new Grasshopper.Kernel.Special.GH_Panel();
                remoteServAddPanel.CreateAttributes();
                remoteServAddPanel.Attributes.Pivot = remoteBoundsPanelPivot;
                remoteServAddPanel.Attributes.Bounds = new System.Drawing.RectangleF(remoteBoundsPanelPivot.X, remoteBoundsPanelPivot.Y, 100, 20);
                remoteServAddPanel.SetUserText(address);
                remoteServAddPanel.NickName = "RemoSharp Bounds";

                // setup the panel that contains the local viewport bounds server
                Grasshopper.Kernel.Special.GH_Panel localServAddPanel = new Grasshopper.Kernel.Special.GH_Panel();
                localServAddPanel.CreateAttributes();
                localServAddPanel.Attributes.Pivot = localBoundsPanelPivot;
                localServAddPanel.Attributes.Bounds = new System.Drawing.RectangleF(remoteBoundsPanelPivot.X, remoteBoundsPanelPivot.Y, 100, 20);
                localServAddPanel.SetUserText("ws://127.0.0.1:18580/RemoSharpCanvasBounds");
                localServAddPanel.NickName = "RemoSharp";

                Grasshopper.Kernel.Special.GH_ButtonObject remoteButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                remoteButton.CreateAttributes();
                remoteButton.Attributes.Pivot = remoteSrvButtonPivot;
                remoteButton.NickName = "RemoSharp";

                Grasshopper.Kernel.Special.GH_ButtonObject localButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                localButton.CreateAttributes();
                localButton.Attributes.Pivot = localSrvButtonPivot;
                localButton.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart remoteWssComp = new WsClientCat.WsClientStart();
                remoteWssComp.CreateAttributes();
                remoteWssComp.Attributes.Pivot = remoteWssPivot;
                remoteWssComp.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientStart localWssComp = new WsClientCat.WsClientStart();
                localWssComp.CreateAttributes();
                localWssComp.Attributes.Pivot = localWssPivot;
                localWssComp.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientSend remoteWsSendComp = new WsClientCat.WsClientSend();
                remoteWsSendComp.CreateAttributes();
                remoteWsSendComp.Attributes.Pivot = remoteWsSendPivot;
                remoteWsSendComp.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientSend localWsSendComp = new WsClientCat.WsClientSend();
                localWsSendComp.CreateAttributes();
                localWsSendComp.Attributes.Pivot = localWsSendPivot;
                localWsSendComp.Params.RepairParamAssociations();

                var guid = this.InstanceGuid;
                Grasshopper.Kernel.Special.GH_Timer trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.Interval = 100;
                trigger.NickName = "RemoSharp";
                //RemoSharp.RemoCompSource sourceComp = new RemoSharp.RemoCompSource();
                //sourceComp.CreateAttributes();
                //sourceComp.Attributes.Pivot = sourceCompPivot;
                //var sourceGuid = sourceComp.InstanceGuid;

                //RemoSharp.RemoCompTarget targetComp = new RemoSharp.RemoCompTarget();
                //targetComp.CreateAttributes();
                //targetComp.Attributes.Pivot = targetCompPivot;

                //RemoSharp.WsClientCat.WsClientStart commandWssComp = new WsClientCat.WsClientStart();
                //commandWssComp.CreateAttributes();
                //commandWssComp.Attributes.Pivot = commandSrvButtonPivot;

                //StreamIPSet commandAddress = new StreamIPSet();
                //commandAddress.DialougeTitle.Text = "Please Set Your Command Server Address";
                //commandAddress.ShowDialog();
                //string commandSrvAddress = commandAddress.WS_Server_Address;
                //Grasshopper.Kernel.Special.GH_Panel commandServAddPanel = new Grasshopper.Kernel.Special.GH_Panel();
                //commandServAddPanel.CreateAttributes();
                //commandServAddPanel.Attributes.Pivot = commandBoundsPanelPivot;
                //commandServAddPanel.Attributes.Bounds = new System.Drawing.RectangleF(commandBoundsPanelPivot.X, commandBoundsPanelPivot.Y, 100, 20);
                //commandServAddPanel.SetUserText(commandSrvAddress);
                //commandServAddPanel.NickName = "RemoSharp";

                //Grasshopper.Kernel.Special.GH_ButtonObject commandButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                //commandButton.CreateAttributes();
                //commandButton.Attributes.Pivot = commandSrvButtonPivot;

                //RemoSharp.WsClientCat.WsClientSend commandWsSendComp = new WsClientCat.WsClientSend();
                //commandWsSendComp.CreateAttributes();
                //commandWsSendComp.Attributes.Pivot = commandWsSendPivot;

                this.OnPingDocument().ScheduleSolution(1, (GH_Document.GH_ScheduleDelegate)(doc =>
                {
                    this.OnPingDocument().AddObject(remoteServAddPanel, true);
                    this.OnPingDocument().AddObject(localServAddPanel, true);
                    this.OnPingDocument().AddObject(remoteButton, true);
                    this.OnPingDocument().AddObject(localButton, true);
                    this.OnPingDocument().AddObject((IGH_DocumentObject)remoteWssComp, true);
                    this.OnPingDocument().AddObject((IGH_DocumentObject)localWssComp, true);
                    this.OnPingDocument().AddObject(remoteWsSendComp, true);
                    this.OnPingDocument().AddObject(localWsSendComp, true);
                    this.OnPingDocument().AddObject(trigger, true);

                    //this.OnPingDocument().AddObject(sourceComp, true);
                    //this.OnPingDocument().AddObject(targetComp, true);
                    //this.OnPingDocument().AddObject(commandWssComp, true);
                    //this.OnPingDocument().AddObject(commandServAddPanel, true);
                    //this.OnPingDocument().AddObject(commandButton, true);
                    //this.OnPingDocument().AddObject(commandWsSendComp, true);


                    //this.OnPingDocument().AddObject(multiSlider, true);
                    //this.OnPingDocument().AddObject(sliderComponent, true);
                    //this.Params.Input[0].AddSource((IGH_Param)sliderComponent);
                    //this.Params.Input[2].AddSource((IGH_Param)multiSlider);

                    trigger.AddTarget(guid);
                    //trigger.AddTarget(sourceGuid);

                    remoteWssComp.Params.Input[2].AddSource((IGH_Param)remoteButton);
                    if(!address.Equals("")) remoteWssComp.Params.Input[0].AddSource((IGH_Param)remoteServAddPanel);
                    localWssComp.Params.Input[2].AddSource((IGH_Param)localButton);
                    localWssComp.Params.Input[0].AddSource((IGH_Param)localServAddPanel);

                    remoteWsSendComp.Params.Input[0].AddSource((IGH_Param)remoteWssComp.Params.Output[0]);
                    remoteWsSendComp.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);

                    localWsSendComp.Params.Input[0].AddSource((IGH_Param)localWssComp.Params.Output[0]);
                    localWsSendComp.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);

                    ////targetComp.Params.Input[0].AddSource((IGH_Param)sourceComp.Params.Output[0]);
                    //commandWssComp.Params.Input[2].AddSource((IGH_Param)commandButton);
                    //commandWssComp.Params.Input[0].AddSource((IGH_Param)commandServAddPanel);

                    //commandWsSendComp.Params.Input[0].AddSource((IGH_Param)commandWssComp.Params.Output[0]);
                    //commandWsSendComp.Params.Input[1].AddSource((IGH_Param)targetComp.Params.Output[0]);

                }));
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("CanvasBounds", "bounds", "A text based representation of the current GH_Canvas active region bounds",
            //    GH_ParamAccess.item);
            pManager.AddTextParameter("CanvasBoundsForXML", "CanvasBounds", "A text based representation of the current GH_Canvas active region bounds",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            #region Checking for GH_Canvas Zoom
            //// Checking for the Zoom Level of GH
            //var textList = new List<string>();
            //var GH_Objects = this.OnPingDocument().Objects;
            //for (int i = 0; i < GH_Objects.Count; i++)
            //{
            //    try
            //    {
            //        IGH_Component comp = (IGH_Component)GH_Objects[i];
            //        string componentType = comp.GetType().ToString();
            //        if (componentType.Equals("RemoSharp.RemoCompSource") || componentType.Equals("RemoSharp.RemoCompTarget"))
            //        {
            //            //string zoomOutMessage = "Zoom Out Please";
            //            //string zoomInMessage = "Zoom in Please";
            //            //var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
            //            //if (zoomLevel > 1)
            //            //{
            //            //    if (comp.Message != "Please Connect a Trigger")
            //            //    {
            //            //        comp.Message = zoomOutMessage;
            //            //        comp.ClearRuntimeMessages();
            //            //        comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomOutMessage);
            //            //    }
            //            //}
            //            //else if (zoomLevel < 1)
            //            //{
            //            //    if (comp.Message != "Please Connect a Trigger")
            //            //    {
            //            //        comp.ClearRuntimeMessages();
            //            //        comp.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, zoomInMessage);
            //            //        comp.Message = zoomInMessage;
            //            //    }
            //            //}
            //            //else
            //            //{
            //            if (comp.Message != "Please Connect a Trigger")
            //            {
            //                comp.ClearRuntimeMessages();
            //                comp.Message = "";
            //            }

            //            //}
            //        }
            //        textList.Add(componentType);
            //    }
            //    catch { }
            //}
            #endregion // Commented Out (obselete)

            this.Message = "Need a Trigger for RT";

            //Point3d pnt = new Point3d(8, 7, 0);
            //double manRes = 1;
            //int resolutionVal = 1;
            //bool lowRes = false;
            //bool highRes = false;
            //string address = "";

            //DA.GetData(0, ref manRes);
            //DA.GetData(1, ref resolutionVal);
            //DA.GetData(2, ref pnt);
            //DA.GetData(3, ref address);

            //if (resolutionVal == 0) lowRes = true;
            //if (resolutionVal == 2) highRes = true;

            //int x = Convert.ToInt32(pnt.X);
            //int y = Convert.ToInt32(-pnt.Y);

            //double scale = 50;
            //if (lowRes) scale = 15;
            //else if (highRes) scale = 100;
            
            //var thisCanvas = Grasshopper.Instances.ActiveCanvas;

            //// getting the active region of the grasshopper canvas
            //var thisCanvasViewPort = thisCanvas.Viewport;
            //var visRg = thisCanvasViewPort.VisibleRegion;
            //var coords1 = Convert.ToInt32((visRg.X + visRg.Width) * thisCanvasViewPort.Zoom);
            //var coords2 = Convert.ToInt32((visRg.Y + visRg.Height) * thisCanvasViewPort.Zoom);

            //int visRgX = Convert.ToInt32(visRg.X);
            //int visRgY = Convert.ToInt32(visRg.Y);

            //visRgY += 0;
            //visRgX += 0;

            //int xPos = x - 15;
            //int yPos = y + 161;

            //if (visRgX < 0)
            //{
            //    xPos += -visRgX;
            //    visRgX = 0;

            //}
            //if (visRgY < 0)
            //{
            //    yPos += -visRgY;
            //    visRgY = 0;
            //}
            //var viewPortCorners = visRgY + "," + coords2 + "," + visRgX + "," + coords1 + "," + thisCanvasViewPort.Zoom;


            //viewPortCorners += "," + xPos + "," + yPos + "," + scale + "," + manRes + "," + address;
            //// getting where the gh window is and what is its size

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

            //DA.SetData(0, viewPortCorners);
            DA.SetData(0, bnds4XML);
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