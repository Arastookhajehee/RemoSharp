using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;
using Grasshopper.GUI;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;
using RemoSharp.RemoCommandTypes;
using Rhino.Commands;

using Newtonsoft.Json;

namespace RemoSharp
{
    public class RemoCompSource : GHCustomComponent
    {
        int setup = 0;
        int commandReset = 0;
        Grasshopper.GUI.Canvas.GH_Canvas canvas;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;
        string commandJson = "";
        
        string username = "";

        float[] downPnt = {0,0};
        float[] upPnt = { 0, 0 };

        PushButton pushButton1;


        float[] PointFromCanvasMouseInteraction(Grasshopper.GUI.Canvas.GH_Viewport vp, MouseEventArgs e)
        {
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            float x = mouseEvent.CanvasX;
            float y = mouseEvent.CanvasY;
            float[] coords = {x,y};
            return coords;
        }

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
            pushButton1 = new PushButton("Set Up",
                   "Creates The Required RemoSharp Components to Connect to a Session.", "Set Up");
            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            AddCustomControl(pushButton1);

            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");

        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                int xShift = 2;
                PointF pivot = this.Attributes.Pivot;
                PointF bffButtonPivot = new PointF(pivot.X + xShift - 214, pivot.Y - 227);
                PointF bffTogglePivot = new PointF(pivot.X + xShift - 214, pivot.Y - 166);
                PointF bffPivot = new PointF(pivot.X + xShift + 32, pivot.Y - 165);
                PointF bffTriggerPivot = new PointF(pivot.X + xShift - 214, pivot.Y - 93);
                PointF triggerPivot = new PointF(pivot.X + xShift - 178, pivot.Y - 54);
                PointF targetPivot = new PointF(pivot.X + xShift + 167, pivot.Y);
                PointF panelPivot = new PointF(pivot.X + xShift + 103, pivot.Y - 164);
                PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 236);
                PointF idPanelPivot = new PointF(pivot.X + xShift + 103, pivot.Y - 300);
                PointF listenPivot = new PointF(pivot.X + xShift + 330, pivot.Y - 226);
                PointF sendIDPivot = new PointF(pivot.X + xShift + 335, pivot.Y - 142);
                PointF sendPivot = new PointF(pivot.X + xShift + 498, pivot.Y - 167);

                PointF commandPivot = new PointF(pivot.X + xShift + 698, pivot.Y - 254);

                #region setup components
                // button
                Grasshopper.Kernel.Special.GH_ButtonObject bffButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                bffButton.CreateAttributes();
                bffButton.Attributes.Pivot = bffButtonPivot;
                bffButton.NickName = "RemoSharp";

                // toggle
                Grasshopper.Kernel.Special.GH_BooleanToggle bffToggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                bffToggle.CreateAttributes();
                bffToggle.Attributes.Pivot = bffTogglePivot;
                bffToggle.NickName = "RemoSharp";
                bffToggle.Value = true;
                bffToggle.ExpireSolution(false);

                // componentName
                var bffComp = new RemoSharp.WebSocket_BFF();
                bffComp.CreateAttributes();
                bffComp.Attributes.Pivot = bffPivot;
                bffComp.Params.RepairParamAssociations();


                // bff trigger
                var bffTrigger = new Grasshopper.Kernel.Special.GH_Timer();
                bffTrigger.CreateAttributes();
                bffTrigger.Attributes.Pivot = bffTriggerPivot;
                bffTrigger.NickName = "RemoSharp WSBFF";
                bffTrigger.Interval = 1000;
                var bffTriggerTarget = bffComp.InstanceGuid;

                // RemoSharp trigger
                var trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.NickName = "RemoSharp";
                trigger.Interval = 100;
                var triggerTarget = this.InstanceGuid;

                // componentName
                var targetComp = new RemoSharp.RemoCompTarget();
                targetComp.CreateAttributes();
                targetComp.Attributes.Pivot = targetPivot;
                targetComp.Params.RepairParamAssociations();

                // componentName
                var panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new Rectangle((int) panelPivot.X, (int) panelPivot.Y, 100, 45);
                panel.SetUserText("");

                // componentName
                var wscComp = new RemoSharp.WsClientCat.WsClientStart();
                wscComp.CreateAttributes();
                wscComp.Attributes.Pivot = wscPivot;
                wscComp.Params.RepairParamAssociations();

                // componentName
                var listenComp = new RemoSharp.WsClientCat.WsClientRecv();
                listenComp.CreateAttributes();
                listenComp.Attributes.Pivot = listenPivot;
                listenComp.Params.RepairParamAssociations();

                // componentName
                var sendComp = new RemoSharp.WsClientCat.WsClientSend();
                sendComp.CreateAttributes();
                sendComp.Attributes.Pivot = sendPivot;
                sendComp.Params.RepairParamAssociations();

                // componentName
                var commandComp = new RemoSharp.CommandExecutor();
                commandComp.CreateAttributes();
                commandComp.Attributes.Pivot = commandPivot;
                commandComp.Params.RepairParamAssociations();

                #endregion

