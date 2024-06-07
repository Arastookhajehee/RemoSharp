using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.Utilities
{
    public class SingleTrueFilter : GH_Component
    {
        int counter = 0;
        /// <summary>
        /// Initializes a new instance of the SingleTrueFilter class.
        /// </summary>
        public SingleTrueFilter()
          : base("SingleTrueFilter", "SingleT",
              "To prevent multiple true outputs for high-frequency update operations, it only sends out a single true with the push of the button",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Trigger", "T", "Trigger to filter counter", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("Boolean", "B", "Boolean to filter", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("Boolean", "B", "Filtered boolean", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool b = false;
            if (!DA.GetData(1, ref b)) return;

            if (b)
            {
                counter++;
                if (counter == 100) counter = 5;
                if (counter == 1)
                {
                    DA.SetData(0, true);
                }
                else
                {
                    DA.SetData(0, false);
                }
            }
            else
            {
                counter = 0;
                DA.SetData(0, false);
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
                return RemoSharp.Properties.Resources.RemoButton.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("BA3AA3DF-4C3F-4D03-B97C-E346C36BF151"); }
        }
    }
}