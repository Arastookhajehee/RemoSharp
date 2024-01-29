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

namespace RemoSharp
{
    public class RemoCompSource : GHCustomComponent
    {
        WebSocket client;
        int commandRepeat = 5;
        Grasshopper.GUI.Canvas.GH_Canvas canvas;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;

        //ToggleSwitch deleteToggle;
        ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;

        bool enable = false;
        bool movingMode = false;
        bool subscribed = false;

        int counterTest = 0;

        //public List<Guid> remoCreatedcomponens = new List<Guid>();

        string username = "";
        string password = "";

        float[] downPnt = {0,0};
        float[] upPnt = { 0, 0 };

        PushButton setupButton;


        float[] PointFromCanvasMouseInteraction(Grasshopper.GUI.Canvas.GH_Viewport vp, MouseEventArgs e)
        {
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            float x = mouseEvent.CanvasX;
            float y = mouseEvent.CanvasY;
            float[] coords = {x,y};
            return coords;
        }

        /// <summary>
        /// Initializes a new instance of the RemoCompSource class.
        /// </summary>
        public RemoCompSource()
          : base("RemoCompSource", "RemoSetup",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoSetup")
        {
            this.NickName = "RemoSetup";
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


            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("WSClient", "wsc", "RemoSharp's Command Websocket Client", GH_ParamAccess.item);
            pManager.AddTextParameter("Password", "pass", "Password to this session",GH_ParamAccess.item,"password");
            pManager.AddBooleanParameter("syncSend", "syncSend", "Syncs this grasshopper script for all other connected clients", GH_ParamAccess.item, false);
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
                    #endregion

                    canvas.MouseUp += Canvas_MouseUp;

                    #region Add Object Sub
                    this.OnPingDocument().ObjectsAdded += RemoCompSource_ObjectsAdded;
                    #endregion

                    #region Remove Object Sub
                    this.OnPingDocument().ObjectsDeleted += RemoCompSource_ObjectsDeleted;
                    #endregion

                    subscribed = true;
                }
            }
            else
            {
                canvas = Grasshopper.Instances.ActiveCanvas;
                #region Wire Connection and Move Sub
                canvas.MouseDown -= Canvas_MouseDown;
                #endregion

                canvas.MouseUp -= Canvas_MouseUp;

                #region Add Object Sub
                this.OnPingDocument().ObjectsAdded -= RemoCompSource_ObjectsAdded;
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted -= RemoCompSource_ObjectsDeleted;
                #endregion

                subscribed = false;
            }

            this.ExpireSolution(true);
        }

