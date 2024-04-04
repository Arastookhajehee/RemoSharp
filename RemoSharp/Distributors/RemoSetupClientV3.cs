using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GHCustomControls;
using WPFNumericUpDown;
using RemoSharp.RemoCommandTypes;
using System.Drawing;
using WebSocketSharp;
using System.Windows.Forms;
using System.Linq;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using RemoSharp.RemoParams;
using System.Reflection;
using GH_IO.Serialization;
using Grasshopper.Kernel.Special;
using static RemoSharp.RemoSetupClient;

using System.Threading.Tasks;
using Grasshopper;
using System.Drawing.Drawing2D;



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
        public List<string> messages = new List<string>();
        public WebSocket client;
        string url = "";
        bool connect = false;
        bool preventUndo = true;
        bool controlDown = false;
        public static string DISCONNECTION_KEYWORD = "<<DisconnectFromRemoSharpServer>>";

        PushButton setupButton;
        ToggleSwitch undoPreventionSwitch;
        ToggleSwitch enableSwitch;

        public bool autoUpdate = true;
        public bool keepRecord = false;
        bool listen = true;

        int commandRepeat = 5;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;

        public bool enable = false;

        public List<Guid> remoCommandIDs = new List<Guid>();

        public string username = "";
        public string password = "";

        float[] downPnt = { 0, 0 };
        float[] upPnt = { 0, 0 };

        List<Guid> addedObjects = new List<Guid>();
        public bool remoParamModeActive = false;
        public PointF mouseLocation = PointF.Empty;
        Guid hoverParam = Guid.Empty;

        public List<IGH_DocumentObject> subscribedObjs = new List<IGH_DocumentObject>();


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


            enableSwitch = new ToggleSwitch("Connect", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;
            AddCustomControl(enableSwitch);

            undoPreventionSwitch = new ToggleSwitch("No Undo", "Warns the user not to use undo or redo", true);
            undoPreventionSwitch.OnValueChanged += UndoPreventionSwitch_OnValueChanged;
            AddCustomControl(undoPreventionSwitch);

            pManager.AddTextParameter("url", "url", "", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("connect", "connect", "", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("KeepAlive", "keepAlive", "", GH_ParamAccess.item);

            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Password", "pass", "Password to this session", GH_ParamAccess.item, "password");
            pManager.AddBooleanParameter("syncSend", "syncSend", "Syncs this grasshopper script for all other connected clients", GH_ParamAccess.item, false);

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
                var thisDoc = this.OnPingDocument();
                if (thisDoc == null) return;
                this.NickName = "RemoSetup";
                AddInteractionButtonsToTopBar(SubcriptionType.Subscribe);

                client = new WebSocket(url);
                //this.Message = "Connecting";

                SubcriptionType subType = SubcriptionType.Subscribe;
                SetUpRemoSharpEvents(subType, subType, subType, subType, subType, subType);

                CommandExecutor executor = thisDoc.Objects.Where(obj => obj is CommandExecutor).FirstOrDefault() as CommandExecutor;
                executor.enable = enable;

            }
            else
            {
                var thisDoc = this.OnPingDocument();
                if (thisDoc == null) return;
                AddInteractionButtonsToTopBar(SubcriptionType.Unsubscribe);

                SubcriptionType unsubType = SubcriptionType.Unsubscribe;
                SetUpRemoSharpEvents(unsubType, unsubType, unsubType, unsubType, unsubType, unsubType);
                client.Close();

                CommandExecutor executor = thisDoc.Objects.Where(obj => obj is CommandExecutor).FirstOrDefault() as CommandExecutor;
                executor.enable = enable;
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
            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            var client = this.client;
            //bool isAlive = client.IsAlive;

            RemoNullCommand nullCommand;
            string connectionString = "";
            switch (clientEvents)
            {
                case SubcriptionType.Subscribe:

                    client.OnMessage += Client_OnMessage;
                    client.OnClose += Client_OnClose;
                    client.OnOpen += Client_OnOpen;
                    this.Message = "Connecting...";
                    Task.Run(() => EstablishConnection(client));

                    break;
                case SubcriptionType.Unsubscribe:
                    client.OnMessage -= Client_OnMessage;
                    client.OnClose -= Client_OnClose;
                    client.OnOpen -= Client_OnOpen;
                    if (!this.Message.Equals("Wrong Username\nor Password"))
                    {
                        this.Message = "Disconnected";
                    }
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    client.OnMessage -= Client_OnMessage;
                    client.OnMessage += Client_OnMessage;
                    client.OnClose -= Client_OnClose;
                    client.OnClose += Client_OnClose;
                    client.OnOpen -= Client_OnOpen;
                    client.OnOpen += Client_OnOpen;

                    Task.Run(() => EstablishConnection(client));

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

        private void EstablishConnection(WebSocket client)
        {
            client.Connect();
            RemoNullCommand nullCommand = new RemoNullCommand(this.username);
            string connectionString = RemoCommand.SerializeToJson(nullCommand);
            client.Send(connectionString);
            if (client.IsAlive) this.Message = "Connected";
        }

        private bool CheckForHealthyGH(out GH_Document gh_document)
        {
            GH_Document thisDoc = this.OnPingDocument();

            if (thisDoc == null || this == null)
            {
                var unsub = SubcriptionType.Unsubscribe;
                this.SetUpRemoSharpEvents(unsub, unsub, unsub, unsub, unsub,unsub);
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

        public static void SubscribeAllParams(RemoSetupClientV3 remoSetupComp, bool subscribe)
        {
            List<string> accaptableTypes = new List<string>() {
            "Grasshopper.Kernel.Special.GH_NumberSlider",
            "Grasshopper.Kernel.Special.GH_Panel",
            "Grasshopper.Kernel.Special.GH_ColourSwatch",
            "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
            "Grasshopper.Kernel.Special.GH_BooleanToggle",
            "Grasshopper.Kernel.Special.GH_ButtonObject"
            };

            var allParams = remoSetupComp.OnPingDocument().Objects;


            if (!subscribe)
            {
                var deletionGuids = remoSetupComp.OnPingDocument().Objects
                    .Where(obj => obj.NickName.Equals(RemoParam.RemoParamKeyword))
                    .Select(obj => remoSetupComp.OnPingDocument().FindObject(obj.InstanceGuid, false))
                    .ToList();

                remoSetupComp.OnPingDocument().RemoveObjects(deletionGuids, false);
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
                        string nickName = item.NickName;
                        bool isSetupButtno = nickName.Equals("RemoSetup");

                        if (isSetupButtno) continue;

                        GroupObjParam(remoSetupComp, item);
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

        public static void SubscribeAllParams(RemoSetupClientV3 remoSetupComp, List<IGH_DocumentObject> allParams, bool subscribe)
        {
            List<string> accaptableTypes = new List<string>() {
            "Grasshopper.Kernel.Special.GH_NumberSlider",
            "Grasshopper.Kernel.Special.GH_Panel",
            "Grasshopper.Kernel.Special.GH_ColourSwatch",
            "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
            "Grasshopper.Kernel.Special.GH_BooleanToggle",
            "Grasshopper.Kernel.Special.GH_ButtonObject"
            };

            foreach (var item in allParams)
            {
                if (subscribe)
                {
                    string typeString = item.GetType().FullName;

                    string pause = "";


                    bool isInTheList = accaptableTypes.Contains(typeString);

                    if (isInTheList)
                    {
                        string nickName = item.NickName;
                        bool isSetupButtno = nickName.Equals("RemoSetup");

                        if (isSetupButtno) continue;

                        GroupObjParam(remoSetupComp, item);
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


        

        public void RemoParameterizeMDSlider(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
            if (sender == null || sender.OnPingDocument() == null) sender.SolutionExpired -= RemoParameterizeMDSlider;
            Grasshopper.Kernel.Special.GH_MultiDimensionalSlider param = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)sender;

            IGH_ParamConditionStatus status = FindIGH_ParamStatus(param);

            if (status.tabKeyIsDown && status.isRemoParamGrouped && status.mouseHoverOnExpiredParam)
            {
                RemoParamMDSlider remoParam = new RemoParamMDSlider(this.username, param, false);
                string json = RemoCommand.SerializeToJson(remoParam);
                if (this.enable) this.client.Send(json);
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
                if (this.enable) this.client.Send(json);
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
                if (this.enable) this.client.Send(json);
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
                if (this.enable) this.client.Send(json);
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
                if (this.enable) this.client.Send(json);
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
                if (this.enable) this.client.Send(json);
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
                    if (this.enable) this.client.Send(json);
                }
            }
            catch
            {
                Console.WriteLine("sync problem");

            }

        }

        public static void GroupObjParam(RemoSetupClientV3 remoSetupComp,  IGH_DocumentObject obj)
        {
            remoSetupComp.OnPingDocument().ScheduleSolution(1, doc =>
            {
                GH_Group group = new GH_Group();
                group.CreateAttributes();
                group.AddObject(obj.InstanceGuid);
                group.Colour = System.Drawing.Color.FromArgb(0, 0, 0, 0);
                group.NickName = RemoParam.RemoParamKeyword;
                remoSetupComp.OnPingDocument().AddObject(group, false);
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
                    .Select(obj => (GH_Group)obj);
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

            IGH_ParamConditionStatus status = new IGH_ParamConditionStatus(isSelected, noInput, tabKeyIsDown, isIn_gh_group, mouseHoverOnExpiredParam, paramIsNotPointComp);
            return status;
        }

        private void ActiveCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            this.mouseLocation = mouseEvent.CanvasLocation;
        }

        private void Client_OnOpen(object sender, EventArgs e)
        {
            RemoNullCommand nullCommand = new RemoNullCommand(username);
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
            //Rhino.RhinoApp.WriteLine("DragOver");
            if (DisconnectOnImproperClose()) return;
        }

        private void ActiveCanvas_MouseDown(object sender, MouseEventArgs e)
        {
            if(DisconnectOnImproperClose()) return;

            GH_Canvas canvas = sender as GH_Canvas;
            GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(canvas.Viewport, e);
            this.downPnt = new float[] { mouseEvent.CanvasLocation.X, mouseEvent.CanvasLocation.Y };
            this.interaction = canvas.ActiveInteraction;
        }

        private bool DisconnectOnImproperClose()
        {
            var thisComp = this;
            var thisDoc = thisComp.OnPingDocument();
            if (thisComp == null || thisDoc == null)
            {
                SubcriptionType unsub = SubcriptionType.Unsubscribe;
                this.SetUpRemoSharpEvents(unsub, unsub, unsub, unsub, unsub, unsub);
                System.Windows.Forms.MessageBox.Show("Connection closed due to improper disconnection!", "RemoSharp Connection Error!");
                return true;
            }
            return false;
        }

        private void ActiveCanvas_MouseUp(object sender, MouseEventArgs e)
        {
            if (DisconnectOnImproperClose()) return;
            GH_Canvas canvas = sender as GH_Canvas;
            GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(canvas.Viewport, e);
            this.upPnt = new float[] { mouseEvent.CanvasLocation.X, mouseEvent.CanvasLocation.Y };

            if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction)
            {
                SendRemoWireCommand();
            }
            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
            {
                SendRemoMoveCommand();
            }


        }

        private void SendRemoMoveCommand()
        {
            GH_Document thisDoc = null;
            if (!CheckForHealthyGH(out thisDoc)) return;

            float downPntX = downPnt[0];
            float downPntY = downPnt[1];
            float upPntX = upPnt[0];
            float upPntY = upPnt[1];

            float distance = (float)Math.Sqrt(Math.Pow(upPntX - downPntX, 2) + Math.Pow(upPntY - downPntY, 2));
            if (distance < 5) return;

            if (thisDoc.SelectedCount < 1) return;

            var selection = this.OnPingDocument().SelectedObjects();

            List<Guid> moveGuids = selection.Select(obj => obj.InstanceGuid).ToList();
            float xDiff = upPntX - downPntX;
            float yDiff = upPntY - downPntY;

            command = new RemoMove(username, moveGuids, new Size((int)xDiff, (int)yDiff));
            SendCommands(this, command, commandRepeat, enable);

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

            int outIndex = -1;
            bool outIsSpecial = false;
            System.Guid outGuid = GetComponentGuidAnd_Output_Index(
              source, out outIndex, out outIsSpecial);

            int inIndex = -1;
            bool inIsSpecial = false;
            if (target == null || source == null) return;
            System.Guid inGuid = GetComponentGuidAnd_Input_Index(
              target, out inIndex, out inIsSpecial);

            string outCompXML = RemoCommand.SerializeToXML(outGuid);
            string inCompXML = RemoCommand.SerializeToXML(inGuid);

            string outCompDocXML = "";
            string inCompDocXML = "";

            command = new RemoConnect(this.username, outGuid, inGuid, remoConnectType,
                outCompXML, inCompXML, outCompDocXML, inCompDocXML);
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

            command = new RemoDelete(username, deleteGuids);
            SendCommands(this, command, commandRepeat, enable);
        }

        private void Client_OnMessage(object sender, MessageEventArgs e)
        {
            if (e.Data.Equals(RemoSetupClient.DISCONNECTION_KEYWORD))
            {
                client.OnMessage -= Client_OnMessage;
                client.OnOpen -= Client_OnOpen;
                client.OnClose -= Client_OnClose;
                client.Close();
                this.Message = "Wrong Username\nor Password";
                this.enableSwitch.CurrentValue = false;
            }

            messages.Add(e.Data);
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

                SetUpRemoSharpEvents(skip, unsub, unsub, skip, skip,skip);
                List<IGH_DocumentObject> objs = e.Objects.ToList();

                RemoPartialDoc remoPartialDoc = new RemoPartialDoc(this.username, objs, activeDoc);

                SendCommands(this, remoPartialDoc, 3, enable);


                //SubscribeAllParams(this, objs, true);

                SetUpRemoSharpEvents(skip, sub, sub, skip, skip, skip);

                //SendCommands(this, remoPartialDoc, 3, enable);

            });

        }


        private void UndoPreventionSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            preventUndo = Convert.ToBoolean(e.Value);
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
                //PointF wscButtonPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF wscTogglePivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF triggerPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 415 + yShift);
                PointF panelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift);
                PointF passPanelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift + 45);
                //PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 336 + yShift);
                PointF listenPivot = new PointF(pivot.X + xShift + 330, pivot.Y - 334 + yShift);

                PointF targetPivot = new PointF(pivot.X + xShift + 200, pivot.Y);
                PointF commandPivot = new PointF(pivot.X + xShift + 598, pivot.Y - 312 + yShift);
                //PointF commandButtonPivot = new PointF(pivot.X + xShift + 350, pivot.Y - 254 + yShift);

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
                wscToggle.Value = true;
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

                //// button
                //Grasshopper.Kernel.Special.GH_ButtonObject commandCompButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                //commandCompButton.CreateAttributes();
                //commandCompButton.Attributes.Pivot = commandButtonPivot;
                //commandCompButton.NickName = "RemoSetup";

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
                    //this.OnPingDocument().AddObject(commandCompButton, true);
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
                    //this.Params.Input[4].AddSource(commandCompButton);
                    this.Params.Input[0].AddSource(addressOutPuts[0]);
                    //this.Params.Input[1].AddSource(wscButton);
                    this.Params.Input[1].AddSource(wscToggle);

                    listenComp.Params.Input[0].AddSource(this.Params.Output[0]);

                    commandComp.Params.Input[0].AddSource(listenComp.Params.Output[0]);
                    commandComp.Params.Input[1].AddSource(panel);
                    //commandComp.Params.Input[2].AddSource(commandCompButton);

                    //bffTrigger.AddTarget(bffTriggerTarget);
                    trigger.AddTarget(listenComp.InstanceGuid);
                    //trigger.AddTarget(targetComp.InstanceGuid);

                });
            }
            this.ExpireSolution(true);
        }

        private void AddInteractionButtonsToTopBar(SubcriptionType subcriptionType)
        {



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
                    break;
                case SubcriptionType.Unsubscribe:
                    if(items.ContainsKey("Look")) items.RemoveByKey("Look");
                    if (items.ContainsKey("Select")) items.RemoveByKey("Select");
                    if (items.ContainsKey("RemoParam")) items.RemoveByKey("RemoParam");
                    if(items.ContainsKey("SyncComps")) items.RemoveByKey("SyncComps");
                    if(items.ContainsKey("SyncCanvas")) items.RemoveByKey("SyncCanvas");
                    break;
                case SubcriptionType.Skip:
                    break;
                case SubcriptionType.Resubscribe:
                    if (items.ContainsKey("Look")) items.RemoveByKey("Look");
                    if (items.ContainsKey("Select")) items.RemoveByKey("Select");
                    if (items.ContainsKey("RemoParam")) items.RemoveByKey("RemoParam");
                    if (items.ContainsKey("SyncComps")) items.RemoveByKey("SyncComps");
                    if (items.ContainsKey("SyncCanvas")) items.RemoveByKey("SyncCanvas");
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

                    break;
                default:
                    break;
            }
        }

        private void RemoParamButton_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;
            var canvas = Instances.ActiveCanvas;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            foreach (var item in remoSetupComp.subscribedObjs)
            {
                item.SolutionExpired -= SendRemoParameterCommand;
                //remoSetupComp.subscribedObjs.Remove(item);
            }

            remoSetupComp.subscribedObjs.Clear();
            var selection = thisDoc.SelectedObjects();
            if (selection.Count < 1) return;

            remoSetupComp.subscribedObjs.AddRange(selection);

            foreach (IGH_DocumentObject item in remoSetupComp.subscribedObjs)
            {
                item.SolutionExpired += SendRemoParameterCommand;                
            }

        }

        private void HighlightRemoParameters(GH_Canvas sender)
        {
            if (this == null || this.OnPingDocument() == null) return;
            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();

            System.Drawing.Color color = System.Drawing.Color.FromArgb(150, 237, 76, 226);

            foreach (IGH_DocumentObject item in remoSetupComp.subscribedObjs)
            {
                System.Drawing.RectangleF bound = item.Attributes.Bounds;

                sender.Graphics.DrawPath(new System.Drawing.Pen(color, 10), DrawFilletedRectangle(bound, 0));
            }            
        }

        public void SendRemoParameterCommand(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        {
           var thisDoc = this.OnPingDocument();
            if (thisDoc == null) return;

            RemoSetupClientV3 remoSetupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            if (remoSetupComp == null) return;

            RemoParameter remoParameter = new RemoParameter(remoSetupComp.username, sender);

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

        private void SyncCanvas_OnValueChanged(object s, EventArgs e)
        {
            var thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            ResetGHColorsToDefault();

            var remoclientv3 = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
            var commandExecutor = (CommandExecutor)thisDoc.Objects.Where(obj => obj is CommandExecutor).FirstOrDefault();
            
            if (remoclientv3 == null || commandExecutor == null) return;

            commandExecutor.errors.Clear();
            commandExecutor.ExpireSolution(true);
            thisDoc.DeselectAll();

            GH_LooseChunk tempChunk = new GH_LooseChunk(null);
            thisDoc.Write(tempChunk);

            GH_Document saveDoc = new GH_Document();
            saveDoc.Read(tempChunk);

            saveDoc.RemoveObjects(saveDoc.Objects.Where(obj => obj.NickName.Contains("RemoSetup")).ToList(), false);

            GH_LooseChunk toSendChunk = new GH_LooseChunk(null);
            saveDoc.Write(toSendChunk);
            string xml = toSendChunk.Serialize_Xml();

            RemoCanvasSync remoCanvasSync = new RemoCanvasSync(username, xml);
            string syncCommand = RemoCommand.SerializeToJson(remoCanvasSync);

            if (remoclientv3.enable)
            {
                remoclientv3.client.Send(syncCommand);
            }           

        }

        private void SyncComponents_OnValueChanged(object sender, EventArgs e)
        {
            List<Guid> guids = new List<Guid>();
            List<string> xmls = new List<string>();
            List<string> docXmls = new List<string>();

            GH_Document thisDoc = Grasshopper.Instances.ActiveCanvas.Document;
            RemoSetupClientV3 setupComp = (RemoSetupClientV3)thisDoc.Objects.Where(obj => obj is RemoSetupClientV3).FirstOrDefault();
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
            RemoSetupClientV3.SendCommands(setupComp, remoCompSync,3,enable);

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
                for (int i = 0; i < 3; i++)
                {
                    //bool clientIsConnected = setupComp.client.IsAlive;
                    setupComp.client.Send(cmdJson);
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
            RemoSetupClientV3.SendCommands(setupComp, remoCanvasView, 3, enable);
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
            RemoSelect cmd = new RemoSelect(setupComp.username, slectionGuids, DateTime.Now.Second);
            RemoSetupClientV3.SendCommands(setupComp, cmd, 3, enable);
        }



        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("WSClient", "wsc", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref url);
            DA.GetData(1, ref connect);
            //DA.GetData(2, ref keepAlive);

            bool syncThisCanvas = false;

            // getting the username information
            DA.GetData(2, ref username);
            //DA.GetData(1, ref client);
            DA.GetData(3, ref password);
            DA.GetData(4, ref syncThisCanvas);
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
}