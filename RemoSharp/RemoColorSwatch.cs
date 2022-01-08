using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using System.Drawing;

namespace RemoSharp
{
    public class RemoColorSwatch : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the RemoColorSwatch class.
        /// </summary>
        public RemoColorSwatch()
          : base("RemoColor", "RemoClr",
              "Remote changing a color swatch",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Set", "Set", "Trigger a Colour Swatch value change on the main remote GH_Canvas", GH_ParamAccess.item, false);
            pManager.AddColourParameter("ColourSwatch", "Color", "Changing a Colour Swatch a panel on the main remote GH_Canvas", GH_ParamAccess.item, Color.White);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("ColorCommand", "ClrCmd", "Command to trigger changing a Colour Swatch on the main remote GH_Canvas", GH_ParamAccess.item);
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
            Color color = Color.White;

            DA.GetData(0, ref RUNComp);
            DA.GetData(1, ref color);

            if (RUNComp)
            {
                string outputData = "RemoParam,"
                    + Convert.ToInt32(this.Component.Attributes.Pivot.X) + ","
                    + Convert.ToInt32(this.Component.Attributes.Pivot.Y) + ","
                    + "ColorSwatchChange," + color.R + "," + color.G + "," + color.B + "," + color.A;
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
                return RemoSharp.Properties.Resources.RemoColor.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("d133ad88-c2d4-402c-9479-67e423e2e4ce"); }
        }
    }
}