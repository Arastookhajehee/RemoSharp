using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using ScriptComponents;
using RemoSharp.RemoCommandTypes;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace RemoSharp
{
    public class RemoSharpComponent : GH_Component
    {

        public string prevScript = "";
        RemoSetupClient client = null;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RemoSharpComponent()
          : base("RemoSharp", "Remos",
              "This tool converts text to executable programs within grasshopper.",
              "RemoSharp", "RemoSharpScript")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Update CScomponent", "updateCS", "Updates the created component with every input data change.", GH_ParamAccess.item);
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (this.Params.Input[0].SourceCount == 0) return;
            if (this.client== null)
            {
                this.client = (RemoSetupClient) this.OnPingDocument().Objects.Where(obj => obj is RemoSharp.RemoSetupClient).FirstOrDefault();
            }
            ScriptComponents.Component_CSNET_Script csComp = (ScriptComponents.Component_CSNET_Script) this.Params.Input[0].Sources[0].Attributes.Parent.DocObject;

            string currentScript = string.Format("{0}{1}{2}", csComp.ScriptSource.UsingCode, csComp.ScriptSource.ScriptCode, csComp.ScriptSource.AdditionalCode);
            if (string.IsNullOrEmpty(currentScript)) return;

            if (prevScript != currentScript)
            {
                RemoScriptCS remoscript = new RemoScriptCS(client.username, csComp);
                string json = RemoCommand.SerializeToJson(remoscript);
                client.client.Send(json);
            }

        }

        
                
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                
                return RemoSharp.Properties.Resources.RemoSharp.ToBitmap();
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0802d15a-354c-432f-8f96-69a948d23d95"); }
        }
    }
}
