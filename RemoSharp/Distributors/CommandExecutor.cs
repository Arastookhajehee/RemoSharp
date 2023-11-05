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
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Commands;

using System.Threading.Tasks;
using System.IO;
using System.Text;
using RemoSharp.WebSocketClient;
using RemoSharp.RemoParams;

namespace RemoSharp
{
    public class CommandExecutor : GHCustomComponent
    {
        List<string> paramTypes = new List<string>()
        {
            "Grasshopper.Kernel.Special.GH_NumberSlider",
            "Grasshopper.Kernel.Special.GH_Panel",
            "Grasshopper.Kernel.Special.GH_ColourSwatch",
            "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
            "Grasshopper.Kernel.Special.GH_BooleanToggle",
            "Grasshopper.Kernel.Special.GH_ButtonObject",
            "Grasshopper.Kernel.Parameters.Param_Point",
            "Grasshopper.Kernel.Parameters.Param_Vector",
            "Grasshopper.Kernel.Parameters.Param_Plane"
        };
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

        List<string> errors = new List<string>();
        List<RemoCommand> retryCommands = new List<RemoCommand>();
        List<Guid> MoveCommands = new List<Guid>();
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
            pManager.AddTextParameter("Commands", "Cmds", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.list, "");
            pManager.AddTextParameter("Username", "User", "This PC's Username", GH_ParamAccess.item, "");
            pManager.AddBooleanParameter("syncSend", "syncSend", "Syncs this grasshopper script for all other connected clients", GH_ParamAccess.item,false);
        }
        
        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("errors", "errors", "list of the commands or messages that the component failed to excecute!", GH_ParamAccess.list);
            //pManager.AddGenericParameter("wires", "wires", "list of the wiring commands coming from the server.", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool cleanErrors = false;
            string username = "";
            if (!DA.GetData(1, ref username)) return;
            if (!DA.GetData(2, ref cleanErrors)) return;

