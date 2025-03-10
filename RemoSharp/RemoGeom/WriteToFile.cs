﻿using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace RemoSharp.RemoGeom
{
    public class WriteToFile : GH_Component
    {
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
                return;
            }

            if (!File.Exists(filePath))
            {
                File.CreateText(filePath);
            }


            //write the content into the file with UTF-8 formatting
           using (StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.UTF8))
            {
                sw.Write(content);
                sw.Close();
            }



            //// Use a StreamWriter to write the content to the file
            //using (StreamWriter writer = new StreamWriter(filePath))
            //{
            //    // write content to file with UTF8 formatting
            //    writer.Write(content,)
                 
            //}
            
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
                return RemoSharp.Properties.Resources.WriteToFile.ToBitmap();
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