        private void MovingModeSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            movingMode = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        private void SendCommands(RemoCommand command, int commandRepeat,bool enabled)
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
                PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 336 + yShift);
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
                panel.Attributes.Bounds = new Rectangle((int) panelPivot.X, (int) panelPivot.Y, 100, 45);
                panel.SetUserText("username");
                panel.NickName = "RemoSetup";

                // componentName
                var passPanel = new Grasshopper.Kernel.Special.GH_Panel();
                passPanel.CreateAttributes();
                passPanel.Attributes.Pivot = passPanelPivot;
                passPanel.Attributes.Bounds = new Rectangle((int)passPanelPivot.X, (int)passPanelPivot.Y, 100, 45);
                passPanel.SetUserText("password");
                passPanel.NickName = "RemoSetup";


                // componentName
                var wscComp = new RemoSharp.WebSocketClient.WebSocketClient();
                wscComp.CreateAttributes();
                wscComp.Attributes.Pivot = wscPivot;
                wscComp.Params.RepairParamAssociations();
                wscComp.NickName = "RemoSetup";
                wscComp.autoUpdateSwitch.CurrentValue = false;
                wscComp.keepRecordSwitch.CurrentValue = true;
                wscComp.autoUpdate = false;
                wscComp.keepRecord = true;

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
                    this.OnPingDocument().AddObject(wscComp, true);
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
                    targetComp.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    this.Params.Input[0].AddSource(panel);
                    this.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    this.Params.Input[2].AddSource(passPanel);
                    this.Params.Input[3].AddSource(commandCompButton);
                    wscComp.Params.Input[0].AddSource(addressOutPuts[0]);
                    wscComp.Params.Input[1].AddSource(wscButton);
                    wscComp.Params.Input[2].AddSource(wscToggle);

                    listenComp.Params.Input[0].AddSource(wscComp.Params.Output[0]);

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
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool syncThisCanvas = false;
            // getting the username information
            DA.GetData(0, ref username);
            DA.GetData(1, ref client);
            DA.GetData(2, ref password);
            DA.GetData(3, ref syncThisCanvas);

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

                RemoSharp.WebSocketClient.WebSocketClient clientComp = (RemoSharp.WebSocketClient.WebSocketClient)this.Params.Input[1].Sources[0].Attributes.Parent.DocObject;
                WebSocket client = clientComp.client;

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
                        string listItemParamNickName = connectionInteraction.source.NickName;

                        command = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid,
                            outIndex, inIndex, outIsSpecial, inIsSpecial, connectionInteraction.RemoConnectType, sourceX, sourceY, targetX, targetY,
                            sourceNickname, targetNickname, listItemParamNickName);
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
                    Math.Abs(downPntX - upPntX ) > 1 &&
                    Math.Abs(downPntY - upPntY) > 1)
                    {
                    //try
                    //{
                    //command = "MoveComponent," + downPntX + "," + downPntY + "," + moveX + "," + moveY + "," + movedObjGuid;


                    
                        var selection = this.OnPingDocument().SelectedObjects();

                        if (selection != null)
                        {

                        List<Guid> moveGuids = selection.Select(obj => obj.InstanceGuid).ToList();
                        float xDiff = upPntX- downPntX;
                        float yDiff = upPntY- downPntY;
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
                //if (this.remoCreatedcomponens.Contains(obj.InstanceGuid))
                //{
                //    remoCreatedcomponens.Remove(obj.InstanceGuid);
                //}
                deleteGuids.Add(obj.InstanceGuid);

                if (obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam"))
                {
                    RemoParam remoParamDeleted = (RemoParam)obj;
                    //Grasshopper.Instances.ActiveCanvas.MouseDown -= remoParamDeleted.ActiveCanvas_MouseDown;
                }

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
            List<string> componentTypes = new List<string>();
            List<string> specialParameters_s = new List<string>();


            var objs = e.Objects;
            foreach (var obj in objs)
            {
                var newCompGuid = obj.InstanceGuid;
                string newCompNickName = obj.NickName;
                var compTypeString = obj.GetType().ToString();
                var pivot = obj.Attributes.Pivot;

                switch (compTypeString)
                {
                    case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                    case ("Grasshopper.Kernel.Special.GH_Panel"):
                    case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                    case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                    case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                    case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                    case ("Grasshopper.Kernel.Parameters.Param_Point"):
                    case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                    case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                        obj.NickName = "local";

                        break;

                    default:
                        break;
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
                componentTypes.Add(compTypeString);


                if (obj is IGH_Component)
                {
                    IGH_Component objComponent = (IGH_Component)obj;
                }
                else if (obj is IGH_Param)
                {
                    IGH_Param objParam = (IGH_Param)obj;
                }

                

                if (compTypeString.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                {
                    Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider)obj;
                    decimal minBound = sliderComponent.Slider.Minimum;
                    decimal maxBound = sliderComponent.Slider.Maximum;
                    decimal currentValue = sliderComponent.Slider.Value;
                    int accuracy = sliderComponent.Slider.DecimalPlaces;
                    var sliderType = sliderComponent.Slider.Type;
                    string specialParts = minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType;

                    specialParameters_s.Add(specialParts);
                }
                else if (compTypeString.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                {
                    Grasshopper.Kernel.Special.GH_Panel panelComponent = (Grasshopper.Kernel.Special.GH_Panel)obj;
                    bool multiLine = panelComponent.Properties.Multiline;
                    bool drawIndicies = panelComponent.Properties.DrawIndices;
                    bool drawPaths = panelComponent.Properties.DrawPaths;
                    bool wrap = panelComponent.Properties.Wrap;
                    Grasshopper.Kernel.Special.GH_Panel.Alignment alignment = panelComponent.Properties.Alignment;
                    float panelSizeX = panelComponent.Attributes.Bounds.Width;
                    float panelSizeY = panelComponent.Attributes.Bounds.Height;

                    string content = panelComponent.UserText;
                    string specialParts = multiLine + "," + drawIndicies + "," + drawPaths + "," + wrap + "," + alignment.ToString() + "," + panelSizeX + "," + panelSizeY + "," + content;

                    specialParameters_s.Add(specialParts);
                }
                else
                {
                    specialParameters_s.Add("");
                }


            }

            if (guids.Count > 0)
            {
                //command = new RemoCreate(username, guids, componentTypes, nickNames,
                //Xs, Ys, isSpecials, specialParameters_s, wireHistories);

                SendCommands(command, commandRepeat, enable);
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

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9a3a9712-9b99-409d-9c02-f6b338305f5b"); }
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


    }
}