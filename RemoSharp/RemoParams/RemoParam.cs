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
using GHCustomControls;
using WPFNumericUpDown;
using System.ComponentModel;
using Rhino.NodeInCode;
using Rhino.UI;
using System.Windows.Forms;
using static System.Windows.Forms.AxHost;
using Grasshopper.GUI.Canvas.Interaction;
using WebSocketSharp;
using RemoSharp.WebSocketClient;
using System.Reflection.Emit;
using GH_IO.Serialization;

namespace RemoSharp.RemoParams
{
    public class RemoParam : GHCustomComponent
    {
        ToggleSwitch enableSwitch;
        //WebSocket client;
        //PushButton shareButton;
        bool approximateCoords = false;
        int setupIndex = 0;
        Guid hoverComponentGuid = Guid.Empty;
        bool mouseLeftDown = false;
        public bool enableRemoParam = true;
        bool localEnable = false;

        Guid associatedRpmData = Guid.Empty;

        public static string RemoParamKeyword = "Hold Tab or F12 to Sync";
        public static string RemoParamSelectionKeyword = "\nSelection Required";


        public Guid syncCompGuid = Guid.Empty;

        /// <summary>
        /// Initializes a new instance of the RemoParam class.
        /// </summary>
        public RemoParam()
          : base("RemoParam", "rpm",
              "Syncs parameter accross connected computers.",
              "RemoSharp", "RemoParams")
        {
            this.syncCompGuid = Guid.NewGuid();
        }

        
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("param", "param", "parameter to be shared across computers", GH_ParamAccess.tree);
            //pManager.AddGenericParameter("Websocket Objects", "WSC", "websocket objects", GH_ParamAccess.item);
            //pManager.AddTextParameter("username","user","The username of the current GH document",GH_ParamAccess.item,"");

            //shareButton = new PushButton("Set Up",
            //            "Creates The Required WS Client Components To Broadcast Canvas Screen.", "Set Up");
            //shareButton.OnValueChanged += shareButton_OnValueChanged;
            //AddCustomControl(shareButton);

            enableSwitch = new ToggleSwitch("Enable", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            
            AddCustomControl(enableSwitch);



        }



        private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            localEnable = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }


        //private void shareButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool currentValue = Convert.ToBoolean(e.Value);
        //    if (!currentValue) return;

        //    var selection = this.OnPingDocument().SelectedObjects();
        //    foreach (var item in selection)
        //    {

        //    }

        //}
        
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
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
            if (setupIndex == 0)
            {
                Grasshopper.Instances.ActiveCanvas.MouseDown += ActiveCanvas_MouseDown;
                Grasshopper.Instances.ActiveCanvas.MouseUp += ActiveCanvas_MouseUp;
                setupIndex++;
            }

            if (!enableRemoParam || !localEnable) return;

            //WsObject wscObj = new WsObject();
            string remoCommandJson = "Hello World";

            //if (!DA.GetData(2, ref wscObj)) return;

            //if (this.Params.Input[2].Sources[0].Attributes.Parent == null ||
            //    !this.Params.Input[2].Sources[0].Attributes.Parent.DocObject.GetType()
            //    .ToString().Equals("RemoSharp.RemoSetupClient"))
            //{
            //    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong Wiring Detected!");
            //}

            //if (!this.Params.Input[3].Sources[0].GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Panel"))
            //{
            //    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Wrong Wiring Detected!");
            //}


            //string username = "";
            //DA.GetData(3, ref username);

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


