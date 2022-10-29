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
            pManager.AddBooleanParameter("Set", "Set", "Trigger a Slider value change on the main remote GH_Canvas", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Slider", "Sldr", "Changing the value of a Slider on the main remote GH_Canvas", GH_ParamAccess.item, 0);
            pManager.AddTextParameter("Username_ID", "user", "The name of this canvas", GH_ParamAccess.item);

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
            string username = "";
            DA.GetData(0, ref RUNComp);
            DA.GetData(1, ref value);
            DA.GetData(2, ref username);

            int coordX = Convert.ToInt32(this.Component.Attributes.Pivot.X) -30;
            int coordY = Convert.ToInt32(this.Component.Attributes.Pivot.Y) - 40;

            if (this.Params.Input[1].Sources.Count < 1) return;

            if (RUNComp)
            {

                Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider)this.Params.Input[1].Sources[0];
                decimal minBound = sliderComponent.Slider.Minimum;
                decimal maxBound = sliderComponent.Slider.Maximum;
                decimal currentValue = sliderComponent.Slider.Value;
                int accuracy = sliderComponent.Slider.DecimalPlaces;
                var sliderType = sliderComponent.Slider.Type;

                Guid sliderGuid = this.Params.Input[1].Sources[0].InstanceGuid;

                string outputData = "ID_" + username + ",RemoParam,"
                    + coordX + ","
                    + coordY + ","
                    + "AddValueToSlider";
                //  command Index -->   4               5                  6                  7                 8                   9
                outputData += "," + minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType + "," + sliderGuid.ToString();

                DA.SetData(0, outputData);
                return;
            }

            DA.SetData(0, "");
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
                return RemoSharp.Properties.Resources.RemoSlider.ToBitmap();
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