using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class RemoSlider : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the RemoSlider class.
        /// </summary>
        public RemoSlider()
          : base("RemoSlider", "RemoSldr",
              "Remote changing a slider",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Set", "Set", "Trigger a Slider value change on the main remote GH_Canvas", GH_ParamAccess.item, false);
            pManager.AddNumberParameter("Slider", "Sldr", "Changing the value of a Slider on the main remote GH_Canvas", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("SliderCommand", "SldrCmd", "Command to trigger changing a Slider on the main remote GH_Canvas", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            bool RUNComp = false;
            double value = 0;
            DA.GetData(0, ref RUNComp);
            DA.GetData(1, ref value);

            if (RUNComp)
            {
                string outputData = "RemoParam,"
                    + Convert.ToInt32(this.Component.Attributes.Pivot.X) + ","
                    + Convert.ToInt32(this.Component.Attributes.Pivot.Y) + ","
                    + "AddValueToSlider," + value;
                DA.SetData(0, outputData);
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
            get { return new Guid("205b9f55-2dc3-4fbf-941f-9f3b6cf29a2a"); }
        }
    }
}