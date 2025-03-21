﻿using GH_IO.Serialization;
using GHCustomControls;
using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using RemoSharp.RemoCommandTypes;
using RemoSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using WebSocketSharp;



namespace RemoSharp.Distributors
{
    public enum SubcriptionType
    {
        Subscribe,
        Unsubscribe,
        Skip,
        Resubscribe
    }
    public class RemoSetupClientV3 : GHCustomComponent
    {
        public static string uncommonSplitCharacter = "Ⅎ";
        public List<string> messages = new List<string>();
        public WebSocket client;

        public NonComponetCommandExecutor nonComponentRemoSetupV3;
        public RemoCommand lastCommand = null;
        public Guid lastRpmGuid = Guid.Empty;

        // periodic timber  for every 60 secs
        System.Timers.Timer timer = new System.Timers.Timer(60000);
        //public System.Timers.timer drawingSyncTimer = new System.Timers.timer(1000);
        int timerCount = 0;
        bool preventUndo = true;
        bool controlDown = false;
        bool shiftDown = false;
        public static string DISCONNECTION_KEYWORD = "<<DisconnectFromRemoSharpServer>>";
        public static string SESSION_ID_KEYWORD = "<<SessionIDChangeMidSession>>";
        public static string CAPACITY_KEYWORD = "<<ServerCapacityReached>>";
        public static string ALREADY_CONNECTED = "<<UserIsAlreadyConnected>>";
        public static string CONNECTION_SUCCESSFUL = "<<ConnectionSuccessful>>";

        public static Dictionary<string, string> ConnectionMessagePairs = new Dictionary<string, string>()
        {
            {DISCONNECTION_KEYWORD, "Wrong Username\nor Password"},
            {SESSION_ID_KEYWORD, "Inconsistent Session ID"},
            {CAPACITY_KEYWORD, "Server Is Full"},
            {ALREADY_CONNECTED, "Already Connected"},
            {CONNECTION_SUCCESSFUL, "Connected"}
        };

        PushButton setupButton;
        ToggleSwitch undoPreventionSwitch;
        ToggleSwitch enableSwitch;
        ToggleSwitch mainSwitch;
        ToggleSwitch allowScriptsSwitch;
        public GHCustomControls.Label usernameLabel;

        public bool autoUpdate = true;
        public bool keepRecord = false;
        bool listen = true;
        public bool isMain = false;
        public bool allowScripts = true;

        int commandRepeat = 1;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;

        public bool enable = false;

        int annotationStringLength = 0;
        public Dictionary<Guid, string> scribleHistory = new Dictionary<Guid, string>();
        public Dictionary<Guid, int> markupHistory = new Dictionary<Guid, int>();



        public List<Guid> remoCommandIDs = new List<Guid>();

        internal string url = "";
        public string username = "";
        internal string password = "";
        public string sessionID = "";

        float[] downPnt = { 0, 0 };
        float[] upPnt = { 0, 0 };

        List<Guid> addedObjects = new List<Guid>();
        public bool remoParamModeActive = false;
        public PointF mouseLocation = PointF.Empty;
        Guid hoverParam = Guid.Empty;

        DateTime recconnectTimer = DateTime.MinValue;

        public List<IGH_DocumentObject> subscribedObjs = new List<IGH_DocumentObject>();


        System.Timers.Timer remoParamExecution = new System.Timers.Timer(200);
        public List<RemoParameter> remoParams = new List<RemoParameter>();

        /// <summary>
        /// Initializes a new instance of the RemoSetupClientV3 class.
        /// </summary>
        public RemoSetupClientV3()
          : base("RemoSetupV3", "RemoSetup",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoSetup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            setupButton = new PushButton("SetUp",
                   "Creates The Required RemoSharp Components to Connect to a Session.", "SetUp");
            setupButton.OnValueChanged += SetupButton_OnValueChanged;
            AddCustomControl(setupButton);

            usernameLabel = new GHCustomControls.Label("username", "username", "username");
            usernameLabel.Border = true;
            AddCustomControl(usernameLabel);

            enableSwitch = new ToggleSwitch("Connect", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;
            AddCustomControl(enableSwitch);

            undoPreventionSwitch = new ToggleSwitch("No Undo", "Warns the user not to use undo or redo", true);
            undoPreventionSwitch.OnValueChanged += UndoPreventionSwitch_OnValueChanged;
            AddCustomControl(undoPreventionSwitch);

            allowScriptsSwitch = new ToggleSwitch("Allow Scripts", "Allow Scripts", true);
            allowScriptsSwitch.OnValueChanged += AllowScriptsSwitch_OnValueChanged;
            AddCustomControl(allowScriptsSwitch);

            mainSwitch = new ToggleSwitch("Main", "Main", false);
            mainSwitch.OnValueChanged += MainSwitch_OnValueChanged;
            AddCustomControl(mainSwitch);

            //pManager.AddTextParameter("url", "url", "", GH_ParamAccess.item, "");
            //pManager.AddTextParameter("session", "session", "", GH_ParamAccess.item, "");
        }

        private void AllowScriptsSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            allowScripts = Convert.ToBoolean(e.Value);
        }

        private void MainSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            isMain = Convert.ToBoolean(e.Value);
        }

        private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {


            enable = Convert.ToBoolean(e.Value);

            var comp = sender as ToggleSwitch;

            //if (this.Params.Input[0].SourceCount == 0)
            //{
            //    this.enableSwitch.CurrentValue = false;
            //    return;
            //}




            if (enable)
            {
                this.nonComponentRemoSetupV3 = new NonComponetCommandExecutor(this.OnPingDocument(), this);
                remoParamExecution = new System.Timers.Timer(200);
                remoParamExecution.AutoReset = true;
                remoParamExecution.Elapsed += RemoParamExecution_Elapsed;
                remoParamExecution.Start();


                var thisDoc = this.OnPingDocument();
                if (thisDoc == null) return;
                if (string.IsNullOrEmpty(this.url)) return;
                this.NickName = "RemoSetup";
                AddInteractionButtonsToTopBar(SubcriptionType.Subscribe);

                client = new WebSocket(url);
                //this.Message = "Connecting";

                SubcriptionType subType = SubcriptionType.Subscribe;
                SetUpRemoSharpEvents(subType, subType, subType, subType, subType, subType);

                //CommandExecutor executor = thisDoc.Objects.Where(obj => obj is CommandExecutor).FirstOrDefault() as CommandExecutor;
                //executor.enable = enable;
                this.nonComponentRemoSetupV3.enable = true;

            }
            else
            {
                remoParamExecution.AutoReset = false;
                remoParamExecution.Elapsed -= RemoParamExecution_Elapsed;
                remoParamExecution.Stop();
                remoParamExecution.Dispose();

                var thisDoc = this.OnPingDocument();
                if (thisDoc == null) return;
                AddInteractionButtonsToTopBar(SubcriptionType.Unsubscribe);

                SubcriptionType unsubType = SubcriptionType.Unsubscribe;

                if (client != null)
                {
                    SetUpRemoSharpEvents(unsubType, unsubType, unsubType, unsubType, unsubType, unsubType);
                    client.Close();
                }

                if (this.nonComponentRemoSetupV3 != null) this.nonComponentRemoSetupV3.enable = false;
            }


        }

        private void RemoParamExecution_Elapsed(object sender, ElapsedEventArgs e)
        {
            while (remoParams.Count > 0)
            {
                RemoParameter remoParam = remoParams[0];
                remoParams.RemoveAt(0);

                this.nonComponentRemoSetupV3.ExecuteRemoParameter(remoParam);
            }
        }

