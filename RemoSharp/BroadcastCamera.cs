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

namespace RemoSharp
{
    public class BroadcastCamera : GH_Component
    {
        IGH_Component Component;
        Rhino.RhinoDoc rhinoDoc;
        /// <summary>
        /// Initializes a new instance of the BroadcastCamera class.
        /// </summary>
        public BroadcastCamera()
          : base("Broadcast Camera", "Cam_Broadcast",
              "Retrieves info from a Viewport in Rhino and broadcasts it as a string.",
              "RemoSharp", "CameraTools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("ReferenceView", "RefView", "The viewport to be broadcasted.", GH_ParamAccess.item, "Perspective");
            pManager.AddTextParameter("TargetView", "TgtView", "The target viewport to be synced.", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("Update", "Update", "Single update of the viewports", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("State", "state", "Result of Camera Sync", GH_ParamAccess.item);
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