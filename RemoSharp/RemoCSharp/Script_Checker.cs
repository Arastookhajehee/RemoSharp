using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class Script_Checker : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Script_Checker class.
        /// </summary>
        public Script_Checker()
          : base("Script_Checker", "Sc_Check",
              "Checks if the command for changing the script is the keep alive message or an actual change of the script.",
              "RemoSharp", "Com_Tools")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Script", "Script", "Script to be read and checked.", GH_ParamAccess.item, "");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Out_Script", "Out_Scr", "Checked text.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string Script = "";
            DA.GetData(0, ref Script);
            
            if (Script != "YAY! Still Friends!")
            {

                currentString = Script;
            }

            DA.SetData(0, currentString);
        }

        public string currentString = "";
        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return RemoSharp.Properties.Resources.Script_Checker.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5bc9bb9e-73b4-4501-b410-10634fedccff"); }
        }
    }
}