        public void SetUpRemoSharpEvents(
              SubcriptionType clientEvents
            , SubcriptionType objectsAdded
            , SubcriptionType objectsDeleted
            , SubcriptionType mouse
            , SubcriptionType keyboard
            , SubcriptionType canvas
            )
        {
            GH_Document thisDoc = this.OnPingDocument();
            var client = this.client;
            if (thisDoc == null) return;
            //bool isAlive = client.IsAlive;

            RemoNullCommand nullCommand;
            string connectionString = "";
            switch (clientEvents)
            {
                case SubcriptionType.Subscribe:
                    recconnectTimer = DateTime.Now;

                    client.OnMessage += Client_OnMessage;
                    client.OnClose += Client_OnClose;
                    client.OnOpen += Client_OnOpen;
                    this.Message = "Connecting...";
                    Task.Run(() => EstablishConnection(client));

                    timer.Elapsed += Timer_Elapsed;
                    timer.AutoReset = true;
                    timer.Start();
                    break;
                case SubcriptionType.Unsubscribe:
                    string pause = "";
                    if (client != null)
                    {
                        client.OnMessage -= Client_OnMessage;
                        client.OnClose -= Client_OnClose;
                        client.OnOpen -= Client_OnOpen;
                    }


                    // get all values from the connection keyword dictionary
                    var allValues = ConnectionMessagePairs.Values.ToList();
                    if (!allValues.Contains(this.Message) || this.Message.Equals("Connected"))
                    {
                        if (string.IsNullOrEmpty(this.username)) this.Message = ConnectionMessagePairs[RemoSetupClientV3.DISCONNECTION_KEYWORD];
                        else this.Message = "Disconnected";
                    }
                    timer.Elapsed -= Timer_Elapsed;
                    timer.Stop();

                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    recconnectTimer = DateTime.Now;

                    client.OnMessage -= Client_OnMessage;
                    client.OnMessage += Client_OnMessage;
                    client.OnClose -= Client_OnClose;
                    client.OnClose += Client_OnClose;
                    client.OnOpen -= Client_OnOpen;
                    client.OnOpen += Client_OnOpen;

                    Task.Run(() => EstablishConnection(client));

                    timer.Elapsed -= Timer_Elapsed;
                    timer.Stop();
                    timer.Elapsed += Timer_Elapsed;
                    timer.Start();



                    break;
                default:
                    break;
            }

            switch (objectsAdded)
            {

                case SubcriptionType.Subscribe:
                    thisDoc.ObjectsAdded += ThisDoc_ObjectsAdded;
                    break;
                case SubcriptionType.Unsubscribe:
                    thisDoc.ObjectsAdded -= ThisDoc_ObjectsAdded;
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    thisDoc.ObjectsAdded -= ThisDoc_ObjectsAdded;
                    thisDoc.ObjectsAdded += ThisDoc_ObjectsAdded;
                    break;
                default:
                    break;
            }

            switch (objectsDeleted)
            {
                case SubcriptionType.Subscribe:
                    thisDoc.ObjectsDeleted += ThisDoc_ObjectsDeleted;
                    break;
                case SubcriptionType.Unsubscribe:
                    thisDoc.ObjectsDeleted -= ThisDoc_ObjectsDeleted;
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    thisDoc.ObjectsDeleted -= ThisDoc_ObjectsDeleted;
                    thisDoc.ObjectsDeleted += ThisDoc_ObjectsDeleted;
                    break;
                default:
                    break;
            }

            switch (mouse)
            {
                case SubcriptionType.Subscribe:
                    Grasshopper.Instances.ActiveCanvas.MouseDown += ActiveCanvas_MouseDown;
                    Grasshopper.Instances.ActiveCanvas.MouseUp += ActiveCanvas_MouseUp;
                    Grasshopper.Instances.ActiveCanvas.DragOver += ActiveCanvas_DragOver;
                    Grasshopper.Instances.ActiveCanvas.MouseMove += ActiveCanvas_MouseMove;
                    break;
                case SubcriptionType.Unsubscribe:
                    Grasshopper.Instances.ActiveCanvas.MouseDown -= ActiveCanvas_MouseDown;
                    Grasshopper.Instances.ActiveCanvas.MouseUp -= ActiveCanvas_MouseUp;
                    Grasshopper.Instances.ActiveCanvas.DragOver -= ActiveCanvas_DragOver;
                    Grasshopper.Instances.ActiveCanvas.MouseMove -= ActiveCanvas_MouseMove;
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:

                    Grasshopper.Instances.ActiveCanvas.MouseDown -= ActiveCanvas_MouseDown;
                    Grasshopper.Instances.ActiveCanvas.MouseDown += ActiveCanvas_MouseDown;
                    Grasshopper.Instances.ActiveCanvas.MouseUp -= ActiveCanvas_MouseUp;
                    Grasshopper.Instances.ActiveCanvas.MouseUp += ActiveCanvas_MouseUp;
                    Grasshopper.Instances.ActiveCanvas.DragOver -= ActiveCanvas_DragOver;
                    Grasshopper.Instances.ActiveCanvas.DragOver += ActiveCanvas_DragOver;
                    Grasshopper.Instances.ActiveCanvas.MouseMove -= ActiveCanvas_MouseMove;
                    Grasshopper.Instances.ActiveCanvas.MouseMove += ActiveCanvas_MouseMove;
                    break;
                default:
                    break;
            }

            switch (keyboard)
            {
                case SubcriptionType.Subscribe:
                    Grasshopper.Instances.ActiveCanvas.KeyDown += ActiveCanvas_KeyDown;
                    Grasshopper.Instances.ActiveCanvas.KeyUp += ActiveCanvas_KeyUp;
                    break;
                case SubcriptionType.Unsubscribe:
                    Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyDown;
                    Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    Grasshopper.Instances.ActiveCanvas.KeyDown -= ActiveCanvas_KeyDown;
                    Grasshopper.Instances.ActiveCanvas.KeyDown += ActiveCanvas_KeyDown;
                    Grasshopper.Instances.ActiveCanvas.KeyUp -= ActiveCanvas_KeyUp;
                    Grasshopper.Instances.ActiveCanvas.KeyUp += ActiveCanvas_KeyUp;
                    break;
                default:
                    break;
            }

            var gh_Canvas = Grasshopper.Instances.ActiveCanvas;
            switch (canvas)
            {
                case SubcriptionType.Subscribe:
                    gh_Canvas.CanvasPrePaintObjects += HighlightRemoParameters;

                    break;
                case SubcriptionType.Unsubscribe:
                    gh_Canvas.CanvasPrePaintObjects -= HighlightRemoParameters;
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    gh_Canvas.CanvasPrePaintObjects -= HighlightRemoParameters;
                    gh_Canvas.CanvasPrePaintObjects += HighlightRemoParameters;
                    break;
                default:
                    break;
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timerCount++;
            if (timerCount > 100) timerCount = 0;

            RemoKeepAlive keepAliveCommand = new RemoKeepAlive();
            string connectionString = RemoCommand.SerializeToJson(keepAliveCommand);
            if (enable) client.Send(connectionString);

            //SendRemoMoveAllCommand();

            if (timerCount % 2 == 0)
            {
                var thisDoc = this.OnPingDocument();
                if (thisDoc == null)
                {
                    var timer = sender as System.Timers.Timer;
                    timer.Stop();
                    // unsubscribe the timer
                    timer.Elapsed -= Timer_Elapsed;
                    timer.Dispose();
                    return;
                }
                thisDoc.ScheduleSolution(1, doc =>
                {
                    Dictionary<Guid, int> duplicates = FindDuplicateGuids(
                        this.OnPingDocument().Objects.Select(obj => obj.InstanceGuid).ToList());
                    foreach (var item in duplicates)
                    {
                        if (item.Value > 1)
                        {
                            for (int i = 0; i < item.Value - 1; i++)
                            {
                                var obj = this.OnPingDocument().Objects.Where(o => o.InstanceGuid == item.Key).LastOrDefault();
                                if (obj == null) continue;

                                this.OnPingDocument().RemoveObject(obj, false);
                            }
                        }
                    }

                });

            }
        }

        // a function that finds duplicate guids and how many there are in a list. it returns a dictionary of guid and int
        // if the int is greater than 1 
        public Dictionary<Guid, int> FindDuplicateGuids(List<Guid> guids)
        {
            Dictionary<Guid, int> duplicateGuids = new Dictionary<Guid, int>();

            foreach (var guid in guids)
            {
                if (duplicateGuids.ContainsKey(guid))
                {
                    duplicateGuids[guid]++;
                }
                else
                {
                    duplicateGuids.Add(guid, 1);
                }
            }

            // remove all single guids from the dictionary
            foreach (var item in duplicateGuids.Where(obj => obj.Value == 1).ToList())
            {
                duplicateGuids.Remove(item.Key);
            }

            return duplicateGuids;
        }



        private void EstablishConnection(WebSocket client)
        {
            client.Connect();
            RemoNullCommand nullCommand = new RemoNullCommand(this.username, this.password, this.sessionID);
            string connectionString = RemoCommand.SerializeToJson(nullCommand);
            client.Send(connectionString);
            if (client.IsAlive)
            {
                this.Message = "Connected";
                Grasshopper.Instances.ActiveCanvas.Invalidate();
            }
        }

        private void EstablishConnection(WebSocket client, bool checkForConnection)
        {
            bool connected = client.IsAlive;
            Rhino.RhinoApp.WriteLine("Connected: " + connected);
            if (connected) return;
            SubcriptionType resub = SubcriptionType.Resubscribe;
            SetUpRemoSharpEvents(resub, resub, resub, resub, resub, resub);
            if (client.IsAlive)
            {
                this.Message = "Connected";
                Grasshopper.Instances.ActiveCanvas.Invalidate();
            }
            this.recconnectTimer = DateTime.Now;
        }

        private bool CheckForHealthyGH(out GH_Document gh_document)
        {
            GH_Document thisDoc = this.OnPingDocument();

            if (thisDoc == null || this == null)
            {
                var unsub = SubcriptionType.Unsubscribe;
                this.SetUpRemoSharpEvents(unsub, unsub, unsub, unsub, unsub, unsub);
                gh_document = null;
                return false;
            }
            gh_document = thisDoc;
            return true;
        }


        private void ActiveCanvas_KeyUp(object sender, KeyEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            GH_Document thisDoc = this.OnPingDocument();
            if (!CheckForHealthyGH(out thisDoc)) return;

            if (e.KeyCode == Keys.ControlKey)
            {
                this.controlDown = false;
                return;
            }
            if (e.KeyCode == Keys.ShiftKey)
            {
                this.shiftDown = false;
                return;
            }

            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.F12)
            {
                //this.remoParamModeActive = false;
                //var gh_groups = this.OnPingDocument().Objects;
                //System.Threading.Tasks.Parallel.ForEach(gh_groups, item =>
                //{
                //    if (!(item is Grasshopper.Kernel.Special.GH_Group)) return;
                //    Grasshopper.Kernel.Special.GH_Group group = (Grasshopper.Kernel.Special.GH_Group)item;
                //    if (group.NickName.Contains(RemoParam.RemoParamKeyword)) group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);

                //});

                IGH_Param igh_param = (IGH_Param)thisDoc.FindObject(hoverParam, false);
                if (igh_param != null)
                {
                    igh_param.RuntimeMessages(GH_RuntimeMessageLevel.Remark)
                    .Remove(igh_param.RuntimeMessages(GH_RuntimeMessageLevel.Remark)
                    .Where(obj => obj.Equals("Syncing")).FirstOrDefault());
                    ResetGHColorsToDefault();
                }

            }

        }