                var addressOutPuts = RemoSharp.RemoCommandTypes.Utilites.CreateServerMakerComponent(this.OnPingDocument(), pivot, -119, -318, false);


                this.OnPingDocument().ScheduleSolution(1, doc =>
                {

                    
                    this.OnPingDocument().AddObject(bffButton, true);
                    this.OnPingDocument().AddObject(bffToggle, true);
                    this.OnPingDocument().AddObject(bffComp, true);
                    this.OnPingDocument().AddObject(bffTrigger, true);
                    this.OnPingDocument().AddObject(trigger, true);
                    this.OnPingDocument().AddObject(targetComp, true);
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(wscComp, true);
                    this.OnPingDocument().AddObject(listenComp, true);
                    this.OnPingDocument().AddObject(sendComp, true);
                    this.OnPingDocument().AddObject(commandComp, true);

                    /*
                    bffButton bffToggle bffComp
                    bffTrigger trigger targetComp
                    panel wscComp idToggle
                    listenComp sendIDComp sendComp
                    listendIDComp commandComp
                    */

                    bffComp.Params.Input[0].AddSource(bffButton);
                    bffComp.Params.Input[1].AddSource(bffToggle);
                    targetComp.Params.Input[0].AddSource(this.Params.Output[0]);
                    this.Params.Input[0].AddSource(panel);
                    wscComp.Params.Input[2].AddSource(bffButton);
                    //wscComp.Params.Input[0].AddSource(addressOutPuts[0]);

                    listenComp.Params.Input[0].AddSource(wscComp.Params.Output[0]);

                    sendComp.Params.Input[0].AddSource(wscComp.Params.Output[0]);
                    sendComp.Params.Input[1].AddSource(targetComp.Params.Output[0]);

                    commandComp.Params.Input[0].AddSource(listenComp.Params.Output[0]);
                    commandComp.Params.Input[1].AddSource(panel);

                    bffTrigger.AddTarget(bffTriggerTarget);
                    trigger.AddTarget(triggerTarget);
                });
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Command", "cmd", "RemoSharp Canvas Interaction Command", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // getting the username information
            DA.GetData(0, ref username);

