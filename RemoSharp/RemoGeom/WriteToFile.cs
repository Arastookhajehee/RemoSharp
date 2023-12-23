using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp.RemoGeom
{
    public class WriteToFile : GH_Component
    {

        int counter = 0;

        /// <summary>
        /// Initializes a new instance of the WriteToFile class.
        /// </summary>
        public WriteToFile()
          : base("WriteToFile", "WriteToFile",
              "WriteToFile",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Write", "Write", "", GH_ParamAccess.item, false);
            pManager.AddTextParameter("content", "txtContent","", GH_ParamAccess.item);
            pManager.AddTextParameter("filePath", "filePath", "", GH_ParamAccess.item);
        }

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
            bool writeToFile = false;
            string content = "";
            string filePath = "";

            DA.GetData(0, ref writeToFile);
            DA.GetData(1, ref content);
            DA.GetData(2,ref filePath);


            if (!writeToFile)
            {
                counter = 0;
                return;
            }

            if (counter == 0)
            {


                if (!File.Exists(filePath))
                {
                    File.CreateText(filePath);
                }

                // Use a StreamWriter to write the content to the file
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.Write(content);
                }
                counter++;
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("30BCEE62-7940-480A-85B0-0A807E6BDE90"); }
        }
    }
}