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


namespace RemoSharp
{
    public class RemoCommands : GH_Component
    {

        
        private string currentXMLString = "";
        private int otherCompInx = -1;
        public int deletionIndex = -1;

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

        //RemoExecutor (connector and Creation) public persistent variables
        public int srcComp = -1;
        public int tgtComp = -1;
        public int srcCompOutputIndex = -1;
        public int tgtCompInputIndex = -1;
        public int connectionMouseDownX = -1;
        public int connectionMouseDownY = -1;
        public int connectionMouseUpX = -1;
        public int connectionMouseUpY = -1;

        

        /// <summary>
        /// Initializes a new instance of the RemoCommands class.
        /// </summary>
        public RemoCommands()
          : base("RemoCommands", "RemoCmds",
              "Excecution of Remote Commands for all manipulations from the client side remotely.",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("DistroCommand", "DstCmd", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.list,new List<string> {""});
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> commandList = new List<string>();
            string command = "";
            if(!DA.GetDataList<string>(0, commandList)) return;

            if (commandList.Count > 1)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "This Component Accepts Only a Single Input." + Environment.NewLine
                    + "Make Sure Only One Wire With A Single Text Block Command is Connected.");
            }
            else
            {
                command = commandList[0];
            }

            if (command == null || command == "") return;
            string substring = command.Substring(0, 5);
            if (substring.Equals("<?xml"))
            {
                if (command.Equals(currentXMLString)) return;
                Exception threadEx = null;
                Thread staThread = new Thread(
                  delegate ()
                  {
                      try
                      {
                          System.Windows.Forms.Clipboard.SetText(command);
                      }
                      catch (Exception ex)
                      {
                          threadEx = ex;
                      }
                  });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
                InputSimulator sim = new InputSimulator();
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, new[] { VirtualKeyCode.VK_V });
                currentXMLString = command;
                return;
            }
            else
            {
                string[] cmds = command.Split(',');



                if (cmds[0] == "MoveComp")
                {
                    int compX = Convert.ToInt32(cmds[1]);
                    int compY = Convert.ToInt32(cmds[2]);
                    int trsX = Convert.ToInt32(cmds[3]);
                    int trsY = Convert.ToInt32(cmds[4]);

                    int otherCompInx = MoveCompFindComponentOnCanvasByCoordinates(compX, compY);


                    for (int i = 0; i < 21; i++)
                    {
                        double ratio = (double)i / 20.0;
                        int offsetSrcComp = RemoConnectFindComponentOnCanvasByCoordinates(compX - Convert.ToInt32(200 * ratio), compY);
                        var offsetComp = this.OnPingDocument().Objects[offsetSrcComp];
                        bool inputIsSlider = offsetComp.ToString().Equals("Grasshopper.Kernel.Special.GH_NumberSlider");
                        bool inputIsDigitScroller = offsetComp.ToString().Equals("Grasshopper.Kernel.Special.GH_DigitScroller");
                        if (inputIsSlider || inputIsDigitScroller)
                        {
                            otherCompInx = offsetSrcComp;
                        }
                    }

                    var otherComp = this.OnPingDocument().Objects[otherCompInx];

                    GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

                    grip.CreateObjectData(otherComp);
                    this.OnPingDocument().Select(grip);

                    Size vec = new Size(trsX, trsY);

                    this.OnPingDocument().TranslateObjects(vec, true);
                    this.OnPingDocument().DeselectAll();
                    return;
                }

                if (cmds[0] == "RemoCreate")
                {

                    string typeName = cmds[1];
                    int pivotX = Convert.ToInt32(cmds[2]);
                    int pivotY = Convert.ToInt32(cmds[3]);

                    try
                    {
                        if (typeName.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                        {

                            decimal minBound = Convert.ToDecimal(cmds[4]); 
                            decimal maxBound = Convert.ToDecimal(cmds[5]); 
                            decimal currentValue = Convert.ToDecimal(cmds[6]); 
                            int accuracy = Convert.ToInt32(cmds[7]);
                            GH_SliderAccuracy acc = (GH_SliderAccuracy)Enum.Parse(typeof(GH_SliderAccuracy), cmds[8]);
                            GH_NumberSlider sliderComponent = new GH_NumberSlider();
                            sliderComponent.CreateAttributes();
                            sliderComponent.Attributes.Pivot = new PointF(pivotX, pivotY);
                            sliderComponent.Slider.Minimum = minBound;
                            sliderComponent.Slider.Maximum = maxBound;
                            sliderComponent.Slider.Value = currentValue;
                            sliderComponent.Slider.DecimalPlaces = accuracy;
                            sliderComponent.Slider.Type = acc;

                            this.OnPingDocument().AddObject(sliderComponent,false);
                            return;
                        }
                        if (typeName.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                        {

                            bool multiLine = Convert.ToBoolean(cmds[4]);
                            bool drawIndicies = Convert.ToBoolean(cmds[5]);
                            bool drawPaths = Convert.ToBoolean(cmds[6]);
                            bool wrap = Convert.ToBoolean(cmds[7]);

                            GH_Panel.Alignment alignment = (GH_Panel.Alignment)Enum.Parse(typeof(GH_Panel.Alignment), cmds[8]);
                            GH_Panel panelComponent = new GH_Panel();
                            panelComponent.CreateAttributes();
                            panelComponent.Attributes.Pivot = new PointF(pivotX, pivotY);
                            panelComponent.Properties.Multiline = multiLine;
                            panelComponent.Properties.DrawIndices = drawIndicies;
                            panelComponent.Properties.DrawPaths = drawPaths;
                            panelComponent.Properties.Wrap = wrap;
                            panelComponent.Properties.Alignment = alignment;

                            this.OnPingDocument().AddObject(panelComponent, false);
                            return;
                        }
                        else
                        {
                            RecognizeAndMake(typeName, pivotX, pivotY);
                        }
                    }
                    catch (Exception e)
                    {
                        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                    }

                    return;
                }

                if (cmds[0] == "RemoHide" || cmds[0] == "RemoUnhide")
                {
                    int pivotX = Convert.ToInt32(cmds[1]);
                    int pivotY = Convert.ToInt32(cmds[2]);

                    int compIndex =  FindComponentOnCanvasByCoordinates(pivotX, pivotY);
                    var otherComp = (IGH_Component) this.OnPingDocument().Objects[compIndex];
                    
                    if (otherComp.Hidden == true)
                    {
                        otherComp.Hidden = false;
                    }
                    else if (otherComp.Hidden == false)
                    {
                        otherComp.Hidden = true;
                    }

                    return;
                }

                if (cmds[0] == "RemoConnect")
                {

                    bool connect = Convert.ToBoolean(cmds[1]);
                    bool disconnect = Convert.ToBoolean(cmds[2]);
                    int srcPivotX = Convert.ToInt32(cmds[3]);
                    int srcPivotY = Convert.ToInt32(cmds[4]);
                    srcCompOutputIndex = Convert.ToInt32(cmds[5]);
                    int tgtPivotX = Convert.ToInt32(cmds[7]);
                    int tgtPivotY = Convert.ToInt32(cmds[8]);
                    tgtCompInputIndex = Convert.ToInt32(cmds[6]);

                    connectionMouseDownX = srcPivotX;
                    connectionMouseDownY = srcPivotY;
                    connectionMouseUpX = tgtPivotX;
                    connectionMouseUpY = tgtPivotY;

                    var ghDocument = this.OnPingDocument();
                    var ghObjectsList = ghDocument.Objects;

                    srcComp = RemoConnectFindComponentOnCanvasByCoordinates(srcPivotX, srcPivotY);
                    tgtComp = RemoConnectFindComponentOnCanvasByCoordinates(tgtPivotX, tgtPivotY);

                    for (int i = 0; i < 21; i++)
                    {
                        double ratio = (double)i / 20.0;
                        int offsetSrcComp = RemoConnectFindComponentOnCanvasByCoordinates(srcPivotX - Convert.ToInt32(200 * ratio), srcPivotY);
                        var offsetComp = this.OnPingDocument().Objects[offsetSrcComp];
                        bool inputIsSlider = offsetComp.ToString().Equals("Grasshopper.Kernel.Special.GH_NumberSlider");
                        bool inputIsDigitScroller = offsetComp.ToString().Equals("Grasshopper.Kernel.Special.GH_DigitScroller");
                        if (inputIsSlider || inputIsDigitScroller)
                        {
                            srcComp = offsetSrcComp;
                        }
                    }

                    var srcObject = ghObjectsList[srcComp];
                    var tgtObject = ghObjectsList[tgtComp];

                    string srcType = CategoryString(srcComp);
                    string tgtType = CategoryString(tgtComp);

                    bool srcIsSpecialType = CheckforSpecialCase(srcType);
                    bool tgtIsSpecialType = CheckforSpecialCase(tgtType);

                    //if (outputFound != null || !outputFound.ToString().Equals("")) srcIsSpecialType = false;
                    //if (inputFound != null || !inputFound.ToString().Equals("")) tgtIsSpecialType = false;

                    string[] tgtComptype = tgtObject.GetType().ToString().Split('.');
                    bool tgtGradientComponent = tgtComptype[tgtComptype.Length - 1].Equals("GH_GradientControl");
                    if (tgtGradientComponent) { tgtIsSpecialType = false; }


                    if (srcIsSpecialType)
                    {
                        if (connect)
                        {
                            if (srcComp == tgtComp) return;
                            if (tgtIsSpecialType)
                            {
                                this.OnPingDocument().ScheduleSolution(0, SpecialToSpecial);
                            }
                            else
                            {
                                this.OnPingDocument().ScheduleSolution(0, SpecialToComp);
                            }
                        }
                        else if (disconnect)
                        {
                            if (tgtIsSpecialType)
                            {
                                this.OnPingDocument().ScheduleSolution(0, DisSpecialFromSpecial);
                            }
                            else
                            {
                                this.OnPingDocument().ScheduleSolution(0, DisSpecialFromComp);
                            }

                        }
                    }
                    else
                    {
                        if (connect)
                        {
                            if (tgtIsSpecialType)
                            {
                                this.OnPingDocument().ScheduleSolution(0, CompToSpecial);
                            }
                            else
                            {
                                this.OnPingDocument().ScheduleSolution(0, CompToComp);
                            }
                        }
                        else if (disconnect)
                        {
                            if (tgtIsSpecialType)
                            {
                                this.OnPingDocument().ScheduleSolution(0, DisCompFromSpecial);
                            }
                            else
                            {
                                this.OnPingDocument().ScheduleSolution(0, DisCompFromComp);
                            }
                        }
                    }
                }


                if (cmds[0].Equals("RemoParam"))
                {
                    int compLocX = Convert.ToInt32(cmds[1]);
                    int compLocY = Convert.ToInt32(cmds[2]);

                    if (cmds[3].Equals("PushTheButton"))
                    {
                        RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_ButtonObject");
                        buttonVal = Convert.ToBoolean(cmds[4]);
                        this.OnPingDocument().ScheduleSolution(0, PushTheButton);
                        return;
                    }
                    if (cmds[3].Equals("ToggleBooleanToggle"))
                    {
                        RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_BooleanToggle");
                        toggleVal = Convert.ToBoolean(cmds[4]);
                        this.OnPingDocument().ScheduleSolution(0, ToggleBooleanToggle);
                        return;
                    }
                    if (cmds[3].Equals("WriteToPanel"))
                    {
                        text = "";
                        RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_Panel");
                        for (int i = 4; i < cmds.Length; i++)
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
                        colorVal = Color.FromArgb(aVal, rVal, gVal, bVal);
                        this.OnPingDocument().ScheduleSolution(0, ColorSwatchChange);
                        return;
                    }
                    if (cmds[3].Equals("AddValueToSlider"))
                    {

                        RemoParamIndex = RemoParamFindObjectOnCanvasByCoordinates(compLocX, compLocY, "GH_NumberSlider");
                        val = Convert.ToDecimal(cmds[4]);
                        this.OnPingDocument().ScheduleSolution(0, AddValueToSlider);
                        return;
                    }


                }

                if (cmds[0].Equals("Selection"))
                {
                    bool selectorAdd = Convert.ToBoolean(cmds[1]);
                    bool selectorRemove = Convert.ToBoolean(cmds[2]);
                    int compX = Convert.ToInt32(cmds[3]);
                    int compY = Convert.ToInt32(cmds[4]);
                    int otherCompInx = FindComponentOnCanvasByCoordinates(compX, compY);
                    var otherComp = this.OnPingDocument().Objects[otherCompInx];
                    GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);
                    grip.CreateObjectData(otherComp);
                    if (selectorAdd)
                    {
                        this.OnPingDocument().Select(grip, true, false);
                    }
                    else if (selectorRemove)
                    {
                        this.OnPingDocument().Select(grip, false, true);
                    }
                }
                else if (cmds[0].Equals("Deletion"))
                {
                    bool delete = Convert.ToBoolean(cmds[1]);
                    int compX = Convert.ToInt32(cmds[2]);
                    int compY = Convert.ToInt32(cmds[3]);

                    if (delete)
                    {
                        deletionIndex = DeletionCommandFindComponentOnCanvasByCoordinates(compX, compY);

                        this.OnPingDocument().ScheduleSolution(0, DeleteComponent);
                        //Grasshopper.Instances.RedrawCanvas();
                    }
                }

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
                    if (distance < minDistance)
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
                var otherComp = this.OnPingDocument().Objects[deletionIndex];
                this.OnPingDocument().RemoveObject(otherComp, true);
            }
            catch (Exception e){
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
            GH_ButtonObject button = (GH_ButtonObject)this.OnPingDocument().Objects[RemoParamIndex];
            button.ButtonDown = buttonVal;
            button.ExpireSolution(true);
        }
        private void ToggleBooleanToggle(GH_Document doc)
        {  
            GH_BooleanToggle toggle = (GH_BooleanToggle)this.OnPingDocument().Objects[RemoParamIndex];
            toggle.Value = toggleVal;
            toggle.ExpireSolution(true);
        }
        private void WriteToPanel(GH_Document doc)
        {
            GH_Panel panel = (GH_Panel) this.OnPingDocument().Objects[RemoParamIndex];
            panel.UserText = text;
            panel.ExpireSolution(true);
        }
        private void ColorSwatchChange(GH_Document doc)
        {
            GH_ColourSwatch colorSW = (GH_ColourSwatch) this.OnPingDocument().Objects[RemoParamIndex];
            colorSW.SwatchColour = colorVal;
            colorSW.ExpireSolution(true);
        }
        private void AddValueToSlider(GH_Document doc)
        {           
            GH_NumberSlider numSlider = (GH_NumberSlider) this.OnPingDocument().Objects[RemoParamIndex];
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

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY)
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
            // making sure the update argument is false to prevent GH crashes
            thisDoc.AddObject(myObject, false);
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

            

            
            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX -30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX + offset, connectionMouseDownY));
                }
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + offset, connectionMouseUpY));
                }
            }



            foundIn.AddSource((IGH_Param)foundOut);
            

            

        }

        // 2 CompToSpecial
        public void CompToSpecial(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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
                foundIn.AddSource((IGH_Param)foundOut);
            }
            else
            {
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];
                closeComponent.AddSource((IGH_Param)foundOut);
            }
        }

        // 3 SpecialToComp
        public void SpecialToComp(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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
                foundIn.AddSource((IGH_Param)foundOut);
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];

                foundIn.AddSource(sourceComponent);
            }
        }

        // 4 SpecialToSpecial
        public void SpecialToSpecial(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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
                foundIn.AddSource((IGH_Param)foundOut);
            }
            else
            {
                var sourceComponent = (IGH_Param)this.OnPingDocument().Objects[srcComp];
                var closeComponent = (IGH_Param)this.OnPingDocument().Objects[tgtComp];

                closeComponent.AddSource((IGH_Param)sourceComponent);
            }
        }

        // 5 CompFromComp
        public void DisCompFromComp(GH_Document doc)
        {

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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

            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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
            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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
            var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(connectionMouseDownX - 30, connectionMouseDownY));
            var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(connectionMouseUpX + 30, connectionMouseUpY));


            for (int i = -10; i < 11; i++)
            {
                int offset = Convert.ToInt32(60 * (double)i / 10.0);
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