            int maxErrorCount = 30;
            int errorCount = this.errors.Count;
            ShowBackgroundDesyncColor(errorCount, maxErrorCount);
            if (errorCount > 0 && errorCount <= 3)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Minimal Desynchronization!");
            }
            else if (errorCount > 3 && errorCount <= 10)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Considarable Desynchronization!");
            }
            else if (errorCount > 10 && errorCount <= 29)
            {

                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Major Desynchronization!\nPlease consider re-syncing!");
            }
            else if (errorCount > maxErrorCount)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Documents Are Desynchronized!\nPlease consider re-syncing!");
                //if (errorCount % 5 == 0)
                //{
                //    Thread errorThread = new Thread(RaiseSyncError);
                //    errorThread.Start();
                //}
            }

            if (cleanErrors)
            {
                errors.Clear();

                bool run = cleanErrors;
                ResetGHColorsToDefault();

                return;
            }

            IGH_Component wscListenerComp = (IGH_Component)this.Params.Input[0].Sources[0].Attributes.Parent.DocObject;
            RemoSharp.RemoSetupClient wsClientComp = (RemoSharp.RemoSetupClient)wscListenerComp.Params.Input[0].Sources[0].Attributes.Parent.DocObject;



            if (this.Params.Input[0].Sources.Count > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This Component Accepts Only a Single Input." + Environment.NewLine
                    + "Make Sure Only One Wire With A Single Text Block Command is Connected.");
                return;
            }


            List<RemoConnect> connectionList = new List<RemoConnect>();

            while (wsClientComp.messages.Count > 0)
            {
                string command = wsClientComp.messages[0];

                if (string.IsNullOrEmpty(command)
                    || command == "null"
                    || command == "Hello World" 
                    || command == "RemoSharp.RemoCommandTypes.RemoNullCommand")
                {
                    wsClientComp.messages.RemoveAt(0);
                    continue;
                }


                if (currentStringCommand.Equals(command))
                {
                    wsClientComp.messages.RemoveAt(0);
                    continue;
                }
                else currentStringCommand = command;

                RemoCommand remoCommand = null;
                try
                {
                    remoCommand = RemoCommand.DeserializeFromJson(command);
                    if (username.Equals(remoCommand.issuerID) || username.IsNullOrEmpty()) 
                    {
                        wsClientComp.messages.RemoveAt(0);
                        continue;
                    } 

                    switch (remoCommand.commandType)
                    {
                        #region moveComp
                        case (CommandType.MoveComponent):
                            RemoMove moveCommand = (RemoMove)remoCommand;
                            ExcecuteMove(moveCommand);
                            break;
                        #endregion
                        case (CommandType.NullCommand):
                            break;
                        #region wireconnection
                        case (CommandType.WireConnection):
                            RemoConnect wireCommand = (RemoConnect)remoCommand;
                            bool wireSuccess = ConnectWires(wireCommand);

                            if (!wireSuccess) 
                            {
                                remoCommand.executionAttempts++;
                                retryCommands.Add(remoCommand);
                                wsClientComp.messages.RemoveAt(0);
                                continue;
                            }

                            break;
                        #endregion
                        #region componentCreation
                        case (CommandType.Create):
                            RemoCreate createCommand = (RemoCreate)remoCommand;
                            ExecuteCreate(createCommand);
                            break;
                        #endregion

                        #region deleteComponent
                        case (CommandType.Delete):
                            RemoDelete remoDelete = (RemoDelete)remoCommand;
                            ExecuteDelete(remoDelete);
                            break;
                        #endregion

                        #region GeometryStream
                        case (CommandType.StreamGeom):
                            DA.SetData(0, remoCommand.ToString());

                            break;
                        #endregion

                        #region hide
                        case (CommandType.Hide):

                            RemoHide hideCommand = (RemoHide)remoCommand;
                            ExecuteHide(hideCommand);
                            break;
                        #endregion

                        #region lock
                        case (CommandType.Lock):

                            RemoLock lockCommand = (RemoLock)remoCommand;
                            ExecuteLock(lockCommand);
                            break;
                        #endregion

                        #region RemoParamSlider
                        case (CommandType.RemoSlider):
                            RemoParamSlider remoSlider = (RemoParamSlider)remoCommand;
                            ExecuteRemoSlider(remoSlider);
                            
                            break;
                        #endregion

                        #region RemoParamButton
                        case (CommandType.RemoButton):
                            RemoParamButton remoButton = (RemoParamButton)remoCommand;
                            ExecuteRemoButton(remoButton);
                            break;
                        #endregion

                        #region RemoParamToggle
                        case (CommandType.RemoToggle):
                            RemoParamToggle remoToggle = (RemoParamToggle)remoCommand;
                            ExecuteRemoToggle(remoToggle);
                            break;
                        #endregion

                        #region RemoParamPanel
                        case (CommandType.RemoPanel):
                            RemoParamPanel remoPanel = (RemoParamPanel)remoCommand;
                            ExecuteRemoPanel(remoPanel);
                            
                            break;
                        #endregion

                        #region RemoParamColor
                        case (CommandType.RemoColor):
                            RemoParamColor remoColor = (RemoParamColor)remoCommand;
                            ExecuteRemoColor(remoColor);
                            
                            break;
                        #endregion

                        #region RemoParamMDSlider
                        case (CommandType.RemoMDSlider):
                            RemoParamMDSlider remoMDSlider = (RemoParamMDSlider)remoCommand;
                            ExecuteRemoMDSlider(remoMDSlider);

                            break;
                        #endregion

                        #region RemoParamPoint3d
                        case (CommandType.RemoPoint3d):
                            RemoParamPoint3d remoPoint3d = (RemoParamPoint3d)remoCommand;
                            
                            ExecuteRemoPoint3d(remoPoint3d);

                            //if (remoPoint3d.objectGuid == Guid.Empty) break;
                            //IGH_Param pointComp = (IGH_Param)this.OnPingDocument().FindObject(remoPoint3d.objectGuid, false);
                            //Param_Point pointParamComp = (Param_Point)pointComp;

                            //GH_Structure<GH_Point> pointTree = new GH_Structure<GH_Point>();
                            //foreach (string item in remoPoint3d.pointsAndTreePath)
                            //{
                            //    string[] coordsAndPath = item.Split(':');
                            //    string[] coordsStrings = coordsAndPath[0].Split(',');
                            //    string[] pathStrings = coordsAndPath[1].Split(',');
                            //    double[] coords = coordsStrings.Select(double.Parse).ToArray();
                            //    int[] path = pathStrings.Select(int.Parse).ToArray();


                            //    pointTree.Append(new GH_Point(new Point3d(coords[0], coords[1], coords[2])), new GH_Path(path));
                            //}

                            //this.OnPingDocument().ScheduleSolution(1, doc =>
                            //{
                            //    pointComp.Attributes.Selected = false;
                            //    pointParamComp.SetPersistentData(pointTree);
                            //    pointParamComp.ExpireSolution(false);
                            //});

                            break;
                        #endregion

                        #region RemoParamVector3d
                        case (CommandType.RemoVector3d):
                            RemoParamVector3d remoVector3d = (RemoParamVector3d)remoCommand;
                            ExecuteRemoVector3d(remoVector3d);
                            
                            break;
                        #endregion

                        #region RemoParamPlane
                        case (CommandType.RemoPlane):
                            RemoParamPlane remoPlane = (RemoParamPlane)remoCommand;
                            ExecuteRemoPlane(remoPlane);
                            break;
                        #endregion

                        #region select
                        case (CommandType.Select):

                            RemoSelect selectionCommand = (RemoSelect)remoCommand;
                            ExecuteSelect(selectionCommand);

                            break;
                        #endregion

                        case (CommandType.RemoCanvasSync):
                            RemoCanvasSync remoCanvasSync = (RemoCanvasSync)remoCommand;

                            SyncCanvasFromRemoCanvasSync(remoCanvasSync);

                            break;
                        default:
                            
                            break;
                    }

                    wsClientComp.messages.RemoveAt(0);
                    remoCommand.executed = true;
                }
                catch
                {
                    remoCommand.executionAttempts++;
                    retryCommands.Add(remoCommand);
                    wsClientComp.messages.RemoveAt(0);
                }

            }


            foreach (RemoCommand remoCommand in retryCommands)
            {
                try
                {
                    if (remoCommand.executionAttempts > 10 && !remoCommand.executed)
                    {
                        errors.Add("Fail: " + remoCommand.ToString());
                        remoCommand.executed = true;
                    }
                    else
                    {
                        switch (remoCommand.commandType)
                        {
                            #region moveComp
                            case (CommandType.MoveComponent):
                                RemoMove moveCommand = (RemoMove)remoCommand;
                                ExcecuteMove(moveCommand);
                                break;
                            #endregion
                            case (CommandType.NullCommand):
                                break;
                            #region wireconnection
                            case (CommandType.WireConnection):
                                RemoConnect wireCommand = (RemoConnect)remoCommand;
                                ConnectWires(wireCommand);
                                break;
                            #endregion
                            #region componentCreation
                            case (CommandType.Create):
                                RemoCreate createCommand = (RemoCreate)remoCommand;
                                ExecuteCreate(createCommand);
                                break;
                            #endregion

                            #region deleteComponent
                            case (CommandType.Delete):
                                RemoDelete remoDelete = (RemoDelete)remoCommand;
                                ExecuteDelete(remoDelete);
                                break;
                            #endregion

                            #region GeometryStream
                            case (CommandType.StreamGeom):
                                DA.SetData(0, remoCommand.ToString());

                                break;
                            #endregion

                            #region hide
                            case (CommandType.Hide):

                                RemoHide hideCommand = (RemoHide)remoCommand;
                                ExecuteHide(hideCommand);
                                break;
                            #endregion

                            #region lock
                            case (CommandType.Lock):

                                RemoLock lockCommand = (RemoLock)remoCommand;
                                ExecuteLock(lockCommand);
                                break;
                            #endregion

                            #region RemoParamSlider
                            case (CommandType.RemoSlider):
                                RemoParamSlider remoSlider = (RemoParamSlider)remoCommand;
                                ExecuteRemoSlider(remoSlider);

                                break;
                            #endregion

                            #region RemoParamButton
                            case (CommandType.RemoButton):
                                RemoParamButton remoButton = (RemoParamButton)remoCommand;
                                ExecuteRemoButton(remoButton);
                                break;
                            #endregion

                            #region RemoParamToggle
                            case (CommandType.RemoToggle):
                                RemoParamToggle remoToggle = (RemoParamToggle)remoCommand;
                                ExecuteRemoToggle(remoToggle);
                                break;
                            #endregion

                            #region RemoParamPanel
                            case (CommandType.RemoPanel):
                                RemoParamPanel remoPanel = (RemoParamPanel)remoCommand;
                                ExecuteRemoPanel(remoPanel);

                                break;
                            #endregion

                            #region RemoParamColor
                            case (CommandType.RemoColor):
                                RemoParamColor remoColor = (RemoParamColor)remoCommand;
                                ExecuteRemoColor(remoColor);

                                break;
                            #endregion

                            #region RemoParamMDSlider
                            case (CommandType.RemoMDSlider):
                                RemoParamMDSlider remoMDSlider = (RemoParamMDSlider)remoCommand;
                                ExecuteRemoMDSlider(remoMDSlider);

                                break;
                            #endregion

                            #region RemoParamPoint3d
                            case (CommandType.RemoPoint3d):
                                RemoParamPoint3d remoPoint3d = (RemoParamPoint3d)remoCommand;
                                ExecuteRemoPoint3d(remoPoint3d);

                                break;
                            #endregion

                            #region RemoParamVector3d
                            case (CommandType.RemoVector3d):
                                RemoParamVector3d remoVector3d = (RemoParamVector3d)remoCommand;
                                ExecuteRemoVector3d(remoVector3d);

                                break;
                            #endregion


                            #region RemoParamPlane
                            case (CommandType.RemoPlane):
                                RemoParamPlane remoPlane = (RemoParamPlane)remoCommand;
                                ExecuteRemoPlane(remoPlane);


                                break;
                            #endregion

                            #region select
                            case (CommandType.Select):

                                RemoSelect selectionCommand = (RemoSelect)remoCommand;
                                ExecuteSelect(selectionCommand);

                                break;
                            #endregion
                            default:

                                break;
                        }
                    }

                    wsClientComp.messages.RemoveAt(0);
                    remoCommand.executed = true;
                }
                catch
                {
                    remoCommand.executionAttempts++;
                }

            }


            for (int i = retryCommands.Count -1; i  > -1; i--)
            {
                RemoCommand remoCommand = retryCommands[i];
                if (remoCommand.executed)
                {
                    retryCommands.RemoveAt(i);
                }
            }

            DA.SetDataList(0, errors);
            //DA.SetDataList(1, connectionList);

        }

        private void ShowBackgroundDesyncColor(int errorCount, int maxErrorCount)
        {
            if (errorCount > maxErrorCount) errorCount = maxErrorCount;

            try
            {
                Color defaultColor = Color.FromArgb(255, 212, 208, 200);
                Color errorColor = System.Drawing.Color.OrangeRed;

                Interval colorRInterval = new Interval(defaultColor.R, errorColor.R); // default r is less than error r
                Interval colorGInterval = new Interval(errorColor.G, defaultColor.G);
                Interval colorBInterval = new Interval(errorColor.B, defaultColor.B);

                double ratio = errorCount / (double)maxErrorCount;
                int backAlpha = 255;
                int backRed = Convert.ToInt32(colorRInterval.ParameterAt(ratio));
                int backGreen = Convert.ToInt32(colorGInterval.ParameterAt(1 - ratio));
                int backBlue = Convert.ToInt32(colorBInterval.ParameterAt(1 - ratio));

                Color backColor = Color.FromArgb(backAlpha, backRed, backGreen, backBlue);
                Grasshopper.GUI.Canvas.GH_Skin.canvas_back = backColor;
            }
            catch
            {

                
            }

            

        }

        private static void ResetGHColorsToDefault()
        {
            //DEFAULTS
            Grasshopper.GUI.Canvas.GH_Skin.canvas_grid = Color.FromArgb(30, 0, 0, 0);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_back = Color.FromArgb(255, 212, 208, 200);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_edge = Color.FromArgb(255, 0, 0, 0);
            Grasshopper.GUI.Canvas.GH_Skin.canvas_shade = Color.FromArgb(80, 0, 0, 0);
        }

        private void SyncCanvasFromRemoCanvasSync(RemoCanvasSync remoCanvasSync)
        {

            errors.Clear();
            ResetGHColorsToDefault();

            //https://stackoverflow.com/questions/674479/how-do-i-get-the-directory-from-a-files-full-path
            string path = @"C:\temp\RemoSharp\ReceiveStream.ghx";
            CheckForDirectoryAndFileExistance(path);

            string stream = remoCanvasSync.xmlString;

            if (string.IsNullOrEmpty(stream) ||
                stream == " " ||
                stream == "Hello World" ||
                !stream.Substring(0, 15).Contains("?xml version")) return;

            using (StreamWriter sw = new StreamWriter(path))
            {
                sw.Write(stream);
            }


            try
            {

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    RemoSetupClient remoSetupComp = GetSourceCompFromInput();

                    if (remoSetupComp == null)
                    {
                        System.Windows.Forms.MessageBox.Show("RemoSharp Sync Failed!", "RemoSharp Sync Failed. Please Try Seting up RemoSharp Again!");
                        return;
                    }

                    var currentCanvas = Grasshopper.Instances.ActiveCanvas;
                    currentCanvas.Document.ObjectsAdded -= remoSetupComp.RemoCompSource_ObjectsAdded;
                    currentCanvas.Document.ObjectsDeleted -= remoSetupComp.RemoCompSource_ObjectsDeleted;
                    currentCanvas.MouseUp -= remoSetupComp.Canvas_MouseUp;
                    currentCanvas.MouseDown -= remoSetupComp.Canvas_MouseDown;


                    List<Guid> localCompIds = new List<Guid>();

                    for (int i = this.OnPingDocument().ObjectCount - 1; i > -1; i--)
                    {
                        var obj = this.OnPingDocument().Objects[i];
                        if (obj.NickName.ToUpper().Contains("LOCAL")) 
                        {
                            localCompIds.Add(obj.InstanceGuid);
                        }
                        else if (
                        obj.NickName.ToUpper().Contains("RemoSetup".ToUpper()) ||
                        obj.GetType().ToString().Equals("RemoSharp.RemoCompSource")
                        )
                        {
                            continue;
                        }

                        else
                        {
                            this.OnPingDocument().RemoveObject(obj, false);
                        }
                    }

                    Grasshopper.Kernel.GH_DocumentIO ioDoc = new GH_DocumentIO();
                    ioDoc.Open(path);
                    var incomingDoc = ioDoc.Document;

                    foreach (var item in incomingDoc.Objects)
                    {
                        bool localDocContainsLocalItem = localCompIds.Contains(item.InstanceGuid);
                        bool typeIsParam = paramTypes.Contains(item.GetType().ToString());
                        if (localDocContainsLocalItem && typeIsParam)
                        {
                            var thisDocParam = this.OnPingDocument().FindObject(item.InstanceGuid,false);
                            var incomingDocParam = incomingDoc.FindObject(item.InstanceGuid,false);
                            switch (item.GetType().ToString())
                            {
                                case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                                    Grasshopper.Kernel.Special.GH_NumberSlider thisDocSlider = (Grasshopper.Kernel.Special.GH_NumberSlider)thisDocParam;
                                    Grasshopper.Kernel.Special.GH_NumberSlider incomingDocSlider = (Grasshopper.Kernel.Special.GH_NumberSlider)incomingDocParam;

                                    incomingDocSlider.Slider.Bounds = thisDocSlider.Slider.Bounds;
                                    incomingDocSlider.Slider.Type = thisDocSlider.Slider.Type;
                                    incomingDocSlider.Slider.DecimalPlaces = thisDocSlider.Slider.DecimalPlaces;
                                    incomingDocSlider.SetSliderValue(thisDocSlider.CurrentValue);
                                    incomingDocSlider.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocSlider, false);

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_Panel"):
                                    Grasshopper.Kernel.Special.GH_Panel thisDocPanel = (Grasshopper.Kernel.Special.GH_Panel) thisDocParam;
                                    Grasshopper.Kernel.Special.GH_Panel incomingDocGH_Panel = (Grasshopper.Kernel.Special.GH_Panel) incomingDocParam;

                                    incomingDocGH_Panel.Properties.Alignment = thisDocPanel.Properties.Alignment;
                                    incomingDocGH_Panel.Properties.Wrap = thisDocPanel.Properties.Wrap;
                                    incomingDocGH_Panel.Properties.Colour = thisDocPanel.Properties.Colour;
                                    incomingDocGH_Panel.Properties.Multiline = thisDocPanel.Properties.Multiline;
                                    incomingDocGH_Panel.Properties.DrawIndices = thisDocPanel.Properties.DrawIndices;
                                    incomingDocGH_Panel.SetUserText(thisDocPanel.UserText);
                                    incomingDocGH_Panel.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocPanel, false);

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                                    Grasshopper.Kernel.Special.GH_ColourSwatch thisDocColor = (Grasshopper.Kernel.Special.GH_ColourSwatch) thisDocParam;
                                    Grasshopper.Kernel.Special.GH_ColourSwatch incomingDocClolor = (Grasshopper.Kernel.Special.GH_ColourSwatch) incomingDocParam;

                                    incomingDocClolor.SwatchColour = thisDocColor.SwatchColour;
                                    incomingDocClolor.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocColor, false);

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                                    Grasshopper.Kernel.Special.GH_MultiDimensionalSlider thisDocMDSlider = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider) thisDocParam;
                                    Grasshopper.Kernel.Special.GH_MultiDimensionalSlider incomingMDSlider = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider) incomingDocParam;

                                    incomingMDSlider.XInterval = thisDocMDSlider.XInterval;
                                    incomingMDSlider.YInterval = thisDocMDSlider.YInterval;
                                    incomingMDSlider.Value = thisDocMDSlider.Value;
                                    incomingMDSlider.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocMDSlider, false);

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                                    Grasshopper.Kernel.Special.GH_BooleanToggle thisDocToggle = (Grasshopper.Kernel.Special.GH_BooleanToggle) thisDocParam;
                                    Grasshopper.Kernel.Special.GH_BooleanToggle incomingDocToggle = (Grasshopper.Kernel.Special.GH_BooleanToggle) incomingDocParam;

                                    incomingDocToggle.Value = thisDocToggle.Value;
                                    incomingDocToggle.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocToggle, false);

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                                    Grasshopper.Kernel.Special.GH_ButtonObject thisDocButton = (Grasshopper.Kernel.Special.GH_ButtonObject) thisDocParam;
                                    //Grasshopper.Kernel.Special.GH_ButtonObject incomingDocGH_ButtonObject = (Grasshopper.Kernel.Special.GH_ButtonObject) incomingDocParam;

                                    this.OnPingDocument().RemoveObject(thisDocButton, false);

                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Point"):
                                    Grasshopper.Kernel.Parameters.Param_Point thisDocPoint = (Grasshopper.Kernel.Parameters.Param_Point)thisDocParam;
                                    Grasshopper.Kernel.Parameters.Param_Point incomingDocPoint = (Grasshopper.Kernel.Parameters.Param_Point)incomingDocParam;

                                    incomingDocPoint.SetPersistentData(thisDocPoint.PersistentData);
                                    incomingDocPoint.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocPoint, false);

                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Vector"):
                                    Grasshopper.Kernel.Parameters.Param_Vector thisDocVector = (Grasshopper.Kernel.Parameters.Param_Vector)thisDocParam;
                                    Grasshopper.Kernel.Parameters.Param_Vector incomingDocVector = (Grasshopper.Kernel.Parameters.Param_Vector)incomingDocParam;

                                    incomingDocVector.SetPersistentData(thisDocVector.PersistentData);
                                    incomingDocVector.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocVector, false);
                                    break;
                                case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                                    Grasshopper.Kernel.Parameters.Param_Plane thisDocPlane = (Grasshopper.Kernel.Parameters.Param_Plane)thisDocParam;
                                    Grasshopper.Kernel.Parameters.Param_Plane incomingDocPlane = (Grasshopper.Kernel.Parameters.Param_Plane)incomingDocParam;

                                    incomingDocPlane.SetPersistentData(thisDocPlane.PersistentData);
                                    incomingDocPlane.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocPlane, false);

                                    break;
                                default:
                                    break;
                            }
                        }
                        //else
                        //{
                        //    if (item.NickName.ToUpper().Contains("local".ToUpper()))
                        //    {
                        //        incomingDoc.RemoveObject(item,false);
                        //    }
                        //}


                    }





                    this.OnPingDocument().MergeDocument(incomingDoc);

                    currentCanvas.Document.ObjectsAdded += remoSetupComp.RemoCompSource_ObjectsAdded;
                    currentCanvas.Document.ObjectsDeleted += remoSetupComp.RemoCompSource_ObjectsDeleted;
                    currentCanvas.MouseUp += remoSetupComp.Canvas_MouseUp;
                    currentCanvas.MouseDown += remoSetupComp.Canvas_MouseDown;
                });

                

            }
            catch
            {

            }

        }

        private RemoSetupClient GetSourceCompFromInput()
        {
            GH_Panel usernamePanel = (GH_Panel)this.Params.Input[1].Sources[0];
            var panelRecipients = usernamePanel.Recipients;
            foreach (IGH_DocumentObject item in panelRecipients)
            {
                if (item.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoSetupClient"))
                {
                    RemoSetupClient sourceComponent = (RemoSetupClient)item.Attributes.Parent.DocObject;
                    return sourceComponent;
                }
            }
            return null;
        }

        private RemoSharp.RemoParams.RemoParam GetSourceCompFromRemoParamInput(Guid paramGuid)
        {
            IGH_Param targetParam = (IGH_Param)this.OnPingDocument().FindObject(paramGuid, false);
            var paramRecipients = targetParam.Recipients;
            foreach (IGH_DocumentObject item in paramRecipients)
            {
                if (item.Attributes.Parent == null) continue;
                if (item.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam"))
                {
                    RemoSharp.RemoParams.RemoParam remoParamComp = (RemoSharp.RemoParams.RemoParam)item.Attributes.Parent.DocObject;
                    return remoParamComp;
                }
            }
            return null;
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

        //private static void RaiseSyncError()
        //{
        //    System.Windows.Forms.MessageBox.Show("Documents out of sync!\n Please consider re-syncing.", "Desyncronization Error");
        //}

        private void ExecuteSelect(RemoSelect selectionCommand)
        {
            this.OnPingDocument().DeselectAll();
            foreach (Guid guid in selectionCommand.selectionGuids)
            {
                var selectionComp = this.OnPingDocument().FindObject(guid, false);
                selectionComp.Attributes.Selected = true;
            }
        }

        private void ExecuteRemoPlane(RemoParamPlane remoPlane)
        {
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
                planeParamComp.RemoveAllSources();

                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(planeComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                planeParamComp.SetPersistentData(planeTree);
                planeParamComp.ExpireSolution(false);

                remoParamComp.enableRemoParam = true;

            });
        }

        private void ExecuteRemoVector3d(RemoParamVector3d remoVector3d)
        {
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
                vectorParamComp.RemoveAllSources();

                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(vectorComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                vectorParamComp.SetPersistentData(vectorTree);
                vectorParamComp.ExpireSolution(false);

                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoPoint3d(RemoParamPoint3d remoPoint3d)
        {
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


                pointTree.Append(new GH_Point(new Point3d(coords[0], coords[1], coords[2])), new GH_Path(path));
            }

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                pointParamComp.RemoveAllSources();

                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(pointComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                //pointComp.Attributes.Selected = false;
                pointParamComp.SetPersistentData(pointTree);
                pointParamComp.ExpireSolution(false);

                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoMDSlider(RemoParamMDSlider remoMDSlider)
        {
            if (remoMDSlider.objectGuid == Guid.Empty) return;
            GH_MultiDimensionalSlider mdSliderComp = (GH_MultiDimensionalSlider)this.OnPingDocument().FindObject(remoMDSlider.objectGuid, false);
            
            mdSliderComp.XInterval = new Interval(remoMDSlider.minBoundX, remoMDSlider.maxBoundX);
            mdSliderComp.YInterval = new Interval(remoMDSlider.minBoundY, remoMDSlider.maxBoundY);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(mdSliderComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                mdSliderComp.Value = new Point3d(remoMDSlider.ValueX, remoMDSlider.ValueY, 0);
                mdSliderComp.ExpireSolution(false);

                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoColor(RemoParamColor remoColor)
        {
            if (remoColor.objectGuid == Guid.Empty) return;
            GH_ColourSwatch colorComp = (GH_ColourSwatch)this.OnPingDocument().FindObject(remoColor.objectGuid, false);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(colorComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                //colorComp.Attributes.Selected = false;
                colorComp.SwatchColour = Color.FromArgb(remoColor.Alpha, remoColor.Red, remoColor.Green, remoColor.Blue);
                colorComp.ExpireSolution(false);

                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoPanel(RemoParamPanel remoPanel)
        {
            if (remoPanel.objectGuid == Guid.Empty) return;
            GH_Panel panelComp = (GH_Panel)this.OnPingDocument().FindObject(remoPanel.objectGuid, false);
            

            panelComp.Properties.Multiline = remoPanel.MultiLine;
            panelComp.Properties.DrawIndices = remoPanel.DrawIndecies;
            panelComp.Properties.DrawPaths = remoPanel.DrawPaths;
            panelComp.Properties.Wrap = remoPanel.Wrap;
            panelComp.Properties.Alignment = (GH_Panel.Alignment)remoPanel.Alignment;

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                panelComp.RemoveAllSources();

                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(panelComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                panelComp.SetUserText(remoPanel.panelContent);
                panelComp.ExpireSolution(false);
                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoToggle(RemoParamToggle remoToggle)
        {
            if (remoToggle.objectGuid == Guid.Empty) return;
            GH_BooleanToggle toggleComp = (GH_BooleanToggle)this.OnPingDocument().FindObject(remoToggle.objectGuid, false);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(toggleComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;
                toggleComp.Value = remoToggle.toggleValue;
                toggleComp.ExpireSolution(false);
                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoButton(RemoParamButton remoButton)
        {
            if (remoButton.objectGuid == Guid.Empty) return;
            GH_ButtonObject buttonComp = (GH_ButtonObject)this.OnPingDocument().FindObject(remoButton.objectGuid, false);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(buttonComp.InstanceGuid);
                remoParamComp.enableRemoParam = false;

                buttonComp.ButtonDown = remoButton.buttonValue;
                buttonComp.ExpireSolution(true);

                remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteRemoSlider(RemoParamSlider remoSlider)
        {
            if (remoSlider.objectGuid == Guid.Empty) return;
            GH_NumberSlider sliderComp = (GH_NumberSlider)this.OnPingDocument().FindObject(remoSlider.objectGuid, false);

            sliderComp.Slider.Minimum = remoSlider.sliderminBound;
            sliderComp.Slider.Maximum = remoSlider.slidermaxBound;
            sliderComp.Slider.DecimalPlaces = remoSlider.decimalPlaces;
            sliderComp.Slider.Type = (GH_SliderAccuracy)remoSlider.sliderType;


            var rpmComps = sliderComp.Recipients.Where(obj => obj.Attributes.Parent != null)
                .Where(obj => obj.Attributes.Parent.DocObject != null)
                .Where(obj => obj.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam")).ToList();


            var rpmComp = (RemoSharp.RemoParams.RemoParam)rpmComps[0].Attributes.Parent.DocObject;

            string rpmNickname = rpmComp.Message;

            var dataComps = this.OnPingDocument().Objects.Where(obj => obj is GH_Component).Select(obj => (GH_Component)obj).ToList();
            //&& !obj.GetType().Equals("RemoSharp.RemoParams.RemoParam")).ToList();
            //.Select(obj => (RemoParamData) obj).ToList()[0];

            var dataComp = dataComps.Where(obj => obj.Message != null).ToList();
            
            var dataComponent = dataComp.Where(obj => obj.Message.Contains("RPM")).ToList();

            var dataC = dataComponent.Where(obj => obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParamData")).ToList();

            var remoParamDataComponent = (RemoSharp.RemoParams.RemoParamData)dataC[0];

            string nickname = "";

            //string nickname2 = datacomp.NickName;




            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                ////var gh_components = this.OnPingDocument().Objects.Select(tempComponent => tempComponent.InstanceGuid).ToList();
                //RemoSharp.RemoParams.RemoParam remoParamComp = GetSourceCompFromRemoParamInput(sliderComp.InstanceGuid);
                //remoParamComp.enableRemoParam = false;

                remoParamDataComponent.remoSliderValue = remoSlider.sliderValue;
                remoParamDataComponent.ExpireSolution(false);

                //remoParamComp.enableRemoParam = true;
            });
        }

        private void ExecuteLock(RemoLock lockCommand)
        {
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
        }

        private void ExecuteHide(RemoHide hideCommand)
        {
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
                        case ("Grasshopper.Kernel.Parameters.Param_Geometry"):
                            Grasshopper.Kernel.Parameters.Param_Geometry paramComponentParam_Geom = (Grasshopper.Kernel.Parameters.Param_Geometry)selection;
                            paramComponentParam_Geom.Hidden = hiddenState;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void ExecuteDelete(RemoDelete remoDelete)
        {
            deletionGuids.Clear();
            deletionGuids.AddRange(remoDelete.objectGuids);
            try
            {
                RemoSetupClient sourceComp = GetSourceCompFromInput();
                this.OnPingDocument().ObjectsDeleted -= sourceComp.RemoCompSource_ObjectsDeleted;
                this.OnPingDocument().ScheduleSolution(1, DeleteComponent);
                this.OnPingDocument().ObjectsDeleted += sourceComp.RemoCompSource_ObjectsDeleted;
            }
            catch
            {
            }
        }

        private void ExecuteCreate(RemoCreate createCommand)
        {
            // important to find the source component to prevent recursive component creation commands

            //GH_Panel usernamePanel = (GH_Panel)this.Params.Input[1].Sources[0];
            //var panelRecipients = usernamePanel.Recipients;
            //foreach (IGH_DocumentObject item in panelRecipients)
            //{
            //    if (item.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoCompSource"))
            //    {
            //        RemoCompSource sourceComponent = (RemoCompSource)item.Attributes.Parent.DocObject;
            //        //sourceComponent.remoCreatedcomponens.AddRange(createCommand.guids);
            //        while (sourceComponent.remoCreatedcomponens.Count > 65)
            //        {
            //            sourceComponent.remoCreatedcomponens.RemoveAt(0);
            //        }
            //        break;
            //    }
            //}

            var thisGHObjects = this.OnPingDocument().Objects.Select(obj => obj.InstanceGuid).ToList();

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                //var gh_components = this.OnPingDocument().Objects.Select(tempComponent => tempComponent.InstanceGuid).ToList();
                RemoSetupClient sourceComp = GetSourceCompFromInput();
                this.OnPingDocument().ObjectsAdded -= sourceComp.RemoCompSource_ObjectsAdded;

                for (int i = 0; i < createCommand.guids.Count; i++)
                {

                    Guid newCompGuid = createCommand.guids[i];
                    string typeName = createCommand.componentTypes[i];
                    int pivotX = createCommand.Xs[i];
                    int pivotY = createCommand.Ys[i];
                    string specialContent = createCommand.specialParameters_s[i];

                    if (thisGHObjects.Contains(newCompGuid)) continue;

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

                            this.OnPingDocument().AddObject(sliderComponent, true);
                            break;
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

                            this.OnPingDocument().AddObject(panelComponent, true);

                            break;
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
                        if (obj == null)
                        {
                            errors.Add("Null Object Found");
                            return;
                        }
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

                this.OnPingDocument().ObjectsAdded += sourceComp.RemoCompSource_ObjectsAdded;
            });

            
        }

        private void AddRemoParamDataComponent(IGH_DocumentObject obj, string rpmType)
        {
            List<RemoSharp.RemoParams.RemoParam> rpmList = this.OnPingDocument().Objects.Where(comps => comps.GetType().ToString().Equals(rpmType))
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

            this.OnPingDocument().ScheduleSolution(0, doc =>
            {
                this.OnPingDocument().AddObject(dataComp, false);
            });
        }
        private void ExcecuteMove(RemoMove moveCommand)
        {
            if (this.MoveCommands.Contains(moveCommand.translationGuid)) return;
            else
            {
                this.MoveCommands.Add(moveCommand.translationGuid);
            }
            while (MoveCommands.Count > 30)
            {
                MoveCommands.RemoveAt(0);
            }

            var currentSelection = this.OnPingDocument().SelectedObjects();
            OnPingDocument().DeselectAll();

            foreach (var item in moveCommand.moveGuids)
            {
                var obj = this.OnPingDocument().FindObject(item, false);
                if (obj == null) continue;
                obj.Attributes.Selected = true;
            }

            this.OnPingDocument().TranslateObjects(moveCommand.vector, true);
            this.OnPingDocument().DeselectAll();

            foreach (var selObj in currentSelection)
            {
                selObj.Attributes.Selected = true;
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
        

        private bool ConnectWires(RemoConnect wireCommand)
        {

            bool connect = wireCommand.RemoConnectType == RemoConnectType.Add || wireCommand.RemoConnectType == RemoConnectType.Replace;
            bool disconnect = wireCommand.RemoConnectType == RemoConnectType.Remove || wireCommand.RemoConnectType == RemoConnectType.Replace;
            System.Guid sourceGuid = wireCommand.sourceObjectGuid;
            int outIndex = wireCommand.sourceOutput;
            bool sourceIsSpecial = wireCommand.isSourceSpecial;
            System.Guid targetGuid = wireCommand.targetObjectGuid;
            int inIndex = wireCommand.targetInput;
            bool targetIsSpecial = wireCommand.isTargetSpecial;
            string sourceNickname = wireCommand.sourceNickname;
            string targetNickname = wireCommand.targetNickname;
            string listItemParamNickName = wireCommand.listItemParamNickName;
            if (connect)
            {
                if (!sourceIsSpecial)
                {
                    var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                    bool isItemComp = source is MathComponents.ArrayComponents.Component_ListItemVariable;
                    if (isItemComp)
                    {
                        MathComponents.ArrayComponents.Component_ListItemVariable listItemComp = (MathComponents.ArrayComponents.Component_ListItemVariable)source;

                        var outPutParams = listItemComp.Params.Output.Where(output => output.NickName.Equals(wireCommand.listItemParamNickName)).ToList();
                        if (outPutParams.Count == 1)
                        {
                            outIndex = listItemComp.Params.Output.IndexOf(outPutParams[0]);
                        }
                        else
                        {
                            int index = Convert.ToInt32(listItemParamNickName);

                            if (index < 0)
                            {
                                //int loopStart = listItemComp.Params.Output[0].NickName.Equals("i") ? -1 : Convert.ToInt32(listItemComp.Params.Output[0].NickName) - 1;
                                //for (int i = loopStart; i > index - 1; i--)
                                //{
                                //    Param_GenericObject genericParam = new Param_GenericObject();
                                //    genericParam.CreateAttributes();
                                //    genericParam.NickName = i.ToString();

                                //    //listItemComp.CreateParameter(GH_ParameterSide)

                                //    listItemComp.Params.Output.Insert(0,genericParam);
                                //    listItemComp.Params.RepairParamAssociations();

                                //    outPutParams = listItemComp.Params.Output.Where(output => output.NickName.Equals(wireCommand.listItemParamNickName)).ToList();
                                //    if (outPutParams.Count == 1)
                                //    {
                                //        outIndex = listItemComp.Params.Output.IndexOf(outPutParams[0]);
                                //    }

                                //}

                                //Param_GenericObject genericParam = new Param_GenericObject();
                                //genericParam.CreateAttributes();
                                //source.Params.Output.Add(genericParam);
                                //source.Params.RepairParamAssociations();
                                //sourceInputCount = source.Params.Output.Count;

                                System.Windows.Forms.MessageBox.Show("Please Use RemoSharp's Item Access Component", "Unsupported Component Behaiviour");
                            }
                            else
                            {
                                int loopStart = listItemComp.Params.Output[listItemComp.Params.Output.Count - 1].NickName.Equals("i") ? 1 :
                                    Convert.ToInt32(listItemComp.Params.Output[listItemComp.Params.Output.Count - 1].NickName) + 1;
                                
                                for (int i = loopStart; i < index + 1; i++)
                                {
                                    Param_GenericObject genericParam = new Param_GenericObject();
                                    genericParam.CreateAttributes();
                                    genericParam.NickName = "+" + i.ToString();

                                    listItemComp.Params.Output.Add(genericParam);
                                    listItemComp.Params.RepairParamAssociations();


                                    outPutParams = listItemComp.Params.Output.Where(output => output.NickName.Equals(wireCommand.listItemParamNickName)).ToList();
                                    if (outPutParams.Count == 1)
                                    {
                                        outIndex = listItemComp.Params.Output.IndexOf(outPutParams[0]);
                                    }

                                }
                            }

                            //listItemComp.VariableParameterMaintenance();


                        }
                    }

                }

                if (sourceIsSpecial)
                {
                    if (targetIsSpecial)
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.RemoveAllSources();
                            target.AddSource(source);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                    else
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.Params.Input[inIndex].RemoveAllSources();
                            target.Params.Input[inIndex].AddSource(source);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                }
                else
                {
                    if (targetIsSpecial)
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.RemoveAllSources();
                            target.AddSource(source.Params.Output[outIndex]);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                    else
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.Params.Input[inIndex].RemoveAllSources();

                            //if (source.GetType().ToString().Equals("MathComponents.ArrayComponents.Component_ListItemVariable"))
                            //{


                            //    int sourceInputCount = source.Params.Output.Count;
                            //    if (outIndex > sourceInputCount - 1)
                            //    {
                            //        var itemComp = (MathComponents.ArrayComponents.Component_ListItemVariable)source;
                            //        while (outIndex > sourceInputCount - 1)
                            //        {
                            //            Param_GenericObject genericParam = new Param_GenericObject();
                            //            IGH_Attributes attributes = source.Params.Output[0].Attributes;
                            //            genericParam.CreateAttributes();
                            //            genericParam.NickName = "+" + sourceInputCount;
                            //            source.Params.Output.Add(genericParam);
                            //            source.Params.RepairParamAssociations();
                            //            sourceInputCount = source.Params.Output.Count;
                            //        }
                            //    }
                            //}

                            target.Params.Input[inIndex].AddSource(source.Params.Output[outIndex]);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                            
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
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.RemoveSource(source);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                    else
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.Params.Input[inIndex].RemoveSource(source);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                }
                else
                {
                    if (targetIsSpecial)
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            
                            target.RemoveSource(source.Params.Output[outIndex]);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                    else
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        if (source == null || target == null) return false;
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.Params.Input[inIndex].RemoveSource(source.Params.Output[outIndex]);
                            source.Attributes.Pivot = new PointF(wireCommand.sourceX, wireCommand.sourceY);
                            target.Attributes.Pivot = new PointF(wireCommand.targetX, wireCommand.targetY);
                            source.ExpireSolution(false);
                            target.ExpireSolution(false);
                            source.NickName = sourceNickname;
                            target.NickName = targetNickname;
                        });
                    }
                }
            }

            return true;
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
            myObject.ExpireSolution(false);

            try
            {
                IGH_Component gh_Component = (IGH_Component) myObject;
                gh_Component.Params.RepairParamAssociations();
                gh_Component.NewInstanceGuid(newCompGuid);
                // making sure the update argument is false to prevent GH crashes
                thisDoc.AddObject(gh_Component, false);
                //GH_RelevantObjectData grip = new GH_RelevantObjectData(gh_Component.Attributes.Pivot);
                //this.OnPingDocument().Select(grip, false, true);

                string rpmType = "RemoSharp.RemoParams.RemoParam";
                if (gh_Component.GetType().ToString().Equals(rpmType))
                {
                    AddRemoParamDataComponent(gh_Component, rpmType);

                }

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