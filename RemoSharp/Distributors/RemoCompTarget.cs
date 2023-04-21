using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;

using RemoSharp.RemoCommandTypes;
using Grasshopper.Kernel.Types;

namespace RemoSharp
{
    public class RemoCompTarget : GHCustomComponent
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        PushButton selectButton;
        PushButton hideButton;
        PushButton lockButton;
        PushButton remoParamButton;
        //ToggleSwitch deleteToggle;
        ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;
        PushButton wsClientButton;
        StackPanel stackPanel;
        StackPanel stackPanel01;
        //StackPanel stackPanel02;

        bool enable = false;
        bool movingMode = false;
        //bool create = false;
        bool select = false;
        //bool delete = false;
        bool hide = false;
        bool lockThis = false;
        bool remoParam = false;


        // remoParam public variables
        public string componentType = "";
        public int RemoMakeindex = -1;
        public int DeleteThisComp = -1;
        public bool DeleteThisCompBool = false;
        public int GHbuttonComp = -1;
        public int remoButtonComp = -1;
        public int wsButtonComp = -1;
        public System.Drawing.PointF compPivot;

        public string currentConnectString = "";
        string cmdJson = "";
        string persistentCommand = "";
        public int con_DisConCounter = 0;

        // Move Mode variables
        int setup = 0;

