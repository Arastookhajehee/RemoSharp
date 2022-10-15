using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.Utilities
{
    public class RotatePlane : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RotatePlane class.
        /// </summary>
        public RotatePlane()
          : base("RotatePlane", "RotPl",
              "Rotates a plane based on 3 axis rotations (local/global)",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "plane", "Plane to be rotated", GH_ParamAccess.item);
            pManager.AddNumberParameter("X", "X", "rotation angle in degrees around the X axis", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Y", "Y", "rotation angle in degrees around the Y axis", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Y", "Z", "rotation angle in degrees around the Z axis", GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Flip", "flip", "Flip PLane Direction", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Global", "glb", "Toggle between rotation around local or Global XYZ axes", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPlaneParameter("Plane", "plane", "Rotated Plane", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane pl = Plane.WorldXY;
            double x = 0;
            double y = 0;
            double z = 0;
            bool flip = false;
            bool global = false;
            DA.GetData(0, ref pl);
            DA.GetData(1, ref x);
            DA.GetData(2, ref y);
            DA.GetData(3, ref z);
            DA.GetData(4, ref flip);
            DA.GetData(5, ref global);

            if (global)
            {
                pl.Rotate(Math.PI * x / 180.0, Vector3d.XAxis, pl.Origin);
                pl.Rotate(Math.PI * y / 180.0, Vector3d.YAxis, pl.Origin);
                pl.Rotate(Math.PI * z / 180.0, Vector3d.ZAxis, pl.Origin);
            }
            else
            {
                pl.Rotate(Math.PI * x / 180.0, pl.XAxis, pl.Origin);
                pl.Rotate(Math.PI * y / 180.0, pl.YAxis, pl.Origin);
                pl.Rotate(Math.PI * z / 180.0, pl.ZAxis, pl.Origin);
            }

            if (flip) pl.Flip();

            DA.SetData(0, pl);
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
                return RemoSharp.Properties.Resources.RotatePlane.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("DDA1A317-D244-4A89-A00B-510C65263D96"); }
        }
    }
}