using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.Utilities
{
    public class UniqueValues : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the UniqueValues class.
        /// </summary>
        public UniqueValues()
          : base("UniqueValues", "uniques",
              "Description",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("valueList", "list", "list", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("uniques", "unqs", "uniques", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> list = new List<string>();
            DA.GetDataList(0,list);

            List<string> uniques = new List<string>();
            foreach (var item in list)
            {
                if (!uniques.Contains(item))
                {
                    uniques.Add(item);
                }
            }
            DA.SetDataList(0,uniques);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8C14ACDB-1481-418A-853D-C01551AAD571"); }
        }
    }
}