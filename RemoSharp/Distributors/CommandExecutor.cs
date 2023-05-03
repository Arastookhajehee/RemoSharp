﻿using Grasshopper.Kernel;
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

namespace RemoSharp
{
    public class CommandExecutor : GHCustomComponent
    {

        PushButton pushButton1;
        private string currentXMLString = "";
        private int otherCompInx = -1;
        public int deletionIndex = -1;
        public Guid compGuid = Guid.Empty;

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
        int outputIndex = -1;
        int inputIndex = -1;

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

            pManager.AddTextParameter("Command", "Cmd", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.list, new List<string> { "" });
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
            List<string> commandList = new List<string>();
            string command = "";
            string username = "";
            if (!DA.GetDataList<string>(0, commandList)) return;
            if (!DA.GetData(1, ref username)) return;

            if (commandList.Count > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This Component Accepts Only a Single Input." + Environment.NewLine
                    + "Make Sure Only One Wire With A Single Text Block Command is Connected.");
            }
            else
            {
                command = commandList[0];
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
                    
                case (CommandType.NullCommand):
                    return;

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

                case (CommandType.Delete):

                    compGuid = remoCommand.objectGuid;
                    try
                    {
                        var deletionComponent = this.OnPingDocument().FindObject(compGuid, false);
                        this.OnPingDocument().ScheduleSolution(0, DeleteComponent);
                    }
                    catch
                    {
                    }
                    return;

                case (CommandType.StreamGeom):
                    DA.SetData(0, remoCommand.ToString());

                    break;

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

                //case (CommandType.RemoParam):
                //    DA.SetData(0, remoCommand.ToString());

                //    break;
                case (CommandType.Select):

                    RemoSelect selectionCommand = (RemoSelect)remoCommand;

                    var selectionComp = this.OnPingDocument().FindObject(selectionCommand.objectGuid, false);
                    this.OnPingDocument().DeselectAll();
                    GH_RelevantObjectData selectionGrip = new GH_RelevantObjectData(selectionComp.Attributes.Pivot);                   
                    selectionGrip.CreateObjectData(selectionComp);
                    
                    this.OnPingDocument().Select(selectionGrip, true, false);            

                    return;

                default:
                    break;
            }



            return;

            string[] cmds = command.Split(',');



            if (cmds[0] == "MoveComponent")
            {

                
            }

            if (cmds[0] == "Create")
            {

                
            }

            if (cmds[0] == "Hide" || cmds[0] == "RemoUnhide")
            {
                
            }

            if (cmds[0] == "Lock")
            {
               
            }

            if (cmds[0] == "WireConnection")
            {
                
            }


