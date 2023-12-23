using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GHCustomControls;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace RemoSharp.RemoParams
{
    
    public class RemoParamData : GHCustomComponent
    {
        public double remoSliderValue = 0;
        public bool booleanValue = false;
        public string textValue = "";
        public System.Drawing.Color colorValue = System.Drawing.Color.Black;
        public Point3d pointValue = Point3d.Unset;
        public GH_Structure<GH_Point> points;
        public Plane planeValue = Plane.Unset;
        public RemoCommandTypes.CommandType valueType = RemoCommandTypes.CommandType.RemoParamNone;

        int maxlabellength = 15;

        GHCustomControls.Label label;

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

            label = new GHCustomControls.Label("value", "values", "value");
            AddCustomControl(label);

        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("RemoParameter", "RemoParameter", "placeholder for shared params", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            switch (valueType)
            {
                case (RemoCommandTypes.CommandType.RemoSlider):
                    this.label.CurrentValue = Math.Round(remoSliderValue,3).ToString();
                    this.label.Refresh();
                    DA.SetData(0, remoSliderValue);
                    break;
                case RemoCommandTypes.CommandType.RemoButton:
                case RemoCommandTypes.CommandType.RemoToggle:
                    this.label.CurrentValue = booleanValue.ToString();
                    this.label.Refresh();
                    DA.SetData(0, booleanValue);
                    break;
                case RemoCommandTypes.CommandType.RemoPanel:
                    this.label.CurrentValue = textValue.Substring(0, maxlabellength);
                    this.label.Refresh();
                    DA.SetData(0, textValue);
                    break;
                case RemoCommandTypes.CommandType.RemoColor:
                    this.label.CurrentValue = colorValue.ToString();
                    this.label.Refresh();
                    DA.SetData(0, colorValue);
                    break;
                case RemoCommandTypes.CommandType.RemoMDSlider:
                    double[] values = new double[3]{pointValue.X,pointValue.Y,pointValue.Z}.Select(obj => Math.Round(obj,3)).ToArray();
                    string displayValue = string.Format("{0},{1},{2]", values[0], values[1], values[2]).Substring(0, maxlabellength);
                    this.label.CurrentValue = displayValue;
                    this.label.Refresh();
                    DA.SetData(0, displayValue);
                    break;
                case RemoCommandTypes.CommandType.RemoPoint3d:
                    DA.SetData(0, points);
                    break;
                case RemoCommandTypes.CommandType.RemoVector3d:
                    break;
                case RemoCommandTypes.CommandType.RemoPlane:
                    break;
                default:
                    break;
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
            get { return new Guid("BD1917DC-D4E0-4E2C-90EE-C314EAC2E267"); }
        }
    }
}