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
using Grasshopper.GUI.Canvas;
using RemoSharp.RemoParams;
using WebSocketSharp;

namespace RemoSharp
{
    public class RemoCompSource : GHCustomComponent
    {
        WebSocket client;
        int setup = 0;
        int commandReset = 0;
        int commandRepeat = 5;
        Grasshopper.GUI.Canvas.GH_Canvas canvas;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        RemoCommand command = null;

        //ToggleSwitch deleteToggle;
        ToggleSwitch movingModeSwitch;
        //ToggleSwitch transparencySwitch;
        ToggleSwitch enableSwitch;

        bool enable = false;
        bool movingMode = false;

        
        public List<Guid> remoCreatedcomponens = new List<Guid>();

        string username = "";

        float[] downPnt = {0,0};
        float[] upPnt = { 0, 0 };

        PushButton setupButton;


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
          : base("RemoCompSource", "RemoSetup",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoSetup")
        {
            this.NickName = "RemoSetup";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            setupButton = new PushButton("Set Up",
                   "Creates The Required RemoSharp Components to Connect to a Session.", "Set Up");
            setupButton.OnValueChanged += SetupButton_OnValueChanged;
            AddCustomControl(setupButton);


            movingModeSwitch = new ToggleSwitch("Moving Mode", "It is recommended to keep it turned off if the user does not wish to move components around", false);
            movingModeSwitch.OnValueChanged += MovingModeSwitch_OnValueChanged;
            enableSwitch = new ToggleSwitch("Enable Interactions", "It has to be turned on if we want interactions with the server", false);
            enableSwitch.OnValueChanged += EnableSwitch_OnValueChanged;

            AddCustomControl(enableSwitch);
            AddCustomControl(movingModeSwitch);


            pManager.AddTextParameter("Username", "user", "This Computer's Username", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("WSClient", "wsc", "RemoSharp's Command Websocket Client", GH_ParamAccess.item);

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

        private void SendCommands(RemoCommand command, int commandRepeat,bool enabled)
        {
            if (!enabled) return;
            string cmdJson = RemoCommand.SerializeToJson(command);
            for (int i = 0; i < commandRepeat; i++)
            {
                client.Send(cmdJson);
            }
        }

        private void SetupButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (this.Params.Input[0].Sources.Count > 0) return;
            if (currentValue)
            {
                int xShift = 2;
                int yShift = 80;
                PointF pivot = this.Attributes.Pivot;
                PointF wscButtonPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 227 + yShift);
                PointF wscTogglePivot = new PointF(pivot.X + xShift - 216, pivot.Y - 197 + yShift);
                PointF triggerPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 415 + yShift);
                PointF panelPivot = new PointF(pivot.X + xShift - 216, pivot.Y - 170 + yShift);
                PointF wscPivot = new PointF(pivot.X + xShift + 150, pivot.Y - 336 + yShift);
                PointF listenPivot = new PointF(pivot.X + xShift + 330, pivot.Y - 334 + yShift);

                PointF targetPivot = new PointF(pivot.X + xShift + 200, pivot.Y);
                PointF commandPivot = new PointF(pivot.X + xShift + 598, pivot.Y - 312 + yShift);
                PointF commandButtonPivot = new PointF(pivot.X + xShift + 350, pivot.Y - 254 + yShift);

                #region setup components
                // button
                Grasshopper.Kernel.Special.GH_ButtonObject wscButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                wscButton.CreateAttributes();
                wscButton.Attributes.Pivot = wscButtonPivot;
                wscButton.NickName = "RemoSetup";

                // toggle
                Grasshopper.Kernel.Special.GH_BooleanToggle wscToggle = new Grasshopper.Kernel.Special.GH_BooleanToggle();
                wscToggle.CreateAttributes();
                wscToggle.Attributes.Pivot = wscTogglePivot;
                wscToggle.NickName = "RemoSetup";
                wscToggle.Value = false;
                wscToggle.ExpireSolution(false);

                // RemoSharp trigger
                var trigger = new Grasshopper.Kernel.Special.GH_Timer();
                trigger.CreateAttributes();
                trigger.Attributes.Pivot = triggerPivot;
                trigger.NickName = "RemoSharp";
                trigger.Interval = 1000;
                trigger.NickName = "RemoSetup";

                // componentName
                var targetComp = new RemoSharp.RemoCompTarget();
                targetComp.CreateAttributes();
                targetComp.Attributes.Pivot = targetPivot;
                targetComp.Params.RepairParamAssociations();
                targetComp.NickName = "RemoSetup";

                // componentName
                var panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new Rectangle((int) panelPivot.X, (int) panelPivot.Y, 100, 45);
                panel.SetUserText("");
                panel.NickName = "RemoSetup";

                // componentName
                var wscComp = new RemoSharp.WebSocketClient.WebSocketClient();
                wscComp.CreateAttributes();
                wscComp.Attributes.Pivot = wscPivot;
                wscComp.Params.RepairParamAssociations();
                wscComp.NickName = "RemoSetup";
                wscComp.autoUpdateSwitch.CurrentValue = false;
                wscComp.keepRecordSwitch.CurrentValue = true;
                wscComp.autoUpdate = false;
                wscComp.keepRecord = true;

                // componentName
                var listenComp = new RemoSharp.WebSocketClient.WSClientListen();
                listenComp.CreateAttributes();
                listenComp.Attributes.Pivot = listenPivot;
                listenComp.Params.RepairParamAssociations();
                listenComp.NickName = "RemoSetup";

                // componentName
                var commandComp = new RemoSharp.CommandExecutor();
                commandComp.CreateAttributes();
                commandComp.Attributes.Pivot = commandPivot;
                commandComp.Params.RepairParamAssociations();
                commandComp.NickName = "RemoSetup";

                // button
                Grasshopper.Kernel.Special.GH_ButtonObject commandCompButton = new Grasshopper.Kernel.Special.GH_ButtonObject();
                commandCompButton.CreateAttributes();
                commandCompButton.Attributes.Pivot = commandButtonPivot;
                commandCompButton.NickName = "RemoSetup";

                #endregion

                var addressOutPuts = RemoSharp.RemoCommandTypes.Utilites.CreateServerMakerComponent(this.OnPingDocument(), pivot, -119, -318 + yShift, true);


                this.OnPingDocument().ScheduleSolution(1, doc =>
                {


                    this.OnPingDocument().AddObject(wscButton, true);
                    this.OnPingDocument().AddObject(wscToggle, true);
                    //this.OnPingDocument().AddObject(bffComp, true);
                    //this.OnPingDocument().AddObject(bffTrigger, true);
                    this.OnPingDocument().AddObject(trigger, true);
                    this.OnPingDocument().AddObject(targetComp, true);
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(wscComp, true);
                    this.OnPingDocument().AddObject(listenComp, true);
                    this.OnPingDocument().AddObject(commandCompButton, true);
                    this.OnPingDocument().AddObject(commandComp, true);

                    /*
                    wscButton wscToggle bffComp
                    bffTrigger trigger targetComp
                    panel wscComp idToggle
                    listenComp sendIDComp sendComp
                    listendIDComp commandComp
                    */

                    //bffComp.Params.Input[0].AddSource(wscButton);
                    //bffComp.Params.Input[1].AddSource(wscToggle);
                    targetComp.Params.Input[0].AddSource(panel);
                    targetComp.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    this.Params.Input[0].AddSource(panel);
                    this.Params.Input[1].AddSource(wscComp.Params.Output[0]);
                    wscComp.Params.Input[0].AddSource(addressOutPuts[0]);
                    wscComp.Params.Input[1].AddSource(wscButton);
                    wscComp.Params.Input[2].AddSource(wscToggle);

                    listenComp.Params.Input[0].AddSource(wscComp.Params.Output[0]);

                    commandComp.Params.Input[0].AddSource(listenComp.Params.Output[0]);
                    commandComp.Params.Input[1].AddSource(panel);
                    commandComp.Params.Input[2].AddSource(commandCompButton);

                    //bffTrigger.AddTarget(bffTriggerTarget);
                    trigger.AddTarget(listenComp.InstanceGuid);
                    //trigger.AddTarget(targetComp.InstanceGuid);

                });
            }
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Command", "cmd", "RemoSharp Canvas Interaction Command", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // getting the username information
            DA.GetData(0, ref username);
            DA.GetData(1, ref client);

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
                        return;
                    }
                    if (canvas.ActiveInteraction != null &&
                      (canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction ||
                    canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction))
                    {
                        canvas.MouseUp += (object sender2, MouseEventArgs e2) => 
                        {
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

                                RemoConnectInteraction connectionInteraction = new RemoConnectInteraction();

                                if (source.Attributes.HasInputGrip)
                                {
                                    if (source.Kind != GH_ParamKind.floating)
                                    {
                                        connectionInteraction = new RemoConnectInteraction(username, target, source, remoConnectType);
                                    }
                                    else
                                    {
                                        if (downPnt[0] < source.Attributes.Pivot.X)
                                        {
                                            connectionInteraction = new RemoConnectInteraction(username, target, source, remoConnectType);
                                        }
                                        else
                                        {
                                            connectionInteraction = new RemoConnectInteraction(username, source, target, remoConnectType);
                                        }
                                        
                                    }
                                    
                                }
                                else
                                {
                                    connectionInteraction = new RemoConnectInteraction(username, source, target, remoConnectType);
                                }


                                if (connectionInteraction.source != null || connectionInteraction.target != null)
                                {
                                    int outIndex = -1;
                                    bool outIsSpecial = false;
                                    System.Guid outGuid = GetComponentGuidAnd_Output_Index(
                                      connectionInteraction.source, out outIndex, out outIsSpecial);

                                    int inIndex = -1;
                                    bool inIsSpecial = false;
                                    System.Guid inGuid = GetComponentGuidAnd_Input_Index(
                                      connectionInteraction.target, out inIndex, out inIsSpecial);


                                    float sourceX = connectionInteraction.source.Attributes.Pivot.X;
                                    float sourceY = connectionInteraction.source.Attributes.Pivot.Y;
                                    float targetX = connectionInteraction.target.Attributes.Pivot.X;
                                    float targetY = connectionInteraction.target.Attributes.Pivot.Y;


                                    command = new RemoConnect(connectionInteraction.issuerID, outGuid, inGuid, 
                                        outIndex, inIndex, outIsSpecial, inIsSpecial, connectionInteraction.RemoConnectType,sourceX,sourceY,targetX,targetY);
                                    SendCommands(command, commandRepeat,enable);

                                }

                                
                                
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

                                        if (movingMode)
                                        {
                                            SendCommands(command,commandRepeat,enable);
                                        }
                                        downPnt[0] = 0;
                                        downPnt[1] = 0;
                                        upPnt[0] = 0;
                                        upPnt[1] = 0;
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
                    List<WireHistory> wireHistories= new List<WireHistory>();
                    

                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {

                        var newCompGuid = obj.InstanceGuid;
                        var compTypeString = obj.GetType().ToString();
                        var pivot = obj.Attributes.Pivot;

                        // check to see if this component has been created from remocreate command coming from outsite
                        bool alreadyMade = remoCreatedcomponens.Contains(newCompGuid);
                        if (alreadyMade) continue;
                        else
                        {
                            remoCreatedcomponens.Add(newCompGuid);
                        }
                        //adding info for RemoCreate Command
                        guids.Add(newCompGuid);
                        componentTypes.Add(compTypeString);
                        Xs.Add((int)pivot.X);
                        Ys.Add((int)pivot.Y);
                        wireHistories.Add(new WireHistory(obj));


                        if (obj is IGH_Component)
                        {
                            IGH_Component objComponent = (IGH_Component)obj;
                            
                        }
                        else if (obj is IGH_Param)
                        {
                            IGH_Param objParam = (IGH_Param)obj;
                        }


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

                    if (guids.Count > 0)
                    {
                        command = new RemoCreate(username, guids, componentTypes,
                        Xs, Ys, isSpecials, specialParameters_s, wireHistories);



                        SendCommands(command,commandRepeat, enable);
                    }
                    else
                    {
                        command = null;
                    }

                    downPnt[0] = 0;
                    downPnt[1] = 0;
                    upPnt[0] = 0;
                    upPnt[1] = 0;
                    interaction = null;
                };
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted += (object sender, GH_DocObjectEventArgs e) =>
                {

                    List<Guid> deleteGuids = new List<Guid>();
                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {
                        // a part of the recursive component creation message sending check
                        if (this.remoCreatedcomponens.Contains(obj.InstanceGuid))
                        {
                            remoCreatedcomponens.Remove(obj.InstanceGuid);
                        }
                        deleteGuids.Add(obj.InstanceGuid);
                        
                        if (obj.GetType().ToString().Equals("RemoSharp.RemoParams.RemoParam"))
                        {
                            RemoParam remoParamDeleted = (RemoParam)obj;
                            Grasshopper.Instances.ActiveCanvas.MouseDown -= remoParamDeleted.ActiveCanvas_MouseDown;
                        }

                    }

                    command = new RemoDelete(username, deleteGuids);
                    SendCommands(command, commandRepeat, enable);

                    downPnt[0] = 0;
                    downPnt[1] = 0;
                    upPnt[0] = 0;
                    upPnt[1] = 0;
                    interaction = null;
                };
                #endregion

            }






            //int commandRepeatCount = 5;
            //DA.SetData(0,command);

            //if (setup > 100) setup = 5;
            //if (commandReset > commandRepeatCount) command = new RemoNullCommand(username);

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