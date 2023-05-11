using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using Rhino;

using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using ScriptComponents;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp
{
    public class BroadcastCamera : GHCustomComponent
    {
        IGH_Component Component;
        PushButton pushButton1;
        /// <summary>
        /// Initializes a new instance of the BroadcastCamera class.
        /// </summary>
        public BroadcastCamera()
          : base("Client Camera", "Client_Cam",
              "Retrieves info from a viewport in the client's Rhino and broadcasts it as a string.",
              "RemoSharp", "BroadcastTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pushButton1 = new PushButton("WS_Client",
                "Creates The Required WS Client Components To Broadcast Canvas Screen.", "WS_Client");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddTextParameter("ReferenceView", "RefView", "The viewport to be broadcasted.", GH_ParamAccess.item, "Perspective");
            pManager.AddTextParameter("TargetView", "TgtView", "The target viewport to be synced.", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Update", "Update", "Single update of the viewports", GH_ParamAccess.item, false);
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 215, pivot.Y - 126);
                System.Drawing.PointF togglePivot = new System.Drawing.PointF(pivot.X - 251, pivot.Y + 15);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 20, pivot.Y - 90);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 189, pivot.Y - 99);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 353, pivot.Y - 2);

                System.Drawing.PointF panelSourcePivot = new System.Drawing.PointF(pivot.X - 404, pivot.Y - 30);
                System.Drawing.PointF panelTargetPivot = new System.Drawing.PointF(pivot.X - 404, pivot.Y - 7);


                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

                Grasshopper.Kernel.Special.GH_Panel panelSource = new Grasshopper.Kernel.Special.GH_Panel();
                panelSource.CreateAttributes();
                panelSource.Attributes.Pivot = panelSourcePivot;
                panelSource.Attributes.Bounds = new System.Drawing.RectangleF(panelSourcePivot.X, panelSourcePivot.Y, 300, 20);
                panelSource.SetUserText("Perspective");

                Grasshopper.Kernel.Special.GH_Panel panelTarget = new Grasshopper.Kernel.Special.GH_Panel();
                panelTarget.CreateAttributes();
                panelTarget.Attributes.Pivot = panelTargetPivot;
                panelTarget.Attributes.Bounds = new System.Drawing.RectangleF(panelTargetPivot.X, panelTargetPivot.Y, 300, 20);
                panelTarget.SetUserText("Top");


                Grasshopper.Kernel.Special.GH_BooleanToggle toggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                toggle.CreateAttributes();
                toggle.Attributes.Pivot = togglePivot;

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
                    this.OnPingDocument().AddObject(toggle, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);
                    this.OnPingDocument().AddObject(panelSource, true);
                    this.OnPingDocument().AddObject(panelTarget, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[1]);
                    this.Params.Input[0].AddSource((IGH_Param)panelSource);
                    this.Params.Input[1].AddSource((IGH_Param)panelTarget);
                    this.Params.Input[2].AddSource((IGH_Param)toggle);
                });

            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("State", "hoverComponentGuid", "Result of Camera Sync", GH_ParamAccess.item);
            pManager.AddTextParameter("CameraInfo", "camInfo", "Viewport info", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            Rhino.RhinoDoc rhinoDoc = Rhino.RhinoDoc.ActiveDoc;

            string refrenceView = "";
            string targetView = "";
            bool update = false;

            // defining the outputs
            string refrenceCam = "";
            string state = "";

            DA.GetData(0, ref refrenceView);
            DA.GetData(1, ref targetView);
            DA.GetData(2, ref update);

            this.Component.Message = "Need a Trigger for RT";
            

            int refIndex = -1;
            
            var camera = rhinoDoc.Views.GetViewList(true, false);
            for (int i = 0; i < camera.Length; i++)
            {
                var tempView = camera[i];
                string viewName = tempView.ActiveViewport.Name;
                if (viewName == refrenceView)
                {
                    refIndex = i;
                }
            }
            
            try
            {
                var refCam = camera[refIndex];

                Plane camPlane;
                refCam.ActiveViewport.GetCameraFrame(out camPlane);
                double focalLength = refCam.ActiveViewport.Camera35mmLensLength;
                Point3d camPos = camPlane.Origin;
                Vector3d camDir = refCam.ActiveViewport.CameraDirection;

                // making sure the projectionMode is the same
                int projectionMode = 0;
                if (refCam.ActiveViewport.IsParallelProjection) { projectionMode = 0; }
                if (refCam.ActiveViewport.IsPerspectiveProjection) { projectionMode = 1; }
                if (refCam.ActiveViewport.IsPlanView) { projectionMode = 2; }
                if (refCam.ActiveViewport.IsTwoPointPerspectiveProjection) { projectionMode = 3; }
                string camLocationPntVec = Rounder(camPos.X) + "," + Rounder(camPos.Y) + "," + Rounder(camPos.Z) + "," + Rounder(camDir.X) + "," + Rounder(camDir.Y) + "," + Rounder(camDir.Z) + "," + Rounder(focalLength);
                string camLocation = targetView + "," + camLocationPntVec + "," + projectionMode;
                if (update)
                {
                    refrenceCam = camLocation;
                    state = "Camera Location Updated!";
                }

            }
            catch
            {
                state = "Reference Viewport Name or Target Viewport Name Is Invalid!";
                if (refIndex == -1)
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Reference Viewport Does Not Exist!");
                }
            }

            DA.SetData(0, state);
            DA.SetData(1, refrenceCam);

        }

        public string Rounder(double number)
        {
            double roundNum = Math.Round(number, 3);
            return roundNum.ToString();
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
                return RemoSharp.Properties.Resources.Broadcast_Camera.ToBitmap();

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("c4e8c449-e556-45c8-9171-5cc3cab6c07e"); }
        }
    }
}