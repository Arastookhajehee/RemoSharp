using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;

using RemoSharp.RemoCommandTypes;
using Grasshopper.Kernel.Types;

namespace RemoSharp
{
    public class RemoCompTarget : GHCustomComponent
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        PushButton selectButton;
        PushButton hideButton;
        PushButton lockButton;
        //PushButton remoParamButton;
        //ToggleSwitch deleteToggle;
        ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;
        StackPanel stackPanel;

        bool enable = false;
        bool movingMode = false;
        //bool create = false;
        bool select = false;
        //bool delete = false;
        bool hide = false;
        bool lockThis = false;


        // remoParam public variables
        public string componentType = "";
        public int RemoMakeindex = -1;
        public int DeleteThisComp = -1;
        public bool DeleteThisCompBool = false;
        public int GHbuttonComp = -1;
        public int remoButtonComp = -1;
        public int wsButtonComp = -1;
        public System.Drawing.PointF compPivot;

        public string currentConnectString = "";
        string cmdJson = "";
        string persistentCommand = "";
        public int con_DisConCounter = 0;

        // Move Mode variables
        int setup = 0;

        /// <summary>
        /// Initializes a new instance of the RemoCompTarget class.
        /// </summary>
        public RemoCompTarget()
          : base("RemoCompTarget", "RemoCompT",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            selectButton = new PushButton("Select",
                "Select a component on the main remote GH_Canvas.", "Sel");
            hideButton = new PushButton("Hide",
                "Hides a component on the main remote GH_Canvas.", "Hide");
            lockButton = new PushButton("Lock",
                            "Unhides a component on the main remote GH_Canvas.", "Lock");

            movingModeSwitch = new ToggleSwitch("Moving Mode", "It is recommended to keep it turned off if the user does not wish to move components around", false);
            movingModeSwitch.OnValueChanged += MovingModeSwitch_OnValueChanged;
            enableSwitch = new ToggleSwitch("Enable Interactions", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            selectButton.OnValueChanged += SelectButton_OnValueChanged;
            hideButton.OnValueChanged += PushButton2_OnValueChanged;
            lockButton.OnValueChanged += PushButton3_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Horizontal, true,
                selectButton, hideButton, lockButton
                );

            AddCustomControl(stackPanel);
            AddCustomControl(enableSwitch);
            AddCustomControl(movingModeSwitch);

            pManager.AddGenericParameter("SourceCommand", "SrcCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item);
        }

        private void EnableSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            enable = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        private void MovingModeSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            movingMode = Convert.ToBoolean(e.Value);
            this.ExpireSolution(true);
        }

        //private void ToggleSwitch_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        //{
        //    bool toggleChangeVal = Convert.ToBoolean(e.Value);
        //    var ghDoc = Grasshopper.Instances.DocumentEditor;
        //    if (toggleChangeVal)
        //    {
        //        ghDoc.Opacity = 0.25;
        //    }
        //    else
        //    {
        //        ghDoc.Opacity = 1;
        //    }
        //}

