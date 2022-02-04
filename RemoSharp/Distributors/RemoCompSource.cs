using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;
using Grasshopper.GUI;

namespace RemoSharp
{
    public class RemoCompSource : GHCustomComponent
    {
        

        #region Custom Visual Controls

        
        ToggleSwitch movingModeToggle;
        ToggleSwitch connectionModeToggle;

        HorizontalSliderInteger inputSlider;
        HorizontalSliderInteger outputSlider;

        ToggleSwitch toggleSwitch;

        StackPanel stackPanel;
        //StackPanel stackPane2;
        //StackPanel stackPane3;

        #endregion

        // Checking if a trigger has been correctly setup for the component
        public int componentProperSetup = 0;
        // having all the inputs as public values accecable from the whole script
        int sourceOutput = 0;
        int targetInput = 0;

        // Mouse Interaction variables
        bool movingMode = false;
        bool connectingMode = false;
        bool connect = false;
        bool disconnect = false;
        bool clickedDown = false;
        bool clickedUP = false;
        Point3d downPnt = new Point3d(0, 0, 0);
        Point3d upPnt = new Point3d(0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the RemoCompSource class.
        /// </summary>
        public RemoCompSource()
          : base("RemoCompSource", "RemoCompS",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            #region Custom Visual Controls

            

            connectionModeToggle = new ToggleSwitch("ConnectingMode"
                , "Turning On/Off Remote ConnectingMode. If On, moves the closest component in the same direction of the mouse."
                , false
                );
            movingModeToggle = new ToggleSwitch("MovingMode"
                , "Turning On/Off Remote ConnectingMode. If On, we can connect/disconnect components remotely."
                , false
                );
            outputSlider = new HorizontalSliderInteger("Output", "The output index of the source component", 0, 0, 10, "", false);
            inputSlider = new HorizontalSliderInteger("Input", "The input index of the target component", 0, 0, 10, "", false);
            

            toggleSwitch = new ToggleSwitch("Transparency", "Toggles transparency of Grasshopper", false);

            outputSlider.OnValueChanged += OutputSlider_OnValueChanged;
            inputSlider.OnValueChanged += InputSlider_OnValueChanged;
            connectionModeToggle.OnValueChanged += ConnectionMode_OnValueChanged;
            movingModeToggle.OnValueChanged += MovingModeToggle_OnValueChanged;
            toggleSwitch.OnValueChanged += ToggleSwitch_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Vecrtical, true,
                connectionModeToggle, movingModeToggle, toggleSwitch
                );
            AddCustomControl(stackPanel);
            //AddCustomControl(outputSlider);
            //AddCustomControl(inputSlider);

            #endregion

            //input for calibrating the component XY grabber
            //pManager.AddTextParameter("", "", "", GH_ParamAccess.item, "");

            //pManager.AddBooleanParameter("Create", "Create",
            //    "Create a new instance of the close component on the main remote GH_Canvas",
            //    GH_ParamAccess.item, false);
            //pManager.AddBooleanParameter("Move", "Mve",
            //    "Move the closest component in the same direction of the target RemoConnector Component.",
            //    GH_ParamAccess.item,false);
            //pManager.AddIntegerParameter("SourceComp Output", "SrcOut", "The output index of the source component",
            //    GH_ParamAccess.item, 0); 
            //pManager.AddIntegerParameter("TargetComp Input", "TrgtIn", "The input index of the target component",
            //     GH_ParamAccess.item, 0);
            //pManager.AddBooleanParameter("Connect", "Cnct",
            //    "Connects components remotely.",
            //    GH_ParamAccess.item, false);
            //pManager.AddBooleanParameter("Disconnect", "DsCnct",
            //    "Disconnects components remotely.",
            //    GH_ParamAccess.item, false);

        }

       

        private void InputSlider_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            targetInput = Convert.ToInt32(e.Value);
        }