        private void ActiveCanvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            GH_Document thisDoc = this.OnPingDocument();
            if (!CheckForHealthyGH(out thisDoc)) return;

            if (e.KeyCode == Keys.ControlKey && preventUndo)
            {
                thisDoc.UndoServer.Clear();
                this.controlDown = true;
                return;
            }
            if (e.KeyCode == Keys.ShiftKey)
            {
                this.shiftDown = true;
                return;
            }

            if (e.KeyCode == Keys.Tab || e.KeyCode == Keys.F12)
            {
                //this.remoParamModeActive = true;
                //var gh_groups = thisDoc.Objects;
                //System.Threading.Tasks.Parallel.ForEach(gh_groups, item =>
                //{
                //    if (!(item is Grasshopper.Kernel.Special.GH_Group)) return;
                //    Grasshopper.Kernel.Special.GH_Group group = (Grasshopper.Kernel.Special.GH_Group)item;
                //    if (group.NickName.Contains(RemoParam.RemoParamKeyword)) group.Colour = System.Drawing.Color.FromArgb(125, 225, 100, 250);
                //});

                var hoverObject = thisDoc.FindObject(this.mouseLocation, 5);
                if (hoverObject != null)
                {

                    if (hoverObject is IGH_Param)
                    {
                        IGH_Param param = (IGH_Param)hoverObject;
                        if (param != null)
                        {
                            if (param.Attributes.Parent == null)
                            {
                                param.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Syncing");
                                hoverParam = param.InstanceGuid;
                                SetColorToSyncMode();
                                //param.SolutionExpired += Param_SolutionExpired;
                            }
                        }
                        else
                        {
                            IGH_Param igh_param = (IGH_Param)thisDoc.FindObject(hoverParam, false);
                            if (igh_param != null)
                            {
                                igh_param.ClearRuntimeMessages();
                                //igh_param.SolutionExpired -= Param_SolutionExpired;
                            }
                        }

                    }
                }
            }