            var remoSetupComps = this.OnPingDocument().Objects.AsParallel().AsUnordered().Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoSetupClient")).ToList();
            if (remoSetupComps.Count != 1)
            {
                string errorString = "A single RemoSetupClient Component is required for RemoParam" +
                    "\nPLease make sure there is a single RemoSetupClient in this Grasshopper Canvas";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorString);
                return;
            }

            RemoSetupClient remoSetupClient = (RemoSetupClient)remoSetupComps[0];

            string username = remoSetupClient.username;
            string password = remoSetupClient.password;
            WebSocket client = remoSetupClient.client;

            if (!remoSetupClient.enable)
            {
                string errorString = "RemoSharp Interactions is desabled!";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, errorString);
                return;
            }

            var inputComp = inputCompSources[0];
            //inputComp.NickName = "RemoParam";

            string paramType = inputComp.GetType().ToString();

            switch (paramType)
            {
                //case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                //    GH_NumberSlider slider = (GH_NumberSlider)inputComp;
                //    RemoParamSlider remoSlider = new RemoParamSlider(username, slider);
                //    remoCommandJson = RemoCommand.SerializeToJson(remoSlider);
                    
                //    SetValuetoParamData(remoCommandJson, slider, remoSlider);

                //    break;
                //case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                //    GH_ButtonObject button = (GH_ButtonObject)inputComp;
                //    RemoParamButton remoButton = new RemoParamButton(username, button);
                //    remoCommandJson = RemoCommand.SerializeToJson(remoButton);

                //    if (hoverComponentGuid != button.InstanceGuid) remoCommandJson = "";

                //    //this.Message = "";
                //    break;
                //case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                //    GH_BooleanToggle toggle = (GH_BooleanToggle)inputComp;
                //    RemoParamToggle remoToggle = new RemoParamToggle(username, toggle);
                //    remoCommandJson = RemoCommand.SerializeToJson(remoToggle);

                //    if (hoverComponentGuid != toggle.InstanceGuid) remoCommandJson = "";

                //    //this.Message = "";
                //    break;
                case ("Grasshopper.Kernel.Special.GH_Panel"):
                case ("Grasshopper.Kernel.Parameters.Param_String"):

                    //GH_Structure<GH_String> textInput = new GH_Structure<GH_String>();
                    //DA.GetDataTree(0, out textInput);

                    //Param_String paramString = (Param_String)inputComp;
                    ////RemoParamText remoText = new RemoParamText(username, paramString, textInput, paramString );
                    //remoCommandJson = RemoCommand.SerializeToJson(remoText);
                    //this.Message = "";
                    break;
                case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                    GH_ColourSwatch colourSwatch = (GH_ColourSwatch)inputComp;
                    RemoParamColor remoColor = new RemoParamColor(username, colourSwatch);
                    remoCommandJson = RemoCommand.SerializeToJson(remoColor);

                    break;
                case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                    GH_MultiDimensionalSlider mdSlider = (GH_MultiDimensionalSlider)inputComp;
                    RemoParamMDSlider remoMDSlider = new RemoParamMDSlider(username, mdSlider, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(remoMDSlider);

                    bool interactingWithMDSlider = hoverComponentGuid == mdSlider.InstanceGuid && mouseLeftDown;
                    if (!interactingWithMDSlider) remoCommandJson = "";

                    //this.Message = this.approximateCoords ? "Round to 3 decimals" : "Absolute";
                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Point"):
                    IGH_Param paramComp = (IGH_Param)inputComp;
                    Param_Point pointComponent = (Param_Point)inputComp;

                    if (!pointComponent.Attributes.Selected || pointComponent.SourceCount > 0) return;
                   
                    //RemoParamPoint3d points = new RemoParamPoint3d(username, pointComponent, pntTree, this.approximateCoords);
                    RemoParamPoint3d remoPoint = new RemoParamPoint3d(username, pointComponent);
                    remoCommandJson = RemoCommand.SerializeToJson(remoPoint);


                    SetValuetoParamData(remoCommandJson, pointComponent, remoPoint);

                    //this.Message = this.approximateCoords ? "Round to 3 decimals" : "Absolute";

                    //if (!inputComp.Attributes.Selected) remoCommandJson = "";
                    //if (!paramComp.Attributes.Selected) this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Unselected Point Component does not send data!");

                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                    Param_Vector vectorComponent = (Param_Vector)inputComp;
                    GH_Structure<IGH_Goo> vecTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out vecTree);

                    RemoParamVector3d vectors = new RemoParamVector3d(username, vectorComponent, vecTree, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(vectors);


                    //this.Message = this.approximateCoords ? "Round to 3 decimals" : "Absolute";
                    break;
                case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                    Param_Plane planeComponent = (Param_Plane)inputComp;
                    GH_Structure<IGH_Goo> planeTree = new GH_Structure<IGH_Goo>();
                    DA.GetDataTree<IGH_Goo>(0, out planeTree);

                    RemoParamPlane planes = new RemoParamPlane(username, planeComponent, planeTree, this.approximateCoords);
                    remoCommandJson = RemoCommand.SerializeToJson(planes);

                    //this.Message = this.approximateCoords ? "Round to 3 decimals" : "Absolute";
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

            try
            {
                client.Send(remoCommandJson);
            }
            catch
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Connection Problem! Please check the RemoSetupClient Component!");
                return;


            }


        }

        private string SetValuetoParamData(string remoCommandJson, GH_NumberSlider slider, RemoParamSlider remoSlider)
        {

            var dataComps = this.OnPingDocument().Objects.Where(obj => obj is GH_Component).Select(obj => (GH_Component)obj).ToList();
            //&& !obj.GetType().Equals("RemoSharp.RemoParams.RemoParam")).ToList();
            //.Select(obj => (RemoParamData) obj).ToList()[0];

            var dataComp = dataComps.Where(obj => obj.Message != null).ToList();

            var dataComponent = dataComp.Where(obj => obj.Message.Contains(this.Message)).ToList();

            var dataC = dataComponent.Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParamData")).ToList();

            var remoParamDataComponent = (RemoSharp.RemoParams.RemoParamData)dataC[0];

            
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                ////var gh_components = this.OnPingDocument().Objects.Select(tempComponent => tempComponent.InstanceGuid).ToList();
                //RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(sliderComp.InstanceGuid);
                //remoParamComp.enableRemoParam = false;

                remoParamDataComponent.remoSliderValue = Convert.ToDouble(remoSlider.sliderValue);
                remoParamDataComponent.ExpireSolution(false);

                //remoParamComp.enableRemoParam = true;
            });
            return remoCommandJson;
        }

        private string SetValuetoParamData(string remoCommandJson, Param_Point point, RemoParamPoint3d remoPoint)
        {

            var dataComps = this.OnPingDocument().Objects.Where(obj => obj is GH_Component).Select(obj => (GH_Component)obj).ToList();
            //&& !obj.GetType().Equals("RemoSharp.RemoParams.RemoParam")).ToList();
            //.Select(obj => (RemoParamData) obj).ToList()[0];

            var dataComp = dataComps.Where(obj => obj.Message != null).ToList();

            var dataComponent = dataComp.Where(obj => obj.Message.Contains(this.Message)).ToList();

            var dataC = dataComponent.Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParamData")).ToList();

            var remoParamDataComponent = (RemoSharp.RemoParams.RemoParamData)dataC[0];


            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                ////var gh_components = this.OnPingDocument().Objects.Select(tempComponent => tempComponent.InstanceGuid).ToList();
                //RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(sliderComp.InstanceGuid);
                //remoParamComp.enableRemoParam = false;

                remoParamDataComponent.valueType = CommandType.RemoPoint3d;
                remoParamDataComponent.points = point.PersistentData;
                remoParamDataComponent.ExpireSolution(true);

                //remoParamComp.enableRemoParam = true;
            });
            return remoCommandJson;
        }

        public void ActiveCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (this == null || this.OnPingDocument() == null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseDown -= ActiveCanvas_MouseDown;
                Grasshopper.Instances.ActiveCanvas.MouseUp -= ActiveCanvas_MouseUp;
            }

            if (e.Button == MouseButtons.Left)
            {
                mouseLeftDown = false;
            }
        }
        public void ActiveCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (this == null || this.OnPingDocument() == null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseDown -= ActiveCanvas_MouseDown;
                Grasshopper.Instances.ActiveCanvas.MouseUp -= ActiveCanvas_MouseUp;
            }
            if (e.Button == MouseButtons.Left)
            {
                float[] mouseCoords = PointFromCanvasMouseInteraction(Grasshopper.Instances.ActiveCanvas.Viewport, e);
                var hoverObject = this.OnPingDocument().FindObject(new System.Drawing.PointF(mouseCoords[0], mouseCoords[1]), 1);
                if (hoverObject == null) hoverComponentGuid = Guid.Empty;
                else
                {
                    hoverComponentGuid = hoverObject.InstanceGuid;
                    mouseLeftDown = true;
                }
            }
            else
            {
                hoverComponentGuid = Guid.Empty;
            }
        }   
            
        

        float[] PointFromCanvasMouseInteraction(Grasshopper.GUI.Canvas.GH_Viewport vp, MouseEventArgs e)
        {
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            float x = mouseEvent.CanvasX;
            float y = mouseEvent.CanvasY;
            float[] coords = { x, y };
            return coords;
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