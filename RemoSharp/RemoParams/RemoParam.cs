using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using RemoSharp.RemoCommandTypes;
using Rhino.Geometry;

namespace RemoSharp.RemoParams
{
    public class RemoParam : GH_Component
    {
        bool approximateCoords = true;
        /// <summary>
        /// Initializes a new instance of the RemoParam class.
        /// </summary>
        public RemoParam()
          : base("RemoParam", "RemoParam",
              "Syncs parameter accross connected computers.",
              "RemoSharp", "RemoParams")
        {
        }

        
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("username","user","The username of the current GH document",GH_ParamAccess.item,"");
            pManager.AddGenericParameter("param", "param", "parameter to be shared across computers", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("resend", "resend", "resend the current parameter", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("command", "cmd", "RemoParam Command", GH_ParamAccess.item);
        }

        // http://james-ramsden.com/append-menu-items-to-grasshopper-components-with-c/
        protected override void AppendAdditionalComponentMenuItems(System.Windows.Forms.ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Approximate", Approximate_Menu);
            Menu_AppendItem(menu, "Absolute", Absolute_Menu);
        }

        private void Approximate_Menu(object sender, EventArgs e)
        {
            approximateCoords = true;
            this.ExpireSolution(true);
        }
        private void Absolute_Menu(object sender, EventArgs e)
        {
            approximateCoords = false;
            this.ExpireSolution(true);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // find the username panel on the grasshopper canvas
            if (this.Params.Input[0].Sources.Count == 0)
            {
                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
                    foreach (var component in this.OnPingDocument().Objects)
                    {
                        string componentType = component.GetType().ToString();
                        if (componentType.Equals("RemoSharp.RemoCompSource"))
                        {
                            RemoSharp.RemoCompSource sourceComp = (RemoSharp.RemoCompSource)component;
                            GH_Panel userPanel = (GH_Panel)sourceComp.Params.Input[0].Sources[0];
                            this.Params.Input[0].AddSource(userPanel);
                            break;
                        }
                    }

                });
            }

            string username = "";
            DA.GetData(0, ref username);

            var inputCompSources = this.Params.Input[1].Sources;
            if (inputCompSources.Count == 0)
            {
                this.Message = "";
                return;
            }
            else if (inputCompSources.Count > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please connect only one data source");
                return;
            }
            var inputComp = inputCompSources[0];

            string paramType = inputComp.GetType().ToString();

            string jsonCommand = "";

            switch (paramType)
            {
                case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                    GH_NumberSlider slider = (GH_NumberSlider)inputComp;
                    RemoParamSlider remoParam = new RemoParamSlider(username, slider);
                    jsonCommand = RemoCommand.SerializeToJson(remoParam);
                    DA.SetData(0, jsonCommand);
                    this.Message = "";
                    return;
                case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                    GH_ButtonObject button = (GH_ButtonObject)inputComp;
                    RemoParamButton remoButton = new RemoParamButton(username, button);
                    jsonCommand = RemoCommand.SerializeToJson(remoButton);
                    DA.SetData(0, jsonCommand);
                    this.Message = "";
                    return;
                case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                    GH_BooleanToggle toggle = (GH_BooleanToggle)inputComp;
                    RemoParamToggle remoToggle = new RemoParamToggle(username, toggle);
                    jsonCommand = RemoCommand.SerializeToJson(remoToggle);
                    DA.SetData(0, jsonCommand);
                    this.Message = "";
                    return;
                case ("Grasshopper.Kernel.Special.GH_Panel"):
                    GH_Panel panel = (GH_Panel)inputComp;
                    RemoParamPanel remoPanel = new RemoParamPanel(username, panel);
                    jsonCommand = RemoCommand.SerializeToJson(remoPanel);
                    DA.SetData(0, jsonCommand);
                    this.Message = "";
                    return;
                case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                    GH_ColourSwatch colourSwatch = (GH_ColourSwatch)inputComp;
                    RemoParamColor remoColor = new RemoParamColor(username, colourSwatch);
                    jsonCommand = RemoCommand.SerializeToJson(remoColor);
                    DA.SetData(0, jsonCommand);
                    this.Message = "";
                    return;
                case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                    GH_MultiDimensionalSlider mdSlider = (GH_MultiDimensionalSlider)inputComp;
                    RemoParamMDSlider remoMDSlider = new RemoParamMDSlider(username, mdSlider, this.approximateCoords);
                    jsonCommand = RemoCommand.SerializeToJson(remoMDSlider);
                    DA.SetData(0, jsonCommand);

                    this.Message = this.approximateCoords ? "Approximate" : "Absolute";
                    return;
                case ("Grasshopper.Kernel.Parameters.Param_Point"):
                    Param_Point pointComponent = (Param_Point)inputComp;
                    GH_Structure<IGH_Goo> pntTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out pntTree);

                    RemoParamPoint3d points = new RemoParamPoint3d(username, pointComponent, pntTree, this.approximateCoords);
                    jsonCommand = RemoCommand.SerializeToJson(points);
                    DA.SetData(0, jsonCommand);

                    this.Message = this.approximateCoords ? "Approximate" : "Absolute";
                    return;
                case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                    Param_Vector vectorComponent = (Param_Vector)inputComp;
                    GH_Structure<IGH_Goo> vecTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out vecTree);

                    RemoParamVector3d vectors = new RemoParamVector3d(username, vectorComponent, vecTree, this.approximateCoords);
                    jsonCommand = RemoCommand.SerializeToJson(vectors);
                    DA.SetData(0, jsonCommand);

                    this.Message = this.approximateCoords ? "Approximate" : "Absolute";
                    return;
                case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                    Param_Plane planeComponent = (Param_Plane)inputComp;
                    GH_Structure<IGH_Goo> planeTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out planeTree);

                    RemoParamPlane planes = new RemoParamPlane(username, planeComponent, planeTree, this.approximateCoords);
                    jsonCommand = RemoCommand.SerializeToJson(planes);
                    DA.SetData(0, jsonCommand);

                    this.Message = this.approximateCoords ? "Approximate" : "Absolute";
                    return;

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
            get { return new Guid("78E06E35-556C-4C05-96C0-51D256F66046"); }
        }
    }
}