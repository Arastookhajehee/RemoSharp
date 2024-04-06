using Grasshopper.GUI.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Drawing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel.Undo;
using Grasshopper.Kernel.Undo.Actions;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Security.Cryptography;
using GhPython;

namespace RemoSharp.RemoCommandTypes
{

    public enum CommandType
    {
        ServerDisconnect = -1,
        NullCommand = 0, // done
        MoveComponent = 1,
        Create = 2, // done
        Delete = 3, // done
        Lock = 4, // done
        Hide = 5, // done
        WireConnection = 6, //done
        RemoParamNone = 7, // done
        Select = 8,
        StreamGeom = 9,
        RemoSlider = 10, // done
        RemoButton = 11, // done
        RemoToggle = 12, // done
        RemoPanel = 13, // done
        RemoColor = 14, // done
        RemoMDSlider = 15,
        RemoPoint3d = 16,
        RemoVector3d = 17,
        RemoPlane = 18,
        RemoCanvasSync = 19,
        WireHistory = 20,
        ItemAddition = 21,
        RemoText = 22,
        RemoScriptCS = 23,
        RemoRelay = 24,
        RemoUndo = 25,
        RemoCompSync = 26,
        CanvasViewport = 27,
        RemoPartialDocument = 28,
        RemoParameter = 29
    }

    public enum RemoConnectType
    {
        None = 0,
        Add = 1,
        Replace = 2,
        Remove = 3
    }
    abstract public class RemoCommand
    {
        public string issuerID;
        public CommandType commandType;
        public Guid objectGuid;
        public int executionAttempts = 0;
        public bool executed = false;
        public Guid commandID;
        public static string SerializeToJson(List<RemoCommand> commands)
        {
            return JsonConvert.SerializeObject(commands, Formatting.None);
        }
        public static string SerializeToJson(RemoCommand commands)
        {
            return JsonConvert.SerializeObject(commands, Formatting.None);
        }

        public static string SerializeToXML(object obj)
        {
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            if (obj is GH_Document)
            {
                GH_Document doc = (GH_Document)obj;
                doc.Write(chunk);
            }
            else if (obj is IGH_DocumentObject)
            {
                IGH_DocumentObject docObj = (IGH_DocumentObject)obj;
                docObj.Write(chunk);
            }
            else return "";
            return MinifyXml(chunk.Serialize_Xml());
        }

        public static string SerializeToXML(Guid componentGuid)
        {
            var component = Grasshopper.Instances.ActiveCanvas.Document.FindObject(componentGuid, false);
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            component.Write(chunk);
            return MinifyXml(chunk.Serialize_Xml());
        }

        public static GH_LooseChunk DeserializeFromXML(string xml)
        {
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            chunk.Deserialize_Xml(xml);
            return chunk;
        }

        public static GH_Document DeserializeGH_DocumentFromXML(string xml)
        {
            GH_Document tempDoc = new GH_Document();
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            chunk.Deserialize_Xml(xml);
            tempDoc.Read(chunk);
            return tempDoc;
        }
        public static string SerizlizeToSinglecomponentDocXML(Guid guid)
        {
            var component = Grasshopper.Instances.ActiveCanvas.Document.FindObject(guid, false);
            GH_Document tempDoc = new GH_Document();
            tempDoc.AddObject(component, true);
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            tempDoc.Write(chunk);
            return MinifyXml(chunk.Serialize_Xml());
        }

        public static string SerizlizeToSinglecomponentDocXML(IGH_DocumentObject component)
        {
            GH_Document tempDoc = new GH_Document();
            tempDoc.AddObject(component, true);
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            tempDoc.Write(chunk);
            return MinifyXml(chunk.Serialize_Xml());
        }

        public static string MinifyXml(string xmlString)
        {
            // Load the XML string into an XDocument
            XDocument xDocument = XDocument.Parse(xmlString);

            // Save the XDocument to a string with no indentation
            return xDocument.ToString(SaveOptions.DisableFormatting);
        }

