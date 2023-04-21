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
        RemoParam = 7, // done
        Select = 8,
        StreamGeom = 9
    }
    public enum RemoParamType
    {
        None = 0,
        Slider = 1, // done
        Button = 2, // done
        Toggle = 3, // done
        Panel = 4, // done
        Color = 5, // done
        MDSlider = 6
    }
    public enum RemoConnectType
    {
        None = 0,
        Add = 1,
        Replace = 2,
        Remove = 3
    }

    public enum ButtonState
    {
        ButtonDown = 1,
        ButtonUp = 0
    }
    public enum ToggleState
    {
        ToggleTrue = 1,
        ToggleFalse = 0
    }

    abstract public class RemoCommand
    {
        public string issuerID;
        public CommandType commandType;
        public Guid objectGuid;

        public static string SerializeToJson(RemoCommand command)
        {
            return JsonConvert.SerializeObject(command,Formatting.Indented);
            string pause = "";
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
            case (int) CommandType.RemoParam:
            
                remoCommand = JsonConvert.DeserializeObject<RemoParam>(commandJson);
                
                    break;
            case (int) CommandType.StreamGeom:
            
                return null;
                
                    break;
            case (int) CommandType.NullCommand:
            
                return null;
                
                    break;
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

    public class RemoParam : RemoCommand
    {
        public RemoParamType remoParamType;
        // for button *
        public bool buttonValue;
        // for toggle *
        public bool toggleValue;
        // for panel *
        public bool panelMultiLine;
        public bool panelDrawIndecies;
        public bool panelDrawPaths;
        public bool panelWrap;
        public int panelAlignment;
        public string panelContent;
        // for color *
        public int colorRed;
        public int colorGreen;
        public int colorBlue;
        public int colorAlpha;
        // for slider *
        public decimal sliderValue;
        public decimal sliderminBound;
        public decimal slidermaxBound;
        public int sliderAccuracy;
        public int sliderType;


        public RemoParam()
        {
            // default constructor
        }

        // for buttons
        public RemoParam(string issuerID, Guid objectGuid,
            ButtonState buttonState
            )
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParam;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Button;
            this.buttonValue = Convert.ToBoolean(buttonState);
        }
        // for toggles
        public RemoParam(string issuerID, Guid objectGuid,
            ToggleState toggleState
            )
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParam;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Toggle;
            this.toggleValue = Convert.ToBoolean(toggleState);
        }
        // for panels
        public RemoParam(string issuerID, Guid objectGuid,
        bool panelMultiLine, bool panelDrawIndecies,
        bool panelDrawPaths, bool panelWrap,
        int panelAlignment, string panelContent)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParam;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Panel;
            this.panelMultiLine = panelMultiLine;
            this.panelDrawIndecies = panelDrawIndecies;
            this.panelDrawPaths = panelDrawPaths;
            this.panelWrap = panelWrap;
            this.panelAlignment = panelAlignment;
            this.panelContent = panelContent;
        }

        // slider
        public RemoParam(string issuerID, Guid objectGuid,
            decimal sliderminBound, decimal slidermaxBound, decimal sliderValue,
            int sliderAccuracy, GH_SliderAccuracy sliderType)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParam;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Slider;
            this.sliderValue = sliderValue;
            this.sliderminBound = sliderminBound;
            this.slidermaxBound = slidermaxBound;
            this.sliderAccuracy = sliderAccuracy;
            this.sliderType = (int)sliderType;
        }

        // color
        public RemoParam(string issuerID, Guid objectGuid,
            int colorRed, int colorGreen, int colorBlue, int colorAlpha)
        {
            this.issuerID = issuerID;
            this.commandType = CommandType.RemoParam;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Color;
            this.colorRed = colorRed;
            this.colorGreen = colorGreen;
            this.colorBlue = colorBlue;
            this.colorAlpha = colorAlpha;
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
