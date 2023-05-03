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

namespace RemoSharp.RemoCommandTypes
{

    public enum CommandType 
    {
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
        RemoPlane = 18
    }
    //public enum RemoParamType
    //{
    //    None = 0,
        
    //}
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

        public static string SerializeToJson(RemoCommand command)
        {
            return JsonConvert.SerializeObject(command,Formatting.Indented);
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

                case (int) CommandType.WireConnection:
            
                remoCommand = JsonConvert.DeserializeObject<RemoConnect>(commandJson);

                    break;
            case (int) CommandType.Delete:
            
                remoCommand = JsonConvert.DeserializeObject<RemoDelete>(commandJson);
                
                    break;
            case (int) CommandType.Hide:
            
                remoCommand = JsonConvert.DeserializeObject<RemoHide>(commandJson);
                
                    break;
            case (int) CommandType.Select:
            
                remoCommand = JsonConvert.DeserializeObject<RemoSelect>(commandJson);
                
                    break;
                case (int) CommandType.MoveComponent:

                remoCommand = JsonConvert.DeserializeObject<RemoMove>(commandJson);
                    break;
            case (int) CommandType.Create:
            
                remoCommand = JsonConvert.DeserializeObject<RemoCreate>(commandJson);
                
                    break;
            case (int) CommandType.Lock:
            
                remoCommand = JsonConvert.DeserializeObject<RemoLock>(commandJson);
                
                    break;
                case (int) CommandType.RemoSlider:
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

                case (int) CommandType.StreamGeom:
            
                return null;
                    //break;
            case (int) CommandType.NullCommand:
            
                return null;
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
        }
        public RemoNullCommand()
        {
            // default constructor
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

        }
    }

    public class RemoConnect : RemoCommand
    {
        public int sourceOutput = -1;
        public int targetInput = -1;
        public bool isSourceSpecial = false;
        public bool isTargetSpecial = false;
        public Guid sourceObjectGuid = Guid.Empty;
        public Guid targetObjectGuid = Guid.Empty;
        public RemoConnectType RemoConnectType = RemoConnectType.None;

        public RemoConnect()
        {
            // default constructor
        }

        public RemoConnect(string issuerID, Guid sourceObjectGuid, Guid targetObjectGuid,
            int sourceOutput, int targetInput, bool isSourceSpecial, bool isTargetSpecial,
            RemoConnectType remoConnectType)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.WireConnection;
            this.RemoConnectType = remoConnectType;
            this.sourceObjectGuid= sourceObjectGuid;
            this.targetObjectGuid= targetObjectGuid;
            this.sourceOutput = sourceOutput;
            this.targetInput = targetInput;
            this.isSourceSpecial = isSourceSpecial;
            this.isTargetSpecial = isTargetSpecial;           

        }
    }

    public class RemoCreate : RemoCommand
    {
        public string componentType;
        public int X;
        public int Y;
        public bool isSpecial;
        public string specialParameters;

        public RemoCreate()
        {
            // default constructor
        }
        // a constructor for general components
        public RemoCreate(string issuerID, Guid objectGuid,
            string componentType,
            int X,
            int Y)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Create;
            this.objectGuid= objectGuid;
            this.componentType = componentType;
            this.X = X;
            this.Y = Y;
            this.isSpecial= false;
        }

        // a constructor for special components like sliders, panels, etc.
        public RemoCreate(string issuerID, Guid objectGuid,
            string componentType,
            int X,
            int Y,
            string specialParameters
            )
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Create;
            this.componentType = componentType;
            this.objectGuid = objectGuid;
            this.X = X;
            this.Y = Y;
            this.isSpecial= true;
            this.specialParameters = specialParameters;
        }

    }

    public class RemoDelete : RemoCommand
    {
        public RemoDelete(string issuerID, Guid objectGuid)
        {
            this.issuerID= issuerID;
            this.commandType= CommandType.Delete;
            this.objectGuid= objectGuid;
        }
        public RemoDelete()
        {
            // default constructor
        }
    }

    public class RemoLock : RemoCommand
    {
        public bool state;
        public int timeSeconds;
        public RemoLock(string issuerID, Guid objectGuid,bool state, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Lock;
            this.objectGuid = objectGuid;
            this.state = state;
            this.timeSeconds = timeSeconds;
        }
        public RemoLock()
        {
            // default constructor
        }
    }

    public class RemoSelect : RemoCommand
    {
        public int timeSeconds;
        public RemoSelect(string issuerID, Guid objectGuid, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Lock;
            this.objectGuid = objectGuid;
            this.timeSeconds = timeSeconds;
        }
        public RemoSelect()
        {
            // default constructor
        }
    }

    public class RemoHide : RemoCommand
    {
        public bool state;
        public bool hidable;
        public int timeSeconds;
        public RemoHide(string issuerID, Guid objectGuid, bool state, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.Hide;
            this.objectGuid = objectGuid;
            this.state = state;
            this.timeSeconds= timeSeconds;
        }
        public RemoHide()
        {
            // default constructor
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
            this.objectGuid = slider.InstanceGuid;
            this.sliderValue = slider.CurrentValue;
            this.sliderminBound = slider.Slider.Minimum;
            this.slidermaxBound = slider.Slider.Maximum;
            this.decimalPlaces = slider.Slider.DecimalPlaces;
            this.sliderType = (int)slider.Slider.Type;
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
            this.objectGuid = button.InstanceGuid;
            this.buttonValue = button.ButtonDown;
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
            this.objectGuid = toggle.InstanceGuid;
            this.toggleValue = toggle.Value;
        }

    }

    // for panel *
    public class RemoParamPanel : RemoCommand
    {
        public bool panelMultiLine;
        public bool panelDrawIndecies;
        public bool panelDrawPaths;
        public bool panelWrap;
        public int panelAlignment;
        public string panelContent;

        public RemoParamPanel() { }

        // for panels
        public RemoParamPanel(string issuerID, GH_Panel panel)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoPanel;
            this.objectGuid = panel.InstanceGuid;
            this.panelMultiLine = panel.Properties.Multiline;
            this.panelDrawIndecies = panel.Properties.DrawIndices;
            this.panelDrawPaths = panel.Properties.DrawPaths;
            this.panelWrap = panel.Properties.Wrap;
            this.panelAlignment = (int) panel.Properties.Alignment;
            this.panelContent = panel.UserText;
        }
    }

    public class RemoParamColor : RemoCommand
    {
        // for color *
        public int colorRed;
        public int colorGreen;
        public int colorBlue;
        public int colorAlpha;
        public RemoParamColor() { }

        // color
        public RemoParamColor(string issuerID, GH_ColourSwatch colourSwatch)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoColor;
            this.objectGuid = colourSwatch.InstanceGuid;
            this.colorRed = colourSwatch.SwatchColour.R;
            this.colorGreen = colourSwatch.SwatchColour.G;
            this.colorBlue = colourSwatch.SwatchColour.B;
            this.colorAlpha = colourSwatch.SwatchColour.A;
        }
    }

    public class RemoParamMDSlider : RemoCommand
    {
        // for md slider
        public double mdSliderValueX;
        public double mdSliderValueY;
        public double mdSliderminBoundX;
        public double mdSlidermaxBoundX;
        public double mdSliderminBoundY;
        public double mdSlidermaxBoundY;
        public RemoParamMDSlider() { }

        public RemoParamMDSlider(string issuerID,
            GH_MultiDimensionalSlider mdSlider, bool approximate)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoMDSlider;
            this.objectGuid = mdSlider.InstanceGuid;
            this.mdSliderValueX = approximate ? Math.Round(mdSlider.Value.X,3): mdSlider.Value.X;
            this.mdSliderValueY = approximate ? Math.Round(mdSlider.Value.Y,3) : mdSlider.Value.Y;
            this.mdSliderminBoundX = mdSlider.XInterval.Min;
            this.mdSlidermaxBoundX = mdSlider.XInterval.Max;
            this.mdSliderminBoundY = mdSlider.YInterval.Min;
            this.mdSlidermaxBoundY = mdSlider.YInterval.Max;
        }
    }

    public class RemoParamPoint3d : RemoCommand
    {
        // for point3d
        public List<string> points_and_TreePath;
        public RemoParamPoint3d() { }
        public RemoParamPoint3d(string issuerID, Param_Point pointComponent, GH_Structure<IGH_Goo> pntTree, bool approximate)
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
            this.points_and_TreePath = pointsAndTreePath;
        }
    }
    public class RemoParamVector3d : RemoCommand
    {
        // for vector3d
        public List<string> vectors_and_TreePath;
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
            this.commandType = CommandType.RemoPoint3d;
            this.objectGuid = vectorComponent.InstanceGuid;
            this.vectors_and_TreePath = vectorsAndTreePath;
        }
    }
    public class RemoParamPlane : RemoCommand
    {
        // for plane
        public List<string> planes_and_TreePath;

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
            this.commandType = CommandType.RemoPoint3d;
            this.objectGuid = planeComponent.InstanceGuid;
            this.planes_and_TreePath = planesAndTreePath;
        }

    }

    public class RemoMove : RemoCommand
    {
        public float moveX;
        public float moveY;
        public int timeSeconds;
        public RemoMove(string issuerID, Guid objectGuid, float moveX, float moveY, int timeSeconds)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.MoveComponent;
            this.objectGuid = objectGuid;
            this.moveX = moveX;
            this.moveY = moveY;
            this.timeSeconds = timeSeconds;
        }
        public RemoMove()
        {
            // default constructor
        }
    }


    

}