            if (setup == 0)
            {
                canvas = Grasshopper.Instances.ActiveCanvas;
                #region Wire Connection and Move Sub
                canvas.MouseDown += (object sender, MouseEventArgs e) =>
                {
                    downPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e);
                    if (e.Button != MouseButtons.Left ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WindowSelectInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_PanInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_ZoomInteraction)
                    {
                        interaction = null;
                        commandReset++;
                        return;
                    }
                    if (canvas.ActiveInteraction != null &&
                      (canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction ||
                    canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction))
                    {
                        canvas.MouseUp += (object sender2, MouseEventArgs e2) => {
                            upPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e);
                            if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction)
                            {
                                Type type = typeof(Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction);
                                object mode = type
                                  .GetField("m_mode", BindingFlags.NonPublic | BindingFlags.Instance)
                                  .GetValue(interaction);
                                IGH_Param source = type
                                  .GetField("m_source", BindingFlags.NonPublic | BindingFlags.Instance)
                                  .GetValue(interaction) as IGH_Param;
                                IGH_Param target = type
                                  .GetField("m_target", BindingFlags.NonPublic | BindingFlags.Instance)
                                  .GetValue(interaction) as IGH_Param;

                                RemoConnectType remoConnectType = RemoConnectType.None;
                                if (mode.ToString().Equals("Replace"))
                                {
                                    remoConnectType = RemoConnectType.Replace;
                                }
                                else if (mode.ToString().Equals("Remove"))
                                {
                                    remoConnectType = RemoConnectType.Remove;
                                }
                                else
                                {
                                    remoConnectType = RemoConnectType.Add;
                                }

                                //PointF sourceCoords = source.Attributes.Pivot;
                                //PointF targetCoords = target.Attributes.Pivot;

                                //Guid sourceGuid = source.InstanceGuid;
                                //Guid targetGuid = target.InstanceGuid;

                                //command = "WireConnection"
                                //  + "," + conDiscon
                                //  + "," + sourceGuid
                                //  + "," + targetGuid;

                                if (source.Attributes.HasInputGrip)
                                {
                                    if (source.Kind != GH_ParamKind.floating)
                                    {
                                        command = new RemoConnectInteraction(username, target, source, remoConnectType);
                                    }
                                    else
                                    {
                                        if (downPnt[0] < source.Attributes.Pivot.X)
                                        {
                                            command = new RemoConnectInteraction(username, target, source, remoConnectType);
                                        }
                                        else
                                        {
                                            command = new RemoConnectInteraction(username, source, target, remoConnectType);
                                        }
                                        
                                    }
                                    
                                }
                                else
                                {
                                    command = new RemoConnectInteraction(username, source, target, remoConnectType);
                                }
                                
                                commandJson = RemoCommand.SerializeToJson(command);
                                commandReset = 0;

                            }
                            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
                            {
                                float downPntX = downPnt[0];
                                float downPntY = downPnt[1];
                                float upPntX = upPnt[0];
                                float upPntY = upPnt[1];

                                //int moveX = upPntX - downPntX;
                                //int moveY = upPntY - downPntY;

                                //var movedObject = this.OnPingDocument().FindObject(new PointF(upPntX, upPntY), 1);
                                //string movedObjGuid = movedObject.InstanceGuid.ToString();
                                

                                if (downPntX != upPntX && downPntY != upPntY)
                                {
                                    //try
                                    //{
                                    //command = "MoveComponent," + downPntX + "," + downPntY + "," + moveX + "," + moveY + "," + movedObjGuid;

                                    var selection = this.OnPingDocument().SelectedObjects();

                                    if (selection != null)
                                    {

                                        Guid selectionGuid = selection[0].InstanceGuid;
                                        command = new RemoMove(username, selectionGuid, upPntX, upPntY, DateTime.Now.Second);
                                        commandJson = RemoCommand.SerializeToJson(command);
                                        //command = "MoveComponent," + downPntX + "," + downPntY + "," + moveX + "," + moveY;
                                        downPnt[0] = 0;
                                        downPnt[1] = 0;
                                        upPnt[0] = 0;
                                        upPnt[1] = 0;
                                            commandReset = 0;
                                    }
                                    //}
                                    //catch
                                    //{
                                    //    command = "";
                                    //}
                                }
                                else command = null;
                            }

                        };
                        interaction = canvas.ActiveInteraction;
                    }
                };
                #endregion

                #region Add Object Sub
                this.OnPingDocument().ObjectsAdded += (object sender, GH_DocObjectEventArgs e) =>
                {

                    List<Guid> guids = new List<Guid>();
                    List<string> componentTypes = new List<string>();
                    List<int> Xs = new List<int>();
                    List<int> Ys = new List<int>();
                    List<bool> isSpecials = new List<bool>();
                    List<string> specialParameters_s = new List<string>();

                    

                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {

                        var newCompGuid = obj.InstanceGuid;
                        var compTypeString = obj.GetType().ToString();
                        var pivot = obj.Attributes.Pivot;

                        //adding info for RemoCreate Command
                        guids.Add(newCompGuid);
                        componentTypes.Add(compTypeString);
                        Xs.Add((int)pivot.X);
                        Ys.Add((int)pivot.Y);


                        if (compTypeString.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                        {
                            Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider) obj;
                            decimal minBound = sliderComponent.Slider.Minimum;
                            decimal maxBound = sliderComponent.Slider.Maximum;
                            decimal currentValue = sliderComponent.Slider.Value;
                            int accuracy = sliderComponent.Slider.DecimalPlaces;
                            var sliderType = sliderComponent.Slider.Type;
                            string specialParts = minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType;

                            isSpecials.Add(true);
                            specialParameters_s.Add(specialParts);
                        }
                        else if (compTypeString.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                        {
                            Grasshopper.Kernel.Special.GH_Panel panelComponent = (Grasshopper.Kernel.Special.GH_Panel) obj;
                            bool multiLine = panelComponent.Properties.Multiline;
                            bool drawIndicies = panelComponent.Properties.DrawIndices;
                            bool drawPaths = panelComponent.Properties.DrawPaths;
                            bool wrap = panelComponent.Properties.Wrap;
                            Grasshopper.Kernel.Special.GH_Panel.Alignment alignment = panelComponent.Properties.Alignment;
                            float panelSizeX = panelComponent.Attributes.Bounds.Width;
                            float panelSizeY = panelComponent.Attributes.Bounds.Height;

                            string content = panelComponent.UserText;
                            string specialParts = multiLine + "," + drawIndicies + "," + drawPaths + "," + wrap + "," + alignment.ToString() + "," + panelSizeX + "," + panelSizeY + "," + content;
                            
                            isSpecials.Add(true);
                            specialParameters_s.Add(specialParts);
                        }
                        else 
                        {
                            isSpecials.Add(false);
                            specialParameters_s.Add("");
                        }


                    }


                    command = new RemoCreate(username, guids, componentTypes,
                    Xs, Ys, isSpecials, specialParameters_s);
                    commandJson = RemoCommand.SerializeToJson(command);

                    downPnt[0] = 0;
                    downPnt[1] = 0;
                    upPnt[0] = 0;
                    upPnt[1] = 0;
                    interaction = null;
                    commandReset = 0;
                };
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted += (object sender, GH_DocObjectEventArgs e) =>
                {

                    List<Guid> deleteGuids = new List<Guid>();
                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {
                        deleteGuids.Add(obj.InstanceGuid);

                    }

                    command = new RemoDelete(username, deleteGuids);
                    commandJson = RemoCommand.SerializeToJson(command);

                    downPnt[0] = 0;
                    downPnt[1] = 0;
                    upPnt[0] = 0;
                    upPnt[1] = 0;
                    interaction = null;
                    commandReset = 0;
                };
                #endregion

            }
            int commandRepeatCount = 5;
            DA.SetData(0,command);

            if (setup > 100) setup = 5;
            if (commandReset > commandRepeatCount) command = new RemoNullCommand(username);

            setup++;
            commandReset++;
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