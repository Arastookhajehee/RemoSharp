using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using System.Linq;
using Rhino.Geometry;
using Machina;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RemoSharp.RT_Robotics
{
    public enum ActionType
    {
        Undefined,
        Translation,
        Rotation,
        Transformation,
        Axes,
        Message,
        Wait,
        Speed,
        Acceleration,
        Precision,
        MotionMode,
        Coordinates,
        PushPop,
        Comment,
        DefineTool,
        AttachTool,
        DetachTool,
        IODigital,
        IOAnalog,
        Temperature,
        Extrusion,
        ExtrusionRate,
        Initialization,
        ExternalAxis,
        CustomCode,
        ArmAngle,
        ArcMotion,
        RG6Gripper
    }

    public class Deserialize_Machina_Actions : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Deserialize_Machina_Actions class.
        /// </summary>
        public Deserialize_Machina_Actions()
          : base("ReadMachinaStream", "Read_M_Acts",
              "Converts a Machina Action Streamfrom text to actual actions",
              "RemoSharp", "RT Robotics")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Action_Stream", "stream", "Action Stream Text", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Action_List", "Act", "Robot ExMachina Action Lists", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Action_List2", "Act2", "Robot ExMachina Action Lists", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Action_List3", "Act3", "Robot ExMachina Action Lists", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string jsonString = "";
            DA.GetData(0, ref jsonString);

            var typeList = new List<int>();
            var actionNames = new List<string>();
            var actionList = new List<Machina.Action>();

            JArray actJsonList = JArray.Parse(jsonString);
            foreach (var item in actJsonList)
            {
                JObject data = (JObject)item;
                var typeData = data.GetValue("Type");
                var typeDataString = data.GetValue("$type");
                int typeIndex = Convert.ToInt32(typeData);
                actionNames.Add(typeDataString.ToString().Split(',')[0]);
                switch (typeIndex)
                {
                    case 0:
                        // Action is Undefined
                        actionList.Add(null);
                        break;
                    case 1:
                        JObject jTrans = (JObject)data.GetValue("translation");
                        bool relT = Convert.ToBoolean(data.GetValue("relative"));
                        double tX = Convert.ToDouble(jTrans.GetValue("X"));
                        double tY = Convert.ToDouble(jTrans.GetValue("Y"));
                        double tZ = Convert.ToDouble(jTrans.GetValue("Z"));
                        Machina.ActionTranslation at = new ActionTranslation(tX, tY , tZ, relT);
                        actionList.Add(at);
                        break;
                    case 2:
                        JObject jRotation = (JObject) data.GetValue("rotation");
                        bool rel = Convert.ToBoolean(data.GetValue("relative"));
                        JObject Q = (JObject)jRotation.GetValue("Q");
                        double W = Convert.ToDouble(Q.GetValue("W"));
                        double X = Convert.ToDouble(Q.GetValue("X"));
                        double Y = Convert.ToDouble(Q.GetValue("Y"));
                        double Z = Convert.ToDouble(Q.GetValue("Z"));
                        Machina.Types.Geometry.Quaternion quaternion = new Machina.Types.Geometry.Quaternion(W, X, Y, Z);
                        var axisAngle = quaternion.ToAxisAngle();
                        Machina.Types.Geometry.Rotation rotation = new Machina.Types.Geometry.Rotation(axisAngle.Axis, axisAngle.Angle);
                        ActionRotation ar = new ActionRotation(rotation, rel);
                        actionList.Add(ar);
                        break;
                    case 3:

                        bool translationFirst = Convert.ToBoolean(data.GetValue("translationFirst"));
                        JObject trTrans = (JObject)data.GetValue("translation");
                        bool trRel = Convert.ToBoolean(data.GetValue("relative"));
                        double trTX = Convert.ToDouble(trTrans.GetValue("X"));
                        double trTY = Convert.ToDouble(trTrans.GetValue("Y"));
                        double trTZ = Convert.ToDouble(trTrans.GetValue("Z"));
                        Machina.ActionTranslation trAt = new ActionTranslation(trTX, trTY, trTZ, trRel);

                        JObject trJ_Rotation = (JObject)data.GetValue("rotation");
                        JObject trQ = (JObject)trJ_Rotation.GetValue("Q");
                        double trRW = Convert.ToDouble(trQ.GetValue("W"));
                        double trRX = Convert.ToDouble(trQ.GetValue("X"));
                        double trRY = Convert.ToDouble(trQ.GetValue("Y"));
                        double trRZ = Convert.ToDouble(trQ.GetValue("Z"));
                        Machina.Types.Geometry.Quaternion trQuaternion = new Machina.Types.Geometry.Quaternion(trRW, trRX, trRY, trRZ);
                        var trAxisAngle = trQuaternion.ToAxisAngle();
                        Machina.Types.Geometry.Rotation trRotation = new Machina.Types.Geometry.Rotation(trAxisAngle.Axis, trAxisAngle.Angle);

                        Machina.ActionTransformation transformation = new ActionTransformation(trAt.translation, trRotation, trRel, translationFirst);
                        actionList.Add(transformation);
                        break;
                    case 4:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionAxes>
                                (data.ToString())
                                );
                        break;
                    case 5:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionMessage>
                                (data.ToString())
                                );
                        break;
                    case 6:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionWait>
                                (data.ToString())
                                );
                        break;
                    case 7:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionSpeed>
                                (data.ToString())
                                );
                        break;
                    case 8:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionAcceleration>
                                (data.ToString())
                                );
                        break;
                    case 9:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionPrecision>
                                (data.ToString())
                                );
                        break;
                    case 10:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionMotionMode>
                                (data.ToString())
                                );
                        break;
                    case 11:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionCoordinates>
                                (data.ToString())
                                );
                        break;
                    case 12:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionPushPop>
                                (data.ToString())
                                );
                        break;
                    case 13:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionComment>
                                (data.ToString())
                                );
                        break;
                    case 14:

                        JObject tool = (JObject)data.GetValue("tool");
                        JObject TCPPosition = (JObject)tool.GetValue("TCPPosition");
                        JObject TCPOrientation = (JObject)tool.GetValue("TCPOrientation");
                        JObject TCPOrientationXAxis = (JObject)TCPOrientation.GetValue("XAxis");
                        JObject TCPOrientationYAxis = (JObject)TCPOrientation.GetValue("YAxis");
                        JObject CenterOfGravity = (JObject)tool.GetValue("CenterOfGravity");

                        string toolName = tool.GetValue("name").ToString();
                        double Weight = Convert.ToDouble(tool.GetValue("Weight"));

                        double tcpX = Convert.ToDouble(TCPPosition.GetValue("X"));
                        double tcpY = Convert.ToDouble(TCPPosition.GetValue("Y"));
                        double tcpZ = Convert.ToDouble(TCPPosition.GetValue("Z"));

                        double orient_XAxis_X = Convert.ToDouble(TCPOrientationXAxis.GetValue("X"));
                        double orient_XAxis_Y = Convert.ToDouble(TCPOrientationXAxis.GetValue("Y"));
                        double orient_XAxis_Z = Convert.ToDouble(TCPOrientationXAxis.GetValue("Z"));

                        double orient_YAxis_X = Convert.ToDouble(TCPOrientationYAxis.GetValue("X"));
                        double orient_YAxis_Y = Convert.ToDouble(TCPOrientationYAxis.GetValue("Y"));
                        double orient_YAxis_Z = Convert.ToDouble(TCPOrientationYAxis.GetValue("Z"));

                        double cog_X = Convert.ToDouble(CenterOfGravity.GetValue("X"));
                        double cog_Y = Convert.ToDouble(CenterOfGravity.GetValue("Y"));
                        double cog_Z = Convert.ToDouble(CenterOfGravity.GetValue("Z"));

                        Machina.ActionDefineTool defineTool = new ActionDefineTool
                            (toolName, tcpX,tcpY,tcpZ,
                            orient_XAxis_X, orient_XAxis_Y, orient_XAxis_Z,
                            orient_YAxis_X, orient_YAxis_Y, orient_YAxis_Z,
                            Weight, cog_X, cog_Y, cog_Z);

                        actionList.Add(defineTool);

                        break;
                    case 15:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionAttachTool>
                                (data.ToString())
                                );
                        break;
                    case 16:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionDetachTool>
                                (data.ToString())
                                );
                        break;
                    case 17:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionIODigital>
                                (data.ToString())
                                );
                        break;
                    case 18:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionIOAnalog>
                                (data.ToString())
                                );
                        break;
                    case 19:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionTemperature>
                                (data.ToString())
                                );
                        break;
                    case 20:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionExtrusion>
                                (data.ToString())
                                );
                        break;
                    case 21:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionExtrusionRate>
                                (data.ToString())
                                );
                        break;
                    case 22:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionInitialization>
                                (data.ToString())
                                );
                        break;
                    case 23:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionExternalAxis>
                                (data.ToString())
                                );
                        break;
                    case 24:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionCustomCode>
                                (data.ToString())
                                );
                        break;
                    case 25:
                            actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionArmAngle>
                                (data.ToString())
                                );
                        
                        break;
                    case 26:
                        actionList.Add(
                                Newtonsoft.Json.JsonConvert.DeserializeObject
                                <Machina.ActionArcMotion>
                                (data.ToString())
                                );
                        break;
                    case 27:

                        ActionRG6Gripper gripperAction = JsonConvert.DeserializeObject<ActionRG6Gripper>(data.ToString());

                        actionList.Add(gripperAction);
                        break;
                    case 28:
                        //actionList.Add(
                        //        Newtonsoft.Json.JsonConvert.DeserializeObject
                        //        <Machina.ActionExternalAxis>
                        //        (data.ToString())
                        //        );
                        break;
                    case 29:
                        //actionList.Add(
                        //        Newtonsoft.Json.JsonConvert.DeserializeObject
                        //        <Machina.ActionExternalAxis>
                        //        (data.ToString())
                        //        );
                        break;
                    case 30:
                        //actionList.Add(
                        //        Newtonsoft.Json.JsonConvert.DeserializeObject
                        //        <Machina.ActionExternalAxis>
                        //        (data.ToString())
                        //        );
                        break;


                    default:
                        break;
                }

                typeList.Add(typeIndex);
            }




            //string subList = jsonString.Substring(1, jsonString.Length - 2);
            //char[] splitters = { '}', ',', '{' };
            //string[] strArray = subList.Split(splitters);
            //List<string> A = new List<string>();
            //A.AddRange(strArray);
            //A = Newtonsoft.Json.Linq.JArray.Parse(jsonString);

            // List < Machina.Action> actionList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Machina.Action>>(jsonString);
            DA.SetDataList(0, actionList);
            //DA.SetDataList(1, typeList);
            //DA.SetDataList(2, actionNames);
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
                return RemoSharp.Properties.Resources.ReadMachinaStream.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("641EB333-C4A8-47F0-8DFA-72EF65F1FAA3"); }
        }
    }
}