        public static RemoCommand DeserializeFromJson(string commandJson)
        {

            //dynamic data = JsonConvert.DeserializeObject(commandJson);
            //int commandType = data.commandType;

            RemoCommand remoCommand = null;

            JObject jsonObject = JObject.Parse(commandJson);
            string jsonCommandType = (string)jsonObject["commandType"];
            int commandType = Convert.ToInt32(jsonCommandType);


            switch (commandType)
            {

                case (int)CommandType.WireConnection:

                    remoCommand = JsonConvert.DeserializeObject<RemoConnect>(commandJson);

                    break;
                case (int)CommandType.Delete:

                    remoCommand = JsonConvert.DeserializeObject<RemoDelete>(commandJson);

                    break;
                case (int)CommandType.Hide:

                    remoCommand = JsonConvert.DeserializeObject<RemoHide>(commandJson);

                    break;
                case (int)CommandType.Select:

                    remoCommand = JsonConvert.DeserializeObject<RemoSelect>(commandJson);

                    break;
                case (int)CommandType.MoveComponent:

                    remoCommand = JsonConvert.DeserializeObject<RemoMove>(commandJson);
                    break;
                case (int)CommandType.Create:

                    remoCommand = JsonConvert.DeserializeObject<RemoCreate>(commandJson);

                    break;
                case (int)CommandType.Lock:

                    remoCommand = JsonConvert.DeserializeObject<RemoLock>(commandJson);

                    break;
                case (int)CommandType.RemoParameter:
                    remoCommand = JsonConvert.DeserializeObject<RemoParameter>(commandJson);
                    break;
                case (int)CommandType.RemoSlider:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamSlider>(commandJson);
                    break;
                case (int)CommandType.RemoButton:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamButton>(commandJson);
                    break;
                case (int)CommandType.RemoToggle:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamToggle>(commandJson);
                    break;
                case (int)CommandType.RemoPanel:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamPanel>(commandJson);
                    break;
                case (int)CommandType.RemoColor:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamColor>(commandJson);
                    break;
                case (int)CommandType.RemoMDSlider:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamMDSlider>(commandJson);
                    break;
                case (int)CommandType.RemoPoint3d:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamPoint3d>(commandJson);
                    break;
                case (int)CommandType.RemoVector3d:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamVector3d>(commandJson);
                    break;
                case (int)CommandType.RemoPlane:
                    remoCommand = JsonConvert.DeserializeObject<RemoParamPlane>(commandJson);
                    break;
                case (int)CommandType.RemoCanvasSync:
                    remoCommand = JsonConvert.DeserializeObject<RemoCanvasSync>(commandJson);
                    break;
                case (int)CommandType.RemoScriptCS:
                    remoCommand = JsonConvert.DeserializeObject<RemoScriptCS>(commandJson);
                    break;
                case (int)CommandType.RemoRelay:
                    remoCommand = JsonConvert.DeserializeObject<RemoRelay>(commandJson);
                    break;
                case (int)CommandType.RemoUndo:
                    remoCommand = JsonConvert.DeserializeObject<RemoUndo>(commandJson);
                    break;
                case (int)CommandType.RemoCompSync:
                    remoCommand = JsonConvert.DeserializeObject<RemoCompSync>(commandJson);
                    break;
                case (int)CommandType.CanvasViewport:
                    remoCommand = JsonConvert.DeserializeObject<RemoCanvasView>(commandJson);
                    break;
                case (int)CommandType.StreamGeom:
                    return null;
                //break;
                case (int)CommandType.NullCommand:
                    remoCommand = JsonConvert.DeserializeObject<RemoNullCommand>(commandJson);
                    break;
                case (int)CommandType.RemoPartialDocument:
                    remoCommand = JsonConvert.DeserializeObject<RemoPartialDoc>(commandJson);
                    break;
                //break;
                default:
                    break;
            }

            return remoCommand;
        }

    }

    public class RemoNullCommand : RemoCommand
    {
        public RemoNullCommand(string issuerID)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.NullCommand;
            this.objectGuid = Guid.Empty;
            this.commandID = Guid.NewGuid();
        }
        public RemoNullCommand()
        {
            // default constructor
        }

