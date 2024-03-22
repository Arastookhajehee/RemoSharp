using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;
using Grasshopper.GUI;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;
using RemoSharp.RemoCommandTypes;
using Rhino.Commands;

using Newtonsoft.Json;
using Grasshopper.GUI.Canvas;
using RemoSharp.RemoParams;
using WebSocketSharp;
using System.Diagnostics.PerformanceData;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.GUI.Base;
using Grasshopper.Kernel.Special;
using System.ComponentModel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Undo;
using Machina;

namespace RemoSharp
{
    public class RemoSetupClient : GHCustomComponent
    {
        public List<string> messages = new List<string>();
        public WebSocket client;
        bool connect = false;
        bool needsRestart = false;
        bool preventUndo = true;
        int reconnectAttempts = 0;
        int maxAttempts = 2;
        public static string DISCONNECTION_KEYWORD = "<<DisconnectFromRemoSharpServer>>";


        public ToggleSwitch autoUpdateSwitch;
        public ToggleSwitch keepRecordSwitch;
        public ToggleSwitch listenSwitch;
        public ToggleSwitch undoPreventionSwitch;


        public bool autoUpdate = true;
        public bool keepRecord = false;
        bool listen = true;
        bool controlDown = false;

        int setup = 0;
        int commandRepeat = 5;
        Grasshopper.GUI.Canvas.GH_Canvas canvas;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;

        //ToggleSwitch deleteToggle;
        //ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;

        public bool enable = false;
        //public bool movingMode = false;
        public bool subscribed = false;

        public bool remoParamModeActive = false;
        public bool doubleClicked = false;
        public PointF mouseLocation = PointF.Empty;

        int counterTest = 0;

        public List<Guid> remoCommandIDs = new List<Guid>();

        public string username = "";
        public string password = "";

        float[] downPnt = { 0, 0 };
        float[] upPnt = { 0, 0 };

        PushButton setupButton;

        float[] PointFromCanvasMouseInteraction(Grasshopper.GUI.Canvas.GH_Viewport vp, MouseEventArgs e)
        {
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            float x = mouseEvent.CanvasX;
            float y = mouseEvent.CanvasY;
            float[] coords = { x, y };
            return coords;
        }

        /// <summary>
        /// Initializes a new instance of the RemoCompSource class.
        /// </summary>
        public RemoSetupClient()
          : base("RemoSetup", "RemoSetup",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoSetup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            setupButton = new PushButton("Set Up",
                   "Creates The Required RemoSharp Components to Connect to a Session.", "Set Up");
            setupButton.OnValueChanged += SetupButton_OnValueChanged;
            AddCustomControl(setupButton);


            //movingModeSwitch = new ToggleSwitch("Moving Mode", "It is recommended to keep it turned off if the user does not wish to move components around", false);
            //movingModeSwitch.OnValueChanged += MovingModeSwitch_OnValueChanged;
            enableSwitch = new ToggleSwitch("Enable Interactions", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            AddCustomControl(enableSwitch);
            //AddCustomControl(movingModeSwitch);


            
            autoUpdateSwitch = new ToggleSwitch("Auto Update", "If turned off, a trigger should be used for the listen component.", false);
            autoUpdateSwitch.OnValueChanged += AutoUPdate_OnValueChanged;
            listenSwitch = new ToggleSwitch("Listen", "If enabled the client listens for messages.", true);
            listenSwitch.OnValueChanged += ListenSwitch_OnValueChanged;
            keepRecordSwitch = new ToggleSwitch("Keep Record", "Keeps all the messages coming from the server", true);
            keepRecordSwitch.OnValueChanged += KeepRecordSwitch_OnValueChanged;
            undoPreventionSwitch = new ToggleSwitch("Prevent Undo", "Warns the user not to use undo or redo", true);
            undoPreventionSwitch.OnValueChanged += UndoPreventionSwitch_OnValueChanged;

            AddCustomControl(listenSwitch);
            AddCustomControl(keepRecordSwitch);
            AddCustomControl(autoUpdateSwitch);
            AddCustomControl(undoPreventionSwitch);

            pManager.AddTextParameter("url", "url", "", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("connect", "connect", "", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("KeepAlive", "keepAlive", "", GH_ParamAccess.item);

            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Password", "pass", "Password to this session", GH_ParamAccess.item, "password");
            pManager.AddBooleanParameter("syncSend", "syncSend", "Syncs this grasshopper script for all other connected clients", GH_ParamAccess.item, false);



        }

        private void UndoPreventionSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            preventUndo = Convert.ToBoolean(e.Value);
        }

        private void AutoUPdate_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            autoUpdate = Convert.ToBoolean(e.Value);
        }

        private void ListenSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            listen = Convert.ToBoolean(e.Value);

            if (client == null) return;

            if (listen) client.OnMessage += Client_OnMessage;
            else client.OnMessage -= Client_OnMessage;

        }

        private void KeepRecordSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            this.keepRecord = Convert.ToBoolean(e.Value);
        }

        private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            enable = Convert.ToBoolean(e.Value);

            var document = this.OnPingDocument();
            if (document == null) return;

            var executor = document.Objects.Where(obj => obj is CommandExecutor).ToList();

