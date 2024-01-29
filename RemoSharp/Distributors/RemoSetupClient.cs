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

namespace RemoSharp
{
    public class RemoSetupClient : GHCustomComponent
    {
        public List<string> messages = new List<string>();
        public WebSocket client;
        bool connect = false;
        bool keepAlive = false;
        bool needsRestart = false;
        bool preventUndo = true;


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
        ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;

        public bool enable = false;
        public bool movingMode = false;
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


            movingModeSwitch = new ToggleSwitch("Moving Mode", "It is recommended to keep it turned off if the user does not wish to move components around", false);
            movingModeSwitch.OnValueChanged += MovingModeSwitch_OnValueChanged;
            enableSwitch = new ToggleSwitch("Enable Interactions", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            AddCustomControl(enableSwitch);
            AddCustomControl(movingModeSwitch);


            
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
            pManager.AddBooleanParameter("KeepAlive", "keepAlive", "", GH_ParamAccess.item);

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


            if (enable)
            {
                if (!subscribed)
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


                    subscribed = true;
                }
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
            }

            this.ExpireSolution(true);
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
                    this.OnPingDocument().UndoServer.Clear();
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
                System.Windows.Forms.MessageBox.Show("PLEASE DO NOT USE UNDO OR REDO", "Syncing Error!");
            }
        }

        private void ActiveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            this.mouseLocation = mouseEvent.CanvasLocation;
        }

        private void MovingModeSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            movingMode = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        private void SendCommands(RemoCommand command, int commandRepeat, bool enabled)
        {
            if (!enabled) return;
            string cmdJson = RemoCommand.SerializeToJson(command);
            for (int i = 0; i < commandRepeat; i++)
            {
                client.Send(cmdJson);
            }
        }

        private void SetupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (this.Params.Input[0].Sources.Count > 0) return;
            if (currentValue)
            {
                int xShift = 2;
                int yShift = 80;
                PointF pivot = this.Attributes.Pivot;
                PointF wscButtonPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF wscTogglePivot = new PointF(pivot.X + xShift - 216, pivot.Y - 197 + yShift);
                PointF triggerPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 415 + yShift);
                PointF panelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift);
                PointF passPanelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift + 45);
                //PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 336 + yShift);
                PointF listenPivot = new PointF(pivot.X + xShift + 330, pivot.Y - 334 + yShift);

                PointF targetPivot = new PointF(pivot.X + xShift + 200, pivot.Y);
                PointF commandPivot = new PointF(pivot.X + xShift + 598, pivot.Y - 312 + yShift);
                PointF commandButtonPivot = new PointF(pivot.X + xShift + 350, pivot.Y - 254 + yShift);

                #region setup components
                // button
                Grasshopper.Kernel.Special.GH_ButtonObject wscButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                wscButton.CreateAttributes();
                wscButton.Attributes.Pivot = wscButtonPivot;
                wscButton.NickName = "RemoSetup";

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


                    this.OnPingDocument().AddObject(wscButton, true);
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
                    this.Params.Input[3].AddSource(panel);
                    //this.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    this.Params.Input[4].AddSource(passPanel);
                    this.Params.Input[5].AddSource(commandCompButton);
                    this.Params.Input[0].AddSource(addressOutPuts[0]);
                    this.Params.Input[1].AddSource(wscButton);
                    this.Params.Input[2].AddSource(wscToggle);

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
            DA.GetData(2, ref keepAlive);

            bool syncThisCanvas = false;

            // getting the username information
            DA.GetData(3, ref username);
            //DA.GetData(1, ref client);
            DA.GetData(4, ref password);
            DA.GetData(5, ref syncThisCanvas);

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
                    //initialTask.Wait();

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





            //if (setup == 0)
            //{

            //}


            if (syncThisCanvas)
            {
                var currentCanvas = Grasshopper.Instances.ActiveCanvas;
                currentCanvas.Document.ObjectsAdded -= this.RemoCompSource_ObjectsAdded;
                currentCanvas.Document.ObjectsDeleted -= this.RemoCompSource_ObjectsDeleted;
                currentCanvas.MouseUp -= this.Canvas_MouseUp;
                currentCanvas.MouseDown -= this.Canvas_MouseDown;

                this.OnPingDocument().DeselectAll();

                string savePath = @"C:\temp\RemoSharp\saveTempFile.ghx";
                string openPath = @"C:\temp\RemoSharp\openTempFile" + username + ".ghx";
                string finalPath = @"C:\temp\RemoSharp\finalTempFile" + username + ".ghx";

                CheckForDirectoryAndFileExistance(savePath);
                CheckForDirectoryAndFileExistance(openPath);
                CheckForDirectoryAndFileExistance(finalPath);

                Grasshopper.Kernel.GH_DocumentIO saveDoc = new GH_DocumentIO(this.OnPingDocument());
                bool saveDocR = saveDoc.SaveQuiet(savePath);

                while (true)
                {
                    try
                    {
                        System.IO.File.Copy(savePath, openPath, true);
                        break;
                    }
                    catch { }
                }

                Grasshopper.Kernel.GH_DocumentIO openDoc = new GH_DocumentIO();
                openDoc.Open(openPath);

                for (int i = openDoc.Document.ObjectCount - 1; i > -1; i--)
                {
                    var obj = openDoc.Document.Objects[i];
                    if (obj.NickName.ToUpper().Contains("RemoSetup".ToUpper()) ||
                        obj.GetType().ToString().Equals("RemoSharp.RemoCompSource"))
                    {
                        openDoc.Document.RemoveObject(obj, false);
                    }

                }
                openDoc.SaveQuiet(savePath);
                openDoc.Document.Dispose();

                currentCanvas.Document.ObjectsAdded += this.RemoCompSource_ObjectsAdded;
                currentCanvas.Document.ObjectsDeleted += this.RemoCompSource_ObjectsDeleted;
                currentCanvas.MouseUp += this.Canvas_MouseUp;
                currentCanvas.MouseDown += this.Canvas_MouseDown;

                try
                {
                    string content = "";
                    using (StreamReader sr = new StreamReader(savePath))
                    {
                        content = sr.ReadToEnd();

                        RemoCanvasSync remoCanvasSync = new RemoCanvasSync(username, content);
                        string cmdJson = RemoCommand.SerializeToJson(remoCanvasSync);
                        if (client != null)
                        {
                            client.Send(cmdJson);
                        }
                        sr.Close();
                    }
                }
                catch { }
            }





            //int commandRepeatCount = 5;
            //DA.SetData(0,command);

            //if (setup > 100) setup = 5;
            //if (commandReset > commandRepeatCount) command = new RemoNullCommand(username);

            //setup++;
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
            while (!client.IsAlive && keepAlive)
            {
                client.Connect();
            }
            if (client.IsAlive)
            {
                this.Message = "Connected.";
            }
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {

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
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                this.ExpireSolution(false);
            });
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

                    var sourceParentComponent = this.OnPingDocument().FindObject(outGuid, false);
                    var targetParentComponent = this.OnPingDocument().FindObject(inGuid, false);
                    float sourceX = sourceParentComponent.Attributes.Pivot.X;
                    float sourceY = sourceParentComponent.Attributes.Pivot.Y;
                    float targetX = targetParentComponent.Attributes.Pivot.X;
                    float targetY = targetParentComponent.Attributes.Pivot.Y;
                    string sourceNickname = sourceParentComponent.NickName;
                    string targetNickname = targetParentComponent.NickName;
                    string listItemParamNickname = connectionInteraction.source.NickName;

                    string outCompXML = GetXMLCodeFromGuid(outGuid);
                    string inCompXML = GetXMLCodeFromGuid(inGuid);


                    command = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid, connectionInteraction.RemoConnectType,
                        outCompXML, inCompXML);
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

                        if (movingMode)
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

        private string GetXMLCodeFromGuid(Guid guid)
        {
            var component = this.OnPingDocument().FindObject(guid, false);

            GH_LooseChunk chunk = new GH_LooseChunk(null);

            component.Write(chunk);
            return chunk.Serialize_Xml();
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
                    associatedAttribute = AddRemoParamDataComponent(obj, rpmType);
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
                    command = new RemoCreate(username, guids, associatedAttributes, componentTypes, componentStructures, specialParameters);

                    SendCommands(command, commandRepeat, enable);
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
            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.AddObject(obj.InstanceGuid);
            group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
            group.NickName = RemoParam.RemoParamKeyword;

            this.OnPingDocument().AddObject(group, false);
        }

        public void GroupObjParam(IGH_DocumentObject obj, string extraText)
        {
            GH_Group group = new GH_Group();
            group.CreateAttributes();
            group.AddObject(obj.InstanceGuid);
            group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
            group.NickName = RemoParam.RemoParamKeyword + extraText;

            this.OnPingDocument().AddObject(group, false);
        }

        private string AddRemoParamDataComponent(IGH_DocumentObject obj, string rpmType)
        {
            List<RemoSharp.RemoParams.RemoParam> rpmList = this.OnPingDocument().Objects
                .Where(comps => comps.GetType().ToString().Equals(rpmType))
                .ToList().Select(comps => (RemoSharp.RemoParams.RemoParam)comps).ToList();


            List<int> rpmIndeceis = rpmList.Select(comps => comps.Message == null ? 0 : Convert.ToInt32(comps.Message.Replace("RPM", ""))).ToList();

            rpmIndeceis.Sort();

            int lastRPMIndex = rpmIndeceis[rpmIndeceis.Count - 1];

            string newRpmNickname = string.Format("RPM{0}", lastRPMIndex + 1);

            var cast = (RemoSharp.RemoParams.RemoParam)obj;
            cast.Message = newRpmNickname;

            var rpmPivot = obj.Attributes.Pivot;
            PointF dataPivot = new PointF(rpmPivot.X - 54, rpmPivot.Y + 103);

            RemoParamData dataComp = new RemoParamData();
            dataComp.CreateAttributes();
            dataComp.Attributes.Pivot = dataPivot;
            dataComp.Params.RepairParamAssociations();
            dataComp.Message = newRpmNickname;

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            dataComp.Write(chunk);

            this.OnPingDocument().ScheduleSolution(0, doc =>
            {
                this.OnPingDocument().AddObject(dataComp, false);
            });

            return chunk.Serialize_Xml();
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
                return RemoSharp.Properties.Resources.SourceComp.ToBitmap();
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