            if (cmds[0].Equals("RemoParam"))
            {
                int compLocX = Convert.ToInt32(cmds[1]);
                int compLocY = Convert.ToInt32(cmds[2]);

                if (cmds[3].Equals("PushTheButton"))
                {
                    RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_ButtonObject");
                    buttonVal = Convert.ToBoolean(cmds[4]);
                    remoParamGuid = Guid.Parse(cmds[5]);
                    this.OnPingDocument().ScheduleSolution(0, PushTheButton);
                    return;
                }
                if (cmds[3].Equals("ToggleBooleanToggle"))
                {
                    RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_BooleanToggle");
                    toggleVal = Convert.ToBoolean(cmds[4]);
                    remoParamGuid = Guid.Parse(cmds[5]);
                    this.OnPingDocument().ScheduleSolution(0, ToggleBooleanToggle);
                    return;
                }
                if (cmds[3].Equals("WriteToPanel"))
                {
                    remoParamGuid = Guid.Parse(cmds[4]);

                    //RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_Panel");
                    text = "";

                    bool multiLine = Convert.ToBoolean(cmds[5]);
                    bool drawIndicies = Convert.ToBoolean(cmds[6]);
                    bool drawPaths = Convert.ToBoolean(cmds[7]);
                    bool wrap = Convert.ToBoolean(cmds[8]);
                    GH_Panel.Alignment alignment = (GH_Panel.Alignment)Enum.Parse(typeof(GH_Panel.Alignment), cmds[9]);
                    //int boundSizeX = Convert.ToInt32(cmds[9]);
                    //int boundSizeY = Convert.ToInt32(cmds[10]);

                    //GH_Panel panelComponent = (GH_Panel)this.OnPingDocument().Objects[RemoParamIndex];
                    GH_Panel panelComponent = (GH_Panel)this.OnPingDocument().FindObject(remoParamGuid, false);

                    //panelComponent.CreateAttributes();
                    //panelComponent.Attributes.Pivot = new PointF(compLocX + 10, compLocY + 5);
                    panelComponent.Properties.Multiline = multiLine;
                    panelComponent.Properties.DrawIndices = drawIndicies;
                    panelComponent.Properties.DrawPaths = drawPaths;
                    panelComponent.Properties.Wrap = wrap;
                    panelComponent.Properties.Alignment = alignment;
                    //panelComponent.Attributes.Bounds = new RectangleF(compLocX + 10, compLocY + 5, boundSizeX, boundSizeY);

                    for (int i = 10; i < cmds.Length; i++)
                    {
                        if (i < cmds.Length - 1)
                        {
                            text += cmds[i] + ",";
                        }
                        else
                        {
                            text += cmds[i];
                        }
                    }

                    
                    this.OnPingDocument().ScheduleSolution(0, WriteToPanel);
                    return;
                }
                if (cmds[3].Equals("ColorSwatchChange"))
                {
                    RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_ColourSwatch");
                    int rVal = Convert.ToInt32(cmds[4]);
                    int gVal = Convert.ToInt32(cmds[5]);
                    int bVal = Convert.ToInt32(cmds[6]);
                    int aVal = Convert.ToInt32(cmds[7]);
                    remoParamGuid = Guid.Parse(cmds[8]);
                    colorVal = Color.FromArgb(aVal, rVal, gVal, bVal);
                    this.OnPingDocument().ScheduleSolution(0, ColorSwatchChange);
                    return;
                }
                if (cmds[3].Equals("AddValueToSlider"))
                {

                    RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_NumberSlider");
                    val = Convert.ToDecimal(cmds[4]);

                    decimal minBound = Convert.ToDecimal(cmds[4]);
                    decimal maxBound = Convert.ToDecimal(cmds[5]);
                    val = Convert.ToDecimal(cmds[6]);
                    int accuracy = Convert.ToInt32(cmds[7]);
                    GH_SliderAccuracy sliderType = (GH_SliderAccuracy)Enum.Parse(typeof(GH_SliderAccuracy), cmds[8]);

                    remoParamGuid = Guid.Parse(cmds[9]);

                    GH_NumberSlider panelComponent = (GH_NumberSlider)this.OnPingDocument().Objects[RemoParamIndex];
                    panelComponent.Slider.Minimum = minBound;
                    panelComponent.Slider.Maximum = maxBound;
                    panelComponent.Slider.DecimalPlaces = accuracy;
                    panelComponent.Slider.Type = sliderType;

                    this.OnPingDocument().ScheduleSolution(0, AddValueToSlider);
                    return;
                }


            }

            if (cmds[0].Equals("Selection"))
            {
                
            }
            else if (cmds[0].Equals("Deletion"))
            {
                
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

        private int FindComponentOnCanvasByCoordinates(int compX, int compY)
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
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));
                    if (distance < minDistance && !component.GetType().ToString().Equals("RemoSharp.RemoCompTarget"))
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

        private int DeletionCommandFindComponentOnCanvasByCoordinates(int compX, int compY)
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

        private void DeleteComponent(GH_Document doc)
        {
            try
            {
                //var otherComp = this.OnPingDocument().Objects[deletionIndex];
                var otherComp = this.OnPingDocument().FindObject(compGuid, false);
                if (otherComp != null) this.OnPingDocument().RemoveObject(otherComp, true);
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