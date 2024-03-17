using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GHCustomControls;
using Grasshopper.Documentation;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace RemoSharp.RemoParams
{
    
    public class RemoParamData : GHCustomComponent
    {

        public GH_Structure<IGH_Goo> currentValue = new GH_Structure<IGH_Goo>();
        public string message = "";

        /// <summary>
        /// Initializes a new instance of the RemoParamData class.
        /// </summary>
        public RemoParamData()
          : base("RemoParamData", "Nickname",
              "Description",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {

            //label = new GHCustomControls.Label("value", "values", "value");
            //AddCustomControl(label);

        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("generic", "generic", "generic", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (currentValue == null) return;
            if (string.IsNullOrEmpty(this.Message)) this.Message = this.message;
            DA.SetDataTree(0, currentValue);
        }

        //private IEnumerable<GH_String> ConvertGooToGH_String(IList gooList)
        //{
        //    List<GH_String> list = new List<GH_String>();
        //    foreach (var itemItem in gooList)
        //    {
        //        GH_String gh_string = new GH_String();
        //        GH_Convert.ToGHString_Primary(itemItem, ref gh_string);
        //        list.Add(gh_string);
        //    }

        //    return list;
        //}



        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return RemoSharp.Properties.Resources.RemoSliderBreaker.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("BD1917DC-D4E0-4E2C-90EE-C314EAC2E267"); }
        }
    }
}