        /// <summary>
        /// Initializes a new instance of the RemoCompTarget class.
        /// </summary>
        public RemoCompTarget()
          : base("RemoCompTarget", "RemoCompT",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            selectButton = new PushButton("Select",
                "Select a component on the main remote GH_Canvas.", "Sel");
            hideButton = new PushButton("Hide",
                "Hides a component on the main remote GH_Canvas.", "Hide");
            lockButton = new PushButton("Lock",
                            "Unhides a component on the main remote GH_Canvas.", "Lock");
            remoParamButton = new PushButton("RmoPrm",
                            "Creates the necessary remote parameter components", "RmoPrm");
            //deleteToggle = new ToggleSwitch("DelAfterCreation","Delete Component Upon Remote Creation",false);
            wsClientButton = new PushButton("WSC",
                "Creates The Required WS Client Components To Broadcast Canvas Screen.", "WSC");

            //transparencySwitch = new ToggleSwitch("Transparency", "Toggles transparency of Grasshopper", false);
            //transparencySwitch.OnValueChanged += ToggleSwitch_OnValueChanged;
            movingModeSwitch = new ToggleSwitch("Moving Mode", "It is recommended to keep it turned off if the user does not wish to move components around", false);
            movingModeSwitch.OnValueChanged += MovingModeSwitch_OnValueChanged;
            enableSwitch = new ToggleSwitch("Enable Interactions", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            selectButton.OnValueChanged += SelectButton_OnValueChanged;
            hideButton.OnValueChanged += PushButton2_OnValueChanged;
            lockButton.OnValueChanged += PushButton3_OnValueChanged;
            remoParamButton.OnValueChanged += PushButton4_OnValueChanged;
            //deleteToggle.OnValueChanged += PushButton5_OnValueChanged;
            wsClientButton.OnValueChanged += PushButton6_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Horizontal, true,
                selectButton, hideButton, lockButton
                );
            stackPanel01 = new StackPanel("C2", Orientation.Horizontal, true,
                remoParamButton,  wsClientButton
                );
            //stackPanel02 = new StackPanel("C3", Orientation.Vecrtical, false,
            //    enableSwitch, transparencySwitch, movingModeSwitch
            //    );
            AddCustomControl(stackPanel);
            AddCustomControl(stackPanel01);
            //AddCustomControl(stackPanel02);
            AddCustomControl(enableSwitch);
            //AddCustomControl(transparencySwitch);
            AddCustomControl(movingModeSwitch);
            //AddCustomControl(transparencySwitch);

            pManager.AddGenericParameter("SourceCommand", "SrcCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item);
        }

        private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            enable = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        private void MovingModeSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            movingMode = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        //private void ToggleSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool toggleChangeVal = Convert.ToBoolean(e.Value);
        //    var ghDoc = Grasshopper.Instances.DocumentEditor;
        //    if (toggleChangeVal)
        //    {
        //        ghDoc.Opacity = 0.25;
        //    }
        //    else
        //    {
        //        ghDoc.Opacity = 1;
        //    }
        //}

        private void SelectButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                select = currentValue;
                this.ExpireSolution(true);
            }
        }

        
        private void PushButton2_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                hide = currentValue;
                this.ExpireSolution(true);
            }
        }
        private void PushButton3_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                lockThis = currentValue;
                this.ExpireSolution(true);
            }
        }

        private void PushButton4_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                remoParam = currentValue;
                this.ExpireSolution(true);
            }
        }

        //private void PushButton5_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool currentValue = Convert.ToBoolean(e.Value);
        //    DeleteThisCompBool = currentValue;
        //    this.ExpireSolution(true);
        //}

        private void PushButton6_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                int shiftX = 192;
                int shiftY = -40;
                System.Drawing.PointF pivot = this.Attributes.Pivot;

                foreach (var obj in this.OnPingDocument().Objects)
                {
                    try
                    {
                        if (obj.GetType().ToString().Equals("RemoSharp.ClientCanvasBounds"))
                        {
                            pivot = obj.Attributes.Pivot;
                            shiftX = -29;
                            shiftY = +240;
                        }
                    }
                    catch { }
                }

                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 255 + shiftX, pivot.Y - 66 + shiftY);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 255 + shiftX, pivot.Y - 18 + shiftY);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 57 + shiftX, pivot.Y - 27 + shiftY);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 60 + shiftX, pivot.Y - 17 + shiftY);


                //StreamIPSet commandAddress = new StreamIPSet();
                //commandAddress.DialougeTitle.Text = "Please Set Your Command Server Address";
                //commandAddress.ShowDialog();
                string commandSrvAddress = "";
                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 100, 20);
                panel.SetUserText(commandSrvAddress);
                panel.NickName = "RemoSharp Command Server";


                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;
                wss.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;
                wsSend.Params.RepairParamAssociations();

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);

                    if (!commandSrvAddress.Equals("")) wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);
                });

            }
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(">⚫<       Command", ">⚫<       Command",
                "Complete command from RemoCompSource and RemoCompTarget regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item);
            pManager.AddTextParameter("json", "json", "json", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // registering s
            if (setup == 0)
            {
                Grasshopper.Instances.ActiveCanvas.KeyDown += ActiveCanvas_KeyDown;
                Grasshopper.Instances.ActiveCanvas.KeyUp += ActiveCanvas_KeyUp;
            }
            setup++;
            if (setup > 100) setup = 5;

            if (!enable) return;
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            RemoCommand cmd = null;
            DA.GetData(0, ref cmd);
            if (cmd == null) return;
            // parsing the incoming command

            //string[] cmds = cmd.Split(',');

            if (cmd.commandType == CommandType.MoveComponent) 
            {
                if (!movingMode) return;

            }


            if (cmd.commandType == CommandType.WireConnection)
            {
                RemoConnectInteraction connectionInteraction = (RemoConnectInteraction)cmd;


                int outIndex = -1;
                bool outIsSpecial = false;
                System.Guid outGuid = GetComponentGuidAnd_Output_Index(
                  connectionInteraction.source, out outIndex, out outIsSpecial);

                //connectionInteraction.sourceOutput = outIndex;
                //connectionInteraction.isSourceSpecial = outIsSpecial;
                //connectionInteraction.sourceObjectGuid = outGuid;

                int inIndex = -1;
                bool inIsSpecial = false;
                System.Guid inGuid = GetComponentGuidAnd_Input_Index(
                  connectionInteraction.target, out inIndex, out inIsSpecial);

                //connectionInteraction.targetInput = inIndex;
                //connectionInteraction.isTargetSpecial = inIsSpecial;
                //connectionInteraction.targetObjectGuid = inGuid;

                RemoConnect remoConnect = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid, outIndex, inIndex, outIsSpecial, inIsSpecial, connectionInteraction.RemoConnectType);

                cmdJson = RemoCommand.SerializeToJson(remoConnect);
                string hi = "";
            }

            // 50%
            if (hide)
            {
                
                bool state = true;

                IGH_DocumentObject selection = this.OnPingDocument().SelectedObjects()[0];
                if (selection is GH_Component)
                {
                   GH_Component hideComponent = (GH_Component)selection;
                    state = !hideComponent.Hidden;
                    hideComponent.Hidden = state;
                }
                //else if (selection is GH_PersistentParam)
                //{
                //    GH_PersistentParam hideComponent = (GH_PersistentParam)selection;
                //    state = !hideComponent.Hidden;
                //    hideComponent.Hidden = state;
                //}

                cmd = new RemoHide(cmd.issuerID, selection.InstanceGuid, state,DateTime.Now.Second);
                cmdJson = RemoCommand.SerializeToJson(cmd);
                DA.SetData(0, cmdJson);
                hide = false;
            }

            if (select)
            {

                IGH_DocumentObject selection = this.OnPingDocument().SelectedObjects()[0];
                
                Guid selectionGuid = selection.InstanceGuid;
                cmd = new RemoSelect(cmd.issuerID, selectionGuid, DateTime.Now.Second);
                cmdJson = RemoCommand.SerializeToJson(cmd);
                DA.SetData(0, cmdJson);
                
                select = false;
            }

            if (lockThis)
            {
                

                Guid selectionGuid = this.OnPingDocument().SelectedObjects()[0].InstanceGuid;
                cmd = new RemoLock(cmd.issuerID, selectionGuid, hide, DateTime.Now.Second);
                cmdJson = RemoCommand.SerializeToJson(cmd);
                DA.SetData(0, cmdJson);
                lockThis = false;
            }

            if (remoParam)
            {

                componentType = FindClosestObjectTypeOnCanvas(out compPivot, out RemoMakeindex);
                if (componentType.Equals("Grasshopper.Kernel.Special.GH_ButtonObject"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoButton);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_BooleanToggle"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoToggle);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoPanel);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_ColourSwatch"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoColorSwatch);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoSlider);
                }
                remoParam = false;
                return;
            }

            

            //string[] outGoingCommand = cmd.Split(',');
            int commandRepeatCount = 6;

            if (cmd.commandType == CommandType.WireConnection)
            {

                con_DisConCounter = 0;
                currentConnectString = cmdJson;
                persistentCommand = cmdJson;


            }
            else if (
                cmd.commandType == CommandType.MoveComponent
                || cmd.commandType == CommandType.Create
                || cmd.commandType == CommandType.Hide
                || cmd.commandType == CommandType.Lock
                || cmd.commandType == CommandType.Delete)
            {

                con_DisConCounter = 0;
                cmdJson = RemoCommand.SerializeToJson(cmd);
                currentConnectString = cmdJson;
                persistentCommand = cmdJson;


            }
            

            if (con_DisConCounter < commandRepeatCount)
            {
                DA.SetData(0, currentConnectString);
            }
            else currentConnectString = "";
            con_DisConCounter++;

            if (cmd.commandType == CommandType.MoveComponent)
            {
                DA.SetData(0, cmdJson);
                return;
            }

        }

        private void ActiveCanvas_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Tab)
            {
                this.movingModeSwitch.CurrentValue = false;
            }
        }

        private void ActiveCanvas_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Tab)
            {
                this.movingModeSwitch.CurrentValue = true;
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
                return RemoSharp.Properties.Resources.TargetComp.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("243dfe88-8c61-451c-996a-2f8f77c9409b"); }
        }




        private string FindClosestObjectTypeOnCanvas(out System.Drawing.PointF compPivot, out int compIndex)
        {
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.GrasshopperDocument;
            var ghObjects = ghDoc.Objects;
            var thisCompLoc = this.Component.Attributes.Pivot;
            int cmpIndex = -1;
            // finding the closest component
            string componentType = "";
            double minDistance = double.MaxValue;
            System.Drawing.PointF newPivot = new System.Drawing.PointF(0, 0);
            try
            {
                for (int i = 0; i < ghObjects.Count; i++)
                {

                    var component = ghObjects[i];
                    var pivot = component.Attributes.Pivot;
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));

                    if (distance < minDistance)
                    {
                        if (distance > 0)
                        {
                            // getting the type of the component via the ToString() method
                            // later the ToString() method is better to be changed to something more reliable
                            minDistance = distance;
                            componentType = component.ToString();
                            newPivot = component.Attributes.Pivot;
                            cmpIndex = i;
                        }
                    }
                }
            }
            catch { }
            compPivot = newPivot;
            compIndex = cmpIndex;
            return componentType;
        }

        private int DeletionFindComponentOnCanvasByCoordinates(float compX, float compY)
        {
            int compCoordX = Convert.ToInt32(compX);
            int compCoordY = Convert.ToInt32(compY);
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.GrasshopperDocument;
            var ghObjects = ghDoc.Objects;
            var thisCompLoc = new System.Drawing.PointF(compCoordX, compCoordY);

            // finding the closest component
            double minDistance = double.MaxValue;
            int objIndex = -1;
            try
            {
                for (int i = 0; i < ghObjects.Count; i++)
                {
                    var component = ghObjects[i];
                    var pivot = component.Attributes.Pivot;
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X)
                                              * (thisCompLoc.X - pivot.X)
                                              + (thisCompLoc.Y - pivot.Y)
                                              * (thisCompLoc.Y - pivot.Y));
                    if (distance < minDistance)
                    {

                        // getting the type of the component via the ToString() method
                        // later the ToString() method is better to be changed to something more reliable
                        minDistance = distance;
                        objIndex = i;
                    }
                }
            }
            catch { }
            return objIndex;
        }

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY)
        {
            var thisDoc = this.Component.OnPingDocument();
            // converting the string format of the closest component to an actual type
            var type = Type.GetType(typeName);
            // most probable the type is going to return null
            // for that we search through all the loaded dlls in Grasshopper and Rhino's application
            // to find out which one matches that of the closest component
            if (type == null)
            {
                // going through the loaded components
                foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // trying for all dll types unless one would return an actual type
                    // since almost all of them give us null we check for this condition
                    if (type == null)
                    {
                        type = a.GetType(typeName);
                    }
                }
            }
            // we can instantiate a class with this line based on the type we found in string format
            // we have to cast it into an (IGH_DocumentObject) format so that we can access the methods
            // that we need to add it to the grasshopper document
            // also in order to add any object into the GH canvas it has to be cast into (IGH_DocumentObject)
            var myObject = (IGH_DocumentObject)Activator.CreateInstance(type);
            // creating atts to create the pivot point
            // this pivot point can be anywhere
            myObject.CreateAttributes();
            //        myObject.Attributes.Pivot = new System.Drawing.PointF(200, 600);
            var currentPivot = new System.Drawing.PointF(pivotX, pivotY);

            myObject.Attributes.Pivot = currentPivot;
            // making sure the update argument is false to prevent GH crashes
            thisDoc.AddObject(myObject, false);
        }


        private IGH_Param FindWebSocketClientComponentOutput(out Grasshopper.Kernel.Special.GH_Panel usernamePanel)
        {

            try
            {
                var idComp = (IGH_Component)this.Params.Output[0].Recipients[0].Attributes.Parent.DocObject;
                Grasshopper.Kernel.Special.GH_Panel idPanel = (Grasshopper.Kernel.Special.GH_Panel)
                    idComp.Params.Input[1].Sources[0];
                var sendComp = (IGH_Component)idComp.Params.Output[0].Recipients[0].Attributes.Parent.DocObject;
                var wscComp = (IGH_Component)sendComp.Params.Input[0].Sources[0].Attributes.Parent.DocObject;
                usernamePanel = idPanel;
                return wscComp.Params.Output[0];
            }
            catch 
            {
                usernamePanel= null;
                return null;
            }
        }

        private void MakeRemoButton(GH_Document doc)
        {
            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            //GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            //grip.CreateObjectData(otherComp);

            //this.Component.OnPingDocument().DeselectAll();
            //this.Component.OnPingDocument().Select(grip);
            //System.Drawing.Size vec = new System.Drawing.Size(-160, 25);
            //this.Component.OnPingDocument().TranslateObjects(vec, true);
            //this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_ButtonObject button = (Grasshopper.Kernel.Special.GH_ButtonObject)otherComp;

            RemoSharp.RemoButton remoButton = new RemoButton();
            remoButton.CreateAttributes();
            remoButton.Attributes.Pivot = new System.Drawing.PointF(pivotX + 44, pivotY + 39 + 7);
            remoButton.Params.RepairParamAssociations();

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 27);
            wsSend.Params.RepairParamAssociations();

            remoButton.Params.Input[0].AddSource( (IGH_Param) button);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoButton.Params.Output[0]);
            Grasshopper.Kernel.Special.GH_Panel usernamePanel = null;
            try { 
                wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput(out usernamePanel));
                remoButton.Params.Input[1].AddSource(usernamePanel);
            }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            remoButton.Params.Input[1].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoButton, true);
            this.OnPingDocument().AddObject(wsSend, true);
        }

        private void MakeRemoToggle(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            //GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            //grip.CreateObjectData(otherComp);

            //this.Component.OnPingDocument().DeselectAll();
            //this.Component.OnPingDocument().Select(grip);
            //System.Drawing.Size vec = new System.Drawing.Size(-160, 25);
            //this.Component.OnPingDocument().TranslateObjects(vec, true);
            //this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_BooleanToggle toggle = (Grasshopper.Kernel.Special.GH_BooleanToggle)otherComp;

            RemoSharp.RemoToggle remoToggle = new RemoToggle();
            remoToggle.CreateAttributes();
            remoToggle.Attributes.Pivot = new System.Drawing.PointF(pivotX + 44, pivotY + 39 + 7);
            remoToggle.Params.RepairParamAssociations();

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 27);
            wsSend.Params.RepairParamAssociations();

            remoToggle.Params.Input[0].AddSource((IGH_Param)toggle);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoToggle.Params.Output[0]);
            Grasshopper.Kernel.Special.GH_Panel usernamePanel = null;
            try
            {
                wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput(out usernamePanel));
                remoToggle.Params.Input[1].AddSource(usernamePanel);
            }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            remoToggle.Params.Input[1].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoToggle, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoPanel(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];
            Grasshopper.Kernel.Special.GH_Panel panel = (Grasshopper.Kernel.Special.GH_Panel)otherComp;

            //GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);


            //float panelSizeX = panel.Attributes.Bounds.Width;
            //float panelSizeY = panel.Attributes.Bounds.Height;
            //System.Drawing.Size vec = new System.Drawing.Size(-180, 25);
            //if (panelSizeX > 280 && panelSizeY < 50) vec = new System.Drawing.Size(-180, -25);
            //grip.CreateObjectData(otherComp);

            //this.Component.OnPingDocument().DeselectAll();
            //this.Component.OnPingDocument().Select(grip);
            //this.Component.OnPingDocument().TranslateObjects(vec, true);
            //this.Component.OnPingDocument().DeselectAll();


            RemoSharp.RemoPanel remoPanel = new RemoPanel();
            remoPanel.CreateAttributes();
            remoPanel.Attributes.Pivot = new System.Drawing.PointF(pivotX + 47 - 4, pivotY + 37 + 79 + 7);
            remoPanel.Params.RepairParamAssociations();

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24 + 101);
            wsSend.Params.RepairParamAssociations();

            remoPanel.Params.Input[0].AddSource((IGH_Param)panel);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoPanel.Params.Output[0]);
            Grasshopper.Kernel.Special.GH_Panel usernamePanel = null;
            try
            {
                wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput(out usernamePanel));
                remoPanel.Params.Input[1].AddSource(usernamePanel);
            }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            remoPanel.Params.Input[1].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoPanel, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoColorSwatch(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            //GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            //grip.CreateObjectData(otherComp);

            //this.Component.OnPingDocument().DeselectAll();
            //this.Component.OnPingDocument().Select(grip);
            //System.Drawing.Size vec = new System.Drawing.Size(-160, 35);
            //this.Component.OnPingDocument().TranslateObjects(vec, true);
            //this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_ColourSwatch color = (Grasshopper.Kernel.Special.GH_ColourSwatch)otherComp;

            RemoSharp.RemoColorSwatch remoColor = new RemoColorSwatch();
            remoColor.CreateAttributes();
            remoColor.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39 + 15, pivotY + 37 + 17);
            remoColor.Params.RepairParamAssociations();

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24 + 7);
            wsSend.Params.RepairParamAssociations();

            remoColor.Params.Input[1].AddSource((IGH_Param)color);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoColor.Params.Output[0]);
            Grasshopper.Kernel.Special.GH_Panel usernamePanel = null;
            try
            {
                wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput(out usernamePanel));
                remoColor.Params.Input[2].AddSource(usernamePanel);
            }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            remoColor.Params.Input[2].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoColor, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoSlider(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            //GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            //grip.CreateObjectData(otherComp);

            //this.Component.OnPingDocument().DeselectAll();
            //this.Component.OnPingDocument().Select(grip);
            //System.Drawing.Size vec = new System.Drawing.Size(-220, 35);
            //this.Component.OnPingDocument().TranslateObjects(vec, true);
            //this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_NumberSlider slider = (Grasshopper.Kernel.Special.GH_NumberSlider)otherComp;

            RemoSharp.RemoSlider remoSlider = new RemoSlider();
            remoSlider.CreateAttributes();
            remoSlider.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39 + 13 - 8, pivotY + 37 + 17);
            remoSlider.Params.RepairParamAssociations();

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24 + 20);
            wsSend.Params.RepairParamAssociations();

            remoSlider.Params.Input[1].AddSource((IGH_Param)slider);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoSlider.Params.Output[0]);
            Grasshopper.Kernel.Special.GH_Panel usernamePanel = null;
            try
            {
                wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput(out usernamePanel));
                remoSlider.Params.Input[2].AddSource(usernamePanel);
            }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;
            remoSlider.Params.Input[2].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoSlider, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private System.Guid GetComponentGuidAnd_Input_Index(
    IGH_Param target,
    out int paramIndex,
    out bool isSpecial)
        {
            if (target.Attributes.Parent == null)
            {
                System.Guid compGuid = target.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {
                var foundComponent = (IGH_Component)target.Attributes.Parent.DocObject;
                int index = foundComponent.Params.Input.IndexOf(target);

                paramIndex = index;
                isSpecial = false;
                return foundComponent.InstanceGuid;
            }
        }

        private System.Guid GetComponentGuidAnd_Output_Index(
          IGH_Param source,
          out int paramIndex,
          out bool isSpecial)
        {

            if (source.Attributes.Parent == null)
            {
                System.Guid compGuid = source.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {
                var foundComponent = (IGH_Component)source.Attributes.Parent.DocObject;
                int index = foundComponent.Params.Output.IndexOf(source);

                paramIndex = index;
                isSpecial = false;
                return foundComponent.InstanceGuid;
            }

        }

        private void DeleteObjectAbove(GH_Document doc)
        {
            var obj = this.OnPingDocument().Objects[DeleteThisComp];
            this.OnPingDocument().RemoveObject(obj, true);
        }
    }
}