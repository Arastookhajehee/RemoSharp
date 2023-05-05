using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Grasshopper.GUI.Base;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using System.Drawing;
using WindowsInput.Native;
using WindowsInput;
using System.Windows.Forms;
using System.Threading;
using GHCustomControls;
using WPFNumericUpDown;

using RemoSharp.RemoCommandTypes;
using WebSocketSharp;
using System.Linq;
using Microsoft.Win32;
using RemoSharp.WsClientCat;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace RemoSharp
{
    public class CommandExecutor : GHCustomComponent
    {

        PushButton pushButton1;
        private string currentXMLString = "";
        public int deletionIndex = -1;
        public Guid compGuid = Guid.Empty;
        public List<Guid> deletionGuids = new List<Guid>();

        // public RemoParam Persistent Variables
        public decimal val = 0;
        public bool buttonVal = false;
        public bool toggleVal = false;
        public string text = "";
        public Color colorVal = Color.DarkGray;

        //RemoParam public component coordinates
        public int remoParamX = -1;
        public int remoParamY = -1;
        public int RemoParamIndex = -1;
        public Guid remoParamGuid = Guid.Empty;

        //RemoExecutor (connector and Creation) public persistent variables
        public int srcComp = -1;
        public int tgtComp = -1;
        public int rightShifttgtComp = -1;
        public int srcCompOutputIndex = -1;
        public int tgtCompInputIndex = -1;
        public int connectionMouseDownX = -1;
        public int connectionMouseDownY = -1;
        public int connectionMouseUpX = -1;
        public int connectionMouseUpY = -1;

        //WireConnection variables
        bool replaceConnections = false;
        public Guid sourceGuid = Guid.Empty;
        public Guid targetGuid = Guid.Empty;

        public string currentStringCommand = "";



        /// <summary>
        /// Initializes a new instance of the CommandExecutor class.
        /// </summary>
        public CommandExecutor()
          : base("CommandExecutor", "CmdExe",
              "Excecution of Remote Commands for all manipulations from the client side remotely.",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pushButton1 = new PushButton("WS_Client",
                "Creates The Required WS Client Components To Broadcast Canvas Screen.", "WS_Client");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddTextParameter("Command", "Cmd", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.item,"");
            pManager.AddTextParameter("Username", "User", "This PC's Username", GH_ParamAccess.item, "");
        }
        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                System.Drawing.PointF pivot = this.Attributes.Pivot;
                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 662, pivot.Y - 40);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 490, pivot.Y + 9);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 280, pivot.Y);
                System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X - 150, pivot.Y + 10);

                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.NickName = "RemoSharp";
                panel.SetUserText("");
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);

                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;
                wss.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                wsRecv.CreateAttributes();
                wsRecv.Attributes.Pivot = wsRecvPivot;
                wsRecv.Params.RepairParamAssociations();

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsRecv, true);

                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    //wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    this.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                });

            }
        }
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("type", "type", "type", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string command = "";
            string username = "";
            if (!DA.GetData(0, ref command)) return;
            if (!DA.GetData(1, ref username)) return;

            if (this.Params.Input[0].Sources.Count > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This Component Accepts Only a Single Input." + Environment.NewLine
                    + "Make Sure Only One Wire With A Single Text Block Command is Connected.");
                return;
            }

            if (string.IsNullOrEmpty(command) || command == "Hello World" || command == "RemoSharp.RemoCommandTypes.RemoNullCommand") return;

            try
            {
                if (currentStringCommand.Equals(command)) return;
                else currentStringCommand = command;
            }
            catch { }


            RemoCommand remoCommand = RemoCommand.DeserializeFromJson(command);
            if (username.Equals(remoCommand.issuerID) || username.IsNullOrEmpty()) return;

            switch (remoCommand.commandType)
            {
                #region moveComp
                case (CommandType.MoveComponent):

                    RemoMove moveCommand = (RemoMove)remoCommand;

                    var currentSelection = this.OnPingDocument().SelectedObjects();
                    OnPingDocument().DeselectAll();

                    var otherComp = this.OnPingDocument().FindObject(moveCommand.objectGuid, false);

                    GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

                    grip.CreateObjectData(otherComp);
                    this.OnPingDocument().Select(grip);

                    int vecX = (int) (moveCommand.moveX - otherComp.Attributes.Pivot.X);
                    int vecY = (int) (moveCommand.moveY - otherComp.Attributes.Pivot.Y);
                    Size vec = new Size(vecX, vecY);

                    this.OnPingDocument().TranslateObjects(vec, true);
                    this.OnPingDocument().DeselectAll();

                    foreach (var selObj in currentSelection)
                    {
                        GH_RelevantObjectData currentGrip = new GH_RelevantObjectData(selObj.Attributes.Pivot);
                        grip.CreateObjectData(selObj);
                        this.OnPingDocument().Select(grip, true, false);
                    }
                    return;
                #endregion
                case (CommandType.NullCommand):
                    return;
                #region wireconnection
                case (CommandType.WireConnection):
                    RemoConnect wireCommand = (RemoConnect)remoCommand;

                    bool connect = wireCommand.RemoConnectType == RemoConnectType.Add || wireCommand.RemoConnectType == RemoConnectType.Replace;
                    bool disconnect = wireCommand.RemoConnectType == RemoConnectType.Remove || wireCommand.RemoConnectType == RemoConnectType.Replace;
                    System.Guid sourceGuid = wireCommand.sourceObjectGuid;
                    int outIndex = wireCommand.sourceOutput;
                    bool sourceIsSpecial = wireCommand.isSourceSpecial;
                    System.Guid targetGuid = wireCommand.targetObjectGuid;
                    int inIndex = wireCommand.targetInput;
                    bool targetIsSpecial = wireCommand.isTargetSpecial;
                    if (connect)
                    {
                        if (sourceIsSpecial)
                        {
                            if (targetIsSpecial)
                            {
                                var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    if (disconnect) target.RemoveAllSources();
                                    target.AddSource(source);
                                });
                            }
                            else
                            {
                                var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    if (disconnect) target.Params.Input[inIndex].RemoveAllSources();
                                    target.Params.Input[inIndex].AddSource(source);
                                });
                            }
                        }
                        else
                        {
                            if (targetIsSpecial)
                            {
                                var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    if (disconnect) target.RemoveAllSources();
                                    target.AddSource(source.Params.Output[outIndex]);
                                });
                            }
                            else
                            {
                                var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    if (disconnect) target.Params.Input[inIndex].RemoveAllSources();
                                    target.Params.Input[inIndex].AddSource(source.Params.Output[outIndex]);
                                });
                            }
                        }

                    }
                    else
                    {
                        if (sourceIsSpecial)
                        {
                            if (targetIsSpecial)
                            {
                                var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    target.RemoveSource(source);
                                });
                            }
                            else
                            {
                                var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    target.Params.Input[inIndex].RemoveSource(source);
                                });
                            }
                        }
                        else
                        {
                            if (targetIsSpecial)
                            {
                                var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    target.RemoveSource(source.Params.Output[outIndex]);
                                });
                            }
                            else
                            {
                                var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                                var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                                this.OnPingDocument().ScheduleSolution(1, doc =>
                                {
                                    target.Params.Input[inIndex].RemoveSource(source.Params.Output[outIndex]);
                                });
                            }
                        }
                    }
                    return;
                #endregion
                
                #region componentCreation
                case (CommandType.Create):
                    RemoCreate createCommand = (RemoCreate)remoCommand;


                    //var objectExists = this.OnPingDocument().Objects
                    //    .OfType<IGH_DocumentObject>()
                    //    .Where(currentObject => currentObject.InstanceGuid == createCommand.objectGuid)
                    //    .ToList();

                    //int currentObjectCount = objectExists.Count;
                    //bool nullGuid = createCommand.objectGuid == Guid.Empty;

                    int currentObjectCount = 0;
                    bool nullGuid = false;

                    foreach (IGH_DocumentObject item in this.OnPingDocument().Objects)
                    {
                        if (item.InstanceGuid == createCommand.objectGuid || createCommand.objectGuid == null)
                        {
                            nullGuid = true;
                            currentObjectCount++;
                        }
                    }


                    if (currentObjectCount > 0 || nullGuid) return;

                    string typeName = createCommand.componentType;

                    if (typeName.Contains("RemoSharp.WsClientCat")) return;
                    if (typeName.Equals("RemoSharp.RemoButton") ||
                        typeName.Equals("RemoSharp.RemoColorSwatch") ||
                        typeName.Equals("RemoSharp.RemoPanel") ||
                        typeName.Equals("RemoSharp.RemoToggle") ||
                        typeName.Equals("RemoSharp.RemoSlider")) return;

                    Guid newCompGuid = createCommand.objectGuid;
                    int pivotX = createCommand.X;
                    int pivotY = createCommand.Y;

                    try
                    {
                        PointF tempPivot = new PointF(pivotX, pivotY);
                        //var currentObject = this.OnPingDocument().FindObject(tempPivot, 2);

                        // find all groups that this object is in.
                        var currentObject = OnPingDocument()
                            .Objects
                            .OfType<IGH_DocumentObject>()
                            .Where(obj => obj.InstanceGuid == newCompGuid)
                            .ToList();
                            

                        if (currentObject.Count > 0)
                        {
                            if (currentObject.GetType().ToString().Equals(typeName)) return;
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        if (typeName.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                        {

                            string[] specialParams = createCommand.specialParameters.Split(',');

                            decimal minBound = Convert.ToDecimal(specialParams[0]);
                            decimal maxBound = Convert.ToDecimal(specialParams[1]);
                            decimal currentValue = Convert.ToDecimal(specialParams[2]);
                            int accuracy = Convert.ToInt32(specialParams[3]);
                            GH_SliderAccuracy acc = (GH_SliderAccuracy)Enum.Parse(typeof(GH_SliderAccuracy), specialParams[4]);
                            GH_NumberSlider sliderComponent = new GH_NumberSlider();
                            //sliderComponent.NewInstanceGuid(new Guid(newGuid));
                            sliderComponent.CreateAttributes();
                            sliderComponent.Attributes.Pivot = new PointF(pivotX, pivotY);
                            sliderComponent.Slider.Minimum = minBound;
                            sliderComponent.Slider.Maximum = maxBound;
                            sliderComponent.Slider.Value = currentValue;
                            sliderComponent.Slider.DecimalPlaces = accuracy;
                            sliderComponent.Slider.Type = acc;
                            sliderComponent.Attributes.Selected = false;
                            sliderComponent.NewInstanceGuid(newCompGuid);

                            this.OnPingDocument().AddObject(sliderComponent, false);

                            //var obj = sliderComponent;
                            //GH_RelevantObjectData grip = new GH_RelevantObjectData(obj.Attributes.Pivot);
                            //this.OnPingDocument().Select(grip, false, true);
                            return;
                        }
                        if (typeName.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                        {

                            string[] specialParams = createCommand.specialParameters.Split(',');


                            bool multiLine = Convert.ToBoolean(specialParams[0]);
                            bool drawIndicies = Convert.ToBoolean(specialParams[1]);
                            bool drawPaths = Convert.ToBoolean(specialParams[2]);
                            bool wrap = Convert.ToBoolean(specialParams[3]);
                            GH_Panel.Alignment alignment = (GH_Panel.Alignment)Enum.Parse(typeof(GH_Panel.Alignment), specialParams[4]);
                            int boundSizeX = Convert.ToInt32(specialParams[5]);
                            int boundSizeY = Convert.ToInt32(specialParams[6]);

                            string contentText = "";
                            for (int i = 7; i < specialParams.Length; i++)
                            {
                                if (i < specialParams.Length - 1)
                                {
                                    contentText += specialParams[i] + ",";
                                }
                                else
                                {
                                    contentText += specialParams[i];
                                }
                            }

                            GH_Panel panelComponent = new GH_Panel();
                            //panelComponent.NewInstanceGuid(new Guid(newGuid));
                            panelComponent.CreateAttributes();
                            panelComponent.Attributes.Pivot = new PointF(pivotX, pivotY);
                            panelComponent.Properties.Multiline = multiLine;
                            panelComponent.Properties.DrawIndices = drawIndicies;
                            panelComponent.Properties.DrawPaths = drawPaths;
                            panelComponent.Properties.Wrap = wrap;
                            panelComponent.Properties.Alignment = alignment;
                            panelComponent.SetUserText(contentText);
                            panelComponent.Attributes.Bounds = new RectangleF(pivotX, pivotY, boundSizeX, boundSizeY);
                            panelComponent.Attributes.Selected = false;
                            panelComponent.NewInstanceGuid(newCompGuid);

                            this.OnPingDocument().AddObject(panelComponent, false);
                            //var obj = panelComponent;
                            //GH_RelevantObjectData grip = new GH_RelevantObjectData(obj.Attributes.Pivot);
                            //this.OnPingDocument().Select(grip, false, true);
                            return;
                        }
                        else if (typeName.Equals("RemoSharp.RemoGeomStreamer"))
                        {
                            string address = createCommand.specialParameters;
                            System.Drawing.PointF pivot = new System.Drawing.PointF(pivotX, pivotY);
                            System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 75, pivot.Y - 80);
                            System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 100, pivot.Y - 40);
                            System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 34, pivot.Y + 50);
                            System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X + 36, pivot.Y + 40);

                            Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                            panel.CreateAttributes();
                            panel.Attributes.Pivot = panelPivot;
                            panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 55, 20);
                            panel.SetUserText(address);
                            panel.Attributes.Selected = false;

                            Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                            button.CreateAttributes();
                            button.Attributes.Pivot = buttnPivot;
                            button.Attributes.Selected = false;

                            RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                            wss.CreateAttributes();
                            wss.Attributes.Pivot = wssPivot;
                            wss.Attributes.Selected = false;

                            RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                            wsRecv.CreateAttributes();
                            wsRecv.Attributes.Pivot = wsRecvPivot;
                            wsRecv.Attributes.Selected = false;

                            RemoSharp.RemoGeomParser remoGeomParser = new RemoGeomParser();
                            remoGeomParser.CreateAttributes();
                            remoGeomParser.Attributes.Pivot = pivot;
                            remoGeomParser.Params.RepairParamAssociations();
                            remoGeomParser.Attributes.Selected = false;
                            remoGeomParser.NewInstanceGuid(newCompGuid);

                            this.OnPingDocument().ScheduleSolution(1, doc =>
                            {
                                this.OnPingDocument().AddObject(panel, true);
                                this.OnPingDocument().AddObject(button, true);
                                this.OnPingDocument().AddObject(wss, true);
                                this.OnPingDocument().AddObject(wsRecv, true);
                                this.OnPingDocument().AddObject(remoGeomParser, true);

                                wss.Params.Input[2].AddSource((IGH_Param)button);
                                wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                                remoGeomParser.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                                wss.Params.Input[0].AddSource((IGH_Param)panel);
                            });

                            //var obj = remoGeomParser;
                            //GH_RelevantObjectData grip = new GH_RelevantObjectData(obj.Attributes.Pivot);
                            //this.OnPingDocument().Select(grip, false, true);
                        }
                        else if (typeName.Equals("RemoSharp.RemoGeomParser"))
                        {
                            string address = createCommand.specialParameters;
                            System.Drawing.PointF pivot = new System.Drawing.PointF(pivotX, pivotY);
                            System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 375, pivot.Y - 121);
                            System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 290, pivot.Y - 85);
                            System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 42, pivot.Y - 92);
                            System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 226, pivot.Y - 14);

                            Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                            panel.CreateAttributes();
                            panel.Attributes.Pivot = panelPivot;
                            panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                            panel.SetUserText(address);
                            panel.Attributes.Selected = false;

                            Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                            button.CreateAttributes();
                            button.Attributes.Pivot = buttnPivot;
                            button.Attributes.Selected = false;

                            RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                            wss.CreateAttributes();
                            wss.Attributes.Pivot = wssPivot;
                            wss.Attributes.Selected = false;

                            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                            wsSend.CreateAttributes();
                            wsSend.Attributes.Pivot = wsSendPivot;
                            wsSend.Attributes.Selected = false;

                            RemoSharp.RemoGeomStreamer remoGeom = new RemoGeomStreamer();
                            remoGeom.CreateAttributes();
                            remoGeom.Attributes.Pivot = pivot;
                            remoGeom.Params.RepairParamAssociations();
                            remoGeom.Attributes.Selected = false;
                            remoGeom.NewInstanceGuid(newCompGuid);

                            this.OnPingDocument().ScheduleSolution(1, doc =>
                            {
                                this.OnPingDocument().AddObject(panel, true);
                                this.OnPingDocument().AddObject(button, true);
                                this.OnPingDocument().AddObject(wss, true);
                                this.OnPingDocument().AddObject(wsSend, true);
                                this.OnPingDocument().AddObject(remoGeom, true);

                                wss.Params.Input[2].AddSource((IGH_Param)button);
                                wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                                wsSend.Params.Input[1].AddSource((IGH_Param)remoGeom.Params.Output[0]);
                                wss.Params.Input[0].AddSource((IGH_Param)panel);
                            });
                            //var obj = remoGeom;
                            //GH_RelevantObjectData grip = new GH_RelevantObjectData(obj.Attributes.Pivot);
                            //this.OnPingDocument().Select(grip, false, true);
                        }
                        else
                        {
                            RecognizeAndMake(typeName, pivotX, pivotY, newCompGuid);
                        }
                    }
                    catch (Exception e)
                    {
                        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                    }

                    return;
                #endregion

                #region deleteComponent
                case (CommandType.Delete):
                    RemoDelete remoDelete = (RemoDelete)remoCommand;
                    deletionGuids.Clear();
                    deletionGuids.AddRange(remoDelete.objectGuids);
                    try
                    {
                        this.OnPingDocument().ScheduleSolution(0, DeleteComponent);
                    }
                    catch
                    {
                    }
                    return;
                #endregion

                #region GeometryStream
                case (CommandType.StreamGeom):
                    DA.SetData(0, remoCommand.ToString());

                    break;
                #endregion

                #region hide
                case (CommandType.Hide):

                    RemoHide hideCommand = (RemoHide)remoCommand;
                    try
                    {
                        IGH_DocumentObject hideComponent = (IGH_DocumentObject)this.OnPingDocument().FindObject(hideCommand.objectGuid, false);
                        GH_Component gh_component = hideComponent as GH_Component;
                        if(gh_component != null) gh_component.Hidden = hideCommand.state;
                        
                    }
                    catch
                    {

                    }
                    return;
                #endregion

                #region lock
                case (CommandType.Lock):

                    RemoLock lockCommand = (RemoLock)remoCommand;
                    Guid lockCompGuid = lockCommand.objectGuid;
                    bool state = lockCommand.state;

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        try
                        {
                            var lockComponent = (IGH_Component)this.OnPingDocument().FindObject(lockCompGuid, false);
                            lockComponent.Locked = state;
                            lockComponent.ExpireSolution(false);
                        }
                        catch
                        {
                            var lockComponent = (IGH_Param)this.OnPingDocument().FindObject(lockCompGuid, false);
                            lockComponent.Locked = state;
                            lockComponent.ExpireSolution(false);
                        }
                    });
                    return;
                #endregion

                #region RemoParamSlider
                case (CommandType.RemoSlider):
                    RemoParamSlider remoSlider = (RemoParamSlider)remoCommand;
                    if (remoSlider.objectGuid == Guid.Empty) return;
                    GH_NumberSlider sliderComp = (GH_NumberSlider) this.OnPingDocument().FindObject(remoSlider.objectGuid, false);

                    sliderComp.Slider.Minimum = remoSlider.sliderminBound;
                    sliderComp.Slider.Maximum = remoSlider.slidermaxBound;
                    sliderComp.Slider.DecimalPlaces = remoSlider.decimalPlaces;
                    sliderComp.Slider.Type = (GH_SliderAccuracy)remoSlider.sliderType;

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        sliderComp.SetSliderValue(remoSlider.sliderValue);
                    });
                    return;
                #endregion

                #region RemoParamButton
                case (CommandType.RemoButton):
                    RemoParamButton remoButton = (RemoParamButton)remoCommand;
                    if (remoButton.objectGuid == Guid.Empty) return;
                    GH_ButtonObject buttonComp = (GH_ButtonObject)this.OnPingDocument().FindObject(remoButton.objectGuid, false);

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        buttonComp.ButtonDown = remoButton.buttonValue;
                        buttonComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamToggle
                case (CommandType.RemoToggle):
                    RemoParamToggle remoToggle = (RemoParamToggle)remoCommand;
                    if (remoToggle.objectGuid == Guid.Empty) return;
                    GH_BooleanToggle toggleComp = (GH_BooleanToggle)this.OnPingDocument().FindObject(remoToggle.objectGuid, false);

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        toggleComp.Value = remoToggle.toggleValue;
                        toggleComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamPanel
                case (CommandType.RemoPanel):
                    RemoParamPanel remoPanel = (RemoParamPanel)remoCommand;
                    if (remoPanel.objectGuid == Guid.Empty) return;
                    GH_Panel panelComp = (GH_Panel)this.OnPingDocument().FindObject(remoPanel.objectGuid, false);

                    panelComp.Properties.Multiline = remoPanel.MultiLine;
                    panelComp.Properties.DrawIndices = remoPanel.DrawIndecies;
                    panelComp.Properties.DrawPaths = remoPanel.DrawPaths;
                    panelComp.Properties.Wrap = remoPanel.Wrap;
                    panelComp.Properties.Alignment = (GH_Panel.Alignment) remoPanel.Alignment;
                    
                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        panelComp.SetUserText(remoPanel.panelContent);
                        panelComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamColor
                case (CommandType.RemoColor):
                    RemoParamColor remoColor = (RemoParamColor)remoCommand;
                    if (remoColor.objectGuid == Guid.Empty) return;
                    GH_ColourSwatch colorComp = (GH_ColourSwatch)this.OnPingDocument().FindObject(remoColor.objectGuid, false);

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        colorComp.SwatchColour = Color.FromArgb(remoColor.Alpha,remoColor.Red, remoColor.Green, remoColor.Blue);
                        colorComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamMDSlider
                case (CommandType.RemoMDSlider):
                    RemoParamMDSlider remoMDSlider = (RemoParamMDSlider)remoCommand;
                    if (remoMDSlider.objectGuid == Guid.Empty) return;
                    GH_MultiDimensionalSlider mdSliderComp = (GH_MultiDimensionalSlider)this.OnPingDocument().FindObject(remoMDSlider.objectGuid, false);

                    mdSliderComp.XInterval = new Interval(remoMDSlider.minBoundX,remoMDSlider.maxBoundX);
                    mdSliderComp.YInterval = new Interval(remoMDSlider.minBoundY,remoMDSlider.maxBoundY);
                    
                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        mdSliderComp.Value = new Point3d(remoMDSlider.ValueX, remoMDSlider.ValueY, 0);
                        mdSliderComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamPoint3d
                case (CommandType.RemoPoint3d):
                    RemoParamPoint3d remoPoint3d = (RemoParamPoint3d)remoCommand;
                    if (remoPoint3d.objectGuid == Guid.Empty) return;
                    IGH_Param pointComp = (IGH_Param)this.OnPingDocument().FindObject(remoPoint3d.objectGuid, false);
                    Param_Point pointParamComp = (Param_Point)pointComp;

                    GH_Structure<GH_Point> pointTree = new GH_Structure<GH_Point>();
                    foreach (string item in remoPoint3d.pointsAndTreePath)
                    {
                        string[] coordsAndPath = item.Split(':');
                        string[] coordsStrings = coordsAndPath[0].Split(',');
                        string[] pathStrings = coordsAndPath[1].Split(',');
                        double[] coords = coordsStrings.Select(double.Parse).ToArray();
                        int[] path = pathStrings.Select(int.Parse).ToArray();


                        pointTree.Append(new GH_Point(new Point3d(coords[0], coords[1], coords[2])),new GH_Path(path));
                    }

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        pointComp.Attributes.Selected = false;
                        pointParamComp.SetPersistentData(pointTree);
                        pointParamComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region RemoParamVector3d
                case (CommandType.RemoVector3d):
                    RemoParamVector3d remoVector3d = (RemoParamVector3d)remoCommand;
                    if (remoVector3d.objectGuid == Guid.Empty) return;
                    IGH_Param vectorComp = (IGH_Param)this.OnPingDocument().FindObject(remoVector3d.objectGuid, false);
                    Param_Vector vectorParamComp = (Param_Vector)vectorComp;

                    GH_Structure<GH_Vector> vectorTree = new GH_Structure<GH_Vector>();
                    foreach (string item in remoVector3d.vectorsAndTreePath)
                    {
                        string[] coordsAndPath = item.Split(':');
                        string[] coordsStrings = coordsAndPath[0].Split(',');
                        string[] pathStrings = coordsAndPath[1].Split(',');
                        double[] coords = coordsStrings.Select(double.Parse).ToArray();
                        int[] path = pathStrings.Select(int.Parse).ToArray();


                        vectorTree.Append(new GH_Vector(new Vector3d(coords[0], coords[1], coords[2])), new GH_Path(path));
                    }

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        vectorParamComp.SetPersistentData(vectorTree);
                        vectorParamComp.ExpireSolution(true);
                    });
                    return;
                #endregion


                #region RemoParamPlane
                case (CommandType.RemoPlane):
                    RemoParamPlane remoPlane = (RemoParamPlane)remoCommand;
                    if (remoPlane.objectGuid == Guid.Empty) return;
                    IGH_Param planeComp = (IGH_Param)this.OnPingDocument().FindObject(remoPlane.objectGuid, false);
                    Param_Plane planeParamComp = (Param_Plane)planeComp;

                    GH_Structure<GH_Plane> planeTree = new GH_Structure<GH_Plane>();
                    foreach (string item in remoPlane.planesAndTreePath)
                    {
                        string[] coordsAndPath = item.Split(':');
                        string[] coordsStrings = coordsAndPath[0].Split(',');
                        string[] pathStrings = coordsAndPath[1].Split(',');
                        double[] coords = coordsStrings.Select(double.Parse).ToArray();
                        int[] path = pathStrings.Select(int.Parse).ToArray();

                        Point3d planeOrigin = new Point3d(coords[0], coords[1], coords[2]);
                        Vector3d planeVecX = new Vector3d(coords[3], coords[4], coords[5]);
                        Vector3d planeVecY = new Vector3d(coords[6], coords[7], coords[8]);
                        planeTree.Append(new GH_Plane(new Plane(planeOrigin, planeVecX, planeVecY)), new GH_Path(path));
                    }

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        planeParamComp.SetPersistentData(planeTree);
                        planeParamComp.ExpireSolution(true);
                    });
                    return;
                #endregion

                #region select
                case (CommandType.Select):

                    RemoSelect selectionCommand = (RemoSelect)remoCommand;

                    this.OnPingDocument().DeselectAll();
                    foreach (Guid guid in selectionCommand.selectionGuids)
                    {
                        var selectionComp = this.OnPingDocument().FindObject(guid, false);
                        selectionComp.Attributes.Selected = true;
                    }
                    
                    //GH_RelevantObjectData selectionGrip = new GH_RelevantObjectData(selectionComp.Attributes.Pivot);                   
                    //selectionGrip.CreateObjectData(selectionComp);

                    //this.OnPingDocument().Select(selectionGrip, true, false);            

                    return;
                #endregion
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
                return RemoSharp.Properties.Resources.CMD.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6b7b7c9e-0e81-4195-93b4-279099080880"); }
        }

        private void DeleteComponent(GH_Document doc)
        {
            try
            {
                //var otherComp = this.OnPingDocument().Objects[deletionIndex];
                foreach (Guid item in deletionGuids)
                {
                    var otherComp = this.OnPingDocument().FindObject(item, false);
                    if (otherComp != null) this.OnPingDocument().RemoveObject(otherComp, true);
                }
            }
            catch (Exception e)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
            }


        }

        // RemoParam Functions (Most of them schedule solutions, so their realtime use might freeze the main GH file)

        private int RemoParamFindObjectOnCanvasByCoordinates(int compCoordX, int compCoordY, string objectType)
        {
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.OnPingDocument();
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
                    string[] componentType = component.ToString().Split('.');
                    string componentTypeString = componentType[componentType.Length - 1];
                    var pivot = component.Attributes.Pivot;
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));
                    if (distance > 0 && objectType.Equals(componentTypeString))
                    {
                        if (distance < minDistance)
                        {
                            // getting the type of the component via the ToString() method
                            // later the ToString() method is better to be changed to something more reliable
                            minDistance = distance;
                            objIndex = i;

                        }
                    }

                }
            }
            catch { }
            objectType = ghObjects[objIndex].ToString();
            return objIndex;
        }

        private void PushTheButton(GH_Document doc)
        {
            //GH_ButtonObject button = (GH_ButtonObject)this.OnPingDocument().Objects[RemoParamIndex];
            GH_ButtonObject button = (GH_ButtonObject)this.OnPingDocument().FindObject(remoParamGuid, false);

            //remoParamGuid
            button.ButtonDown = buttonVal;
            button.ExpireSolution(true);
        }
        private void ToggleBooleanToggle(GH_Document doc)
        {
            //GH_BooleanToggle toggle = (GH_BooleanToggle)this.OnPingDocument().Objects[RemoParamIndex];
            GH_BooleanToggle toggle = (GH_BooleanToggle)this.OnPingDocument().FindObject(remoParamGuid, false);
            toggle.Value = toggleVal;
            toggle.ExpireSolution(true);
        }
        private void WriteToPanel(GH_Document doc)
        {
            //GH_Panel panel = (GH_Panel)this.OnPingDocument().Objects[RemoParamIndex];
            GH_Panel panel = (GH_Panel)this.OnPingDocument().FindObject(remoParamGuid, false);
            panel.SetUserText(text);
            //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "panel found");
            panel.ExpireSolution(true);
        }
        private void ColorSwatchChange(GH_Document doc)
        {
            //GH_ColourSwatch colorSW = (GH_ColourSwatch)this.OnPingDocument().Objects[RemoParamIndex];
            GH_ColourSwatch colorSW = (GH_ColourSwatch)this.OnPingDocument().FindObject(remoParamGuid, false);
            colorSW.SwatchColour = colorVal;
            colorSW.ExpireSolution(true);
        }
        private void AddValueToSlider(GH_Document doc)
        {
            //GH_NumberSlider numSlider = (GH_NumberSlider)this.OnPingDocument().Objects[RemoParamIndex];
            GH_NumberSlider numSlider = (GH_NumberSlider)this.OnPingDocument().FindObject(remoParamGuid, false);
            numSlider.SetSliderValue(val);
        }

        private int MoveCompFindComponentOnCanvasByCoordinates(int compX, int compY)
        {
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.OnPingDocument();
            var ghObjects = ghDoc.Objects;
            var thisCompLoc = new System.Drawing.PointF(compX, compY);

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

                    if (distance > 0)
                    {
                        if (distance < minDistance)
                        {

                            // getting the type of the component via the ToString() method
                            // later the ToString() method is better to be changed to something more reliable
                            minDistance = distance;
                            objIndex = i;

                        }
                    }

                }
            }
            catch { }
            return objIndex;
        }

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY,Guid newCompGuid)
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
            var myObject = (IGH_DocumentObject)Activator.CreateInstance(type);
            // creating atts to create the pivot point
            // this pivot point can be anywhere
            myObject.CreateAttributes();
            //        myObject.Attributes.Pivot = new System.Drawing.PointF(200, 600);
            var currentPivot = new System.Drawing.PointF(pivotX, pivotY);

            myObject.Attributes.Pivot = currentPivot;
            myObject.Attributes.Selected = false;
            //myObject.Attributes.Selected = true;
            myObject.ExpireSolution(true);

            try
            {
                IGH_Component gh_Component = (IGH_Component) myObject;
                gh_Component.Params.RepairParamAssociations();
                gh_Component.NewInstanceGuid(newCompGuid);
                // making sure the update argument is false to prevent GH crashes
                thisDoc.AddObject(gh_Component, false);
                //GH_RelevantObjectData grip = new GH_RelevantObjectData(gh_Component.Attributes.Pivot);
                //this.OnPingDocument().Select(grip, false, true);
            }
            catch
            {
                // making sure the update argument is false to prevent GH crashes
                myObject.NewInstanceGuid(newCompGuid);
                thisDoc.AddObject(myObject, false);
                //GH_RelevantObjectData grip = new GH_RelevantObjectData(myObject.Attributes.Pivot);
                //this.OnPingDocument().Select(grip, false, true);
            }

        }

        int RemoConnectFindComponentOnCanvasByCoordinates(int compCoordX, int compCoordY)
        {
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.OnPingDocument();
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
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));

                    if (distance > 0)
                    {
                        if (distance < minDistance)
                        {

                            // getting the type of the component via the ToString() method
                            // later the ToString() method is better to be changed to something more reliable
                            minDistance = distance;
                            objIndex = i;

                        }
                    }
                }
            }
            catch { }
            return objIndex;
        }

        string CategoryString(int compIndex)
        {
            string tgtCategory = this.OnPingDocument().Objects[compIndex].Category;
            string tgtSubcategory = this.OnPingDocument().Objects[compIndex].SubCategory;
            return tgtCategory + tgtSubcategory;
        }
        bool CheckforSpecialCase(string type)
        {
            //    List<string> specialTypeStrings = new List<string>{"Parameters","Special","PlanktonGh","Heteroptera",
            //        "PRC_IOClasses", "GalapagosComponents", "FUROBOT"};
            List<string> specialTypeStrings = new List<string> { "ParamsUtil", "ParamsGeometry", "ParamsPrimitive", "ParamsInput" };
            bool isSpecialType = false;
            for (int i = 0; i < specialTypeStrings.Count; i++)
            {
                if (type.Equals(specialTypeStrings[i]))
                {
                    isSpecialType = true;
                }
            }
            return isSpecialType;
        }



        // 1 componentToComponent
        public void CompToComp(GH_Document doc)
        {

            //public int connectionMouseDownX = -1;
            //public int connectionMouseDownY = -1;
            //public int connectionMouseUpX = -1;
            //public int connectionMouseUpY = -1;

            //var sourceComponent = (GH_Component)this.OnPingDocument().Objects[srcComp];
            //var closeComponent = (GH_Component)this.OnPingDocument().Objects[tgtComp];




            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));

            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }

            if (foundIn.Attributes.Parent.DocObject.InstanceGuid != foundOut.Attributes.Parent.DocObject.InstanceGuid)
            {
                if (replaceConnections) foundIn.RemoveAllSources();
                replaceConnections = false;
                foundIn.AddSource((IGH_Param)foundOut);
            }



        }

        // 2 CompToSpecial
        public void CompToSpecial(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }

            if (foundOut != null && foundIn != null)
            {
                if (foundIn.Attributes.Parent.DocObject.InstanceGuid != foundOut.Attributes.Parent.DocObject.InstanceGuid)
                {
                    if (replaceConnections) foundIn.RemoveAllSources();
                    replaceConnections = false;
                    foundIn.AddSource((IGH_Param)foundOut);
                }
            }
            else
            {
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];
                if (replaceConnections) closeComponent.RemoveAllSources();
                replaceConnections = false;
                closeComponent.AddSource((IGH_Param)foundOut);
            }
        }

        // 3 SpecialToComp
        public void SpecialToComp(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {

                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }
            if (foundOut != null && foundIn != null)
            {
                if (replaceConnections) foundIn.RemoveAllSources();
                replaceConnections = false;
                foundIn.AddSource((IGH_Param)foundOut);
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];

                if (replaceConnections) foundIn.RemoveAllSources();
                replaceConnections = false;
                foundIn.AddSource(sourceComponent);
            }
        }

        // 4 SpecialToSpecial
        public void SpecialToSpecial(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }
            if (foundOut != null && foundIn != null)
            {
                if (foundIn.Attributes.Parent.DocObject.InstanceGuid != foundOut.Attributes.Parent.DocObject.InstanceGuid)
                {
                    if (replaceConnections) foundIn.RemoveAllSources();
                    replaceConnections = false;
                    foundIn.AddSource((IGH_Param)foundOut);
                }
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];

                if (sourceComponent.InstanceGuid != closeComponent.InstanceGuid)
                {
                    if (replaceConnections) closeComponent.RemoveAllSources();
                    replaceConnections = false; 
                    closeComponent.AddSource((IGH_Param)sourceComponent);
                }
            }
        }

        // 5 CompFromComp
        public void DisCompFromComp(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }


            foundIn.RemoveSource((IGH_Param)foundOut);

        }

        // 6 CompFromSpecial
        public void DisCompFromSpecial(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }

            if (foundOut != null && foundIn != null)
            {
                foundIn.RemoveSource((IGH_Param)foundOut);
            }
            else
            {
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];

                closeComponent.RemoveSource((IGH_Param)foundOut);
            }

        }

        // 7 SpecialFromComp
        public void DisSpecialFromComp(GH_Document doc)
        {
            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }

            if (foundOut != null && foundIn != null)
            {
                foundIn.RemoveSource((IGH_Param)foundOut);
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];

                foundIn.RemoveSource(sourceComponent);
            }
        }

        // 8 SpecialFromSpecial
        public void DisSpecialFromSpecial(GH_Document doc)
        {
            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 5, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 5, connectionMouseUpY));


            for (int i = -7; i < 8; i++)
            {
                int offset = i;
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }

            if (foundOut != null && foundIn != null)
            {
                foundIn.RemoveSource((IGH_Param)foundOut);
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];

                closeComponent.RemoveSource(sourceComponent);
            }
        }

    }
}