            if (executor.Count == 0) 
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Setup Error!");
                return;
            }
            CommandExecutor commandExecutorComponent = (CommandExecutor)executor[0];
            commandExecutorComponent.enable = enable;

            if (enable)
            {



                if (this == null || this.OnPingDocument() == null)
                {
                    return;
                }
                canvas = Grasshopper.Instances.ActiveCanvas;
                #region Wire Connection and Move Sub
                canvas.MouseDown += Canvas_MouseDown;
                canvas.KeyDown += Canvas_KeyDown;
                #endregion

                canvas.MouseUp += Canvas_MouseUp;
                canvas.KeyUp += Canvas_KeyUp;

                #region Add Object Sub
                this.OnPingDocument().ObjectsAdded += RemoCompSource_ObjectsAdded;
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted += RemoCompSource_ObjectsDeleted;
                #endregion

                #region UndoRedo
                //this.OnPingDocument().UndoStateChanged += RemoSetupClient_UndoStateChanged;
                #endregion

                #region remoparam mouse move
                Grasshopper.Instances.ActiveCanvas.MouseMove += ActiveCanvas_MouseMove;
                Grasshopper.Instances.ActiveCanvas.KeyDown += ActiveCanvas_KeyDown;
                Grasshopper.Instances.ActiveCanvas.KeyUp += ActiveCanvas_KeyUp;
                #endregion


                SubscribeAllParams(this, true);

            }
            else
            {
                canvas = Grasshopper.Instances.ActiveCanvas;
                #region Wire Connection and Move Sub
                canvas.MouseDown -= Canvas_MouseDown;
                canvas.KeyDown -= Canvas_KeyDown;
                #endregion

                canvas.MouseUp -= Canvas_MouseUp;
                canvas.KeyUp -= Canvas_KeyUp;


                #region Add Object Sub
                this.OnPingDocument().ObjectsAdded -= RemoCompSource_ObjectsAdded;
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted -= RemoCompSource_ObjectsDeleted;
                #endregion

                #region UndoRedo
                this.OnPingDocument().UndoStateChanged -= RemoSetupClient_UndoStateChanged;
                #endregion

                #region remoparam mouse move
                Grasshopper.Instances.ActiveCanvas.MouseMove -= ActiveCanvas_MouseMove;
                Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyDown;
                Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                #endregion

                subscribed = false;

                SubscribeAllParams(this, false);
            }

            this.ExpireSolution(true);
        }

        private void SubscribeAllParams(RemoSetupClient remoSetupComp, bool subscribe)
        {
            List<string> accaptableTypes = new List<string>() {
            "Grasshopper.Kernel.Special.GH_NumberSlider",
            "Grasshopper.Kernel.Special.GH_Panel",
            "Grasshopper.Kernel.Special.GH_ColourSwatch",
            "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
            "Grasshopper.Kernel.Special.GH_BooleanToggle",
            "Grasshopper.Kernel.Special.GH_ButtonObject"
            };

            var allParams = this.OnPingDocument().Objects;


            if (!subscribe)
            {
                var deletionGuids = this.OnPingDocument().Objects
                    .Where(obj => obj.NickName.Equals(RemoParam.RemoParamKeyword))
                    .Select(obj => this.OnPingDocument().FindObject(obj.InstanceGuid,false))
                    .ToList();

                this.OnPingDocument().RemoveObjects(deletionGuids, false);
            }

            foreach (var item in allParams)
            {
                if (subscribe)
                {
                    string typeString = item.GetType().FullName;

                    string pause = "";


                    bool isInTheList = accaptableTypes.Contains(typeString);

                    if (isInTheList)
                    {
                        string nickName = item .NickName;
                        bool isSetupButtno = nickName.Equals("RemoSetup");

                        if (isSetupButtno) continue;

                        GroupObjParam(item);
                    }

                }
                switch (item.GetType().ToString())
                {
                    
                    case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
                            item.SolutionExpired += remoSetupComp.RemoParameterizeSlider;
                        }
                        else
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
                        }
                        break;
                    case ("Grasshopper.Kernel.Special.GH_Panel"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
                            item.SolutionExpired += remoSetupComp.RemoParameterizePanel;
                        }
                        else 
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
                        }
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
                            item.SolutionExpired += remoSetupComp.RemoParameterizeColor;
                        }
                        else 
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
                        }
                        break;
                    case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
                            item.SolutionExpired += remoSetupComp.RemoParameterizeMDSlider;
                        }
                        else 
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
                        }
                        break;
                    case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
                            item.SolutionExpired += remoSetupComp.RemoParameterizeToggle;
                        }
                        else 
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
                        }
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                        if (subscribe)
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
                            item.SolutionExpired += remoSetupComp.RemoParameterizeButton;
                        }
                        else 
                        {
                            item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void Canvas_KeyUp(object sender, KeyEventArgs e)
        {
            var keyCode = (int) e.KeyCode;
            var controlKey = (int) Keys.ControlKey;
            if (keyCode == controlKey)
            {
                this.controlDown = false;
            }
        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            var keyCode = (int) e.KeyCode;
            var controlKey = (int)Keys.ControlKey;
            if (keyCode == controlKey)
            {
                this.controlDown = true;

                if (preventUndo)
                {
                    var thisDoc = this.OnPingDocument();
                    if (thisDoc != null) 
                    {
                        var undoServer = thisDoc.UndoServer;
                        if (undoServer != null) undoServer.Clear();
                    }

                }

            }
        }

        private void RemoSetupClient_UndoStateChanged(object sender, GH_DocUndoEventArgs e)
        {
            if (e.Record == null) return;
            if (e.Record.State == GH_UndoState.undo) return;
            if (e.Record.ActionCount != 1) return;
            if (!this.controlDown) return;

            RemoUndo undoCommand = new RemoUndo(username, e);
            SendCommands(undoCommand, commandRepeat, enable);
        }

        private void ActiveCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (this == null)
            {
                Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyUp;
            }
            if (this.OnPingDocument() == null)
            {
                Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyUp;
            }
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.F12)
            {
                this.remoParamModeActive = false;
                var gh_groups = this.OnPingDocument().Objects;
                System.Threading.Tasks.Parallel.ForEach(gh_groups, item =>
                {
                    if (!(item is Grasshopper.Kernel.Special.GH_Group)) return;
                    Grasshopper.Kernel.Special.GH_Group group = (Grasshopper.Kernel.Special.GH_Group)item;
                    if(group.NickName.Contains(RemoParam.RemoParamKeyword)) group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);

                });

            }
            
        }

        private void ActiveCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (this == null)
            {
                Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyUp;
            }
            if (this.OnPingDocument() == null)
            {
                Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyUp;
            }
            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.F12)
            {
                this.remoParamModeActive= true;
                var gh_groups = this.OnPingDocument().Objects;
                System.Threading.Tasks.Parallel.ForEach(gh_groups, item =>
                {
                    if (!(item is Grasshopper.Kernel.Special.GH_Group)) return;
                    Grasshopper.Kernel.Special.GH_Group group = (Grasshopper.Kernel.Special.GH_Group)item;
                    if (group.NickName.Contains(RemoParam.RemoParamKeyword)) group.Colour = System.Drawing.Color.FromArgb(125, 225, 100, 250);
                });
            }
            
            else if (e.KeyCode == Keys.Z && controlDown && preventUndo)
            {
                System.Windows.Forms.MessageBox.Show("PLEASE DO NOT USE UNDO OR REDO (●◡~)", "Syncing Error!");
            }
        }

        private void ActiveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            this.mouseLocation = mouseEvent.CanvasLocation;
        }

        //private void MovingModeSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    movingMode = Convert.ToBoolean(e.Value);
        //    this.ExpireSolution(true);
        //}

        private void SendCommands(RemoCommand command, int commandRepeat, bool enabled)
        {
            if (!enabled) return;
            string cmdJson = RemoCommand.SerializeToJson(command);

            for (int i = 0; i < commandRepeat; i++)
            {

                int stringLength = cmdJson.Length;

                client.Send(cmdJson);
            }
        }

        private static void SendCommands(RemoSetupClient setupComp, RemoCommand command)
        {
            string cmdJson = RemoCommand.SerializeToJson(command);

            try
            {
                for (int i = 0; i < 3; i++)
                {
                    setupComp.client.Send(cmdJson);
                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Connection to the server may not be properly working!", "Conncetion Error", MessageBoxButtons.OK);
            }
        }

        private void SetupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (this.Params.Input[0].Sources.Count > 0) return;
            if (currentValue)
            {

                AddInteractionButtonsToTopBar();


                int xShift = 2;
                int yShift = 80;
                PointF pivot = this.Attributes.Pivot;
                //PointF wscButtonPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF wscTogglePivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF triggerPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 415 + yShift);
                PointF panelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift);
                PointF passPanelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift + 45);
                //PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 336 + yShift);
                PointF listenPivot = new PointF(pivot.X + xShift + 330, pivot.Y - 334 + yShift);

                PointF targetPivot = new PointF(pivot.X + xShift + 200, pivot.Y);
                PointF commandPivot = new PointF(pivot.X + xShift + 598, pivot.Y - 312 + yShift);
                PointF commandButtonPivot = new PointF(pivot.X + xShift + 350, pivot.Y - 254 + yShift);

                #region setup components
                //// button
                //Grasshopper.Kernel.Special.GH_ButtonObject wscButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                //wscButton.CreateAttributes();
                //wscButton.Attributes.Pivot = wscButtonPivot;
                //wscButton.NickName = "RemoSetup";

                // toggle
                Grasshopper.Kernel.Special.GH_BooleanToggle wscToggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                wscToggle.CreateAttributes();
                wscToggle.Attributes.Pivot = wscTogglePivot;
                wscToggle.NickName = "RemoSetup";
                wscToggle.Value = false;
                wscToggle.ExpireSolution(false);

                // RemoSharp trigger
                var trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.NickName = "RemoSharp";
                trigger.Interval = 500;
                trigger.NickName = "RemoSetup";

                // componentName
                var targetComp = new RemoSharp.RemoCompTarget();
                targetComp.CreateAttributes();
                targetComp.Attributes.Pivot = targetPivot;
                targetComp.Params.RepairParamAssociations();
                targetComp.NickName = "RemoSetup";

                // componentName
                var panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new Rectangle((int)panelPivot.X, (int)panelPivot.Y, 100, 45);
                panel.SetUserText("username");
                panel.NickName = "RemoSetup";

                // componentName
                var passPanel = new Grasshopper.Kernel.Special.GH_Panel();
                passPanel.CreateAttributes();
                passPanel.Attributes.Pivot = passPanelPivot;
                passPanel.Attributes.Bounds = new Rectangle((int)passPanelPivot.X, (int)passPanelPivot.Y, 100, 45);
                passPanel.SetUserText("password");
                passPanel.NickName = "RemoSetup";


                //// componentName
                //var wscComp = new RemoSharp.WebSocketClient.WebSocketClient();
                //wscComp.CreateAttributes();
                //wscComp.Attributes.Pivot = wscPivot;
                //wscComp.Params.RepairParamAssociations();
                //wscComp.NickName = "RemoSetup";
                //wscComp.autoUpdateSwitch.CurrentValue = false;
                //wscComp.keepRecordSwitch.CurrentValue = true;
                //wscComp.autoUpdate = false;
                //wscComp.keepRecord = true;

                // componentName
                var listenComp = new RemoSharp.WebSocketClient.WSClientListen();
                listenComp.CreateAttributes();
                listenComp.Attributes.Pivot = listenPivot;
                listenComp.Params.RepairParamAssociations();
                listenComp.NickName = "RemoSetup";

                // componentName
                var commandComp = new RemoSharp.CommandExecutor();
                commandComp.CreateAttributes();
                commandComp.Attributes.Pivot = commandPivot;
                commandComp.Params.RepairParamAssociations();
                commandComp.NickName = "RemoSetup";

                // button
                Grasshopper.Kernel.Special.GH_ButtonObject commandCompButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                commandCompButton.CreateAttributes();
                commandCompButton.Attributes.Pivot = commandButtonPivot;
                commandCompButton.NickName = "RemoSetup";

                #endregion

                var addressOutPuts = RemoSharp.RemoCommandTypes.Utilites.CreateServerMakerComponent(this.OnPingDocument(), pivot, -119, -318 + yShift, true);


                this.OnPingDocument().ScheduleSolution(1, doc =>
                {


                    //this.OnPingDocument().AddObject(wscButton, true);
                    this.OnPingDocument().AddObject(wscToggle, true);
                    //this.OnPingDocument().AddObject(bffComp, true);
                    //this.OnPingDocument().AddObject(bffTrigger, true);
                    this.OnPingDocument().AddObject(trigger, true);
                    this.OnPingDocument().AddObject(targetComp, true);
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(passPanel, true);
                    //this.OnPingDocument().AddObject(wscComp, true);
                    this.OnPingDocument().AddObject(listenComp, true);
                    this.OnPingDocument().AddObject(commandCompButton, true);
                    this.OnPingDocument().AddObject(commandComp, true);

                    /*
                    wscButton wscToggle bffComp
                    bffTrigger trigger targetComp
                    panel wscComp idToggle
                    listenComp sendIDComp sendComp
                    listendIDComp commandComp
                    */

                    //bffComp.Params.Input[0].AddSource(wscButton);
                    //bffComp.Params.Input[1].AddSource(wscToggle);
                    targetComp.Params.Input[0].AddSource(panel);
                    targetComp.Params.Input[1].AddSource(this.Params.Output[0]);
                    this.Params.Input[2].AddSource(panel);
                    //this.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    this.Params.Input[3].AddSource(passPanel);
                    this.Params.Input[4].AddSource(commandCompButton);
                    this.Params.Input[0].AddSource(addressOutPuts[0]);
                    //this.Params.Input[1].AddSource(wscButton);
                    this.Params.Input[1].AddSource(wscToggle);

                    listenComp.Params.Input[0].AddSource(this.Params.Output[0]);

                    commandComp.Params.Input[0].AddSource(listenComp.Params.Output[0]);
                    commandComp.Params.Input[1].AddSource(panel);
                    commandComp.Params.Input[2].AddSource(commandCompButton);

                    //bffTrigger.AddTarget(bffTriggerTarget);
                    trigger.AddTarget(listenComp.InstanceGuid);
                    //trigger.AddTarget(targetComp.InstanceGuid);

                });
            }
            this.ExpireSolution(true);
        }

        private void AddInteractionButtonsToTopBar()
        {


            ToolStripItemCollection items = ((ToolStrip)(Grasshopper.Instances.DocumentEditor).Controls[0].Controls[1]).Items;
            if (!items.ContainsKey("Look")) { 
                items.Add(new ToolStripButton("Look", (Image)Properties.Resources.Sync_Camera.ToBitmap(), onClick: (s, e) => LookButton_OnValueChanged(s, e))
                {
                    AutoSize = true,
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    ImageAlign = ContentAlignment.MiddleCenter,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Margin = new Padding(0, 0, 0, 0),
                    Name = "Look",
                    Size = new Size(28, 28),
                    ToolTipText = "Looks in the same spot in other connected GH documents.",
                });
            }

            if (!items.ContainsKey("Select"))
            {
                items.Add(new ToolStripButton("Select", (Image)Properties.Resources.SyncGHviewport.ToBitmap(), onClick: (s, e) => SelectButton_OnValueChanged(s, e))
                {
                    AutoSize = true,
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    ImageAlign = ContentAlignment.MiddleCenter,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Margin = new Padding(0, 0, 0, 0),
                    Name = "Select",
                    Size = new Size(28, 28),
                    ToolTipText = "Selects components on other connected GH documents.",
                });
            }

            if (!items.ContainsKey("SyncComps"))
            {
                items.Add(new ToolStripButton("SyncComps", (Image)Properties.Resources.BroadcastCanvas.ToBitmap(), onClick: (s, e) => SyncComponents_OnValueChanged(s, e))
                {
                    AutoSize = true,
                    DisplayStyle = ToolStripItemDisplayStyle.Image,
                    ImageAlign = ContentAlignment.MiddleCenter,
                    ImageScaling = ToolStripItemImageScaling.SizeToFit,
                    Margin = new Padding(0, 0, 0, 0),
                    Name = "SyncComps",
                    Size = new Size(28, 28),
                    ToolTipText = "Syncronize the Selected Components.",
                });
            }

        }
        
        private void SyncComponents_OnValueChanged(object sender, EventArgs e)
        {
            List<Guid> guids = new List<Guid>();
            List<string> xmls = new List<string>();
            List<string> docXmls = new List<string>();

            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClient setupComp = (RemoSetupClient)thisDoc.Objects.Where(obj => obj is RemoSetupClient).FirstOrDefault();
            if (setupComp == null) return;

            var selection = thisDoc.SelectedObjects();

            thisDoc.UnselectedObjects();

            foreach (var item in selection)
            {

                Guid itemGuid = item.InstanceGuid;

                string componentXML = RemoCommand.SerializeToXML(item);
                string componentDocXML = RemoCommand.SerizlizeToSinglecomponentDocXML(item);

                guids.Add(itemGuid);
                xmls.Add(componentXML);
                docXmls.Add(componentDocXML);
            }

            RemoCompSync remoCompSync = new RemoCompSync(setupComp.username, guids, xmls, docXmls);
            RemoSetupClient.SendCommands(setupComp, remoCompSync);

        }

        

        private void LookButton_OnValueChanged(object sender, EventArgs e)
        {
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClient setupComp = (RemoSetupClient)thisDoc.Objects.Where(obj => obj is RemoSetupClient).FirstOrDefault();
            if (setupComp == null) return;

            var bounds_for_xml = Grasshopper.Instances.ActiveCanvas.Viewport.VisibleRegion;
            var screenMidPnt = Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint;
            var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;
            string bnds4XML = bounds_for_xml.X
                + "," + bounds_for_xml.Y
                + "," + bounds_for_xml.Width
                + "," + bounds_for_xml.Height
                + "," + screenMidPnt.X
                + "," + screenMidPnt.Y
                + "," + zoomLevel;
            RemoCanvasView remoCanvasView = new RemoCanvasView(setupComp.username, bnds4XML);
            RemoSetupClient.SendCommands(setupComp, remoCanvasView);
        }

        private void SelectButton_OnValueChanged(object sender, EventArgs e)
        {
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClient setupComp = (RemoSetupClient)thisDoc.Objects.Where(obj => obj is RemoSetupClient).FirstOrDefault();
            if (setupComp == null) return;

            var selection = thisDoc.SelectedObjects();

            List<Guid> slectionGuids = new List<Guid>();
            foreach (var item in selection)
            {
                slectionGuids.Add(item.InstanceGuid);
            }
            RemoSelect cmd = new RemoSelect(setupComp.username, slectionGuids, DateTime.Now.Second);
            RemoSetupClient.SendCommands(setupComp, cmd); 
        }

        

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Command", "cmd", "RemoSharp Canvas Interaction Command", GH_ParamAccess.item);
            pManager.AddGenericParameter("WSClient", "wsc", "", GH_ParamAccess.item);

        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            



            string url = "";
            DA.GetData(0, ref url);
            DA.GetData(1, ref connect);
            //DA.GetData(2, ref keepAlive);

            bool syncThisCanvas = false;

            // getting the username information
            DA.GetData(2, ref username);
            //DA.GetData(1, ref client);
            DA.GetData(3, ref password);
            DA.GetData(4, ref syncThisCanvas);

            if (!connect)
            {
                if (client == null) return;
                client.Close();
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.OnMessage -= Client_OnMessage;
                this.Message = "Disconnected";
            }

            if (client != null && !url.Equals(client.Url.AbsoluteUri))
            {
                needsRestart = true;
                if (client != null)
                {
                    client.OnMessage -= Client_OnMessage;
                    client.OnOpen -= Client_OnOpen;
                    client.OnClose -= Client_OnClose;
                    client.Close();
                    client = null;
                }
            }

            if (needsRestart && !connect)
            {
                this.Message = "Disconnected";
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Please Restart Connection");
                return;
            }

            if (connect)
            {
                if (client == null || needsRestart) client = new WebSocket(url);

                if (!client.IsAlive)
                {
                    this.Message = "Connecting...";
                    client.Close();
                    client.OnOpen -= Client_OnOpen;
                    client.OnClose -= Client_OnClose;
                    client.OnMessage -= Client_OnMessage;

                    client = new WebSocket(url);
                    Task initialTask = Task.Run(() => InitialConnect());

                    client.OnOpen += Client_OnOpen;
                    client.OnClose += Client_OnClose;
                    client.OnMessage += Client_OnMessage;

                    needsRestart = false;
                }
                else
                {
                    this.Message = "Connected";
                }
            }

            DA.SetData(0, client);


            if (syncThisCanvas)
            {
                ExcecuteShareDocument();
                return;
            }
        }

        private void ExcecuteShareDocument()
        {
            this.OnPingDocument().DeselectAll();

            GH_LooseChunk tempChunk = new GH_LooseChunk(null);
            this.OnPingDocument().Write(tempChunk);

            GH_Document saveDoc = new GH_Document();
            saveDoc.Read(tempChunk);

            saveDoc.RemoveObjects(saveDoc.Objects.Where(obj => obj.NickName.Contains("RemoSetup")).ToList(), false);

            GH_LooseChunk toSendChunk = new GH_LooseChunk(null);
            saveDoc.Write(toSendChunk);
            string xml = toSendChunk.Serialize_Xml();

            RemoCanvasSync remoCanvasSync  = new RemoCanvasSync(username, xml);
            string syncCommand = RemoCommand.SerializeToJson(remoCanvasSync);
            this.client.Send(syncCommand);
        }

        private void InitialConnect()
        {
            client.Connect();
        }
        private void Client_OnClose(object sender, CloseEventArgs e)
        {

            this.Message = "Disconnected";
            ConnectToServer();
         }

        private void ConnectToServer()
        {
            bool keepAlive = reconnectAttempts < maxAttempts;
            bool shouldTryAgain = !client.IsAlive && keepAlive;

            while (shouldTryAgain)
            {
                if (client == null) return;
                client.Connect();
                reconnectAttempts++;
                keepAlive = reconnectAttempts < maxAttempts;
                shouldTryAgain = !client.IsAlive && keepAlive;
            }
            if (client.IsAlive)
            {
                reconnectAttempts = 0;
                this.Message = "Connected.";
            }
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {

            if (e.Data.Equals(RemoSetupClient.DISCONNECTION_KEYWORD))
            {
                client.OnMessage -= Client_OnMessage;
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.Close();
                client = null;
                this.Message = "Wrong Username\nor Password";
            }

            messages.Add(e.Data);
            if (!this.keepRecord)
            {

                while (messages.Count > 1)
                {
                    messages.RemoveAt(0);
                }
            }

            try
            {
                Grasshopper.Kernel.GH_Document thisGH_Doc = this.OnPingDocument();
                if (thisGH_Doc != null)
                {
                    thisGH_Doc.ScheduleSolution(1, doc =>
                    {
                        var recepients = this.Params.Output[0].Recipients;
                        foreach (var item in recepients)
                        {
                            if (item is Grasshopper.Kernel.Parameters.Param_GenericObject)
                            {
                                if (item.Attributes.Parent.DocObject is RemoSharp.WebSocketClient.WSClientListen)
                                {
                                    if (this.autoUpdate)
                                    {
                                        item.Attributes.Parent.DocObject.ExpireSolution(true);
                                    }
                                    else
                                    {
                                        item.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Please Attach a Trigger to this component\nto read incoming messages");
                                    }
                                }
                            }
                        }
                    });
                }
            }
            catch
            {
            }
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            this.Message = "Connected";

            RemoNullCommand nullCommand = new RemoNullCommand(username);
            string connectionString = RemoCommand.SerializeToJson(nullCommand);
            client.Send(connectionString);

            reconnectAttempts = 0;

            Grasshopper.Instances.ActiveCanvas.Update();


            //this.OnPingDocument().ScheduleSolution(1, doc =>
            //{
                
            //    this.ExpireSolution(false);
            //});
        }

        private void CheckForDirectoryAndFileExistance(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path))) Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (!File.Exists(path))
            {
                using (var file = File.Create(path))
                {
                    byte[] byteArray = Encoding.ASCII.GetBytes("First Line");
                    file.Write(byteArray, 0, 0);
                    file.Close();
                }
            }

        }

        public void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            var thisGH_doc = this.OnPingDocument();
            if (thisGH_doc == null || this == null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseDown -= Canvas_MouseDown;
                System.Windows.Forms.MessageBox.Show("RemoSharp Interactions Disabled!", "RemoSharp Critical Error!");
                return;
            }

            downPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e);
            if (e.Button != MouseButtons.Left ||
              canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WindowSelectInteraction ||
              canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_PanInteraction ||
              canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_ZoomInteraction)
            {
                interaction = null;
                return;
            }
            if (canvas.ActiveInteraction == null) return;

            if (canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction ||
                canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction ||
                canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
            {
                //if (setup == 0) { }
                this.interaction = canvas.ActiveInteraction;
            }
        }

        public void Canvas_MouseUp(object sender, MouseEventArgs e)
        {

            if (this.OnPingDocument() == null || this == null)
            {
                Grasshopper.Instances.ActiveCanvas.MouseUp -= Canvas_MouseUp;
                //System.Windows.Forms.MessageBox.Show("Wire Interactions (up) Disabled!", "RemoSharp Critical Error!");
                return;
            }


            upPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e);
            if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction)
            {
                Type type = typeof(Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction);
                object mode = type
                  .GetField("m_mode", BindingFlags.NonPublic | BindingFlags.Instance)
                  .GetValue(interaction);
                IGH_Param source = type
                  .GetField("m_source", BindingFlags.NonPublic | BindingFlags.Instance)
                  .GetValue(interaction) as IGH_Param;
                IGH_Param target = type
                  .GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance)
                  .GetValue(interaction) as IGH_Param;

                RemoConnectType remoConnectType = RemoConnectType.None;
                if (mode.ToString().Equals("Replace"))
                {
                    remoConnectType = RemoConnectType.Replace;
                }
                else if (mode.ToString().Equals("Remove"))
                {
                    remoConnectType = RemoConnectType.Remove;
                }
                else
                {
                    remoConnectType = RemoConnectType.Add;
                }

                RemoConnectInteraction connectionInteraction = new RemoConnectInteraction();

                if (source.Attributes.HasInputGrip)
                {
                    if (source.Kind != GH_ParamKind.floating)
                    {
                        connectionInteraction = new RemoConnectInteraction(username, target, source, remoConnectType);
                    }
                    else
                    {
                        if (downPnt[0] < source.Attributes.Pivot.X)
                        {
                            connectionInteraction = new RemoConnectInteraction(username, target, source, remoConnectType);
                        }
                        else
                        {
                            connectionInteraction = new RemoConnectInteraction(username, source, target, remoConnectType);
                        }

                    }

                }
                else
                {
                    connectionInteraction = new RemoConnectInteraction(username, source, target, remoConnectType);
                }


                if (connectionInteraction.source != null && connectionInteraction.target != null)
                {
                    int outIndex = -1;
                    bool outIsSpecial = false;

                    if (connectionInteraction.source == null || connectionInteraction.target == null) return;
                    System.Guid outGuid = GetComponentGuidAnd_Output_Index(
                      connectionInteraction.source, out outIndex, out outIsSpecial);

                    int inIndex = -1;
                    bool inIsSpecial = false;

                    if (connectionInteraction.target == null || connectionInteraction.source == null) return;
                    System.Guid inGuid = GetComponentGuidAnd_Input_Index(
                      connectionInteraction.target, out inIndex, out inIsSpecial);

                    string outCompXML = RemoCommand.SerializeToXML(outGuid);
                    string inCompXML = RemoCommand.SerializeToXML(inGuid);

                    string outCompDocXML = RemoCommand.SerizlizeToSinglecomponentDocXML(outGuid);
                    string inCompDocXML = RemoCommand.SerizlizeToSinglecomponentDocXML(inGuid);

                    command = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid, connectionInteraction.RemoConnectType,
                        outCompXML, inCompXML,outCompDocXML,inCompDocXML);
                    SendCommands(command, commandRepeat, enable);

                }



            }
            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
            {
                float downPntX = downPnt[0];
                float downPntY = downPnt[1];
                float upPntX = upPnt[0];
                float upPntY = upPnt[1];

                //int moveX = upPntX - downPntX;
                //int moveY = upPntY - downPntY;

                //var movedObject = this.OnPingDocument().FindObject(new PointF(upPntX, upPntY), 1);
                //string movedObjGuid = movedObject.InstanceGuid.ToString();

                if (
                Math.Abs(downPntX - upPntX) > 1 &&
                Math.Abs(downPntY - upPntY) > 1)
                {
                    //try
                    //{
                    //command = "MoveComponent," + downPntX + "," + downPntY + "," + moveX + "," + moveY + "," + movedObjGuid;



                    var selection = this.OnPingDocument().SelectedObjects();

                    if (selection != null)
                    {

                        List<Guid> moveGuids = selection.Select(obj => obj.InstanceGuid).ToList();
                        float xDiff = upPntX - downPntX;
                        float yDiff = upPntY - downPntY;

                        command = new RemoMove(username, moveGuids, new Size((int)xDiff, (int)yDiff));

                        if (enable)
                        {
                            SendCommands(command, commandRepeat, enable);
                        }
                        downPnt[0] = 0;
                        downPnt[1] = 0;
                        upPnt[0] = 0;
                        upPnt[1] = 0;
                    }
                    //}
                    //catch
                    //{
                    //    command = "";
                    //}
                }
                else command = null;
            }


        }

        

        public void RemoCompSource_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            List<Guid> deleteGuids = new List<Guid>();
            var objs = e.Objects;

            if (this.OnPingDocument() == null || this == null)
            {
                Grasshopper.Instances.ActiveCanvas.Document.ObjectsDeleted -= RemoCompSource_ObjectsDeleted;
                System.Windows.Forms.MessageBox.Show("Object Deletion Interactions Disabled!", "RemoSharp Critical Error!");
                return;
            }

            foreach (var obj in objs)
            {
                //// a part of the recursive component creation message sending check
                //if (this.remoCreatedcomponens.Contains(dupObj.InstanceGuid))
                //{
                //    remoCreatedcomponens.Remove(dupObj.InstanceGuid);
                //}

                //if (obj.GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Relay")) continue;

                deleteGuids.Add(obj.InstanceGuid);

                //if (obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam"))
                //{
                //    RemoParam remoParamDeleted = (RemoParam)obj;
                //    Grasshopper.Instances.ActiveCanvas.MouseDown -= remoParamDeleted.ActiveCanvas_MouseDown;
                //}

            }

            command = new RemoDelete(username, deleteGuids);
            SendCommands(command, commandRepeat, enable);

            downPnt[0] = 0;
            downPnt[1] = 0;
            upPnt[0] = 0;
            upPnt[1] = 0;
            interaction = null;
        }

        public void RemoCompSource_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {

            var thisGH_doc = this.OnPingDocument();
            if (thisGH_doc == null || this == null)
            {
                Grasshopper.Instances.ActiveCanvas.Document.ObjectsAdded -= RemoCompSource_ObjectsAdded;
                System.Windows.Forms.MessageBox.Show("Object Creation Interactions Disabled!", "RemoSharp Critical Error!");
                return;
            }

            List<Guid> guids = new List<Guid>();
            List<string> associatedAttributes = new List<string>();
            List<string> componentTypes = new List<string>();
            List<string> componentStructures = new List<string>();
            List<string> specialParameters = new List<string>();

            var objs = e.Objects;
            foreach (var obj in objs)
            {
                var newCompGuid = obj.InstanceGuid;
                string newCompNickName = obj.NickName;
                var compTypeString = obj.GetType().ToString();
                var pivot = obj.Attributes.Pivot;

                switch (compTypeString)
                {
                    case ("Grasshopper.Kernel.Special.GH_Group"):
                        continue;
                    case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                        //obj.NickName = "local";
                        obj.SolutionExpired += RemoParameterizeSlider;
                        GroupObjParam(obj);
                        break;
                    case ("Grasshopper.Kernel.Special.GH_Panel"):
                        //obj.NickName = "local";
                        obj.SolutionExpired += RemoParameterizePanel;
                        GroupObjParam(obj);
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                        //obj.NickName = "local";
                        obj.SolutionExpired += RemoParameterizeColor;
                        GroupObjParam(obj,RemoParam.RemoParamSelectionKeyword);
                        break;
                    case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                        obj.SolutionExpired += RemoParameterizeMDSlider;
                        GroupObjParam(obj);
                        break;
                    case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                        obj.SolutionExpired += RemoParameterizeToggle;
                        GroupObjParam(obj);
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                        //obj.NickName = "local";
                        obj.SolutionExpired += RemoParameterizeButton;
                        GroupObjParam(obj);
                        break;
                    case ("Grasshopper.Kernel.Parameters.Param_Point"):
                        //obj.NickName = "local";
                        break;
                    case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                        break;
                    case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                        //obj.NickName = "local";
                        break;
                    case ("RemoSharp.RemoParams.RemoParamData"):
                        continue;
                    case ("ScriptComponents.Component_CSNET_Script"):
                        ScriptComponents.Component_CSNET_Script csComponent = (ScriptComponents.Component_CSNET_Script) obj;
                        break;
                    case ("Robots.Grasshopper.LibraryParam"):
                    case ("Robots.Grasshopper.LoadRobotSystem"):


                        string objectTypeDetails = obj.GetType().ToString();
                        var objectDetails = obj;

                        //continue;

                        break;
                    default:
                        break;
                }

                string associatedAttribute = "";
                string rpmType = "RemoSharp.RemoParams.RemoParam";
                if (compTypeString.Equals(rpmType))
                {
                    AddRemoParamDataComponent(obj, rpmType);
                    continue;
                }



                // check to see if this component has been created from remocreate command coming from outsite
                //bool alreadyMade = remoCreatedcomponens.Contains(newCompGuid);
                //if (alreadyMade) continue;
                //else
                //{
                //    remoCreatedcomponens.Add(newCompGuid);
                //}

                //adding info for RemoCreate Command
                guids.Add(newCompGuid);
                associatedAttributes.Add(associatedAttribute);
                componentTypes.Add(compTypeString);

                // from David Rutten's code from a google search
                // TO DO: add link to website
                var chunk = new GH_LooseChunk(null);
                obj.Write(chunk);

                string xml = chunk.Serialize_Xml();
                specialParameters.Add(xml);

                if (!compTypeString.Contains("IronPython.NewTypes.GhPython.Assemblies.DotNetCompiledComponent"))
                { 
                    componentStructures.Add(RecognizeStructure(compTypeString, xml, newCompGuid)); 
                }
                else
                {
                    componentStructures.Add("");
                }
            }

            if (guids.Count > 0)
            {
                if (guids.Count == 1 && componentTypes[0].Equals("Grasshopper.Kernel.Special.GH_Relay"))
                {
                    
                    var relay = (objs[0] as GH_Relay);

                    IGH_Param sourceOutput = null;
                    IGH_Param targetInput = null;
                    var ghEvent = this.OnPingDocument().FindWireAt(new PointF(mouseLocation.X,mouseLocation.Y),5,ref sourceOutput, ref targetInput);

                    Guid sourceGuid = sourceOutput.InstanceGuid;
                    Guid targetguid = targetInput.InstanceGuid;
                    int sourceOutputIndex = sourceOutput.Attributes.Parent == null ? -1 : FindOutputIndexFromGH_Param(sourceOutput, out sourceGuid);
                    int targetInputIndex = targetInput.Attributes.Parent == null ? -1 : FindInputIndexFromGH_Param(targetInput, out targetguid);

                    command = new RemoRelay(username,relay,sourceGuid,targetguid,sourceOutputIndex,targetInputIndex);
                    SendCommands(command, commandRepeat, enable);
                    
                }
                else
                {
                    RemoPartialDoc remoPartialDoc = new RemoPartialDoc(this.username, e.Objects.ToList());
                    SendCommands(remoPartialDoc, 5, enable);
                    //return;
                    //command = new RemoCreate(username, guids, associatedAttributes, componentTypes, componentStructures, specialParameters);

                    //SendCommands(command, commandRepeat, enable);
                }
                
            }
            else
            {
                command = null;
            }

            downPnt[0] = 0;
            downPnt[1] = 0;
            upPnt[0] = 0;
            upPnt[1] = 0;
            interaction = null;

        }    

        private int FindOutputIndexFromGH_Param(IGH_Param sourceOutput, out Guid parentGuid)
        {
            IGH_Component comp = (IGH_Component)sourceOutput.Attributes.Parent.DocObject;
            parentGuid = comp.InstanceGuid;
            return comp.Params.Output.IndexOf(sourceOutput);
        }

        private int FindInputIndexFromGH_Param(IGH_Param targetInput, out Guid parentGuid)
        {
            IGH_Component comp = (IGH_Component)targetInput.Attributes.Parent.DocObject;
            parentGuid = comp.InstanceGuid;
            return comp.Params.Input.IndexOf(targetInput);
        }

        public void RemoParameterizeMDSlider(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeMDSlider;
            Grasshopper.Kernel.Special.GH_MultiDimensionalSlider param = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
            {
                RemoParamMDSlider remoParam = new RemoParamMDSlider(this.username, param,false);
                string json = RemoCommand.SerializeToJson(remoParam);
                this.client.Send(json);
            }
        }

        public void RemoParameterizeToggle(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeToggle;
            Grasshopper.Kernel.Special.GH_BooleanToggle param = (Grasshopper.Kernel.Special.GH_BooleanToggle)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
            {
                RemoParamToggle remoParam = new RemoParamToggle(this.username, param);
                string json = RemoCommand.SerializeToJson(remoParam);
                this.client.Send(json);
            }
        }

        public void RemoParameterizeColor(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeColor;
            Grasshopper.Kernel.Special.GH_ColourSwatch param = (Grasshopper.Kernel.Special.GH_ColourSwatch)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected)
            {
                RemoParamColor remoParam = new RemoParamColor(this.username, param);
                string json = RemoCommand.SerializeToJson(remoParam);
                this.client.Send(json);
            }
        }

        public void RemoParameterizePanel(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizePanel;
            Grasshopper.Kernel.Special.GH_Panel param = (Grasshopper.Kernel.Special.GH_Panel)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected && status.noInput)
            {               
                RemoParamPanel remoParam = new RemoParamPanel(this.username, param);
                string json = RemoCommand.SerializeToJson(remoParam);
                this.client.Send(json);
            }
        }

        public void RemoParameterizeButton(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeButton;
            Grasshopper.Kernel.Special.GH_ButtonObject param = (Grasshopper.Kernel.Special.GH_ButtonObject)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
            {
                
                RemoParamButton remoParam = new RemoParamButton(this.username, param);
                string json = RemoCommand.SerializeToJson(remoParam);
                this.client.Send(json);
            }
        }

        public void RemoParameterizePointParam(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            //try
            //{
                if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizePointParam;
                Grasshopper.Kernel.Parameters.Param_Point param = (Grasshopper.Kernel.Parameters.Param_Point)sender;

                IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

                if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected && status.noInput)
                {
                    GH_LooseChunk chunk = new GH_LooseChunk(null);
                    param.WriteFull(chunk);
                    RemoParamPoint3d remoParam = new RemoParamPoint3d(this.username, param);
                    string json = RemoCommand.SerializeToJson(remoParam);
                    this.client.Send(json);
                }

            //}
            //catch
            //{
            //    Console.WriteLine("sync problem");
            //}
            
        }

        

        public void RemoParameterizeSlider(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            try
            {
                if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeSlider;
                Grasshopper.Kernel.Special.GH_NumberSlider param = (Grasshopper.Kernel.Special.GH_NumberSlider)sender;

                IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

                if (status.tabKeyIsDown && status.isRemoParamGrouped)
                {

                    RemoParamSlider remoParamSlider = new RemoParamSlider(this.username, param);
                    string json = RemoCommand.SerializeToJson(remoParamSlider);
                    this.client.Send(json);
                }
            }
            catch
            {
                Console.WriteLine("sync problem");

            }

        }

        public void GroupObjParam(IGH_DocumentObject obj)
        {
            this.OnPingDocument().ScheduleSolution(1, doc => 
            {
                GH_Group group = new GH_Group();
                group.CreateAttributes();
                group.AddObject(obj.InstanceGuid);
                group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
                group.NickName = RemoParam.RemoParamKeyword;
                this.OnPingDocument().AddObject(group, false);
            });
        }

        public void GroupRemoParamDataComponents(RemoParam remoParamComp, RemoParamData remoParamData)
        {

            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.AddObject(remoParamComp.InstanceGuid);
            group.AddObject(remoParamData.InstanceGuid);
            Random rand = new Random();
            int hue1 = rand.Next(100, 255);
            int hue2 = rand.Next(100, 255);
            int hue3 = rand.Next(100, 255);
            group.Colour = System.Drawing.Color.FromArgb(50, hue1, hue2, hue3);
            group.Border = GH_GroupBorder.Blob;
            group.NickName = "";

            remoParamComp.groupGuid = group.InstanceGuid;

            this.OnPingDocument().AddObject(group, false);
        }

        public Color FromHSVA(double hue, double saturation, double value, double alpha)
        {
            if (hue < 0 || hue >= 360) throw new ArgumentOutOfRangeException(nameof(hue), "Hue must be between 0 and 360");
            if (saturation < 0 || saturation > 1) throw new ArgumentOutOfRangeException(nameof(saturation), "Saturation must be between 0 and 1");
            if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 1");
            if (alpha < 0 || alpha > 1) throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be between 0 and 1");

            double chroma = value * saturation;
            double hueSection = hue / 60.0;
            double x = chroma * (1 - Math.Abs(hueSection % 2 - 1));
            double m = value - chroma;

            double red = 0, green = 0, blue = 0;
            if (hueSection >= 0 && hueSection < 1)
            {
                red = chroma;
                green = x;
            }
            else if (hueSection >= 1 && hueSection < 2)
            {
                red = x;
                green = chroma;
            }
            else if (hueSection >= 2 && hueSection < 3)
            {
                green = chroma;
                blue = x;
            }
            else if (hueSection >= 3 && hueSection < 4)
            {
                green = x;
                blue = chroma;
            }
            else if (hueSection >= 4 && hueSection < 5)
            {
                red = x;
                blue = chroma;
            }
            else if (hueSection >= 5 && hueSection < 6)
            {
                red = chroma;
                blue = x;
            }

            red = (red + m) * 255;
            green = (green + m) * 255;
            blue = (blue + m) * 255;
            alpha = alpha * 255; // Convert alpha to 0-255 range

            return Color.FromArgb((int)alpha, (int)red, (int)green, (int)blue);
        }

        public void GroupObjParam(IGH_DocumentObject obj, string extraText)
        {
            this.OnPingDocument().ScheduleSolution(1, doc => 
            {
                GH_Group group = new GH_Group();
                group.CreateAttributes();
                group.AddObject(obj.InstanceGuid);
                group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
                group.NickName = RemoParam.RemoParamKeyword + extraText;


                this.OnPingDocument().AddObject(group, false);

            });
            
        }

        private void AddRemoParamDataComponent(IGH_DocumentObject obj, string rpmType)
        {
            //List<RemoSharp.RemoParams.RemoParam> rpmList = this.OnPingDocument().Objects
            //    .Where(comps => comps.GetType().ToString().Equals(rpmType))
            //    .ToList().Select(comps => (RemoSharp.RemoParams.RemoParam)comps).ToList();


            //List<int> rpmIndeceis = rpmList.Select(comps => comps.Message == null ? 0 : Convert.ToInt32(comps.Message.Replace("RPM", ""))).ToList();

            //rpmIndeceis.Sort();

            //int lastRPMIndex = rpmIndeceis[rpmIndeceis.Count - 1];

            //string newRpmNickname = string.Format("RPM{0}", lastRPMIndex + 1);

            //var cast = (RemoSharp.RemoParams.RemoParam)obj;
            //cast.Message = newRpmNickname;
            //cast.message = newRpmNickname;



            //dataComp.Message = newRpmNickname;
            //dataComp.message = newRpmNickname;

            RemoParam rpmComp = obj as RemoParam;
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            rpmComp.Write(chunk);
            string xml = chunk.Serialize_Xml();

            

            this.OnPingDocument().ScheduleSolution(0, doc =>
            {
                var rpmPivot = obj.Attributes.Pivot;
                PointF dataPivot = new PointF(rpmPivot.X + 36, rpmPivot.Y);

                Guid dataGuid = Guid.NewGuid();
                RemoParam remoParamComp = obj as RemoParam;
                RemoParamData dataComp = new RemoParamData();
                dataComp.CreateAttributes();
                dataComp.Attributes.Pivot = dataPivot;
                dataComp.Params.RepairParamAssociations();
                dataComp.ExpireSolution(true);

                GH_LooseChunk chunk2 = new GH_LooseChunk(null);
                dataComp.Write(chunk2);


                RemoCreate remoCreate = new RemoCreate(this.username
                    , new List<Guid>() { rpmComp.InstanceGuid }
                    , new List<String>() { chunk2.Serialize_Xml() }
                    , new List<string>() { rpmComp.GetType().FullName }
                    , new List<string>() { xml }
                    , new List<string>() { xml }
                    );

                SendCommands(remoCreate, commandRepeat, enable);

                

                

                this.OnPingDocument().AddObject(dataComp, false);

                GroupRemoParamDataComponents(remoParamComp, dataComp);

            });
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
                return RemoSharp.Properties.Resources.RemoSharp.ToBitmap();
            }
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

        string RecognizeStructure(string typeName, string specialContent, Guid newCompGuid)
        {
            var thisDoc = this.OnPingDocument();
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
            var dupObj = (IGH_DocumentObject)Activator.CreateInstance(type);
            // creating atts to create the pivot point
            // this pivot point can be anywhere
            dupObj.CreateAttributes();

            var chunk2 = new GH_LooseChunk(null);
            chunk2.Deserialize_Xml(specialContent);
            dupObj.Read(chunk2);

            dupObj.ExpireSolution(false);

            if (dupObj is Grasshopper.Kernel.IGH_Component)
            {
                Grasshopper.Kernel.IGH_Component structure = (Grasshopper.Kernel.IGH_Component)dupObj;
                foreach (var item in structure.Params.Input)
                {
                    item.RemoveAllSources();
                }
            }
            else
            {
                Grasshopper.Kernel.IGH_Param structure = (Grasshopper.Kernel.IGH_Param)dupObj;
                structure.RemoveAllSources();
            }

            var structureChunk = new GH_LooseChunk(null);
            dupObj.Write(structureChunk);

            string structureXml = structureChunk.Serialize_Xml();
            return structureXml;

        }

        IGH_ParamConditionStatus FindIGH_ParamStatus(IGH_Param param)
        {
            bool isSelected = param.Attributes.Selected;
            bool noInput = param.SourceCount < 1;

            bool tabKeyIsDown = this.remoParamModeActive;

            bool paramIsNotPointComp = param is Grasshopper.Kernel.Parameters.Param_Point;

            bool isIn_gh_group = false;
            bool mouseHoverOnExpiredParam = false;
            if (tabKeyIsDown)
            {

                var gh_groups = this.OnPingDocument().Objects.AsParallel().AsUnordered()
                    .Where(obj => obj is Grasshopper.Kernel.Special.GH_Group && obj.NickName.Contains(RemoParam.RemoParamKeyword))
                    .Select(obj => (GH_Group) obj);
                foreach (var item in gh_groups)
                {
                    if (item.ObjectIDs.Contains(param.InstanceGuid))
                    {
                        isIn_gh_group = true;
                        break;
                    }
                }
                if (isIn_gh_group && !paramIsNotPointComp)
                {
                    var hoverObj = Grasshopper.Instances.ActiveCanvas.Document.FindObject(this.mouseLocation, 2);
                    if (hoverObj != null)
                    {
                        mouseHoverOnExpiredParam = hoverObj.InstanceGuid == param.InstanceGuid;
                    }
                }
                
            }

            IGH_ParamConditionStatus status = new IGH_ParamConditionStatus(isSelected, noInput, tabKeyIsDown, isIn_gh_group, mouseHoverOnExpiredParam,paramIsNotPointComp);
            return status;
        }


        public class IGH_ParamConditionStatus
        {
            public bool isSelected;
            public bool noInput;
            public bool tabKeyIsDown;
            public bool mouseHoverOnExpiredParam;
            public bool paramIsNotPointComp;
            public bool isRemoParamGrouped;

            public IGH_ParamConditionStatus
                (bool isSelected, bool noInput, bool tabKeyIsDown, bool isRemoParamGrouped,
             bool mouseHoverOnExpiredParam, bool paramIsNotPointComp)
            {
                this.isSelected = isSelected;
                this.noInput = noInput;
                this.tabKeyIsDown = tabKeyIsDown;
                this.isRemoParamGrouped= isRemoParamGrouped;
                this.mouseHoverOnExpiredParam = mouseHoverOnExpiredParam;
                this.paramIsNotPointComp = paramIsNotPointComp;
            }

        }

        private IGH_Param FindRelaySourceOutput(RemoRelay remoRelay)
        {
            IGH_Component comp = (IGH_Component)this.OnPingDocument().FindObject(remoRelay.sourceGuid, false);
            return (IGH_Param)comp.Params.Output[remoRelay.sourceIndex];
        }

        private IGH_Param FindRelayTargetInput(RemoRelay remoRelay)
        {
            IGH_Component comp = (IGH_Component)this.OnPingDocument().FindObject(remoRelay.targetGuid, false);
            return (IGH_Param)comp.Params.Input[remoRelay.targetIndex];
        }


        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("F1C5724E-4417-41B2-A3F4-028990100603"); }
        }
    }
}