        private void OutputSlider_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            sourceOutput = Convert.ToInt32(e.Value);
        }

        private void ConnectionMode_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            connectingMode = currentValue;
            //this.ExpireSolution(true);
        }
        private void MovingModeToggle_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            movingMode = currentValue;
        }




        private void ToggleSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool toggleChangeVal = Convert.ToBoolean(e.Value);
            var ghDoc = Grasshopper.Instances.DocumentEditor;
            if (toggleChangeVal)
            {
                ghDoc.Opacity = 0.45;
            }
            else
            {
                ghDoc.Opacity = 1;
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(
                ">⚫<             Command",
                ">⚫<             Command",
                "Command to be executed to make, connect, and disconnect components on the main remote GH_Canvas.",
                GH_ParamAccess.item
                );
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            

            componentProperSetup++;
            if (componentProperSetup < 15) {
                this.Message = "Please Connect a Trigger";
                DA.SetData(0, "");
                return;
            }
            if (componentProperSetup == 15)
            {
                this.Message = "";
            }
            else if (componentProperSetup == 300)
            {
                componentProperSetup = 20;
            }


            #region Event Handeling for Mouse Interaction -> Connect/Disconnect/Move
            Grasshopper.Instances.ActiveCanvas.KeyDown += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                if (System.Windows.Forms.Keys.ControlKey == e.KeyCode && componentProperSetup > 15)
                {
                    disconnect = true;
                };
            };
            Grasshopper.Instances.ActiveCanvas.KeyUp += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                if (System.Windows.Forms.Keys.ControlKey == e.KeyCode && componentProperSetup > 15)
                {
                    disconnect = false;
                };
            };
            Grasshopper.Instances.ActiveCanvas.KeyDown += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                if (System.Windows.Forms.Keys.ShiftKey == e.KeyCode && componentProperSetup > 15)
                {
                    connect = true;
                };
            };
            Grasshopper.Instances.ActiveCanvas.KeyUp += (object sender, System.Windows.Forms.KeyEventArgs e) =>
            {
                if (System.Windows.Forms.Keys.ShiftKey == e.KeyCode && componentProperSetup > 15)
                {
                    connect = false;
                };
            };
            Grasshopper.Instances.ActiveCanvas.MouseDown += (object sender, System.Windows.Forms.MouseEventArgs e) =>
            {
                var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
                Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(vp, e);
                float x = mouseEvent.CanvasX;
                float y = mouseEvent.CanvasY;
                double dbX = Convert.ToDouble(x);
                double dbY = Convert.ToDouble(y);
                if (mouseEvent.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (connect || disconnect || movingMode && componentProperSetup > 15)
                    {
                        clickedDown = true;
                        downPnt.X = dbX;
                        downPnt.Y = dbY;
                    }
                }
            };
            Grasshopper.Instances.ActiveCanvas.MouseUp += (object sender, System.Windows.Forms.MouseEventArgs e) =>
            {
                var vp = Grasshopper.Instances.ActiveCanvas.Viewport;
                Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new GH_CanvasMouseEvent(vp, e);
                float x = mouseEvent.CanvasX;
                float y = mouseEvent.CanvasY;
                double dbX = Convert.ToDouble(x);
                double dbY = Convert.ToDouble(y);
                if (mouseEvent.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if (connect || disconnect || movingMode && componentProperSetup > 15)
                    {
                        clickedUP = true;
                        upPnt.X = dbX;
                        upPnt.Y = dbY;
                    }
                }
            };

            #endregion


            // defining all the trigger variables

            // For calibrating the components
            //string shift = "";
            //DA.GetData(0, ref shift);
            //string[] shifts = shift.Split(',');
            //int shiftX = Convert.ToInt32(shifts[0]);
            //int shiftY = Convert.ToInt32(shifts[1]);

            //DA.GetData(0, ref create);
            //DA.GetData(1, ref move);
            //DA.GetData(2, ref sourceOutput);
            //DA.GetData(3, ref targetInput);
            //DA.GetData(4, ref connect);
            //DA.GetData(5, ref disconnect);

            //setting the output command string
            string cmd = "";

            if (!movingMode && !connect && !disconnect)
            {
                DA.SetData(0, "");
                return;
            }

            if (clickedDown && clickedUP && movingMode)
            {


                int downPntX = Convert.ToInt32(downPnt.X);
                int downPntY = Convert.ToInt32(downPnt.Y);
                int upPntX = Convert.ToInt32(upPnt.X);
                int upPntY = Convert.ToInt32(upPnt.Y);

                int moveX = upPntX - downPntX;
                int moveY = upPntY - downPntY;

                if (downPntX != upPntX && downPntY != upPntY)
                {
                    cmd = "MoveComp," + downPntX + "," + downPntY + "," + moveX + "," + moveY;
                }
                DA.SetData(0, cmd);
                clickedDown = false;
                clickedUP = false;
                return;
            }

            if (clickedDown && clickedUP && connectingMode)
            {
                if (connect || disconnect)
                {

                    int downPntX = Convert.ToInt32(downPnt.X);
                    int downPntY = Convert.ToInt32(downPnt.Y);
                    int upPntX = Convert.ToInt32(upPnt.X);
                    int upPntY = Convert.ToInt32(upPnt.Y);
                    if (downPntX != upPntX && downPntY != upPntY)
                    {
                        cmd = "RemoConnect," + connect + "," + disconnect + "," + downPntX + "," + downPntY + "," + sourceOutput + "," + targetInput + "," + upPntX + "," + upPntY;
                    }

                    //// coordinateChecker
                    //cmd = cmd + "," + shiftX;
                    //cmd = cmd + "," + shiftY;

                    DA.SetData(0, cmd);
                    connect = false;
                    disconnect = false;
                    clickedDown = false;
                    clickedUP = false;
                    return;
                }
                else
                {
                    cmd = "";
                    DA.SetData(0, cmd);
                    connect = false;
                    disconnect = false;
                    clickedDown = false;
                    clickedUP = false;
                    return;
                }

            }

            DA.SetData(0, "");
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
                return RemoSharp.Properties.Resources.SourceComp.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9a3a9712-9b99-409d-9c02-f6b338305f5b"); }
        }

        

        

    }
}