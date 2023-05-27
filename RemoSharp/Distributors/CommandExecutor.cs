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
              "RemoSharp", "RemoSetup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Cmd", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.item,"");
            pManager.AddTextParameter("Username", "User", "This PC's Username", GH_ParamAccess.item, "");
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

                    // important to find the source component to prevent recursive component creation commands
                    GH_Panel usernamePanel = (GH_Panel) this.Params.Input[1].Sources[0];
                    var panelRecipients = usernamePanel.Recipients;
                    foreach (IGH_DocumentObject item in panelRecipients)
                    {
                        if (item.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoCompSource"))
                        {
                            RemoCompSource sourceComponent = (RemoCompSource)item.Attributes.Parent.DocObject;
                            sourceComponent.remoCreatedcomponens.AddRange(createCommand.guids);
                            while (sourceComponent.remoCreatedcomponens.Count > 65)
                            {
                                sourceComponent.remoCreatedcomponens.RemoveAt(0);
                            }
                            break;
                        }
                    }
                    
                    //var gh_components = this.OnPingDocument().Objects.Select(tempComponent => tempComponent.InstanceGuid).ToList();

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                    
                    for (int i = 0; i < createCommand.guids.Count; i++)
                    {

                        Guid newCompGuid = createCommand.guids[i];
                        string typeName = createCommand.componentTypes[i];
                        int pivotX = createCommand.Xs[i];
                        int pivotY = createCommand.Ys[i];
                        string specialContent = createCommand.specialParameters_s[i];
                        
                        //temporary cleared to test new method
                        //if (gh_components.Contains(newCompGuid)) continue;
                        try
                        {
                            if (typeName.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                            {

                                string[] specialParams = specialContent.Split(',');

                                decimal minBound = Convert.ToDecimal(specialParams[0]);
                                decimal maxBound = Convert.ToDecimal(specialParams[1]);
                                decimal currentValue = Convert.ToDecimal(specialParams[2]);
                                int accuracy = Convert.ToInt32(specialParams[3]);
                                GH_SliderAccuracy acc = (GH_SliderAccuracy)Enum.Parse(typeof(GH_SliderAccuracy), specialParams[4]);
                                GH_NumberSlider sliderComponent = new GH_NumberSlider();

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

                                return;
                            }
                            if (typeName.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                            {

                                string[] specialParams = specialContent.Split(',');


                                bool multiLine = Convert.ToBoolean(specialParams[0]);
                                bool drawIndicies = Convert.ToBoolean(specialParams[1]);
                                bool drawPaths = Convert.ToBoolean(specialParams[2]);
                                bool wrap = Convert.ToBoolean(specialParams[3]);
                                GH_Panel.Alignment alignment = (GH_Panel.Alignment)Enum.Parse(typeof(GH_Panel.Alignment), specialParams[4]);
                                int boundSizeX = Convert.ToInt32(specialParams[5]);
                                int boundSizeY = Convert.ToInt32(specialParams[6]);

                                string contentText = "";
                                for (int j = 7; j < specialParams.Length; j++)
                                {
                                    if (j < specialParams.Length - 1)
                                    {
                                        contentText += specialParams[j] + ",";
                                    }
                                    else
                                    {
                                        contentText += specialParams[j];
                                    }
                                }

                                GH_Panel panelComponent = new GH_Panel();

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

                                return;
                            }
                            else
                            {
                                RecognizeAndMake(typeName, pivotX, pivotY, newCompGuid);
                                string makingDone = "";
                                if (makingDone == null)
                                {

                                }
                            }
                        }
                        catch (Exception e)
                        {
                            this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                        }
                    }
                    for (int i = 0; i < createCommand.guids.Count; i++)
                    {
                        Guid newCompGuid = createCommand.guids[i];
                        WireHistory wireHistory = createCommand.wireHistorys[i];

                        var obj = this.OnPingDocument().FindObject(newCompGuid, false);
                        if (obj is IGH_Param)
                        {
                            IGH_Param igh_param = (IGH_Param)obj;
                            if (!igh_param.Attributes.HasInputGrip) continue;
                            WireConnection item = wireHistory.wireHistory[0];
                            
                                for (int j = 0; j < item.sourceGuids.Count; j++)
                                {
                                    var sourceObj = this.OnPingDocument().FindObject(item.sourceGuids[j], false);
                                    if (item.sourceIndecies[j] == -1)
                                    {

                                        igh_param.AddSource((IGH_Param)sourceObj);
                                    }
                                    else
                                    {

                                        IGH_Component igh_component = (IGH_Component)sourceObj;
                                        int outputIndex = item.sourceIndecies[j];
                                        igh_param.AddSource(igh_component.Params.Output[outputIndex]);

                                    }
                                }
                        }
                        else
                        {
                            IGH_Component igh_Component = (IGH_Component)obj;
                            for (int k = 0; k < igh_Component.Params.Input.Count; k++)
                            {
                                WireConnection item = wireHistory.wireHistory[k];
                               
                                    for (int j = 0; j < item.sourceGuids.Count; j++)
                                    {
                                        var sourceObj = this.OnPingDocument().FindObject(item.sourceGuids[j], false);
                                        if (item.sourceIndecies[j] == -1)
                                        {

                                            igh_Component.Params.Input[k].AddSource((IGH_Param)sourceObj);
                                        }
                                        else
                                        {

                                            IGH_Component igh_component = (IGH_Component)sourceObj;
                                            int outputIndex = item.sourceIndecies[j];
                                            igh_Component.Params.Input[k].AddSource(igh_component.Params.Output[outputIndex]);

                                        }
                                    }
                            }
                        }

                    }
                    });
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

                    for (int i = 0; i < hideCommand.guids.Count; i++)
                    {
                        Guid compGuid = hideCommand.guids[i];
                        bool hiddenState = hideCommand.states[i];

                        if (compGuid == Guid.Empty) continue;

                        IGH_DocumentObject selection = (IGH_DocumentObject)this.OnPingDocument().FindObject(compGuid, false);

                        if (selection is GH_Component)
                        {
                            GH_Component hideComponent = (GH_Component)selection;
                            hideComponent.Hidden = hiddenState;
                        }
                        else if (selection.SubCategory == "Geometry")
                        {
                            switch (selection.GetType().ToString())
                            {
                                case ("Grasshopper.Kernel.Parameters.Param_Point"):
                                    Grasshopper.Kernel.Parameters.Param_Point paramComponentParam_Point = (Grasshopper.Kernel.Parameters.Param_Point)selection;
                                    paramComponentParam_Point.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Circle"):
                                    Grasshopper.Kernel.Parameters.Param_Circle paramComponentParam_Circle = (Grasshopper.Kernel.Parameters.Param_Circle)selection;
                                    paramComponentParam_Circle.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Arc"):
                                    Grasshopper.Kernel.Parameters.Param_Arc paramComponentParam_Arc = (Grasshopper.Kernel.Parameters.Param_Arc)selection;
                                    paramComponentParam_Arc.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Curve"):
                                    Grasshopper.Kernel.Parameters.Param_Curve paramComponentParam_Curve = (Grasshopper.Kernel.Parameters.Param_Curve)selection;
                                    paramComponentParam_Curve.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Line"):
                                    Grasshopper.Kernel.Parameters.Param_Line paramComponentParam_Line = (Grasshopper.Kernel.Parameters.Param_Line)selection;
                                    paramComponentParam_Line.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                                    Grasshopper.Kernel.Parameters.Param_Plane paramComponentParam_Plane = (Grasshopper.Kernel.Parameters.Param_Plane)selection;
                                    paramComponentParam_Plane.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Rectangle"):
                                    Grasshopper.Kernel.Parameters.Param_Rectangle paramComponentParam_Rectangle = (Grasshopper.Kernel.Parameters.Param_Rectangle)selection;
                                    paramComponentParam_Rectangle.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Box"):
                                    Grasshopper.Kernel.Parameters.Param_Box paramComponentParam_Box = (Grasshopper.Kernel.Parameters.Param_Box)selection;
                                    paramComponentParam_Box.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Surface"):
                                    Grasshopper.Kernel.Parameters.Param_Surface paramComponentParam_Surface = (Grasshopper.Kernel.Parameters.Param_Surface)selection;
                                    paramComponentParam_Surface.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Brep"):
                                    Grasshopper.Kernel.Parameters.Param_Brep paramComponentParam_Brep = (Grasshopper.Kernel.Parameters.Param_Brep)selection;
                                    paramComponentParam_Brep.Hidden = hiddenState;
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Mesh"):
                                    Grasshopper.Kernel.Parameters.Param_Mesh paramComponentParam_Mesh = (Grasshopper.Kernel.Parameters.Param_Mesh)selection;
                                    paramComponentParam_Mesh.Hidden = hiddenState;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    return;
                #endregion

                #region lock
                case (CommandType.Lock):

                    RemoLock lockCommand = (RemoLock)remoCommand;

                    this.OnPingDocument().ScheduleSolution(1, doc =>
                    {
                        for (int i = 0; i < lockCommand.guids.Count; i++)
                        {
                            Guid guid = lockCommand.guids[i];
                            var lockComponent = this.OnPingDocument().FindObject(guid, false);
                            if (lockComponent is GH_Component)
                            {
                                GH_Component lockComp = (GH_Component)lockComponent;
                                lockComp.Locked = lockCommand.states[i];
                                lockComp.ExpireSolution(false);
                            }
                            else if (lockComponent is IGH_Param)
                            {
                                IGH_Param lockParam = (IGH_Param)lockComponent;
                                lockParam.Locked = lockCommand.states[i];
                                lockParam.ExpireSolution(false);
                            }
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
                        colorComp.Attributes.Selected = false;
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