        public override string ToString()
        {
            return string.Format("RemoNull Command from {0}", this.issuerID);
        }
    }

    public class RemoConnectInteraction : RemoCommand
    {
        public IGH_Param source = null;
        public IGH_Param target = null;
        public int sourceOutput = -1;
        public int targetInput = -1;
        public bool isSourceSpecial = false;
        public bool isTargetSpecial = false;
        public RemoConnectType RemoConnectType = RemoConnectType.None;

        public RemoConnectInteraction()
        {
            // default constructor
        }

        public RemoConnectInteraction(string issuerID, IGH_Param source, IGH_Param target,
            RemoConnectType remoConnectType)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.WireConnection;
            this.RemoConnectType = remoConnectType;
            this.source = source;
            this.target = target;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoConnectInteraction Command from {0}", this.issuerID);
        }
    }

    public class RemoConnect : RemoCommand
    {
        public Guid sourceObjectGuid = Guid.Empty;
        public Guid targetObjectGuid = Guid.Empty;
        public RemoConnectType RemoConnectType = RemoConnectType.None;
        public string sourceXML;
        public string targetXML;
        public string sourceType;
        public string targetType;

        public RemoConnect()
        {
            // default constructor
        }

        public RemoConnect(string issuerID, Guid sourceObjectGuid, Guid targetObjectGuid,
            RemoConnectType remoConnectType,
            string sourceXML, string targetXML, string sourceType, string targetType)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.WireConnection;
            this.RemoConnectType = remoConnectType;
            this.sourceObjectGuid = sourceObjectGuid;
            this.targetObjectGuid = targetObjectGuid;
            this.sourceXML = sourceXML;
            this.targetXML = targetXML;
            this.sourceType = sourceType;
            this.targetType = targetType;
        }

        public override string ToString()
        {
            return string.Format("RemoConnect Command from {0}", this.issuerID);
        }
    }

    public class RemoCreate : RemoCommand
    {
        public List<Guid> guids;
        public List<string> attributeXMLs;
        public List<string> componentTypes;

        public RemoCreate()
        {
            // default constructor
        }
        // a constructor for general components
        public RemoCreate(string issuerID,
            List<Guid> guids,
            List<string> associatedAttributes,
            List<string> componentTypes)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Create;
            this.objectGuid = Guid.Empty;
            this.guids = guids;
            this.attributeXMLs = associatedAttributes;
            this.componentTypes = componentTypes;
            this.commandID = Guid.NewGuid();
        }


        public override string ToString()
        {
            return string.Format("RemoCreate Command from {0}", this.issuerID);
        }

    }

    public class RemoScriptCS : RemoCommand
    {
        public string xmlContent;
        public RemoScriptCS() { }

        public RemoScriptCS(string issuerID, ScriptComponents.Component_CSNET_Script csComponent)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoScriptCS;
            this.objectGuid = csComponent.InstanceGuid;
            this.commandID = Guid.NewGuid();

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            csComponent.Write(chunk);

            this.xmlContent = chunk.Serialize_Xml();

        }
    }

    public class RemoDelete : RemoCommand
    {
        public List<Guid> objectGuids;
        public RemoDelete() { }
        public RemoDelete(string issuerID, List<Guid> objectGuids)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Delete;
            this.objectGuid = Guid.Empty;
            this.objectGuids = objectGuids;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoDelete Command from {0}", this.issuerID);
        }
    }

    public class RemoPartialDoc : RemoCommand
    {
        public string xml;
        public List<WireHistory> pythonWireHistories;
        public Dictionary<Guid, List<Guid>> relayConnections;
        
        public RemoPartialDoc() { }

        public static List<string> specialComponentTypes  = new List<string>()
        {  };
       
        public RemoPartialDoc(string issuerID, List<IGH_DocumentObject> objects, GH_Document currentDoc)
        {
            this.issuerID = issuerID;
            this.executed = false;
            this.objectGuid = Guid.Empty;
            this.commandID = Guid.NewGuid();
            this.commandType = CommandType.RemoPartialDocument;
            this.executionAttempts = 0;
            this.pythonWireHistories = new List<WireHistory>();
            this.relayConnections = new Dictionary<Guid, List<Guid>>();

            string xml = "";
            GH_Document tempDoc = new GH_Document();

            List<WireHistory> hitory = new List<WireHistory>();

            if (objects.Count == 1 && objects[0] is GH_Relay)
            {
                GH_Relay relay = (GH_Relay)objects[0];
                this.relayConnections.Add(relay.Sources[0].InstanceGuid, relay.Recipients.Select(obj => obj.InstanceGuid).ToList());
            }

            foreach (var item in objects)
            {
                
                string type = item.GetType().FullName;

                if (type.Equals("GhPython.Component.ZuiPythonComponent"))
                {
                    string xmlHistory = SerializeToXML(item);
                    hitory.Add(new WireHistory(item.InstanceGuid, xmlHistory));

                    GhPython.Component.ZuiPythonComponent zuiPythonComponent = (GhPython.Component.ZuiPythonComponent)item;
                    pythonWireHistories.Add(new WireHistory(zuiPythonComponent));

                    GH_LooseChunk chunk = new GH_LooseChunk(null);
                    zuiPythonComponent.Write(chunk);
                    
                    GhPython.Component.ZuiPythonComponent pythonComponent = new GhPython.Component.ZuiPythonComponent();
                    pythonComponent.CreateAttributes();
                    pythonComponent.Read(chunk);
                    pythonComponent.NewInstanceGuid(zuiPythonComponent.InstanceGuid);

                    tempDoc.AddObject(pythonComponent, false);

                }
                else
                {
                    string xmlHistory = SerializeToXML(item);
                    hitory.Add(new WireHistory(item.InstanceGuid, xmlHistory));

                    tempDoc.AddObject(item, false);
                }
            }
            xml = RemoCommand.SerializeToXML(tempDoc);

            currentDoc.RemoveObjects(objects, false);
            currentDoc.MergeDocument(tempDoc, true, true);

            foreach (var item in hitory)
            {
                var obj = currentDoc.FindObject(item.componentGuid, false);
                var chunk = DeserializeFromXML(item.wireHistoryXml);
                obj.Read(chunk);

                if (obj is IGH_Param)
                {
                    IGH_Param gH_Param = (IGH_Param)obj;
                    gH_Param.RelinkProxySources(currentDoc);
                }
                else if (obj is IGH_Component)
                {
                    IGH_Component gH_Component = (IGH_Component)obj;
                    foreach (var input in gH_Component.Params.Input)
                    {
                        input.RelinkProxySources(currentDoc);
                    }
                }
            }

            this.xml = xml;

        }
    }

    public class RemoRelay : RemoCommand
    {
        public Guid sourceGuid;
        public Guid targetGuid;
        public int sourceIndex;
        public int targetIndex;
        public string relayXML;
        public RemoRelay() { }
        public RemoRelay(
            string issuerID,
            GH_Relay relayComponent,
            Guid sourceGuid,
            Guid targetGuid,
            int sourceIndex,
            int targetIndex
            )
        {


            this.issuerID = issuerID;
            this.commandType = CommandType.RemoRelay;
            this.objectGuid = relayComponent.InstanceGuid;
            this.sourceGuid = sourceGuid;
            this.targetGuid = targetGuid;
            this.sourceIndex = sourceIndex;
            this.targetIndex = targetIndex;
            this.commandID = Guid.NewGuid();

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            relayComponent.Write(chunk);
            this.relayXML = chunk.Serialize_Xml();
        }



        public override string ToString()
        {
            return string.Format("RemoRelay Command from {0}", this.issuerID);
        }
    }

    public class RemoUndo : RemoCommand 
     {

        public string name;
        public int actionCount;

        public Guid sourceCompGuid = Guid.Empty;
        public Guid targetCompGuid = Guid.Empty;
        public int sourceIndex = 0;
        public int targetIndex = 0;

        public RemoUndo() { }
        public RemoUndo(string issuerID, GH_DocUndoEventArgs e)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoUndo;
            this.objectGuid = Guid.Empty;
            var rec = e.Record;
            //this.expiresDisplay = rec.ExpiresDisplay;
            //this.expiresSolution = rec.ExpiresSolution;
            //this.state = (int)rec.State;
            this.name = rec.Name;
            this.actionCount = rec.ActionCount;

            int actCount = rec.ActionCount;

            if (rec.Name.Contains("New wire"))
            {
                GH_WireAction wireAction = (GH_WireAction)rec.Actions[0];

                var wState = wireAction.State;

                Type type = typeof(GH_WireAction);
                Grasshopper.Kernel.GH_WireTopologyDiagram mode = type
                  .GetField("m_wires", BindingFlags.NonPublic | BindingFlags.Instance)
                  .GetValue(wireAction) as Grasshopper.Kernel.GH_WireTopologyDiagram;

                IGH_Param sourceOutput = e.Document.FindParameter(mode[0].SourceParameterID);
                IGH_Param targetInput = e.Document.FindParameter(mode[0].TargetParameterID);

                Guid sourceGuid = sourceOutput.InstanceGuid;
                Guid targetguid = targetInput.InstanceGuid;
                int sourceOutputIndex = sourceOutput.Attributes.Parent == null ? -1 : FindOutputIndexFromGH_Param(sourceOutput, out sourceGuid);
                int targetInputIndex = targetInput.Attributes.Parent == null ? -1 : FindInputIndexFromGH_Param(targetInput, out targetguid);

                this.sourceCompGuid = sourceGuid;
                this.targetCompGuid = targetguid;
                this.sourceIndex = sourceOutputIndex;
                this.targetIndex = targetInputIndex;
            }
            //else if (rec.Name.Contains("Remove wire"))
            //    return;
            //{
            //    GH_WireAction wireAction = (GH_WireAction)rec.Actions[0];

            //    var wState = wireAction.State;

            //    var latestNewWire = e.Document.UndoServer.UndoGuids[0];

                

            //    Type type = typeof(GH_WireAction);
            //    Grasshopper.Kernel.GH_WireTopologyDiagram mode = type
            //      .GetField("m_wires", BindingFlags.NonPublic | BindingFlags.Instance)
            //      .GetValue(wireAction) as Grasshopper.Kernel.GH_WireTopologyDiagram;

            //    IGH_Param sourceOutput = e.Document.FindParameter(mode[0].SourceParameterID);
            //    IGH_Param targetInput = e.Document.FindParameter(mode[0].TargetParameterID);

            //    Guid sourceGuid = sourceOutput.InstanceGuid;
            //    Guid targetguid = targetInput.InstanceGuid;
            //    int sourceOutputIndex = sourceOutput.Attributes.Parent == null ? -1 : FindOutputIndexFromGH_Param(sourceOutput, out sourceGuid);
            //    int targetInputIndex = targetInput.Attributes.Parent == null ? -1 : FindInputIndexFromGH_Param(targetInput, out targetguid);

            //    this.sourceCompGuid = sourceGuid;
            //    this.targetCompGuid = targetguid;
            //    this.sourceIndex = sourceOutputIndex;
            //    this.targetIndex = targetInputIndex;
            //}
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
    }

    public class RemoCompSync : RemoCommand
    {
        public List<Guid> componentGuids;
        public List<string> componentXMLs;
        public List<RemoPartialDoc> partialDocCommands;

        //constructor
        public RemoCompSync() { }
        public RemoCompSync(string issuerID, List<Guid> componentGuids, List<string> componentXMLs, List<RemoPartialDoc> partialDocCommands)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoCompSync;
            this.objectGuid = Guid.Empty;
            this.componentGuids = componentGuids;
            this.componentXMLs = componentXMLs;
            this.partialDocCommands = partialDocCommands;
        }
    }

    public class RemoLock : RemoCommand
    {
        public List<bool> states;
        public List<Guid> guids;
        public int timeSeconds;
        public RemoLock(string issuerID, List<Guid> guids, List<bool> states, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Lock;
            this.objectGuid = Guid.Empty;
            this.states = states;
            this.guids = guids;
            this.timeSeconds = timeSeconds;
            this.commandID = Guid.NewGuid();

        }
        public RemoLock()
        {
            // default constructor
        }

        public override string ToString()
        {
            return string.Format("RemoLock Command from {0}", this.issuerID);
        }
    }

    public class RemoSelect : RemoCommand
    {
        public int timeSeconds;
        public List<Guid> selectionGuids;
        public RemoSelect(string issuerID, List<Guid> objectGuids, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Select;
            this.objectGuid = Guid.Empty;
            this.timeSeconds = timeSeconds;
            this.selectionGuids = objectGuids;
            this.commandID = Guid.NewGuid();

        }
        public RemoSelect()
        {
            // default constructor
        }

        public override string ToString()
        {
            return string.Format("RemoSelect Command from {0}", this.issuerID);
        }
    }

    public class RemoHide : RemoCommand
    {
        public List<bool> states;
        public List<Guid> guids;
        public int timeSeconds;
        public RemoHide(string issuerID, List<Guid> guids, List<bool> states, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Hide;
            this.objectGuid = Guid.Empty;
            this.states = states;
            this.guids = guids;
            this.timeSeconds= timeSeconds;
            this.commandID = Guid.NewGuid();

        }
        public RemoHide()
        {
            // default constructor
        }

        public override string ToString()
        {
            return string.Format("RemoHide Command from {0}", this.issuerID);
        }
    }

    public class RemoParameter : RemoCommand
    {
        public string xml;
        public string persistentDataXML;
        public bool hasPersistentData;
        public RemoParameter() { }
        public RemoParameter(string issuerID, IGH_DocumentObject parameter)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParameter;
            this.objectGuid = parameter.InstanceGuid;
            this.commandID = Guid.NewGuid();
            
            GH_LooseChunk chunk = new GH_LooseChunk(null);
            parameter.Write(chunk);
            this.xml = chunk.Serialize_Xml();

            bool hasPersData = false;
            object persistentData = GetPersistentData(parameter, out hasPersData);

            string persDataXML = "";
            if (hasPersData)
            {
                GH_LooseChunk persDataChunk = new GH_LooseChunk(null);
                object[] arguments = new object[] { persDataChunk };
                InvokeWriteMethod(persistentData, arguments);

                persDataXML = persDataChunk.Serialize_Xml();
            }
            this.persistentDataXML = persDataXML;
            this.hasPersistentData = hasPersData;

        }

        public override string ToString()
        {
            return string.Format("RemoParameter Command from {0}", this.issuerID);
        }

        public static object GetPersistentData(object objectToInspect, out bool hasPersistentData)
        {
            string propertyName = "PersistentData";
            // Use reflection to get the Type of the object
            Type type = objectToInspect.GetType();

            // Get the PropertyInfo object for the specified property
            PropertyInfo propertyInfo = type.GetProperty(propertyName);

            if (propertyInfo != null)
            {
                // Use the PropertyInfo object to get the value of the property
                hasPersistentData = true;
                return propertyInfo.GetValue(objectToInspect);
            }
            else
            {
                // The property was not found on the object
                hasPersistentData = false;
                return null;
            }
        }

        public static bool InvokeWriteMethod(object objectToInvoke, object[] parameters = null)
        {
            string methodName = "Write";
            // Use reflection to get the Type of the object
            Type type = objectToInvoke.GetType();

            // Get the MethodInfo object for the specified method
            MethodInfo methodInfo = type.GetMethod(methodName);

            if (methodInfo != null)
            {
                // Use the MethodInfo object to invoke the method on the object
                methodInfo.Invoke(objectToInvoke, parameters);
                return true;
            }
            return false;
        }

        public static bool InvokeReadMethod(object objectToInvoke, object[] parameters = null)
        {
            string methodName = "Read";
            if (objectToInvoke == null) return false;
            // Use reflection to get the Type of the object
            Type type = objectToInvoke.GetType();

            // Get the MethodInfo object for the specified method
            MethodInfo methodInfo = type.GetMethod(methodName);

            if (methodInfo != null)
            {
                // Use the MethodInfo object to invoke the method on the object
                methodInfo.Invoke(objectToInvoke, parameters);
                return true;
            }
            return false;
        }

        public static bool InvokeWritePersistentDataMethod(object objectToInvoke, object[] parameters = null)
        {
            string methodName = "Write";
            // Use reflection to get the Type of the object
            Type type = objectToInvoke.GetType();

            // Get the MethodInfo object for the specified method
            MethodInfo methodInfo = type.GetMethod(methodName);

            if (methodInfo != null)
            {
                // Use the MethodInfo object to invoke the method on the object
                methodInfo.Invoke(objectToInvoke, parameters);
                return true;
            }
            return false;
        }

        public static bool InvokeReadPersistentDataMethod(object objectToInvoke, object[] parameters = null)
        {
            string methodName = "SetPersistentData";
            // Use reflection to get the Type of the object
            Type type = objectToInvoke.GetType();


            var param_gh = (Param_Brep)objectToInvoke;

            GH_Structure<GH_Brep> persistentData = new GH_Structure<GH_Brep>();


            // Get the MethodInfo object for the specified method
            MethodInfo methodInfo = type.GetMethod(methodName);

            if (methodInfo != null)
            {
                // Use the MethodInfo object to invoke the method on the object
                object[] auguments = new object[] { persistentData };
                methodInfo.Invoke(objectToInvoke, auguments);
                return true;
            }
            return false;
        }

    }

    public class RemoParamSlider : RemoCommand
    {
        // for slider *
        public decimal sliderValue;
        public decimal sliderminBound;
        public decimal slidermaxBound;
        public int decimalPlaces;
        public int sliderType;

        RemoParamSlider() { }
        public RemoParamSlider(string issuerID,
            GH_NumberSlider slider)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoSlider;
            this.objectGuid = slider == null ?Guid.Empty: slider.InstanceGuid;
            this.sliderValue = slider == null ? 0:slider.CurrentValue;
            this.sliderminBound = slider == null ? 0:slider.Slider.Minimum;
            this.slidermaxBound = slider == null ? 0 : slider.Slider.Maximum;
            this.decimalPlaces = slider == null ? 0 : slider.Slider.DecimalPlaces;
            this.sliderType = slider == null ? 0 : (int)slider.Slider.Type;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamSlider Command from {0}", this.issuerID);
        }
    }

    // for buttons
    public class RemoParamButton: RemoCommand
    {
        // for button *
        public bool buttonValue;

        public RemoParamButton() { }
        public RemoParamButton(string issuerID, GH_ButtonObject button
            )
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoButton;
            this.objectGuid = button == null ? Guid.Empty : button.InstanceGuid;
            this.buttonValue = button == null ? false : button.ButtonDown;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamButton Command from {0}", this.issuerID);
        }
    }

    public class RemoParamToggle : RemoCommand
    {
        public RemoParamToggle() { }
        // for toggle *
        public bool toggleValue;
        // for toggles
        public RemoParamToggle(string issuerID, GH_BooleanToggle toggle)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoToggle;
            this.objectGuid = toggle == null ? Guid.Empty : toggle.InstanceGuid;
            this.toggleValue = toggle == null ? false : toggle.Value;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamToggle Command from {0}", this.issuerID);
        }
    }

    // for panel *
    public class RemoParamPanel : RemoCommand
    {
        public bool MultiLine;
        public bool DrawIndecies;
        public bool DrawPaths;
        public bool Wrap;
        public int Alignment;
        public string panelContent;
        public string xmlContent;

        public RemoParamPanel() { }

        // for panels
        public RemoParamPanel(string issuerID, GH_Panel panel)
        {

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            panel.Write(chunk);

            this.issuerID = issuerID;
            this.commandType = CommandType.RemoPanel;
            this.objectGuid = panel == null ? Guid.Empty : panel.InstanceGuid;
            this.xmlContent = chunk.Serialize_Xml();
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamPanel Command from {0}", this.issuerID);
        }
    }

    public class RemoParamColor : RemoCommand
    {
        // for color *
        public int Red;
        public int Green;
        public int Blue;
        public int Alpha;
        public RemoParamColor() { }

        // color
        public RemoParamColor(string issuerID, GH_ColourSwatch colourSwatch)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoColor;
            this.objectGuid = colourSwatch == null ? Guid.Empty : colourSwatch.InstanceGuid;
            this.Red = colourSwatch == null ? 0 : colourSwatch.SwatchColour.R;
            this.Green = colourSwatch == null ? 0 : colourSwatch.SwatchColour.G;
            this.Blue = colourSwatch == null ? 0 : colourSwatch.SwatchColour.B;
            this.Alpha = colourSwatch == null ? 0 : colourSwatch.SwatchColour.A;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamColor Command from {0}", this.issuerID);
        }
    }

    public class RemoParamMDSlider : RemoCommand
    {
        // for md slider
        public double ValueX;
        public double ValueY;
        public double minBoundX;
        public double maxBoundX;
        public double minBoundY;
        public double maxBoundY;
        public RemoParamMDSlider() { }

        public RemoParamMDSlider(string issuerID,
            GH_MultiDimensionalSlider mdSlider, bool approximate)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoMDSlider;
            this.objectGuid = mdSlider == null ? Guid.Empty : mdSlider.InstanceGuid;
            this.ValueX = mdSlider == null ? 0 : approximate ? Math.Round(mdSlider.Value.X,3): mdSlider.Value.X;
            this.ValueY = mdSlider == null ? 0 : approximate ? Math.Round(mdSlider.Value.Y,3) : mdSlider.Value.Y;
            this.minBoundX = mdSlider == null ? 0 : mdSlider.XInterval.Min;
            this.maxBoundX = mdSlider == null ? 0 : mdSlider.XInterval.Max;
            this.minBoundY = mdSlider == null ? 0 : mdSlider.YInterval.Min;
            this.maxBoundY = mdSlider == null ? 0 : mdSlider.YInterval.Max;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamMDSlider Command from {0}", this.issuerID);
        }
    }

    public class RemoParamPoint3d : RemoCommand
    {
        // for point3d
        public List<string> pointsAndTreePath;
        public string pointXML;
        public RemoParamPoint3d() { }
        public RemoParamPoint3d(string issuerID, Param_Point pointComponent, GH_Structure<GH_Point> pntTree, bool approximate)
        {
            
            List<string> pointsAndTreePath = new List<string>();
            foreach (GH_Path path in pntTree.Paths)
            {
                var branch = pntTree.get_Branch(path);
                foreach (var item in branch)
                {
                    GH_Point pnt = (GH_Point)item;
                    string coordPath = string.Format("{0},{1},{2}:{3}"
                        , approximate? Math.Round(pnt.Value.X,3): pnt.Value.X
                        , approximate ? Math.Round(pnt.Value.Y, 3) : pnt.Value.Y
                        , approximate ? Math.Round(pnt.Value.Z, 3) : pnt.Value.Z
                        , path.ToString().Substring(1, path.ToString().Length - 2));

                    pointsAndTreePath.Add(coordPath);
                }
            }

            this.issuerID = issuerID;
            this.commandType = CommandType.RemoPoint3d;
            this.objectGuid = pointComponent.InstanceGuid;
            this.pointsAndTreePath = pointsAndTreePath;
            this.commandID = Guid.NewGuid();

        }

        public RemoParamPoint3d(string issuerID, Param_Point pointComponent)
        {

            GH_LooseChunk chunk = new GH_LooseChunk(null);
            pointComponent.PersistentData.Write(chunk);

            this.issuerID = issuerID;
            this.commandType = CommandType.RemoPoint3d;
            this.objectGuid = pointComponent.InstanceGuid;
            this.pointXML = chunk.Serialize_Xml();
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamPoint3d Command from {0}", this.issuerID);
        }
    }
    public class RemoParamVector3d : RemoCommand
    {
        // for vector3d
        public List<string> vectorsAndTreePath;
        public RemoParamVector3d() { }
        public RemoParamVector3d(string issuerID, Param_Vector vectorComponent, GH_Structure<IGH_Goo> vecTree, bool approximate)
        {

            List<string> vectorsAndTreePath = new List<string>();
            foreach (GH_Path path in vecTree.Paths)
            {
                var branch = vecTree.get_Branch(path);
                foreach (var item in branch)
                {
                    GH_Vector vec = (GH_Vector)item;
                    string coordPath = string.Format("{0},{1},{2}:{3}"
                        , approximate ? Math.Round(vec.Value.X, 3) : vec.Value.X
                        , approximate ? Math.Round(vec.Value.Y, 3) : vec.Value.Y
                        , approximate ? Math.Round(vec.Value.Z, 3) : vec.Value.Z
                        , path.ToString().Substring(1, path.ToString().Length - 2));

                    vectorsAndTreePath.Add(coordPath);
                }
            }

            this.issuerID = issuerID;
            this.commandType = CommandType.RemoVector3d;
            this.objectGuid = vectorComponent.InstanceGuid;
            this.vectorsAndTreePath = vectorsAndTreePath;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamVector3d Command from {0}", this.issuerID);
        }
    }
    public class RemoParamPlane : RemoCommand
    {
        // for plane
        public List<string> planesAndTreePath;

        public RemoParamPlane() { }
        public RemoParamPlane(string issuerID, Param_Plane planeComponent, GH_Structure<IGH_Goo> planeTree, bool approximate)
        {

            List<string> planesAndTreePath = new List<string>();
            foreach (GH_Path path in planeTree.Paths)
            {
                var branch = planeTree.get_Branch(path);
                foreach (var item in branch)
                {
                    GH_Plane plane = (GH_Plane)item;
                    string coordPath = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}:{9}"
                        , approximate ? Math.Round(plane.Value.Origin.X, 3) : plane.Value.Origin.X
                        , approximate ? Math.Round(plane.Value.Origin.Y, 3) : plane.Value.Origin.Y
                        , approximate ? Math.Round(plane.Value.Origin.Z, 3) : plane.Value.Origin.Z

                        , approximate ? Math.Round(plane.Value.XAxis.X, 3) : plane.Value.XAxis.X
                        , approximate ? Math.Round(plane.Value.XAxis.Y, 3) : plane.Value.XAxis.Y
                        , approximate ? Math.Round(plane.Value.XAxis.Z, 3) : plane.Value.XAxis.Z

                        , approximate ? Math.Round(plane.Value.YAxis.X, 3) : plane.Value.YAxis.X
                        , approximate ? Math.Round(plane.Value.YAxis.Y, 3) : plane.Value.YAxis.Y
                        , approximate ? Math.Round(plane.Value.YAxis.Z, 3) : plane.Value.YAxis.Z

                        , path.ToString().Substring(1, path.ToString().Length - 2));

                    planesAndTreePath.Add(coordPath);
                }
            }

            this.issuerID = issuerID;
            this.commandType = CommandType.RemoPlane;
            this.objectGuid = planeComponent.InstanceGuid;
            this.planesAndTreePath = planesAndTreePath;
            this.commandID = Guid.NewGuid();

        }

        public override string ToString()
        {
            return string.Format("RemoParamPlane Command from {0}", this.issuerID);
        }
    }

    public class RemoCanvasView : RemoCommand
    {
        public string canvasViewport;
        public RemoCanvasView() { }
        public RemoCanvasView(string issuerID, string canvasViewport)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.CanvasViewport;
            this.canvasViewport = canvasViewport;
            this.commandID = Guid.NewGuid();
        }
    }

    public class RemoMove : RemoCommand
    {
        //public float moveX;
        //public float moveY;
        public Guid translationGuid;
        public List<Guid> moveGuids= new List<Guid>();
        public Size vector;
        public string relayXMLContent = "";
        public RemoMove(string issuerID, List<Guid> moveGuids, System.Drawing.Size vector)
        {
            this.issuerID = issuerID;
            this.objectGuid = Guid.Empty;
            this.translationGuid = Guid.NewGuid();
            this.commandType = CommandType.MoveComponent;
            this.moveGuids = moveGuids;
            this.vector = vector;
            this.commandID = Guid.NewGuid();

            //this.objXs = objXs;
            //this.objYs = objYs;
        }
        public RemoMove(string issuerID, List<Guid> moveGuids, System.Drawing.Size vector, string relayXMLContent)
        {
            this.issuerID = issuerID;
            this.objectGuid = Guid.Empty;
            this.translationGuid = Guid.NewGuid();
            this.commandType = CommandType.MoveComponent;
            this.moveGuids = moveGuids;
            this.relayXMLContent = relayXMLContent;
            this.commandID = Guid.NewGuid();

            //this.objXs = objXs;
            //this.objYs = objYs;
        }
        public RemoMove()
        {
            // default constructor
        }

        public override string ToString()
        {
            return string.Format("RemoMove Command from {0}", this.issuerID);
        }
    }

    public class RemoCanvasSync : RemoCommand
    {
        public string xmlString = "";

        public RemoCanvasSync() { }

        public RemoCanvasSync(string issuerID, string xmlString)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoCanvasSync;
            this.objectGuid = Guid.Empty;
            this.xmlString = xmlString;
            this.commandID = Guid.NewGuid();

        }

    }

    //public class WireHistory : RemoCommand
    //{
    //    public List<WireConnection> wireHistory;

    //    public WireHistory() { }
    //    public WireHistory(string issuerID, IGH_DocumentObject component)
    //    {
    //        this.issuerID = issuerID;
    //        this.commandType = CommandType.WireHistory;
    //        this.objectGuid = Guid.Empty;
    //        this.commandID = Guid.NewGuid();


    //        List<WireConnection> wireHistory = new List<WireConnection>();

    //        if (component is IGH_Param)
    //        {
    //            wireHistory.Add(new WireConnection((IGH_Param)component));
    //            this.wireHistory = wireHistory;
    //        }
    //        else
    //        {
    //            IGH_Component gh_component = (IGH_Component)component;
    //            foreach (IGH_Param item in gh_component.Params.Input)
    //            {
    //                wireHistory.Add(new WireConnection(item));
    //            }
    //            this.wireHistory = wireHistory;
    //        }

    //    }
    //}
    
    //public class WireConnection
    //{
    //    public int inputIndex;
    //    public List<Guid> sourceGuids;
    //    public List<int> sourceIndecies;

    //    public WireConnection() { }
    //    public WireConnection(IGH_Param input) 
    //    {
            
            
    //        List<Guid> sourceGuids = new List<Guid>();
    //        List<int> sourceIndecies = new List<int>();

    //        var inputs = input.Sources;
    //        for (int i = 0; i < inputs.Count; i++)
    //        {
    //            IGH_Param output= inputs[i];

    //            if (output.Attributes.Parent == null)
    //            {
    //                sourceGuids.Add(output.InstanceGuid);
    //                sourceIndecies.Add(-1);
    //            }
    //            else
    //            {
    //                sourceGuids.Add(output.Attributes.Parent.DocObject.InstanceGuid);
    //                IGH_Component parent = (IGH_Component)output.Attributes.Parent.DocObject;
    //                sourceIndecies.Add(parent.Params.Output.IndexOf(output));
    //            }
    //        }

    //        this.sourceGuids= sourceGuids;
    //        this.sourceIndecies = sourceIndecies;

    //        if (input.Attributes.Parent == null) this.inputIndex = -1;
    //        else
    //        {
    //            IGH_Component parent = (IGH_Component)input.Attributes.Parent.DocObject;
    //            this.inputIndex = parent.Params.Input.IndexOf(input);
    //        }

    //    }
    //}

    public class WireHistory : RemoCommand
    {
        public Guid componentGuid;
        public string wireHistoryXml;
        public Dictionary<int, List<Guid>> inputGuidsDictionary; // inputIndex, sourceGuids

        public WireHistory() { }
        public WireHistory(Guid componentGuid, string wireHistoryXml)
        {
            this.componentGuid = componentGuid;
            this.wireHistoryXml = wireHistoryXml;
            this.inputGuidsDictionary = null;
        }
        public WireHistory(GhPython.Component.ZuiPythonComponent pythonComponent)
        {
            this.componentGuid = pythonComponent.InstanceGuid;
            inputGuidsDictionary = new Dictionary<int, List<Guid>>();
            for (int i = 0; i < pythonComponent.Params.Input.Count; i++)
            {
                var input = pythonComponent.Params.Input[i];
                List<Guid> sourceGuids = new List<Guid>();
                foreach (var source in input.Sources)
                {
                    
                    sourceGuids.Add(source.InstanceGuid);
                  
                }
                inputGuidsDictionary.Add(i, sourceGuids);
            }
            this.wireHistoryXml = "";
        }
    }

}