        private void SelectButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                select = currentValue;
                this.ExpireSolution(true);
            }
        }

        
        private void PushButton2_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                hide = currentValue;
                this.ExpireSolution(true);
            }
        }
        private void PushButton3_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                lockThis = currentValue;
                this.ExpireSolution(true);
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter(">⚫<       Command", ">⚫<       Command",
                "Complete command from RemoCompSource and RemoCompTarget regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // registering s
            if (setup == 0)
            {
                Grasshopper.Instances.ActiveCanvas.KeyDown += ActiveCanvas_KeyDown;
                Grasshopper.Instances.ActiveCanvas.KeyUp += ActiveCanvas_KeyUp;
            }
            setup++;
            if (setup > 100) setup = 5;

            if (!enable) return;
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            RemoCommand cmd = null;
            DA.GetData(0, ref cmd);
            if (cmd == null) return;
            // parsing the incoming command

            //string[] cmds = cmd.Split(',');

            if (cmd.commandType == CommandType.MoveComponent) 
            {
                if (!movingMode) return;

            }


            if (cmd.commandType == CommandType.WireConnection)
            {
                RemoConnectInteraction connectionInteraction = (RemoConnectInteraction)cmd;

                if (connectionInteraction.source == null || connectionInteraction.target == null) 
                {
                    cmdJson = "";
                }
                else
                {
                    int outIndex = -1;
                    bool outIsSpecial = false;
                    System.Guid outGuid = GetComponentGuidAnd_Output_Index(
                      connectionInteraction.source, out outIndex, out outIsSpecial);

                    //connectionInteraction.sourceOutput = outIndex;
                    //connectionInteraction.isSourceSpecial = outIsSpecial;
                    //connectionInteraction.sourceObjectGuid = outGuid;

                    int inIndex = -1;
                    bool inIsSpecial = false;
                    System.Guid inGuid = GetComponentGuidAnd_Input_Index(
                      connectionInteraction.target, out inIndex, out inIsSpecial);

                    //connectionInteraction.targetInput = inIndex;
                    //connectionInteraction.isTargetSpecial = inIsSpecial;
                    //connectionInteraction.targetObjectGuid = inGuid;

                    RemoConnect remoConnect = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid, outIndex, inIndex, outIsSpecial, inIsSpecial, connectionInteraction.RemoConnectType);

                    cmdJson = RemoCommand.SerializeToJson(remoConnect);
                }
            }

            // 50%
            if (hide)
            {

                List<bool> states = new List<bool>();
                List<Guid> guids = new List<Guid>();
                bool notFound = false;
                var selectionObjs = this.OnPingDocument().SelectedObjects();
                foreach ( var selection in selectionObjs ) 
                {
                    guids.Add(selection.InstanceGuid);
                    if (selection is GH_Component)
                    {
                       GH_Component hideComponent = (GH_Component)selection;
                        states.Add(!hideComponent.Hidden);
                        hideComponent.Hidden = !hideComponent.Hidden;
                    }
                    else if (selection.SubCategory == "Geometry")
                    {
                        switch (selection.GetType().ToString())
                        {
                            case ("Grasshopper.Kernel.Parameters.Param_Point"):
                                Grasshopper.Kernel.Parameters.Param_Point paramComponentParam_Point = (Grasshopper.Kernel.Parameters.Param_Point)selection;
                                states.Add(!paramComponentParam_Point.Hidden);
                                paramComponentParam_Point.Hidden = !paramComponentParam_Point.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Circle"):
                                Grasshopper.Kernel.Parameters.Param_Circle paramComponentParam_Circle = (Grasshopper.Kernel.Parameters.Param_Circle) selection;
                                states.Add(!paramComponentParam_Circle.Hidden);
                                paramComponentParam_Circle.Hidden = !paramComponentParam_Circle.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Arc"):
                                Grasshopper.Kernel.Parameters.Param_Arc paramComponentParam_Arc = (Grasshopper.Kernel.Parameters.Param_Arc) selection;
                                states.Add(!paramComponentParam_Arc.Hidden);
                                paramComponentParam_Arc.Hidden = !paramComponentParam_Arc.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Curve"):
                                Grasshopper.Kernel.Parameters.Param_Curve paramComponentParam_Curve = (Grasshopper.Kernel.Parameters.Param_Curve) selection;
                                states.Add(!paramComponentParam_Curve.Hidden);
                                paramComponentParam_Curve.Hidden = !paramComponentParam_Curve.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Line"):
                                Grasshopper.Kernel.Parameters.Param_Line paramComponentParam_Line = (Grasshopper.Kernel.Parameters.Param_Line) selection;
                                states.Add(!paramComponentParam_Line.Hidden);
                                paramComponentParam_Line.Hidden = !paramComponentParam_Line.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                                Grasshopper.Kernel.Parameters.Param_Plane paramComponentParam_Plane = (Grasshopper.Kernel.Parameters.Param_Plane) selection;
                                states.Add(!paramComponentParam_Plane.Hidden);
                                paramComponentParam_Plane.Hidden = !paramComponentParam_Plane.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Rectangle"):
                                Grasshopper.Kernel.Parameters.Param_Rectangle paramComponentParam_Rectangle = (Grasshopper.Kernel.Parameters.Param_Rectangle) selection;
                                states.Add(!paramComponentParam_Rectangle.Hidden);
                                paramComponentParam_Rectangle.Hidden = !paramComponentParam_Rectangle.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Box"):
                                Grasshopper.Kernel.Parameters.Param_Box paramComponentParam_Box = (Grasshopper.Kernel.Parameters.Param_Box) selection;
                                states.Add(!paramComponentParam_Box.Hidden);
                                paramComponentParam_Box.Hidden = !paramComponentParam_Box.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Surface"):
                                Grasshopper.Kernel.Parameters.Param_Surface paramComponentParam_Surface = (Grasshopper.Kernel.Parameters.Param_Surface) selection;
                                states.Add(!paramComponentParam_Surface.Hidden);
                                paramComponentParam_Surface.Hidden = !paramComponentParam_Surface.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Brep"):
                                Grasshopper.Kernel.Parameters.Param_Brep paramComponentParam_Brep = (Grasshopper.Kernel.Parameters.Param_Brep) selection;
                                states.Add(!paramComponentParam_Brep.Hidden);
                                paramComponentParam_Brep.Hidden = !paramComponentParam_Brep.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Mesh"):
                                Grasshopper.Kernel.Parameters.Param_Mesh paramComponentParam_Mesh = (Grasshopper.Kernel.Parameters.Param_Mesh) selection;
                                states.Add(!paramComponentParam_Mesh.Hidden);
                                paramComponentParam_Mesh.Hidden = !paramComponentParam_Mesh.Hidden;
                                break;
                            default:
                                notFound = true;
                                break;
                        }
                    
                    }
                    else
                    {
                        guids.Add(Guid.Empty);
                        states.Add(false);

                    }
                
                }



                if (!notFound)
                {
                    cmd = new RemoHide(cmd.issuerID, guids, states, DateTime.Now.Second);
                    cmdJson = RemoCommand.SerializeToJson(cmd);
                    DA.SetData(0, cmdJson);
                }
                hide = false;
            }

            if (select)
            {

                var selection = this.OnPingDocument().SelectedObjects();

                List<Guid> slectionGuids = new List<Guid>();
                foreach (var item in selection)
                {
                    slectionGuids.Add(item.InstanceGuid);
                }
                cmd = new RemoSelect(cmd.issuerID, slectionGuids, DateTime.Now.Second);
                cmdJson = RemoCommand.SerializeToJson(cmd);
                DA.SetData(0, cmdJson);
                
                select = false;
            }

            if (lockThis)
            {
                

                Guid selectionGuid = this.OnPingDocument().SelectedObjects()[0].InstanceGuid;
                cmd = new RemoLock(cmd.issuerID, selectionGuid, hide, DateTime.Now.Second);
                cmdJson = RemoCommand.SerializeToJson(cmd);
                DA.SetData(0, cmdJson);
                lockThis = false;
            }

            //if (remoParam)
            //{

            //    componentType = FindClosestObjectTypeOnCanvas(out compPivot, out RemoMakeindex);
            //    if (componentType.Equals("Grasshopper.Kernel.Special.GH_ButtonObject"))
            //    {
            //        this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoButton);
            //    }
            //    else if (componentType.Equals("Grasshopper.Kernel.Special.GH_BooleanToggle"))
            //    {
            //        this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoToggle);
            //    }
            //    else if (componentType.Equals("Grasshopper.Kernel.Special.GH_Panel"))
            //    {
            //        this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoPanel);
            //    }
            //    else if (componentType.Equals("Grasshopper.Kernel.Special.GH_ColourSwatch"))
            //    {
            //        this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoColorSwatch);
            //    }
            //    else if (componentType.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
            //    {
            //        this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoSlider);
            //    }
            //    remoParam = false;
            //    return;
            //}

            

            //string[] outGoingCommand = cmd.Split(',');
            int commandRepeatCount = 6;

            if (cmd.commandType == CommandType.WireConnection)
            {

                con_DisConCounter = 0;
                currentConnectString = cmdJson;
                persistentCommand = cmdJson;


            }
            else if (
                cmd.commandType == CommandType.MoveComponent
                || cmd.commandType == CommandType.Create
                || cmd.commandType == CommandType.Hide
                || cmd.commandType == CommandType.Lock
                || cmd.commandType == CommandType.Delete)
            {

                con_DisConCounter = 0;
                cmdJson = RemoCommand.SerializeToJson(cmd);
                currentConnectString = cmdJson;
                persistentCommand = cmdJson;


            }
            

            if (con_DisConCounter < commandRepeatCount)
            {
                DA.SetData(0, currentConnectString);
            }
            else currentConnectString = "";
            con_DisConCounter++;

            if (cmd.commandType == CommandType.MoveComponent)
            {
                DA.SetData(0, cmdJson);
                return;
            }

        }

        private void ActiveCanvas_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Tab)
            {
                this.movingModeSwitch.CurrentValue = false;
            }
        }

        private void ActiveCanvas_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Tab)
            {
                this.movingModeSwitch.CurrentValue = true;
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
                return RemoSharp.Properties.Resources.TargetComp.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("243dfe88-8c61-451c-996a-2f8f77c9409b"); }
        }

        private System.Guid GetComponentGuidAnd_Input_Index(
    IGH_Param target,
    out int paramIndex,
    out bool isSpecial)
        {
            if (target.Attributes.Parent == null)
            {
                System.Guid compGuid = target.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {
                var foundComponent = (IGH_Component)target.Attributes.Parent.DocObject;
                int index = foundComponent.Params.Input.IndexOf(target);

                paramIndex = index;
                isSpecial = false;
                return foundComponent.InstanceGuid;
            }
        }

        private System.Guid GetComponentGuidAnd_Output_Index(
          IGH_Param source,
          out int paramIndex,
          out bool isSpecial)
        {

            if (source.Attributes.Parent == null)
            {
                System.Guid compGuid = source.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {
                var foundComponent = (IGH_Component)source.Attributes.Parent.DocObject;
                int index = foundComponent.Params.Output.IndexOf(source);

                paramIndex = index;
                isSpecial = false;
                return foundComponent.InstanceGuid;
            }

        }
    }
}