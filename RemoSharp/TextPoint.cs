using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class TextPoint : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the TextPoint class.
        /// </summary>
        public TextPoint()
          : base("TextPoint", "TextPoint",
              "Replaces certain text in a script with point coordinates",
              "RemoSharp", "Inputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input_Script", "Script", "The script containing point names to be replaced", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Point_Names", "Pnt_Ns", "List of points names in the inputScript to be replaced.", GH_ParamAccess.list);
            pManager.AddPointParameter("Point_Inputs", "Pnts", "List of points from which the coordinates are extracted, replacing the point names.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output_Script", "Script", "The script text from which the point names are replaced.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string Input_Script = "";
            List<string> PointNames = new List<string>();
            List<Point3d> Point_Inputs = new List<Point3d>();

            DA.GetData(0, ref Input_Script);
            DA.GetDataList(1, PointNames);
            DA.GetDataList(2, Point_Inputs);


            if (PointNames.Count != Point_Inputs.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The count of point names and the points do not match!");
            }
            else
            {
                for (int i = 0; i < PointNames.Count; i++)
                {

                    Point3d Point_Input = Point_Inputs[i];

                    double xVal = Point_Input.X;
                    double yVal = Point_Input.Y;
                    double zVal = Point_Input.Z;

                    string newPntString = "new Point3d(" + xVal + ", " + yVal + ", " + zVal + ")";

                    Input_Script = Input_Script.Replace(PointNames[i], newPntString);
                }

                DA.SetData(0, Input_Script);
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
                return RemoSharp.Properties.Resources.Text_Point.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4e899454-4040-4298-8d19-1a51d3ec64ed"); }
        }
    }
}