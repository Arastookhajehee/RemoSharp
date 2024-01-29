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
using Grasshopper.Documentation;
using Grasshopper;

namespace RemoSharp.RemoParams
{
    public class RemoParam : GHCustomComponent
    {
        //WebSocket client;
        //PushButton shareButton;
        public bool enableRemoParam = true;

        Guid associatedRpmData = Guid.Empty;

        public static string RemoParamKeyword = "Hold Tab or F12 to Sync";
        public static string RemoParamSelectionKeyword = "\nSelection Required";


        string username = "";
        string password = "";
        RemoSetupClient remoSetupClient = null;


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

            //enableSwitch = new ToggleSwitch("Enable", "It has to be turned on if we want interactions with the server", false);
            //enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            
            //AddCustomControl(enableSwitch);



        }

        //private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    throw new NotImplementedException();
        //}


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

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (remoSetupClient == null) FindRemoSetupComponent();

            var inputCompSources = this.Params.Input[0].Sources;

            var inputComp = inputCompSources[0];

            GH_Structure<IGH_Goo> dataTree = new GH_Structure<IGH_Goo>();
            DA.GetDataTree(0, out dataTree);

            RemoParameter remoText = new RemoParameter(username,this.Message, dataTree);
            string remoCommandJson = RemoCommand.SerializeToJson(remoText);

            if (remoSetupClient != null) remoSetupClient.client.Send(remoCommandJson);

            RemoParamData dataComp = (RemoParamData)this.OnPingDocument().Objects
                .Where(obj => obj is RemoParamData).Select(obj => obj as RemoParamData)
                .Where(obj => obj.Message.Equals(this.Message)).FirstOrDefault();

            dataComp.currentValue = dataTree;

            this.OnPingDocument().ScheduleSolution(0, doc => {
                dataComp.currentValue = dataTree;
                dataComp.ExpireSolution(true);
            });

        }

        private void FindRemoSetupComponent()
        {

            var remoSetupComps = this.OnPingDocument().Objects.Where(obj => obj is RemoSharp.RemoSetupClient).FirstOrDefault();
            if (remoSetupComps == null)
            {
                string errorString = "A single RemoSetupClient Component is required for RemoParam" +
                    "\nPLease make sure there is a single RemoSetupClient in this Grasshopper Canvas";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, errorString);
                return;
            }

            RemoSetupClient remoSetupClient = (RemoSetupClient)remoSetupComps;

            this.username = remoSetupClient.username;
            this.password = remoSetupClient.password;
            this.remoSetupClient = remoSetupClient;
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