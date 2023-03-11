using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoSharp.Utilities
{

    public enum CommandType 
    {
        NullCommand = 0,
        MoveComp = 1,
        RemoCreate = 2,
        RemoDelete = 3,
        RemoLock = 4,
        RemoConnect = 5,
        RemoParam = 6,
        RemoSelect = 7,
    }
    public enum RemoParamType
    {
        None = 0,
        Slider = 1,
        Button = 2,
        Toggle = 3,
        Panel = 4,
        Color = 5,
        MDSlider = 6,
    }


    abstract public class RemoCommand
    {
        public string issuerID;
        public CommandType commandType;
        public Guid objectGuid;

    }

    public class RemoCreate : RemoCommand
    {
        string componentType;
        int X;
        int Y;
        string specialParameters;

        // a constructor for general components
        public RemoCreate(string issuerID, CommandType commandType, Guid objectGuid,
            string componentType,
            int X,
            int Y)
        {
            this.issuerID = issuerID;
            this.commandType = commandType;
            this.objectGuid= objectGuid;
            this.componentType = componentType;
            this.X = X;
            this.Y = Y;
        }

        // a constructor for special components like sliders, panels, etc.
        public RemoCreate(string issuerID, CommandType commandType, Guid objectGuid,
            string componentType,
            int X,
            int Y,
            string specialParameters
            )
        {
            this.issuerID = issuerID;
            this.commandType = commandType;
            this.componentType = componentType;
            this.objectGuid = objectGuid;
            this.X = X;
            this.Y = Y;
            this.specialParameters = specialParameters;
        }

    }

    public class RemoParam : RemoCommand
    {
        public RemoParamType remoParamType;
        // for button
        public bool buttonValue;
        // for toggle
        public bool toggleValue;
        // for panel
        public bool panelMultiLine;
        public bool panelDrawIndecies;
        public bool panelDrawPaths;
        public bool panelWrap;
        public int panelAlignment;
        public string panelContent;
        // for color
        public int colorRed;
        public int colorGreen;
        public int colorBlue;
        public int colorAlpha;
        // for slider
        public decimal sliderValue;
        public decimal sliderminBound;
        public decimal slidermaxBound;
        public int sliderAccuracy;
        public int sliderType;

        public RemoParam(string issuerID, CommandType commandType, Guid objectGuid,
            bool buttonValue
            )
        {
            this.issuerID = issuerID;
            this.commandType = commandType;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Button;
            this.buttonValue = buttonValue;
        }
        public RemoParam(string issuerID, CommandType commandType, Guid objectGuid,
            bool toggleValue, bool isToggle
            )
        {
            this.issuerID = issuerID;
            this.commandType = commandType;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Toggle;
            this.toggleValue = toggleValue;
        }

        public RemoParam(string issuerID, CommandType commandType, Guid objectGuid,
        bool panelMultiLine,
        bool panelDrawIndecies,
        bool panelDrawPaths,
        bool panelWrap,
        int panelAlignment,
        string panelContent)
        { 
            this.issuerID = issuerID;
            this.commandType = commandType;
            this.objectGuid = objectGuid;
            this.remoParamType = RemoParamType.Panel;
            this.panelMultiLine = panelMultiLine;
            this.panelDrawIndecies= panelDrawIndecies;
            this.panelDrawPaths= panelDrawPaths;
            this.panelWrap = panelWrap;
            this.panelAlignment= panelAlignment;
            this.panelContent= panelContent;
        }
    }
    public class RemoConnect : RemoCommand 
    {
        public RemoConnect()
        {

        }
    }
}
