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
using GH_IO.Serialization;
using System.Xml;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using Grasshopper;

namespace RemoSharp
{
    public class CommandExecutor : GHCustomComponent
    {
        List<WireHistory> wireHistories = new List<WireHistory>();

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
            pManager.AddBooleanParameter("syncSend", "syncSend", "Syncs this grasshopper script for all other connected clients", GH_ParamAccess.item, false);
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

            if (true)
            {
                while (wireHistories.Count > 0)
                {



                    WireHistory item = wireHistories[0];

                    var historyComponent = this.OnPingDocument().FindObject(item.componentGuid, false);

                    var chunk2 = new GH_LooseChunk(null);
                    chunk2.Deserialize_Xml(item.wireHistoryXml);
                    wireHistories.RemoveAt(0);
                    try
                    {
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            historyComponent.Read(chunk2);
                            if (historyComponent is Grasshopper.Kernel.IGH_Component)
                            {
                                Grasshopper.Kernel.IGH_Component historyGH_comp = (IGH_Component)historyComponent;
                                foreach (var inputParam in historyGH_comp.Params.Input)
                                {
                                    inputParam.RelinkProxySources(this.OnPingDocument());
                                }
                            }
                            else if (historyComponent is Grasshopper.Kernel.IGH_Param)
                            {
                                Grasshopper.Kernel.IGH_Param historyGH_param = (Grasshopper.Kernel.IGH_Param)historyComponent;
                                historyGH_param.RelinkProxySources(this.OnPingDocument());
                            }

                        });
                    }
                    catch
                    {

                    }
                    this.OnPingDocument().UpdateAllSubsidiaries();

                }
            }

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
                        #region relayCreation
                        case (CommandType.RemoRelay):
                            RemoRelay relayCommand = (RemoRelay)remoCommand;
                            ExecuteRemoRelay(relayCommand);
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

                        case (CommandType.RemoCanvasSync):
                            RemoCanvasSync remoCanvasSync = (RemoCanvasSync)remoCommand;

                            SyncCanvasFromRemoCanvasSync(remoCanvasSync);

                            break;

                        case (CommandType.RemoScriptCS):
                            RemoScriptCS remoScriptCS = (RemoScriptCS)remoCommand;

                            ExecuteSelectRemoScriptCS(remoScriptCS);

                            break;
                        case (CommandType.RemoUndo):
                            RemoUndo remoUndo = (RemoUndo)remoCommand;

                            ExecuteRemoUndo(remoUndo);

                            break;
                        case (CommandType.RemoCompSync):
                            RemoCompSync remoCompSync = (RemoCompSync)remoCommand;

                            ExecuteRemoCompSync(remoCompSync);

                            break;
                        case (CommandType.RemoText):
                            RemoParameter remoParameter = (RemoParameter)remoCommand;

                            ExecuteRemoText(remoParameter);

                            break;
                        case (CommandType.CanvasViewport):
                            RemoCanvasView remoCanvasView = (RemoCanvasView)remoCommand;

                            ExecuteRemoCanvasView(remoCanvasView);

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
                            #region relayCreation
                            case (CommandType.RemoRelay):
                                RemoRelay relayCommand = (RemoRelay)remoCommand;
                                ExecuteRemoRelay(relayCommand);
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

                            #region RemoParamText
                            case (CommandType.RemoText):
                                RemoParameter remoText = (RemoParameter)remoCommand;
                                ExecuteRemoText(remoText);


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


            for (int i = retryCommands.Count - 1; i > -1; i--)
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

        private void ExecuteRemoCanvasView(RemoCanvasView remoCanvasView)
        {
            string str = remoCanvasView.canvasViewport;

            if (str == null || str == "" || str == " " || str == "Hello World") return;

            string[] parts = str.Split(',');
            Single x = Convert.ToSingle(parts[4]);
            Single y = Convert.ToSingle(parts[5]);
            float zoom = (float)Convert.ToDouble(parts[6]);
            Grasshopper.Instances.ActiveCanvas.Viewport.MidPoint = new System.Drawing.PointF(x, y);
            Grasshopper.Instances.ActiveCanvas.Viewport.Zoom = zoom;
        }

        private void ExecuteRemoCompSync(RemoCompSync remoCompSync)
        {


            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                for (int i = 0; i < remoCompSync.componentTypes.Count; i++)
                {
                    string type = remoCompSync.componentTypes[i];
                    Guid guid = remoCompSync.componentGuids[i];

                    var comp = this.OnPingDocument().FindObject(guid, false);
                    if (comp == null) RecognizeAndMakeSyncable(type, guid);
                }

                for (int i = 0; i < remoCompSync.componentTypes.Count; i++)
                {
                    string type = remoCompSync.componentTypes[i];
                    Guid guid = remoCompSync.componentGuids[i];
                    string xml = remoCompSync.componentXMLs[i];

                    var targetComp = this.OnPingDocument().FindObject(guid, false);

                    GH_LooseChunk targetAttributes = DeserilizeXMLAttributes(xml);
                    RelinkComponentWires(targetComp, targetAttributes);
                    if (targetComp is ScriptComponents.Component_CSNET_Script) targetComp.ExpireSolution(false);
                }



            });
        }

        private IGH_DocumentObject RecognizeAndMakeSyncable(string typeName, Guid guid)
        {

            var thisDoc = this.OnPingDocument();

            RemoSetupClient sourceComp = thisDoc.Objects
                .Where(obj => obj is RemoSharp.RemoSetupClient)
                .FirstOrDefault() as RemoSetupClient;
            this.OnPingDocument().ObjectsAdded -= sourceComp.RemoCompSource_ObjectsAdded;


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
            myObject.NewInstanceGuid(guid);
            // creating atts to create the pivot point
            // this pivot point can be anywhere
            myObject.CreateAttributes();
            thisDoc.AddObject(myObject, false);


            this.OnPingDocument().ObjectsAdded += sourceComp.RemoCompSource_ObjectsAdded;

            return myObject;
        }

        private void ExecuteRemoUndo(RemoUndo remoUndo)
        {
            if (!remoUndo.name.Equals("New wire")) return;

            IGH_Param sourceParam = remoUndo.sourceIndex == -1 ? this.OnPingDocument().FindParameter(remoUndo.sourceCompGuid) :
                this.OnPingDocument().FindComponent(remoUndo.sourceCompGuid).Params.Output[remoUndo.sourceIndex];
            IGH_Param targetParam = remoUndo.targetIndex == -1 ? this.OnPingDocument().FindParameter(remoUndo.targetCompGuid) :
                this.OnPingDocument().FindComponent(remoUndo.targetCompGuid).Params.Input[remoUndo.targetIndex];

            this.OnPingDocument().ScheduleSolution(1, doc => 
            {
                targetParam.RemoveSource(sourceParam);
                // for some reason the source target are flipped! O.o
            });
        }

        private void ExecuteRemoRelay(RemoRelay relayCommand)
        {

            var thisGHObjects = this.OnPingDocument().Objects.Select(obj => obj.InstanceGuid).ToList();
            if (thisGHObjects.Contains(relayCommand.objectGuid)) return;
            
            
            GH_Relay relay = new GH_Relay();
            relay.CreateAttributes();
            relay.NewInstanceGuid(relayCommand.objectGuid);

            IGH_Param sourceOutput = relayCommand.sourceIndex == -1
                ? (IGH_Param)this.OnPingDocument().FindObject(relayCommand.sourceGuid, false)
                : FindRelaySourceOutput(relayCommand);
            IGH_Param targetInput = relayCommand.targetIndex == -1
                ? (IGH_Param) this.OnPingDocument().FindObject(relayCommand.targetGuid, false)
                : FindRelayTargetInput(relayCommand);

            this.OnPingDocument().ScheduleSolution(1, doc => 
            {
                RemoSetupClient sourceComp = GetSourceCompFromInput();
                this.OnPingDocument().ObjectsAdded -= sourceComp.RemoCompSource_ObjectsAdded;

                GH_LooseChunk chunk = new GH_LooseChunk(null);
                chunk.Deserialize_Xml(relayCommand.relayXML);
                relay.Read(chunk);

                this.OnPingDocument().AddObject(relay, false);
                targetInput.RemoveSource(sourceOutput);
                relay.AddSource(sourceOutput);
                targetInput.AddSource(relay);

                this.OnPingDocument().ObjectsAdded += sourceComp.RemoCompSource_ObjectsAdded;
            });

        }

        private IGH_Param FindRelaySourceOutput(RemoRelay remoRelay)
        {
            IGH_Component comp = (IGH_Component) this.OnPingDocument().FindObject(remoRelay.sourceGuid,false);
            return (IGH_Param)comp.Params.Output[remoRelay.sourceIndex];
        }

        private IGH_Param FindRelayTargetInput(RemoRelay remoRelay)
        {
            IGH_Component comp = (IGH_Component)this.OnPingDocument().FindObject(remoRelay.targetGuid, false);
            return (IGH_Param)comp.Params.Input[remoRelay.targetIndex];
        }

        private void ExecuteSelectRemoScriptCS(RemoScriptCS remoScriptCS)
        {
            System.Guid componentGuid = remoScriptCS.objectGuid;

            var component = this.OnPingDocument().FindObject(componentGuid, false);

            ScriptComponents.Component_CSNET_Script csComp = (ScriptComponents.Component_CSNET_Script)component;
            
            RemoSharpComponent remoSharpComp = null;
            foreach (var item in csComp.Params.Output)
            {
                bool found = false;
                if (found) break;
                foreach (var rec in item.Recipients)
                {
                    if (rec.Attributes.Parent == null) continue;
                    if (rec.Attributes.Parent.DocObject is RemoSharpComponent)
                    {
                        remoSharpComp = (RemoSharpComponent)rec.Attributes.Parent.DocObject;
                        found = true;
                    }
                }
            }

            GH_LooseChunk sourceAttributes = DeserilizeXMLAttributes(remoScriptCS.xmlContent);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RelinkComponentWires(component, sourceAttributes);
                remoSharpComp.prevScript = string.Format("{0}{1}{2}", csComp.ScriptSource.UsingCode, csComp.ScriptSource.ScriptCode, csComp.ScriptSource.AdditionalCode);
                component.ExpireSolution(false);
            });
        }

        private void ExecuteRemoText(RemoParameter remoParameter)
        {
            this.OnPingDocument().ScheduleSolution(1, doc =>
            {

                RemoParam remoParamComp = this.OnPingDocument().FindObject(remoParameter.remoParamGuid, false) as RemoSharp.RemoParams.RemoParam;
                var remoGroup = this.OnPingDocument().FindObject(remoParamComp.groupGuid, false) as GH_Group;

                foreach (var item in remoGroup.ObjectIDs)
                {
                    var obj = this.OnPingDocument().FindObject(item, false);
                    if (obj is RemoSharp.RemoParams.RemoParamData)
                    {
                        try
                        {
                            RemoParamData remoParamData = obj as RemoParamData;
                            GH_LooseChunk chunk = new GH_LooseChunk(null);
                            chunk.Deserialize_Xml(remoParameter.xmlTree);

                            GH_Structure<IGH_Goo> gooTree = new GH_Structure<IGH_Goo>();
                            gooTree.Read(chunk);

                            remoParamData.currentValue = gooTree;
                            remoParamData.ExpireSolution(true);
                        }
                        catch
                        {

                        }
                    }
                }


                
                
            });
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
            //string path = @"C:\temp\RemoSharp\ReceiveStream.ghx";

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

                    GH_LooseChunk recieveChunk = new GH_LooseChunk(null);
                    recieveChunk.Deserialize_Xml(remoCanvasSync.xmlString);

                    GH_Document incomingDoc = new GH_Document();
                    incomingDoc.Read(recieveChunk);

                    foreach (var item in incomingDoc.Objects)
                    {
                        bool localDocContainsLocalItem = localCompIds.Contains(item.InstanceGuid);
                        bool typeIsParam = paramTypes.Contains(item.GetType().ToString());
                        if (localDocContainsLocalItem && typeIsParam)
                        {
                            var thisDocParam = this.OnPingDocument().FindObject(item.InstanceGuid, false);
                            var incomingDocParam = incomingDoc.FindObject(item.InstanceGuid, false);
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
                                    incomingDocSlider.SolutionExpired += remoSetupComp.RemoParameterizeSlider;

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_Panel"):
                                    Grasshopper.Kernel.Special.GH_Panel thisDocPanel = (Grasshopper.Kernel.Special.GH_Panel)thisDocParam;
                                    Grasshopper.Kernel.Special.GH_Panel incomingDocGH_Panel = (Grasshopper.Kernel.Special.GH_Panel)incomingDocParam;

                                    incomingDocGH_Panel.Properties.Alignment = thisDocPanel.Properties.Alignment;
                                    incomingDocGH_Panel.Properties.Wrap = thisDocPanel.Properties.Wrap;
                                    incomingDocGH_Panel.Properties.Colour = thisDocPanel.Properties.Colour;
                                    incomingDocGH_Panel.Properties.Multiline = thisDocPanel.Properties.Multiline;
                                    incomingDocGH_Panel.Properties.DrawIndices = thisDocPanel.Properties.DrawIndices;
                                    incomingDocGH_Panel.SetUserText(thisDocPanel.UserText);
                                    incomingDocGH_Panel.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocPanel, false);
                                    incomingDocGH_Panel.SolutionExpired += remoSetupComp.RemoParameterizePanel;

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                                    Grasshopper.Kernel.Special.GH_ColourSwatch thisDocColor = (Grasshopper.Kernel.Special.GH_ColourSwatch)thisDocParam;
                                    Grasshopper.Kernel.Special.GH_ColourSwatch incomingDocClolor = (Grasshopper.Kernel.Special.GH_ColourSwatch)incomingDocParam;

                                    incomingDocClolor.SwatchColour = thisDocColor.SwatchColour;
                                    incomingDocClolor.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocColor, false);
                                    incomingDocClolor.SolutionExpired += remoSetupComp.RemoParameterizeColor;


                                    break;
                                case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                                    Grasshopper.Kernel.Special.GH_MultiDimensionalSlider thisDocMDSlider = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)thisDocParam;
                                    Grasshopper.Kernel.Special.GH_MultiDimensionalSlider incomingMDSlider = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)incomingDocParam;

                                    incomingMDSlider.XInterval = thisDocMDSlider.XInterval;
                                    incomingMDSlider.YInterval = thisDocMDSlider.YInterval;
                                    incomingMDSlider.Value = thisDocMDSlider.Value;
                                    incomingMDSlider.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocMDSlider, false);
                                    incomingMDSlider.SolutionExpired += remoSetupComp.RemoParameterizeMDSlider;

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                                    Grasshopper.Kernel.Special.GH_BooleanToggle thisDocToggle = (Grasshopper.Kernel.Special.GH_BooleanToggle)thisDocParam;
                                    Grasshopper.Kernel.Special.GH_BooleanToggle incomingDocToggle = (Grasshopper.Kernel.Special.GH_BooleanToggle)incomingDocParam;

                                    incomingDocToggle.Value = thisDocToggle.Value;
                                    incomingDocToggle.ExpireSolution(false);

                                    this.OnPingDocument().RemoveObject(thisDocToggle, false);
                                    incomingDocToggle.SolutionExpired += remoSetupComp.RemoParameterizeToggle;

                                    break;
                                case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                                    Grasshopper.Kernel.Special.GH_ButtonObject thisDocButton = (Grasshopper.Kernel.Special.GH_ButtonObject)thisDocParam;
                                    //Grasshopper.Kernel.Special.GH_ButtonObject incomingDocGH_ButtonObject = (Grasshopper.Kernel.Special.GH_ButtonObject) incomingDocParam;

                                    this.OnPingDocument().RemoveObject(thisDocButton, false);
                                    incomingDocParam.SolutionExpired += remoSetupComp.RemoParameterizeButton;


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


                    SubscribeAllParams(remoSetupComp);

                });



            }
            catch
            {

            }

        }

        private void SubscribeAllParams(RemoSetupClient remoSetupComp)
        {
            List<string> accaptableTypes = new List<string>() {
            "Grasshopper.Kernel.Special.GH_NumberSlider",
            "Grasshopper.Kernel.Special.GH_Panel",
            "Grasshopper.Kernel.Special.GH_ColourSwatch",
            "Grasshopper.Kernel.Special.GH_MultiDimensionalSlider",
            "Grasshopper.Kernel.Special.GH_BooleanToggle",
            "Grasshopper.Kernel.Special.GH_ButtonObject"
            };

            var allParams = this.OnPingDocument().Objects
                .Where(obj =>accaptableTypes.Contains(obj.GetType().ToString()))
                .Where(obj => !obj.NickName.Contains("RemoSetup"));

            foreach (var item in allParams)
            {
                switch (item.GetType().ToString())
                {
                    case ("Grasshopper.Kernel.Special.GH_NumberSlider"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizeSlider;
                        break;
                    case ("Grasshopper.Kernel.Special.GH_Panel"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizePanel;
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ColourSwatch"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizeColor;
                        break;
                    case ("Grasshopper.Kernel.Special.GH_MultiDimensionalSlider"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizeMDSlider;
                        break;
                    case ("Grasshopper.Kernel.Special.GH_BooleanToggle"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizeToggle;
                        break;
                    case ("Grasshopper.Kernel.Special.GH_ButtonObject"):
                        item.SolutionExpired += remoSetupComp.RemoParameterizeButton;
                        break;
                    default:
                        break;
                }
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

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {


                GH_LooseChunk chunk = new GH_LooseChunk(null);
                chunk.Deserialize_Xml(remoPoint3d.pointXML);

                GH_Structure<GH_Point> gh_points = new GH_Structure<GH_Point>();
                gh_points.Read(chunk);

                if (pointParamComp.SourceCount > 0) return;
                pointParamComp.Attributes.Selected = false;
                
                pointParamComp.SetPersistentData(gh_points);
                pointParamComp.ExpireSolution(false);

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

                mdSliderComp.Value = new Point3d(remoMDSlider.ValueX, remoMDSlider.ValueY, 0);
                mdSliderComp.ExpireSolution(false);

            });
        }

        private void ExecuteRemoColor(RemoParamColor remoColor)
        {
            if (remoColor.objectGuid == Guid.Empty) return;
            GH_ColourSwatch colorComp = (GH_ColourSwatch)this.OnPingDocument().FindObject(remoColor.objectGuid, false);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                colorComp.Attributes.Selected = false;
                //colorComp.Attributes.Selected = false;
                colorComp.SwatchColour = Color.FromArgb(remoColor.Alpha, remoColor.Red, remoColor.Green, remoColor.Blue);
                colorComp.ExpireSolution(false);

            });
        }

        private void ExecuteRemoPanel(RemoParamPanel remoPanel)
        {
            if (remoPanel.objectGuid == Guid.Empty) return;
            GH_Panel panelComp = (GH_Panel)this.OnPingDocument().FindObject(remoPanel.objectGuid, false);

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            chunk.Deserialize_Xml(remoPanel.xmlContent);

            if (chunk.ChunkExists("Attributes"))
            {
                var attChunk = chunk.FindChunk("Attributes");
                if (attChunk.ItemExists("Selected"))
                {
                    GH_IO.Types.GH_Item item = attChunk.FindItem("Selected");
                    item.Read(ChangedItem());
                }
            }

            this.OnPingDocument().ScheduleSolution(0, doc => 
            {
                panelComp.Attributes.Selected = false;
                RelinkComponentWires(panelComp, chunk);
                panelComp.ExpireSolution(true);
            });

        }

        public System.Xml.XmlNode ChangedItem()
        {
            // Create an XmlDocument object
            XmlDocument xmlDoc = new XmlDocument();

            // Create the <item> element
            XmlElement itemElement = xmlDoc.CreateElement("item");

            // Set the attributes of <item>
            itemElement.SetAttribute("name", "Selected");
            itemElement.SetAttribute("type_name", "gh_bool");
            itemElement.SetAttribute("type_code", "1");

            // Set the text of the <item> element
            itemElement.InnerText = "false";

            // Append the <item> element to the XmlDocument
            xmlDoc.AppendChild(itemElement);

            // To save the document (optional)
            // xmlDoc.Save("path_to_save.xml");

            // To print the document to console (for verification)
            return xmlDoc.FirstChild;
        } 
        private void ExecuteRemoToggle(RemoParamToggle remoToggle)
        {
            if (remoToggle.objectGuid == Guid.Empty) return;
            GH_BooleanToggle toggleComp = (GH_BooleanToggle)this.OnPingDocument().FindObject(remoToggle.objectGuid, false);

            //RemoParamData remoParamDataComponent = FindAssociatedRemoParamDataComp(toggleComp);


            this.OnPingDocument().ScheduleSolution(1, doc =>
            {

                toggleComp.Value = remoToggle.toggleValue;
                toggleComp.ExpireSolution(true);

            });
        }

        private void ExecuteRemoButton(RemoParamButton remoButton)
        {
            if (remoButton.objectGuid == Guid.Empty) return;
            GH_ButtonObject buttonComp = (GH_ButtonObject)this.OnPingDocument().FindObject(remoButton.objectGuid, false);

            //RemoParamData remoParamDataComponent = FindAssociatedRemoParamDataComp(buttonComp);


            this.OnPingDocument().ScheduleSolution(1, doc =>
            {

                buttonComp.ButtonDown = remoButton.buttonValue;
                buttonComp.ExpireSolution(true);

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

            //RemoParamData remoParamDataComponent = FindAssociatedRemoParamDataComp(comp);


            this.OnPingDocument().ScheduleSolution(1, doc =>
            {

                sliderComp.SetSliderValue(remoSlider.sliderValue);
                sliderComp.ExpireSolution(false);

            });
        }

        private RemoParamData FindAssociatedRemoParamDataComp(IGH_Param paramComp)
        {
            var rpmComps = paramComp.Recipients.Where(obj => obj.Attributes.Parent != null)
                .Where(obj => obj.Attributes.Parent.DocObject != null)
                .Where(obj => obj.Attributes.Parent.DocObject.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam")).ToList();


            var rpmComp = (RemoSharp.RemoParams.RemoParam)rpmComps[0].Attributes.Parent.DocObject;

            string rpmNickname = rpmComp.Message;

            var dataComps = this.OnPingDocument().Objects.Where(obj => obj is GH_Component).Select(obj => (GH_Component)obj).ToList();
            //&& !obj.GetType().Equals("RemoSharp.RemoParams.RemoParam")).ToList();
            //.Select(obj => (RemoParamData) obj).ToList()[0];

            var dataComp = dataComps.Where(obj => obj.Message != null).ToList();

            var dataC = dataComp.Where(obj => obj.Message.Equals(rpmComp.Message) && obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParamData")).ToList();

            var remoParamDataComponent = (RemoSharp.RemoParams.RemoParamData)dataC[0];
            return remoParamDataComponent;
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
            try
            {
                List<IGH_Param> relaySources = new List<IGH_Param>();
                List<IGH_Param> relayTargets = new List<IGH_Param>();
                var deletionObjs = this.OnPingDocument().Objects.AsParallel().AsUnordered().Where(obj => remoDelete.objectGuids.Contains(obj.InstanceGuid));
                var deletionList = deletionObjs.ToList();
                if (deletionList.Count == 0) return;
                if (deletionList.Count == 1)
                {
                    var singleObj = deletionList[0];
                    if (singleObj.GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Relay"))
                    {
                        Grasshopper.Kernel.Special.GH_Relay relay = (Grasshopper.Kernel.Special.GH_Relay)singleObj;
                        relaySources.AddRange(relay.Sources);
                        relayTargets.AddRange(relay.Recipients);
                    }
                }

                RemoSetupClient sourceComp = GetSourceCompFromInput();
                this.OnPingDocument().ObjectsDeleted -= sourceComp.RemoCompSource_ObjectsDeleted;
                this.OnPingDocument().ScheduleSolution(1, doc => 
                {
                    this.OnPingDocument().RemoveObjects(deletionObjs, false);

                    foreach (var target in relayTargets)
                    {
                        foreach (var source in relaySources)
                        {
                            target.AddSource(source);
                        }
                    }
                });
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
                    string associatedAttribute = createCommand.associatedAttributes[i];
                    string typeName = createCommand.componentTypes[i];
                    string componentStructure = createCommand.componentStructures[i];
                    string specialContent = createCommand.specialParameters[i];

                    if (thisGHObjects.Contains(newCompGuid)) continue;

                    //temporary cleared to test new method
                    //if (gh_components.Contains(newCompGuid)) continue;
                    try
                    {

                        RecognizeAndMake(typeName, componentStructure, newCompGuid, associatedAttribute);
                        WireHistory wireHistory = new WireHistory(newCompGuid, specialContent);
                        wireHistories.Add(wireHistory);

                    }
                    catch (Exception e)
                    {
                        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                    }
                }

                this.OnPingDocument().ObjectsAdded += sourceComp.RemoCompSource_ObjectsAdded;
            });


        }

        private void AddRemoParamDataComponent(IGH_DocumentObject obj,string dataCompXml)
        {


            this.OnPingDocument().ScheduleSolution(0, doc =>
            {
                var rpmPivot = obj.Attributes.Pivot;
                PointF dataPivot = new PointF(rpmPivot.X + 36, rpmPivot.Y);

                RemoParam remoParamComp = obj as RemoParam;
                RemoParamData dataComp = new RemoParamData();
                dataComp.CreateAttributes();
                dataComp.Attributes.Pivot = dataPivot;
                dataComp.Params.RepairParamAssociations();
                dataComp.Params.RepairProxyParams(this.OnPingDocument());


                GH_LooseChunk chunk2 = new GH_LooseChunk(null);
                chunk2.Deserialize_Xml(dataCompXml);
                dataComp.Read(chunk2);

                dataComp.ExpireSolution(true);



                this.OnPingDocument().AddObject(dataComp, false);

                GroupRemoParamDataComponents(remoParamComp, dataComp);

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

        //private void DeleteComponent(GH_Document doc)
        //{
        //    try
        //    {
        //        ////var otherComp = this.OnPingDocument().Objects[deletionIndex];
        //        //foreach (Guid item in deletionGuids)
        //        //{

        //        //}
        //        //var otherComp = this.OnPingDocument().FindObject(item, false);
        //        if (otherComp != null) this.OnPingDocument().RemoveObjects(deletionGuids, true);
        //    }
        //    catch (Exception e)
        //    {
        //        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
        //    }


        //}


        private bool ConnectWires(RemoConnect wireCommand)
        {
            System.Guid sourceId = wireCommand.sourceObjectGuid;
            System.Guid targetId = wireCommand.targetObjectGuid;

            var sourceComp = this.OnPingDocument().FindObject(sourceId, false);
            var targetComp = this.OnPingDocument().FindObject(targetId, false);

            GH_LooseChunk sourceAttributes = DeserilizeXMLAttributes(wireCommand.sourceXML);
            GH_LooseChunk targetAttributes = DeserilizeXMLAttributes(wireCommand.targetXML);

            this.OnPingDocument().ScheduleSolution(1, doc =>
            {
                RelinkComponentWires(sourceComp, sourceAttributes);
                RelinkComponentWires(targetComp, targetAttributes);
                if (sourceComp is ScriptComponents.Component_CSNET_Script) sourceComp.ExpireSolution(false);
                if (targetComp is ScriptComponents.Component_CSNET_Script) targetComp.ExpireSolution(false);
            });


            return true;
            
        }

        private void RelinkComponentWires(IGH_DocumentObject sourceComp, GH_LooseChunk attributes)
        {


            if (sourceComp is Grasshopper.Kernel.IGH_Component)
            {
                Grasshopper.Kernel.IGH_Component sourceGH_Comp = (IGH_Component)sourceComp;

                List<ParameterWireHistory> paramHistories = new List<ParameterWireHistory>();
                foreach (var item in sourceGH_Comp.Params.Output)
                {
                    paramHistories.Add(new ParameterWireHistory(item.NickName, item.Recipients.ToList()));
                }

                sourceComp.Read(attributes);

                foreach (var inputParam in sourceGH_Comp.Params.Input)
                {
                    inputParam.RelinkProxySources(this.OnPingDocument());
                }

                List<string> remainingNicknames = sourceGH_Comp.Params.Output.Select(obj => obj.NickName).ToList();
                foreach (ParameterWireHistory item in paramHistories)
                {
                    if (!remainingNicknames.Contains(item.paramNickname)) continue;
                    IGH_Param sourceParam = sourceGH_Comp.Params.Output.Where(obj => obj.NickName == item.paramNickname).FirstOrDefault();
                    foreach (var receiving in item.paramObjects)
                    {
                        receiving.AddSource(sourceParam);
                    }
                }

            }
            else if (sourceComp is Grasshopper.Kernel.IGH_Param)
            {
                Grasshopper.Kernel.IGH_Param sourceCompGH_param = (Grasshopper.Kernel.IGH_Param)sourceComp;

                ParameterWireHistory paramHistory = new ParameterWireHistory(sourceCompGH_param.NickName, sourceCompGH_param.Recipients.ToList());

                sourceCompGH_param.Read(attributes);

                sourceCompGH_param.RelinkProxySources(this.OnPingDocument());

                foreach (var receiving in paramHistory.paramObjects)
                {
                    receiving.AddSource(sourceCompGH_param);
                }
            }
        }

        public class ParameterWireHistory
            {
            public string paramNickname;
            public List<IGH_Param> paramObjects;
            //constructor
            public ParameterWireHistory(string paramNickname, List<IGH_Param> paramObjects) 
            {
                this.paramNickname = paramNickname;
                this.paramObjects = paramObjects;
            }
        }

        private GH_LooseChunk DeserilizeXMLAttributes(string sourceXML)
        {
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            chunk.Deserialize_Xml(sourceXML);
            return chunk;
        }

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY,Guid newCompGuid, string associatedAttribute)
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
                    AddRemoParamDataComponent(gh_Component,associatedAttribute);

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

        private void RecognizeAndMake(string typeName, string componentStructure, Guid newCompGuid, string associatedAttribute)
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

            var chunk2 = new GH_LooseChunk(null);
            chunk2.Deserialize_Xml(componentStructure);
            myObject.Read(chunk2);

            myObject.Attributes.Selected = false;
            //myObject.Attributes.Selected = true;
            myObject.ExpireSolution(false);


            if (myObject is Grasshopper.Kernel.Special.GH_NumberSlider)
            {
                Grasshopper.Kernel.Special.GH_NumberSlider comp = (Grasshopper.Kernel.Special.GH_NumberSlider) myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizeSlider;
                remoSetupClient.GroupObjParam(comp);

            }
            else if (myObject is Grasshopper.Kernel.Parameters.Param_Point)
            {
                Grasshopper.Kernel.Parameters.Param_Point comp = (Grasshopper.Kernel.Parameters.Param_Point)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.AsParallel().AsUnordered()
                    .Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
            }
            else if  (myObject is Grasshopper.Kernel.Special.GH_ButtonObject) 
            {
                Grasshopper.Kernel.Special.GH_ButtonObject comp = (Grasshopper.Kernel.Special.GH_ButtonObject)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizeButton;
                remoSetupClient.GroupObjParam(comp);
            }
            else if (myObject is Grasshopper.Kernel.Special.GH_Panel)
            {
                Grasshopper.Kernel.Special.GH_Panel comp = (Grasshopper.Kernel.Special.GH_Panel)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizePanel;
                remoSetupClient.GroupObjParam(comp);
            }
            else if (myObject is Grasshopper.Kernel.Special.GH_ColourSwatch)
            {
                Grasshopper.Kernel.Special.GH_ColourSwatch comp = (Grasshopper.Kernel.Special.GH_ColourSwatch)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizeColor;
                remoSetupClient.GroupObjParam(comp,RemoParam.RemoParamSelectionKeyword);
            }
            else if (myObject is Grasshopper.Kernel.Special.GH_BooleanToggle)
            {
                Grasshopper.Kernel.Special.GH_BooleanToggle comp = (Grasshopper.Kernel.Special.GH_BooleanToggle)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizeToggle;
                remoSetupClient.GroupObjParam(comp);
            }
            else if (myObject is Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)
            {
                Grasshopper.Kernel.Special.GH_MultiDimensionalSlider comp = (Grasshopper.Kernel.Special.GH_MultiDimensionalSlider)myObject;
                RemoSetupClient remoSetupClient = this.OnPingDocument().Objects.Where(obj => obj is RemoSetupClient).Select(obj => obj as RemoSetupClient).FirstOrDefault();
                //comp.NickName = "local";
                comp.SolutionExpired += remoSetupClient.RemoParameterizeMDSlider;
                remoSetupClient.GroupObjParam(comp);
            }


            try
            {
                IGH_Component gh_Component = (IGH_Component)myObject;
                gh_Component.Params.RepairParamAssociations();
                gh_Component.NewInstanceGuid(newCompGuid);
                // making sure the update argument is false to prevent GH crashes
                thisDoc.AddObject(gh_Component, false);
                //GH_RelevantObjectData grip = new GH_RelevantObjectData(gh_Component.Attributes.Pivot);
                //this.OnPingDocument().Select(grip, false, true);

                string rpmType = "RemoSharp.RemoParams.RemoParam";
                if (gh_Component.GetType().ToString().Equals(rpmType))
                {
                    AddRemoParamDataComponent(gh_Component, associatedAttribute);
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