            bool controlDown = this.controlDown;
            bool zIsDown = e.KeyCode == Keys.Z;
            if (controlDown && zIsDown)
            {
                if (preventUndo) { }
                {
                    System.Windows.Forms.MessageBox.Show("PLEASE DO NOT USE UNDO OR REDO (●◡~)", "Syncing Error!");
                }
            }



        }

        //private void Param_SolutionExpired(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    Rhino.RhinoApp.WriteLine(sender.ToString() + "Solution Expired");
        //}

        //public static void SubscribeAllParams(RemoSetupClientV3 remoSetupComp, bool subscribe)
        //{
        //    List<string> accaptableTypes = new List<string>() {
        //    "Grasshopper.Kernel.Special.GH_NumberSlider",
        //    "Grasshopper.Kernel.Special.GH_Panel",
        //    "Grasshopper.Kernel.Special.GH_ColourSwatch",
        //    "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
        //    "Grasshopper.Kernel.Special.GH_BooleanToggle",
        //    "Grasshopper.Kernel.Special.GH_ButtonObject"
        //    };

        //    var allParams = remoSetupComp.OnPingDocument().Objects;


        //    if (!subscribe)
        //    {
        //        var deletionGuids = remoSetupComp.OnPingDocument().Objects
        //            .Where(obj => obj.NickName.Equals(RemoParam.RemoParamKeyword))
        //            .Select(obj => remoSetupComp.OnPingDocument().FindObject(obj.InstanceGuid, false))
        //            .ToList();

        //        remoSetupComp.OnPingDocument().RemoveObjects(deletionGuids, false);
        //    }

        //    foreach (var item in allParams)
        //    {
        //        if (subscribe)
        //        {
        //            string typeString = item.GetType().FullName;

        //            string pause = "";


        //            bool isInTheList = accaptableTypes.Contains(typeString);

        //            if (isInTheList)
        //            {
        //                string nickName = item.NickName;
        //                bool isSetupButtno = nickName.Equals("RemoSetup");

        //                if (isSetupButtno) continue;

        //                GroupObjParam(remoSetupComp, item);
        //            }

        //        }
        //        switch (item.GetType().ToString())
        //        {

        //            case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeSlider;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_Panel"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizePanel;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeColor;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeMDSlider;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeToggle;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeButton;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
        //                }
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

        //public static void SubscribeAllParams(RemoSetupClientV3 remoSetupComp, List<IGH_DocumentObject> allParams, bool subscribe)
        //{
        //    List<string> accaptableTypes = new List<string>() {
        //    "Grasshopper.Kernel.Special.GH_NumberSlider",
        //    "Grasshopper.Kernel.Special.GH_Panel",
        //    "Grasshopper.Kernel.Special.GH_ColourSwatch",
        //    "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
        //    "Grasshopper.Kernel.Special.GH_BooleanToggle",
        //    "Grasshopper.Kernel.Special.GH_ButtonObject"
        //    };

        //    foreach (var item in allParams)
        //    {
        //        if (subscribe)
        //        {
        //            string typeString = item.GetType().FullName;

        //            string pause = "";


        //            bool isInTheList = accaptableTypes.Contains(typeString);

        //            if (isInTheList)
        //            {
        //                string nickName = item.NickName;
        //                bool isSetupButtno = nickName.Equals("RemoSetup");

        //                if (isSetupButtno) continue;

        //                GroupObjParam(remoSetupComp, item);
        //            }

        //        }
        //        switch (item.GetType().ToString())
        //        {

        //            case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeSlider;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeSlider;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_Panel"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizePanel;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizePanel;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeColor;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeColor;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeMDSlider;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeMDSlider;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeToggle;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeToggle;
        //                }
        //                break;
        //            case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
        //                if (subscribe)
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
        //                    item.SolutionExpired += remoSetupComp.RemoParameterizeButton;
        //                }
        //                else
        //                {
        //                    item.SolutionExpired -= remoSetupComp.RemoParameterizeButton;
        //                }
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}




        //public void RemoParameterizeMDSlider(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeMDSlider;
        //    Grasshopper.Kernel.Special.GH_MultiDimensionalSlider param = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
        //    {
        //        RemoParamMDSlider remoParam = new RemoParamMDSlider(this.username, this.sessionID, param, false);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }
        //}

        //public void RemoParameterizeToggle(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeToggle;
        //    Grasshopper.Kernel.Special.GH_BooleanToggle param = (Grasshopper.Kernel.Special.GH_BooleanToggle)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
        //    {
        //        RemoParamToggle remoParam = new RemoParamToggle(this.username, this.sessionID, param);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }
        //}

        //public void RemoParameterizeColor(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeColor;
        //    Grasshopper.Kernel.Special.GH_ColourSwatch param = (Grasshopper.Kernel.Special.GH_ColourSwatch)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected)
        //    {
        //        RemoParamColor remoParam = new RemoParamColor(this.username, this.sessionID, param);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }
        //}

        //public void RemoParameterizePanel(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizePanel;
        //    Grasshopper.Kernel.Special.GH_Panel param = (Grasshopper.Kernel.Special.GH_Panel)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected && status.noInput)
        //    {
        //        RemoParamPanel remoParam = new RemoParamPanel(this.username, this.sessionID, param);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }
        //}

        //public void RemoParameterizeButton(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeButton;
        //    Grasshopper.Kernel.Special.GH_ButtonObject param = (Grasshopper.Kernel.Special.GH_ButtonObject)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
        //    {

        //        RemoParamButton remoParam = new RemoParamButton(this.username, this.sessionID, param);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }
        //}

        //public void RemoParameterizePointParam(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    //try
        //    //{
        //    if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizePointParam;
        //    Grasshopper.Kernel.Parameters.Param_Point param = (Grasshopper.Kernel.Parameters.Param_Point)sender;

        //    IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //    if (status.tabKeyIsDown && status.isRemoParamGrouped && status.isSelected && status.noInput)
        //    {
        //        GH_LooseChunk chunk = new GH_LooseChunk(null);
        //        param.WriteFull(chunk);
        //        RemoParamPoint3d remoParam = new RemoParamPoint3d(this.username, param);
        //        string json = RemoCommand.SerializeToJson(remoParam);
        //        if (this.enable) this.client.Send(json);
        //    }

        //    //}
        //    //catch
        //    //{
        //    //    Console.WriteLine("sync problem");
        //    //}

        //}



        //public void RemoParameterizeSlider(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    try
        //    {
        //        if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeSlider;
        //        Grasshopper.Kernel.Special.GH_NumberSlider param = (Grasshopper.Kernel.Special.GH_NumberSlider)sender;

        //        IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

        //        if (status.tabKeyIsDown && status.isRemoParamGrouped)
        //        {

        //            RemoParamSlider remoParamSlider = new RemoParamSlider(this.username, this.sessionID, param);
        //            string json = RemoCommand.SerializeToJson(remoParamSlider);
        //            if (this.enable) this.client.Send(json);
        //        }
        //    }
        //    catch
        //    {
        //        Console.WriteLine("sync problem");

        //    }

        //}

        //public static void GroupObjParam(RemoSetupClientV3 remoSetupComp, IGH_DocumentObject obj)
        //{
        //    remoSetupComp.OnPingDocument().ScheduleSolution(1, doc =>
        //    {
        //        GH_Group group = new GH_Group();
        //        group.CreateAttributes();
        //        group.AddObject(obj.InstanceGuid);
        //        group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        //        group.NickName = RemoParam.RemoParamKeyword;
        //        remoSetupComp.OnPingDocument().AddObject(group, false);
        //    });
        //}

        //public void GroupRemoParamDataComponents(RemoParam remoParamComp, RemoParamData remoParamData)
        //{

        //    GH_Group group = new GH_Group();
        //    group.CreateAttributes();
        //    group.AddObject(remoParamComp.InstanceGuid);
        //    group.AddObject(remoParamData.InstanceGuid);
        //    Random rand = new Random();
        //    int hue1 = rand.Next(100, 255);
        //    int hue2 = rand.Next(100, 255);
        //    int hue3 = rand.Next(100, 255);
        //    group.Colour = System.Drawing.Color.FromArgb(50, hue1, hue2, hue3);
        //    group.Border = GH_GroupBorder.Blob;
        //    group.NickName = "";

        //    remoParamComp.groupGuid = group.InstanceGuid;

        //    this.OnPingDocument().AddObject(group, false);
        //}

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

        //public void GroupObjParam(IGH_DocumentObject obj, string extraText)
        //{
        //    this.OnPingDocument().ScheduleSolution(1, doc =>
        //    {
        //        GH_Group group = new GH_Group();
        //        group.CreateAttributes();
        //        group.AddObject(obj.InstanceGuid);
        //        group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
        //        group.NickName = RemoParam.RemoParamKeyword + extraText;


        //        this.OnPingDocument().AddObject(group, false);

        //    });

        //}

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
                this.isRemoParamGrouped = isRemoParamGrouped;
                this.mouseHoverOnExpiredParam = mouseHoverOnExpiredParam;
                this.paramIsNotPointComp = paramIsNotPointComp;
            }

        }

        //IGH_ParamConditionStatus FindIGH_ParamStatus(IGH_Param param)
        //{
        //    bool isSelected = param.Attributes.Selected;
        //    bool noInput = param.SourceCount < 1;

        //    bool tabKeyIsDown = this.remoParamModeActive;

        //    bool paramIsNotPointComp = param is Grasshopper.Kernel.Parameters.Param_Point;

        //    bool isIn_gh_group = false;
        //    bool mouseHoverOnExpiredParam = false;
        //    if (tabKeyIsDown)
        //    {

        //        var gh_groups = this.OnPingDocument().Objects.AsParallel().AsUnordered()
        //            .Where(obj => obj is Grasshopper.Kernel.Special.GH_Group && obj.NickName.Contains(RemoParam.RemoParamKeyword))
        //            .Select(obj => (GH_Group)obj);
        //        foreach (var item in gh_groups)
        //        {
        //            if (item.ObjectIDs.Contains(param.InstanceGuid))
        //            {
        //                isIn_gh_group = true;
        //                break;
        //            }
        //        }
        //        if (isIn_gh_group && !paramIsNotPointComp)
        //        {
        //            var hoverObj = Grasshopper.Instances.ActiveCanvas.Document.FindObject(this.mouseLocation, 2);
        //            if (hoverObj != null)
        //            {
        //                mouseHoverOnExpiredParam = hoverObj.InstanceGuid == param.InstanceGuid;
        //            }
        //        }

        //    }

        //    IGH_ParamConditionStatus status = new IGH_ParamConditionStatus(isSelected, noInput, tabKeyIsDown, isIn_gh_group, mouseHoverOnExpiredParam, paramIsNotPointComp);
        //    return status;
        //}

        private void ActiveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DisconnectOnImproperClose())
            {
                GH_Canvas canvas = (GH_Canvas)sender;
                canvas.MouseMove -= ActiveCanvas_MouseMove;
                System.Windows.Forms.MessageBox.Show("Connection closed due to improper disconnection!", "RemoSharp Connection Error!");
                return;
            }
            var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            this.mouseLocation = mouseEvent.CanvasLocation;
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            RemoNullCommand nullCommand = new RemoNullCommand(this.username, this.password, this.sessionID);
            string connectionString = RemoCommand.SerializeToJson(nullCommand);
            client.Send(connectionString);
        }

        private void Client_OnClose(object sender, CloseEventArgs e)
        {
            this.Message = "Reconnecting!";
            Task.Run(() => ConnectToServer());
        }

        private void ConnectToServer()
        {


            int reconnectAttempts = 1;
            while (enable)
            {
                client.Connect();
                if (client.IsAlive)
                {
                    this.Message = "Connected";
                    Grasshopper.Instances.ActiveCanvas.Invalidate();
                }
                else
                {
                    this.Message = $"Reconnecting! ({reconnectAttempts})";
                }
                reconnectAttempts++;
            }
            if (!enable)
            {
                this.Message = "Connection Failed";
                SubcriptionType unsub = SubcriptionType.Unsubscribe;
                SetUpRemoSharpEvents(unsub, unsub, unsub, unsub, unsub, unsub);
                this.enableSwitch.CurrentValue = false;
            }
        }

        private void ActiveCanvas_DragOver(object sender, DragEventArgs e)
        {
            GH_Canvas canvas = sender as GH_Canvas;
            if (DisconnectOnImproperClose())
            {
                canvas.DragOver -= ActiveCanvas_DragOver;
                return;
            }
        }

        private void ActiveCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            GH_Canvas canvas = sender as GH_Canvas;
            if (DisconnectOnImproperClose())
            {
                canvas.MouseDown -= ActiveCanvas_MouseDown;
                return;
            }

            GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(canvas.Viewport, e);
            this.downPnt = new float[] { mouseEvent.CanvasLocation.X, mouseEvent.CanvasLocation.Y };
            this.interaction = canvas.ActiveInteraction;


            //DateTime dateTime = DateTime.Now;
            //if (dateTime.Subtract(this.recconnectTimer).Minutes > 2 )
            //{
            //    Task.Run(() => EstablishConnection(client,true));
            //}
        }

        private bool DisconnectOnImproperClose()
        {
            var thisComp = this;
            var thisDoc = thisComp.OnPingDocument();
            if (thisComp == null || thisDoc == null)
            {
                SubcriptionType unsub = SubcriptionType.Unsubscribe;
                this.SetUpRemoSharpEvents(unsub, unsub, unsub, unsub, unsub, unsub);
                return true;
            }
            return false;
        }

        private void ActiveCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            GH_Canvas canvas = sender as GH_Canvas;

            if (DisconnectOnImproperClose())
            {
                canvas.MouseUp -= ActiveCanvas_MouseUp;
                return;
            }
            GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(canvas.Viewport, e);
            this.upPnt = new float[] { mouseEvent.CanvasLocation.X, mouseEvent.CanvasLocation.Y };

            if (lastCommand != null)
            {
                if (lastCommand.commandType == CommandType.RemoParameter)
                {
                    // a timer to wait for 150 milliseconds and then send the command
                    // the timer is disposed after being elapsed

                    System.Timers.Timer rpmTimer = new System.Timers.Timer(400);
                    rpmTimer.AutoReset = false;
                    rpmTimer.Elapsed += (s, ev) =>
                    {
                        var comp = this.OnPingDocument().FindObject(this.lastRpmGuid, false);
                        RemoParameter remoParam = new RemoParameter(this.username, this.sessionID, comp);
                        SendCommands(this, remoParam, commandRepeat, enable);
                        this.lastCommand = null;
                        this.lastRpmGuid = Guid.Empty;
                        rpmTimer.Dispose();
                    };
                    rpmTimer.Start();


                }
            }

            if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction)
            {
                SendRemoWireCommand();
            }
            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
            {
                SendRemoMoveAllCommand();
            }
            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction)
            {
                SendRemoReWireCommand();
            }
            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_SplitInteraction)
            {
                SendRemoMoveAllCommand();
            }
            else if (interaction == null)
            {
                GH_Document doc = canvas.Document;

                var lastAction = doc.UndoServer.FirstUndoName;

                bool scirbbleSelected = doc.SelectedObjects().Any(o => o is Grasshopper.Kernel.Special.GH_Scribble);

                if (lastAction == null) return;
                if (lastAction.Equals("Align") || scirbbleSelected)
                {
                    SendRemoMoveCommand();
                }



            }


        }

        private void SendRemoReWireCommand()
        {
            Type type = typeof(Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction);

            IGH_Param source = type
              .GetField("m_source", BindingFlags.NonPublic | BindingFlags.Instance)
              .GetValue(interaction) as IGH_Param;
            IGH_Param target = type
              .GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance)
              .GetValue(interaction) as IGH_Param;


            command = new RemoReWire(this.username, this.sessionID, source, target);

            SendCommands(this, command, commandRepeat, enable);
        }

        private void SendRemoMoveAllCommand()
        {
            GH_Document thisDoc = null;
            if (!CheckForHealthyGH(out thisDoc)) return;

            var selection = thisDoc.Objects;

            if (selection.Count != 0)
            {

                command = new RemoMove(this.username, this.sessionID, selection.ToList());
                SendCommands(this, command, commandRepeat, enable);
            }

        }

        private void SendRemoMoveCommand()
        {
            GH_Document thisDoc = null;
            if (!CheckForHealthyGH(out thisDoc)) return;

            var selection = thisDoc.SelectedObjects();

            if (selection.Count != 0)
            {

                // get the distance of the mouse down and up points
                float x = this.upPnt[0] - this.downPnt[0];
                float y = this.upPnt[1] - this.downPnt[1];
                // distance formula
                float distance = (float)Math.Sqrt(x * x + y * y);
                if (distance < 2) return;
                command = new RemoMove(this.username, this.sessionID, selection);
                SendCommands(this, command, commandRepeat, enable);
            }

        }

        private void SendRemoWireCommand()
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

            if (source == null || target == null) return;

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

            //int outIndex = -1;
            //bool outIsSpecial = false;
            //System.Guid outGuid = GetComponentGuidAnd_Output_Index(
            //  source, out outIndex, out outIsSpecial);

            //int inIndex = -1;
            //bool inIsSpecial = false;
            //if (target == null || source == null) return;
            //System.Guid inGuid = GetComponentGuidAnd_Input_Index(
            //  target, out inIndex, out inIsSpecial);

            var souceComp = source.Attributes.Parent == null ? source : source.Attributes.Parent.DocObject;
            var targetComp = target.Attributes.Parent == null ? target : target.Attributes.Parent.DocObject;


            string outCompXML = RemoCommand.SerializeToXML(souceComp);
            string inCompXML = RemoCommand.SerializeToXML(targetComp);

            string sourceType = souceComp.GetType().FullName;
            string targetType = targetComp.GetType().FullName;

            command = new RemoConnect(this.username, this.sessionID, souceComp.InstanceGuid, targetComp.InstanceGuid, remoConnectType,
                outCompXML, inCompXML, sourceType, targetType);
            SendCommands(this, command, commandRepeat, enable);
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


        private void ThisDoc_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            GH_Document activeDoc = (GH_Document)sender;
            if (activeDoc == null) return;



            List<Guid> deleteGuids = new List<Guid>();
            var objs = e.Objects;

            foreach (var obj in objs)
            {
                deleteGuids.Add(obj.InstanceGuid);
            }

            command = new RemoDelete(this.username, this.sessionID, deleteGuids);
            SendCommands(this, command, commandRepeat, enable);

            //GH_Document temp = new GH_Document();
            //this.OnPingDocument().MergeDocument(temp,true,true);


        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.Equals(RemoSetupClientV3.DISCONNECTION_KEYWORD))
            {
                client.OnMessage -= Client_OnMessage;
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.Close();
                this.Message = RemoSetupClientV3.ConnectionMessagePairs[DISCONNECTION_KEYWORD];
                this.enable = false;
                this.enableSwitch.CurrentValue = false;

                SubcriptionType sub = SubcriptionType.Unsubscribe;
                SetUpRemoSharpEvents(sub, sub, sub, sub, sub, sub);
                return;
            }
            else if (e.Data.Equals(RemoSetupClientV3.CONNECTION_SUCCESSFUL))
            {
                this.Message = RemoSetupClientV3.ConnectionMessagePairs[CONNECTION_SUCCESSFUL];
                return;
            }
            else if (e.Data.Equals(RemoSetupClientV3.CAPACITY_KEYWORD))
            {
                client.OnMessage -= Client_OnMessage;
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.Close();
                this.Message = RemoSetupClientV3.ConnectionMessagePairs[CAPACITY_KEYWORD];
                this.enable = false;
                this.enableSwitch.CurrentValue = false;

                SubcriptionType sub = SubcriptionType.Unsubscribe;
                SetUpRemoSharpEvents(sub, sub, sub, sub, sub, sub);
                return;
            }
            else if (e.Data.Equals(RemoSetupClientV3.SESSION_ID_KEYWORD))
            {
                client.OnMessage -= Client_OnMessage;
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.Close();
                this.Message = RemoSetupClientV3.ConnectionMessagePairs[SESSION_ID_KEYWORD];
                this.enable = false;
                this.enableSwitch.CurrentValue = false;

                SubcriptionType sub = SubcriptionType.Unsubscribe;
                SetUpRemoSharpEvents(sub, sub, sub, sub, sub, sub);
                return;
            }

            messages.Add(e.Data);

            nonComponentRemoSetupV3.SolveInstance();


            if (!this.keepRecord)
            {

                while (messages.Count > 1)
                {
                    messages.RemoveAt(0);
                }
            }
        }

        private void ThisDoc_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            GH_Document activeDoc = (GH_Document)sender;
            if (activeDoc == null) return;


            activeDoc.ScheduleSolution(1, doc =>
            {

                addedObjects.Clear();
                addedObjects.AddRange(e.Objects.Select(obj => obj.InstanceGuid));

                var skip = SubcriptionType.Skip;
                var unsub = SubcriptionType.Unsubscribe;
                var sub = SubcriptionType.Subscribe;
                SetUpRemoSharpEvents(skip, unsub, unsub, skip, skip, skip);

                try
                {
                    List<IGH_DocumentObject> objs = e.Objects.ToList();

                    //bool drawingsIncuded = objs.Any(o => o is Grasshopper.Kernel.Special.GH_Scribble || o is Grasshopper.Kernel.Special.GH_Markup);
                    //if (drawingsIncuded)
                    //{
                    //    drawingSyncTimer.AutoReset = true;
                    //    drawingSyncTimer.Elapsed += DrawingSyncTimer_Elapsed;
                    //    if (!drawingSyncTimer.Enabled) drawingSyncTimer.Start();
                    //}

                    Guid relayGuid = Guid.Empty;
                    List<IGH_Param> relayRecepients = new List<IGH_Param>();

                    // For relays we temporarily save the connections to connect them again later
                    if (objs.Count == 1 && objs[0] is GH_Relay)
                    {
                        var relay = (objs[0] as GH_Relay);
                        relayGuid = relay.InstanceGuid;
                        relayRecepients.AddRange(relay.Recipients);
                    }

                    var selection = activeDoc.SelectedObjects();

                    RemoPartialDoc remoPartialDoc = new RemoPartialDoc(this.username, this.sessionID, objs, activeDoc);

                    //IGH_Param newRelay = activeDoc.FindObject<IGH_Param>(relayGuid, false);
                    //foreach (var target in relayRecepients)
                    //{
                    //    target.AddSource(newRelay);
                    //}

                    foreach (var item in selection)
                    {
                        item.Attributes.Selected = true;
                    }

                    SendCommands(this, remoPartialDoc, commandRepeat, enable);
                }
                catch (Exception error)
                {
                    Rhino.RhinoApp.WriteLine(error.Message);
                }
                SetUpRemoSharpEvents(skip, sub, sub, skip, skip, skip);
            });

        }

        //public void DrawingSyncTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    if (sender == null)
        //    {
        //        drawingSyncTimer.Elapsed -= DrawingSyncTimer_Elapsed;
        //        drawingSyncTimer.Stop();
        //        drawingSyncTimer.Dispose();
        //    }

        //    // find all scribbles and markups
        //    var scribbles = this.OnPingDocument().Objects.Where(o => o is Grasshopper.Kernel.Special.GH_Scribble);
        //    var markups = this.OnPingDocument().Objects.Where(o => o is Grasshopper.Kernel.Special.GH_Markup).ToList();
        //    // create a list of all scribbles and markups
        //    List<IGH_DocumentObject> scribblesAndMarkups = new List<IGH_DocumentObject>();
        //    scribblesAndMarkups.AddRange(scribbles);
        //    scribblesAndMarkups.AddRange(markups);

        //    if (scribblesAndMarkups.Count == 0)
        //    {
        //        drawingSyncTimer.Elapsed -= DrawingSyncTimer_Elapsed;
        //        drawingSyncTimer.Stop();
        //        return;
        //    }


        //    foreach (var item in scribbles)
        //    {
        //        GH_Scribble scribble = (GH_Scribble)item;
        //        if (!this.scribleHistory.ContainsKey(scribble.InstanceGuid))
        //        {
        //            this.scribleHistory.Add(item.InstanceGuid, scribble.Text + uncommonSplitCharacter + scribble.Font.Size);
        //            List<IGH_DocumentObject> sendables = new List<IGH_DocumentObject>() { scribble };
        //            SendSyncComponentCommand(sendables, 1, this.sessionID, this.enable, true);
        //        }
        //        else
        //        {
        //            if (!this.scribleHistory[item.InstanceGuid].Equals(scribble.Text + uncommonSplitCharacter + scribble.Font.Size))
        //            {
        //                this.scribleHistory[item.InstanceGuid] = scribble.Text + uncommonSplitCharacter + scribble.Font.Size;
        //                List<IGH_DocumentObject> sendables = new List<IGH_DocumentObject>() { scribble };
        //                SendSyncComponentCommand(sendables, 1, this.sessionID, this.enable, true);
        //            }
        //        }
        //    }



        //}



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

        private void UndoPreventionSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            preventUndo = Convert.ToBoolean(e.Value);
        }

        private void SetupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);

            if (currentValue)
            {
                if (this.client != null)
                {
                    client.Close();
                    this.enableSwitch.CurrentValue = false;
                }

                LoginDialouge loginDialouge = new LoginDialouge();
                loginDialouge.Show();

            }
        }

        private void AddInteractionButtonsToTopBar(SubcriptionType subcriptionType)
        {


            ToolStrip bar = (ToolStrip)Grasshopper.Instances.DocumentEditor.Controls[0].Controls[1];

            ToolStripItemCollection items = ((ToolStrip)(Grasshopper.Instances.DocumentEditor).Controls[0].Controls[1]).Items;

            switch (subcriptionType)
            {
                case SubcriptionType.Subscribe:
                    if (!items.ContainsKey("Look"))
                    {
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
                        items.Add(new ToolStripButton("Select", (Image)Properties.Resources.Distributor.ToBitmap(), onClick: (s, e) => SelectButton_OnValueChanged(s, e))
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

                    if (!items.ContainsKey("RemoParam"))
                    {
                        items.Add(new ToolStripButton("RemoParam", (Image)Properties.Resources.RemoSlider.ToBitmap(), onClick: (s, e) => RemoParamButton_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "RemoParam",
                            Size = new Size(28, 28),
                            ToolTipText = "Turn the parameter components shareable.",
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



                    if (!items.ContainsKey("SyncAnnotations"))
                    {
                        items.Add(new ToolStripButton("SyncAnnotations", (Image)Properties.Resources.Annotate.ToBitmap(), onClick: (s, e) => SyncAnnotations_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "SyncAnnotations",
                            Size = new Size(28, 28),
                            ToolTipText = "Syncronize Document Scribbles and Drawings.",
                        });
                    }

                    if (!items.ContainsKey("RequestSync"))
                    {
                        items.Add(new ToolStripButton("RequestSync", (Image)Properties.Resources.RequestSync.ToBitmap(), onClick: (s, e) => RequestSyncComponents_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "RequestSync",
                            Size = new Size(28, 28),
                            ToolTipText = "Requests a sync from the main reference script.",
                        });
                    }

                    if (!items.ContainsKey("SyncCanvas"))
                    {
                        items.Add(new ToolStripButton("SyncCanvas", (Image)Properties.Resources.CanvasSync.ToBitmap(), onClick: (s, e) => SyncCanvas_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "SyncCanvas",
                            Size = new Size(28, 28),
                            ToolTipText = "Syncronize all the components from this canvas to the other canvases.",
                        });
                    }

                    if (!items.ContainsKey("FixCanvas"))
                    {
                        items.Add(new ToolStripButton("FixCanvas", (Image)Properties.Resources.FixCanvas.ToBitmap(), onClick: (s, e) => FixCanvas_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "FixCanvas",
                            Size = new Size(28, 28),
                            ToolTipText = "Fixes the Canvas Duplicate Components.",
                        });
                    }
                    break;
                case SubcriptionType.Unsubscribe:
                    if (items.ContainsKey("Look")) items.RemoveByKey("Look");
                    if (items.ContainsKey("Select")) items.RemoveByKey("Select");
                    if (items.ContainsKey("RemoParam")) items.RemoveByKey("RemoParam");
                    if (items.ContainsKey("SyncComps")) items.RemoveByKey("SyncComps");
                    if (items.ContainsKey("SyncAnnotations")) items.RemoveByKey("SyncAnnotations");
                    if (items.ContainsKey("RequestSync")) items.RemoveByKey("RequestSync");
                    if (items.ContainsKey("SyncCanvas")) items.RemoveByKey("SyncCanvas");
                    if (items.ContainsKey("FixCanvas")) items.RemoveByKey("FixCanvas");
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    if (items.ContainsKey("Look")) items.RemoveByKey("Look");
                    if (items.ContainsKey("Select")) items.RemoveByKey("Select");
                    if (items.ContainsKey("RemoParam")) items.RemoveByKey("RemoParam");
                    if (items.ContainsKey("SyncComps")) items.RemoveByKey("SyncComps");
                    if (items.ContainsKey("SyncAnnotations")) items.RemoveByKey("SyncAnnotations");
                    if (items.ContainsKey("RequestSync")) items.RemoveByKey("RequestSync");
                    if (items.ContainsKey("SyncCanvas")) items.RemoveByKey("SyncCanvas");
                    if (items.ContainsKey("FixCanvas")) items.RemoveByKey("FixCanvas");
                    if (!items.ContainsKey("Look"))
                    {
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
                        items.Add(new ToolStripButton("Select", (Image)Properties.Resources.Distributor.ToBitmap(), onClick: (s, e) => SelectButton_OnValueChanged(s, e))
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
                    if (!items.ContainsKey("RemoParam"))
                    {
                        items.Add(new ToolStripButton("RemoParam", (Image)Properties.Resources.RemoSlider.ToBitmap(), onClick: (s, e) => RemoParamButton_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "RemoParam",
                            Size = new Size(28, 28),
                            ToolTipText = "Turn the parameter components shareable.",
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

                    if (!items.ContainsKey("SyncAnnotations"))
                    {
                        items.Add(new ToolStripButton("SyncAnnotations", (Image)Properties.Resources.Annotate.ToBitmap(), onClick: (s, e) => SyncAnnotations_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "SyncAnnotations",
                            Size = new Size(28, 28),
                            ToolTipText = "Syncronize Document Scribbles and Drawings.",
                        });
                    }
                    if (!items.ContainsKey("RequestSync"))
                    {
                        items.Add(new ToolStripButton("RequestSync", (Image)Properties.Resources.RequestSync.ToBitmap(), onClick: (s, e) => RequestSyncComponents_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "RequestSync",
                            Size = new Size(28, 28),
                            ToolTipText = "Requests a sync from the main reference script.",
                        });
                    }
                    if (!items.ContainsKey("SyncCanvas"))
                    {
                        items.Add(new ToolStripButton("SyncCanvas", (Image)Properties.Resources.CanvasSync.ToBitmap(), onClick: (s, e) => SyncCanvas_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "SyncCanvas",
                            Size = new Size(28, 28),
                            ToolTipText = "Syncronize all the components from this canvas to the other canvases.",
                        });
                    }
                    if (!items.ContainsKey("FixCanvas"))
                    {
                        items.Add(new ToolStripButton("FixCanvas", (Image)Properties.Resources.FixCanvas.ToBitmap(), onClick: (s, e) => FixCanvas_OnValueChanged(s, e))
                        {
                            AutoSize = true,
                            DisplayStyle = ToolStripItemDisplayStyle.Image,
                            ImageAlign = ContentAlignment.MiddleCenter,
                            ImageScaling = ToolStripItemImageScaling.SizeToFit,
                            Margin = new Padding(0, 0, 0, 0),
                            Name = "FixCanvas",
                            Size = new Size(28, 28),
                            ToolTipText = "Fixes the Canvas Duplicate Components.",
                        });
                    }
                    break;
                default:
                    break;
            }
        }

        private void RequestSyncComponents_OnValueChanged(object s, EventArgs e)
        {
            RemoRequestSync remoRequestSync = new RemoRequestSync(this.username, this.sessionID);
            SendCommands(this, remoRequestSync, 1, enable);
        }

        private void SyncAnnotations_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
            var canvas = Instances.ActiveCanvas;

            var selectedAnnotations = thisDoc.Objects.Where(o => o is GH_Scribble || o is GH_Markup).ToList();

            bool thereIsSelection = selectedAnnotations.Any(o => o.Attributes.Selected);

            // ask the user with a dialouge if they want to continue of there is no selection
            if (!thereIsSelection)
            {
                DialogResult result = MessageBox.Show("There is no selected Scribble or Markup.\nDo you want to sync all Annotations?\nThis may take some time.", "No Selection", MessageBoxButtons.YesNo);
                if (result == DialogResult.No) return;
            }

            selectedAnnotations = thereIsSelection ?
                selectedAnnotations.Where(o => o.Attributes.Selected).ToList() : selectedAnnotations;


            SendSyncComponentCommand(selectedAnnotations, 1, this.sessionID, this.enable, true);


        }

        private void RemoParamButton_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
            var canvas = Instances.ActiveCanvas;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            foreach (var item in thisDoc.SelectedObjects())
            {

                if (remoSetupComp.subscribedObjs.Contains(item))
                {
                    item.SolutionExpired -= SendRemoParameterCommand;
                    remoSetupComp.subscribedObjs.Remove(item);
                }
                else
                {
                    item.SolutionExpired += SendRemoParameterCommand;
                    remoSetupComp.subscribedObjs.Add(item);
                }
            }

            Grasshopper.Instances.ActiveCanvas.Invalidate();
        }

        private void Item_SolutionExpired(IGH_DocumentObject sender, EventArgs e)
        {

            List<IGH_DocumentObject> objs = new List<IGH_DocumentObject>() { sender };

            // find the remoSetupCompV3 on the active canvas document
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
            RemoSetupClientV3 remoSetupClientV3 = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();

            SendSyncComponentCommand(objs, 1, remoSetupClientV3.sessionID, remoSetupClientV3.enable, false);
        }

        private void Markup_PreviewExpired(IGH_DocumentObject sender, GH_PreviewExpiredEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Item_DisplayExpired(IGH_DocumentObject sender, GH_DisplayExpiredEventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            if (sender is GH_ButtonObject)
            {
                RemoParamButton remoParamButton = new RemoParamButton(remoSetupComp.username, this.sessionID, (GH_ButtonObject)sender);
                SendCommands(remoSetupComp, remoParamButton, 1, enable);
                return;
            }

            RemoParameter remoParameter = new RemoParameter(remoSetupComp.username, remoSetupComp.sessionID, sender);
            this.lastCommand = remoParameter;
            this.lastRpmGuid = sender.InstanceGuid;
            SendCommands(remoSetupComp, remoParameter, 1, enable);
        }

        private void Item_AttributesChanged(IGH_DocumentObject sender, GH_AttributesChangedEventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            if (sender is GH_ButtonObject)
            {
                RemoParamButton remoParamButton = new RemoParamButton(remoSetupComp.username, this.sessionID, (GH_ButtonObject)sender);
                SendCommands(remoSetupComp, remoParamButton, 1, enable);
                return;
            }

            RemoParameter remoParameter = new RemoParameter(remoSetupComp.username, remoSetupComp.sessionID, sender);

            SendCommands(remoSetupComp, remoParameter, 1, enable);
        }

        private void Item_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            if (sender is GH_ButtonObject)
            {
                RemoParamButton remoParamButton = new RemoParamButton(remoSetupComp.username, this.sessionID, (GH_ButtonObject)sender);
                SendCommands(remoSetupComp, remoParamButton, 1, enable);
                return;
            }

            RemoParameter remoParameter = new RemoParameter(remoSetupComp.username, remoSetupComp.sessionID, sender);

            SendCommands(remoSetupComp, remoParameter, 1, enable);
        }

        private void HighlightRemoParameters(GH_Canvas sender)
        {
            if (this == null || this.OnPingDocument() == null) return;
            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();

            System.Drawing.Color color = System.Drawing.Color.FromArgb(150, 237, 76, 226);

            foreach (IGH_DocumentObject item in remoSetupComp.subscribedObjs)
            {
                System.Drawing.RectangleF bound = item.Attributes.Bounds;

                var itemDoc = item.OnPingDocument();

                if (itemDoc == null)
                {
                    remoSetupComp.subscribedObjs.Remove(item);
                    return;
                }

                sender.Graphics.DrawPath(new System.Drawing.Pen(color, 10), DrawFilletedRectangle(bound, 0));
            }
        }

        public void SendRemoParameterCommand(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
             
            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            if (sender is GH_ButtonObject)
            {
                RemoParamButton remoParamButton = new RemoParamButton(remoSetupComp.username, this.sessionID, (GH_ButtonObject)sender);
                
                SendCommands(remoSetupComp, remoParamButton, 1, enable);
                return;
            }

            RemoParameter remoParameter = new RemoParameter(remoSetupComp.username, remoSetupComp.sessionID, sender);
            this.lastCommand = remoParameter;
            this.lastRpmGuid = sender.InstanceGuid;
            SendCommands(remoSetupComp, remoParameter, 1, enable);

        } 

        GraphicsPath DrawFilletedRectangle(RectangleF rectangle, float radius)
        {
            // Check for valid radius
            if (radius <= 0) radius = 1;

            // Create a GraphicsPath object
            GraphicsPath path = new GraphicsPath();

            // Add arcs for the rounded corners
            path.AddArc(rectangle.X, rectangle.Y, radius * 2, radius * 2, 180, 90); // Top left corner
            path.AddArc(rectangle.Right - radius * 2, rectangle.Y, radius * 2, radius * 2, 270, 90); // Top right corner
            path.AddArc(rectangle.Right - radius * 2, rectangle.Bottom - radius * 2, radius * 2, radius * 2, 0, 90); // Bottom right corner
            path.AddArc(rectangle.X, rectangle.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Bottom left corner

            // Connect the arcs with lines
            path.CloseFigure(); // This connects the end of the last arc with the start of the first arc

            return path;
        }


        private void FixCanvas_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = Grasshopper.Instances.ActiveCanvas.Document;

            var remoclientv3 = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            var commandExecutor = (CommandExecutor)thisDoc.Objects.Where(obj => obj is CommandExecutor).FirstOrDefault();

            // come back to this

            thisDoc.ScheduleSolution(1, doc =>
            {
                Dictionary<Guid, int> duplicates = FindDuplicateGuids(
                    thisDoc.Objects.Select(obj => obj.InstanceGuid).ToList());
                foreach (var item in duplicates)
                {
                    if (item.Value > 1)
                    {
                        for (int i = 0; i < item.Value - 1; i++)
                        {
                            var obj = thisDoc.Objects.Where(o => o.InstanceGuid == item.Key).LastOrDefault();
                            if (obj == null) continue;

                            thisDoc.RemoveObject(obj, false);
                        }
                    }
                }

            });

        }

        private void SyncCanvas_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            ResetGHColorsToDefault();

            var remoclientv3 = this;

            if (remoclientv3 == null) return;

            thisDoc.DeselectAll();
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

            GH_LooseChunk tempChunk = new GH_LooseChunk(null);
            thisDoc.Write(tempChunk);

            GH_Document saveDoc = new GH_Document();
            saveDoc.Read(tempChunk);

            saveDoc.RemoveObjects(saveDoc.Objects.Where(obj => obj.NickName.Contains("RemoSetup")).ToList(), false);

            GH_LooseChunk toSendChunk = new GH_LooseChunk(null);
            saveDoc.Write(toSendChunk);
            string xml = toSendChunk.Serialize_Xml();

            RemoCanvasSync remoCanvasSync = new RemoCanvasSync(username, this.sessionID, xml);
            string syncCommand = RemoCommand.SerializeToJson(remoCanvasSync);

            if (remoclientv3.enable)
            {
                remoclientv3.client.Send(syncCommand);
            }

        }

        private void SyncComponents_OnValueChanged(object sender, EventArgs e)
        {
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            var selectionObjs = thisDoc.SelectedObjects().ToList();

            // if there is more than 10 components selected give a waring saying it is too much and return
            if (selectionObjs.Count > 10)
            {
                DialogResult result = MessageBox.Show("There are more than 10 components selected.\n" +
                    "This may freeze your computer for several minutes!.\n" +
                    "Do you want to continue?", "Too Many Components\n" +
                    "Note:\n" +
                    "Please remember to rewire your syncronized component outputs afterwards as the may disconnect on the receving computers!", MessageBoxButtons.YesNo);
                if (result == DialogResult.No) return;
            }
            else
            {
                // dialouge saying that the components will be syncronized but the outputs will be disconnected
                MessageBox.Show("The selected components will be syncronized.\n" +
                                "Please remember to rewire your syncronized component outputs afterwards as the may disconnect on the receving computers!",
                                "Syncronize Components", MessageBoxButtons.OK);
            }

            // 

            SendSyncComponentCommand(selectionObjs, this.commandRepeat, this.sessionID, this.enable, false);
        }

        private static void SendSyncComponentCommand(List<IGH_DocumentObject> selectionObjs, int commandRepeat, string sessionID, bool enable, bool annotationOnly)
        {
            List<Guid> guids = new List<Guid>();
            List<string> xmls = new List<string>();

            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClientV3 setupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (setupComp == null) return;


            if (selectionObjs.Count == 0) return;

            thisDoc.DeselectAll();
            Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

            thisDoc.ScheduleSolution(1, doc =>
            {
                var skip = SubcriptionType.Skip;
                var unsubscribe = SubcriptionType.Unsubscribe;
                var subscribe = SubcriptionType.Subscribe;
                setupComp.SetUpRemoSharpEvents(skip, unsubscribe, unsubscribe, skip, skip, unsubscribe);

                try
                {

                    //RemoPartialDoc remoPartialDoc = new RemoPartialDoc(setupComp.username, selectionObjs.ToList(), thisDoc);

                    if (annotationOnly)
                    {
                        RemoCommand remoCompSync = new RemoAnnotation(setupComp.username, sessionID, selectionObjs.ToList(), thisDoc);
                        RemoSetupClientV3.SendCommands(setupComp, remoCompSync, commandRepeat, enable);
                    }
                    else
                    {
                        RemoCommand remoCompSync = new RemoCompSync(setupComp.username, sessionID, selectionObjs.ToList(), thisDoc);
                        RemoSetupClientV3.SendCommands(setupComp, remoCompSync, commandRepeat, enable);
                    }

                }
                catch (Exception error)
                {
                    Rhino.RhinoApp.WriteLine(error.Message);
                }

                setupComp.SetUpRemoSharpEvents(skip, subscribe, subscribe, skip, skip, subscribe);
            });
        }

        private List<IGH_DocumentObject> ReSelectSingleParameters()
        {
            var currSelection = new List<IGH_DocumentObject>();

            var selectedObjs = this.OnPingDocument().SelectedObjects();

            foreach (IGH_DocumentObject item in selectedObjs)
            {
                if (item is IGH_Component)
                {
                    IGH_Component comp = (IGH_Component)item;
                    int sourceCount = 0;
                    for (int i = 0; i < comp.Params.Input.Count; i++)
                    {
                        var input = comp.Params.Input[i];
                        if (input.SourceCount != 0)
                        {
                            sourceCount++;
                        }
                    }
                    if (sourceCount == 0) currSelection.Add(item);
                }
                else if (item is IGH_Param)
                {
                    IGH_Param comp = (IGH_Param)item;
                    if (comp.SourceCount == 0)
                    {
                        currSelection.Add(item);
                    }
                }
            }

            return currSelection;
        }

        private static void ResetGHColorsToDefault()
        {
            //DEFAULTS
            Grasshopper.GUI.Canvas.GH_Skin.canvas_grid = Color.FromArgb(30, 0, 0, 0);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_back = Color.FromArgb(255, 212, 208, 200);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_edge = Color.FromArgb(255, 0, 0, 0);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
        }

        private static void SetColorToSyncMode()
        {
            //DEFAULTS
            Grasshopper.GUI.Canvas.GH_Skin.canvas_grid = Color.FromArgb(80, 255, 255, 255);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_back = Color.FromArgb(255, 212, 208, 200);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_edge = Color.FromArgb(255, 0, 0, 0);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
        }

        private static void HighlightParameter()
        {
            //DEFAULTS
        }

        public static void SendCommands(RemoSetupClientV3 setupComp, RemoCommand command, int repeat, bool enable)
        {
            if (!enable) return;

            string cmdJson = RemoCommand.SerializeToJson(command);

            try
            {
                for (int i = 0; i < repeat; i++)
                {
                    //bool clientIsConnected = setupComp.client.IsAlive;
                    setupComp.client.Send(cmdJson);
                    string pause = "";
                }
            }
            catch
            {
                System.Windows.Forms.MessageBox.Show("Connection to the server may not be properly working!", "Conncetion Error", MessageBoxButtons.OK);
            }
        }

        private void LookButton_OnValueChanged(object sender, EventArgs e)
        {
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClientV3 setupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (setupComp == null) return;


            var zoomLevel = Grasshopper.Instances.ActiveCanvas.Viewport.Zoom;

            RemoCanvasView remoCanvasView = new RemoCanvasView(setupComp.username, this.sessionID, thisDoc.SelectedObjects(), zoomLevel);
            RemoSetupClientV3.SendCommands(setupComp, remoCanvasView, commandRepeat, enable);
        }

        private void SelectButton_OnValueChanged(object sender, EventArgs e)
        {
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClientV3 setupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (setupComp == null) return;

            var selection = thisDoc.SelectedObjects();

            List<Guid> slectionGuids = new List<Guid>();
            foreach (var item in selection)
            {
                slectionGuids.Add(item.InstanceGuid);
            }
            RemoSelect cmd = new RemoSelect(setupComp.username, setupComp.sessionID, slectionGuids, DateTime.Now.Second);
            RemoSetupClientV3.SendCommands(setupComp, cmd, commandRepeat, enable);
        }



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
            //DA.GetData(0, ref url);
            //DA.GetData(1, ref sessionID);
            if (string.IsNullOrEmpty(this.username) || this.username == "username")
            {
                this.enableSwitch.CurrentValue = false;
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
                return RemoSharp.Properties.Resources.Setup_Component.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8BECA1B2-2251-4AB5-AFC2-89D5E68CBAA6"); }
        }
    }

    public class MarkUpCollectionHistory
    {
        public float width;
        public GH_MarkupDashPattern pattern;
        public System.Drawing.Color color;

        public MarkUpCollectionHistory(GH_Markup markup)
        {
            GH_MarkupAttributes markupAttributes = new GH_MarkupAttributes(markup);
            // get the properties of the markupattribute
            var properties = markupAttributes.Properties;

            this.width = properties.Width;
            this.pattern = properties.Pattern;
            this.color = properties.Colour;
        }

        // an equality method to compare two markups
        public bool EqualsOther(MarkUpCollectionHistory markupAttributes)
        {
            // get the properties of the markupattribute
            if (this.width != markupAttributes.width) return false;
            if (this.pattern != markupAttributes.pattern) return false;
            if (this.color != markupAttributes.color) return false;

            return true;
        }

    }

}