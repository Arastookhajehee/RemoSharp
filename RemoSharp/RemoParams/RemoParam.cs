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
using RemoSharp.WsClient;
using RemoSharp.WsClientCat;
using GHCustomControls;
using WPFNumericUpDown;
using System.ComponentModel;
using Rhino.NodeInCode;
using Rhino.UI;
using System.Windows.Forms;

namespace RemoSharp.RemoParams
{
    public class RemoParam : WsClientComponent
    {
        PushButton pushButton1;
        bool approximateCoords = true;
        /// <summary>
        /// Initializes a new instance of the RemoParam class.
        /// </summary>
        public RemoParam()
          : base("RemoParam", "RemoParam",
              "Syncs parameter accross connected computers.",
              "RemoParams")
        {
        }

        
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("param", "param", "parameter to be shared across computers", GH_ParamAccess.tree);
            pManager.AddBooleanParameter("resend", "resend", "resend the current parameter", GH_ParamAccess.item, false);
            pManager.AddGenericParameter("Websocket Objects", "WSC", "websocket objects", GH_ParamAccess.item);
            pManager.AddTextParameter("username","user","The username of the current GH document",GH_ParamAccess.item,"");

            pushButton1 = new PushButton("Set Up",
                        "Creates The Required WS Client Components To Broadcast Canvas Screen.", "Set Up");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (!currentValue) return;

            int sourceCompIndex = -1;
            int targetCompIndex = -1;

            this.Params.Input[2].WireDisplay = GH_ParamWireDisplay.hidden;
            this.Params.Input[3].WireDisplay = GH_ParamWireDisplay.hidden;

            this.Params.Input[2].Sources.Clear();
            this.Params.Input[3].Sources.Clear();

            var objects = this.OnPingDocument().Objects;
            for (int i = 0; i < objects.Count; i++)
            {
                var component = this.OnPingDocument().Objects[i];
                string componentType = component.GetType().ToString();
                if (componentType.Equals("RemoSharp.RemoCompSource")) sourceCompIndex = i;
                if (componentType.Equals("RemoSharp.RemoCompTarget")) targetCompIndex = i;
            }
       
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RemoCompTarget targetComp = (RemoCompTarget)objects[targetCompIndex];
                WsClientSend sendComp = (WsClientSend)targetComp.Params.Output[0].Recipients[0].Attributes.Parent.DocObject;
                this.Params.Input[2].AddSource(sendComp.Params.Input[0].Sources[0]);

                RemoCompSource sourceComp = (RemoCompSource)objects[sourceCompIndex];
                GH_Panel userPanel = (GH_Panel)sourceComp.Params.Input[0].Sources[0];
                this.Params.Input[3].AddSource(userPanel);

            });


        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter("command", "cmd", "RemoParam Command", GH_ParamAccess.item);
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

            WsObject wscObj = new WsObject();
            string remoCommandJson = "Hello World";

            if (!DA.GetData(2, ref wscObj)) return;

            if (this.Params.Input[2].Sources[0].Attributes.Parent == null ||
                !this.Params.Input[2].Sources[0].Attributes.Parent.DocObject.GetType()
                .ToString().Equals("RemoSharp.WsClientCat.WsClientStart"))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong Wiring Detected!");
            }

            if (!this.Params.Input[3].Sources[0].GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Panel"))
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong Wiring Detected!");
            }


            string username = "";
            DA.GetData(3, ref username);

            var inputCompSources = this.Params.Input[0].Sources;
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

            switch (paramType)
            {
                case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                    GH_NumberSlider slider = (GH_NumberSlider)inputComp;
                    RemoParamSlider remoSlider = new RemoParamSlider(username, slider);
                    remoCommandJson = RemoCommand.SerializeToJson(remoSlider);
                    this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                    GH_ButtonObject button = (GH_ButtonObject)inputComp;
                    RemoParamButton remoButton = new RemoParamButton(username, button);
                    remoCommandJson = RemoCommand.SerializeToJson(remoButton);
                    this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                    GH_BooleanToggle toggle = (GH_BooleanToggle)inputComp;
                    RemoParamToggle remoToggle = new RemoParamToggle(username, toggle);
                    remoCommandJson = RemoCommand.SerializeToJson(remoToggle);
                    this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_Panel"):
                    GH_Panel panel = (GH_Panel)inputComp;
                    RemoParamPanel remoPanel = new RemoParamPanel(username, panel);
                    remoCommandJson = RemoCommand.SerializeToJson(remoPanel);
                    this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                    GH_ColourSwatch colourSwatch = (GH_ColourSwatch)inputComp;
                    RemoParamColor remoColor = new RemoParamColor(username, colourSwatch);
                    remoCommandJson = RemoCommand.SerializeToJson(remoColor);
                    this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                    GH_MultiDimensionalSlider mdSlider = (GH_MultiDimensionalSlider)inputComp;
                    RemoParamMDSlider remoMDSlider = new RemoParamMDSlider(username, mdSlider, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(remoMDSlider);

                    this.Message = this.approximateCoords ? "Approximate\nto 3 decimals" : "Absolute";
                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Point"):
                    IGH_Param paramComp = (IGH_Param)inputComp;
                    Param_Point pointComponent = (Param_Point)inputComp;
                    GH_Structure<IGH_Goo> pntTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out pntTree);

                    RemoParamPoint3d points = new RemoParamPoint3d(username, pointComponent, pntTree, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(points);

                    this.Message = this.approximateCoords ? "Approximate\nto 3 decimals" : "Absolute";
                    if (!inputComp.Attributes.Selected) remoCommandJson = "";
                    if (!paramComp.Attributes.Selected) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unselected Point Component does not send data!");
                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                    Param_Vector vectorComponent = (Param_Vector)inputComp;
                    GH_Structure<IGH_Goo> vecTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out vecTree);

                    RemoParamVector3d vectors = new RemoParamVector3d(username, vectorComponent, vecTree, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(vectors);


                    this.Message = this.approximateCoords ? "Approximate\nto 3 decimals" : "Absolute";
                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                    Param_Plane planeComponent = (Param_Plane)inputComp;
                    GH_Structure<IGH_Goo> planeTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out planeTree);

                    RemoParamPlane planes = new RemoParamPlane(username, planeComponent, planeTree, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(planes);

                    this.Message = this.approximateCoords ? "Approximate\nto 3 decimals" : "Absolute";
                    break;

                default:
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Unsupported Data Type Input!\n" +
                        " Supported components:\n" +
                        "number slider, button, toggle\n" +
                        "panel, colourswatch, MDslider\n" +
                        "point, vector, plane");
                    return;
            }

            if (string.IsNullOrEmpty(remoCommandJson)) return;

            wscObj.send(remoCommandJson);

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
            get { return new Guid("78E06E35-556C-4C05-96C0-51D256F66046"); }
        }
    }
}