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

namespace RemoSharp
{
    public class RemoCompSource : GHCustomComponent
    {
        int setup = 0;
        int commandReset = 0;
        double distance = 0;
        Grasshopper.GUI.Canvas.GH_Canvas canvas;
        Grasshopper.GUI.Canvas.Interaction.IGH_MouseInteraction interaction;
        string command = null;
        string button;

        Point2d downPnt = new Point2d(0, 0);
        Point2d upPnt = new Point2d(0, 0);

        Point2d PointFromCanvasMouseInteraction(Grasshopper.GUI.Canvas.GH_Viewport vp, MouseEventArgs e, out string button)
        {
            Grasshopper.GUI.GH_CanvasMouseEvent mouseEvent = new Grasshopper.GUI.GH_CanvasMouseEvent(vp, e);
            float x = mouseEvent.CanvasX;
            float y = mouseEvent.CanvasY;
            double dbX = Convert.ToDouble(x);
            double dbY = Convert.ToDouble(y);
            button = mouseEvent.Button.ToString();
            return new Point2d(dbX, dbY);
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

            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "cmd", "RemoSharp Canvas Interaction Command", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            if (setup == 0)
            {
                canvas = Grasshopper.Instances.ActiveCanvas;
                #region Wire Connection and Move Sub
                canvas.MouseDown += (object sender, MouseEventArgs e) =>
                {
                    string downButton = "";
                    Point2d downPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e, out downButton);
                    if (e.Button != MouseButtons.Left ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WindowSelectInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_PanInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_ZoomInteraction)
                    {
                        command = "";
                        interaction = null;
                        return;
                    }
                    if (canvas.ActiveInteraction != null &&
                      (canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_WireInteraction ||
                      canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_RewireInteraction ||
                    canvas.ActiveInteraction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction))
                    {
                        canvas.MouseUp += (object sender2, MouseEventArgs e2) => {
                            string upButton = "";
                            upPnt = PointFromCanvasMouseInteraction(canvas.Viewport, e2, out upButton);
                            button = downButton + ";" + upButton;
                            distance = upPnt.DistanceTo(downPnt);
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
                                string conDiscon = "";
                                if (mode.ToString().Equals("Replace"))
                                {
                                    conDiscon = "True,True";
                                }
                                else if (mode.ToString().Equals("Remove"))
                                {
                                    conDiscon = "False,True";
                                }
                                else
                                {
                                    conDiscon = "True,False";
                                }

                                string sourceCoords = source.Attributes.Pivot.X + "," + source.Attributes.Pivot.Y;
                                string targetCoords = target.Attributes.Pivot.X + "," + target.Attributes.Pivot.Y;

                                string sourceGuid = source.InstanceGuid.ToString() + "," + sourceCoords;
                                string targetGuid = target.InstanceGuid.ToString() + "," + targetCoords;

                                command = "RemoConnect"
                                  + "," + conDiscon
                                  + "," + sourceGuid
                                  + "," + targetGuid;
                                commandReset = 0;

                            }
                            else if (interaction is Grasshopper.GUI.Canvas.Interaction.GH_DragInteraction)
                            {
                                int downPntX = Convert.ToInt32(downPnt.X);
                                int downPntY = Convert.ToInt32(downPnt.Y);
                                int upPntX = Convert.ToInt32(upPnt.X);
                                int upPntY = Convert.ToInt32(upPnt.Y);

                                int moveX = upPntX - downPntX;
                                int moveY = upPntY - downPntY;

                                //var movedObject = this.OnPingDocument().FindObject(new PointF(upPntX, upPntY), 1);
                                //string movedObjGuid = movedObject.InstanceGuid.ToString();

                                if (downPntX != upPntX && downPntY != upPntY)
                                {
                                    //try
                                    //{
                                        //command = "MoveComp," + downPntX + "," + downPntY + "," + moveX + "," + moveY + "," + movedObjGuid;
                                        command = "MoveComp," + downPntX + "," + downPntY + "," + moveX + "," + moveY;
                                        downPnt = new Point2d(0, 0);
                                        upPnt = new Point2d(0, 0);
                                        commandReset = 0;
                                    //}
                                    //catch
                                    //{
                                    //    command = "";
                                    //}
                                }
                                else command = "";
                            }

                        };
                        interaction = canvas.ActiveInteraction;
                    }
                };
                #endregion

                #region Add Object Sub
                this.OnPingDocument().ObjectsAdded += (object sender, GH_DocObjectEventArgs e) =>
                {
                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {
                        string name = obj.Name;

                        var newCompGuid = obj.InstanceGuid.ToString();
                        var compTypeString = obj.GetType().ToString();
                        var pivot = obj.Attributes.Pivot;

                        command = "RemoCreate" + "," + compTypeString + "," + newCompGuid + "," + (int)pivot.X + "," + (int)pivot.Y;

                        if (compTypeString.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                        {
                            Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider) obj;
                            decimal minBound = sliderComponent.Slider.Minimum;
                            decimal maxBound = sliderComponent.Slider.Maximum;
                            decimal currentValue = sliderComponent.Slider.Value;
                            int accuracy = sliderComponent.Slider.DecimalPlaces;
                            var sliderType = sliderComponent.Slider.Type;
                            command += "," + minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType;

                            downPnt = new Point2d(0, 0);
                            upPnt = new Point2d(0, 0);
                            interaction = null;
                            commandReset = 0;
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
                            command += "," + multiLine + "," + drawIndicies + "," + drawPaths + "," + wrap + "," + alignment.ToString() + "," + panelSizeX + "," + panelSizeY + "," + content;
                        }
                        else if (compTypeString.Equals("RemoSharp.RemoGeomStreamer"))
                        {

                            StreamIPSet gmAddress = new StreamIPSet();
                            gmAddress.ShowDialog();
                            string address = gmAddress.WS_Server_Address;



                            System.Drawing.PointF geomPivot = new System.Drawing.PointF((int)pivot.X, (int)pivot.Y);
                            System.Drawing.PointF panelPivot = new System.Drawing.PointF(geomPivot.X - 75, geomPivot.Y - 80);
                            System.Drawing.PointF buttnPivot = new System.Drawing.PointF(geomPivot.X - 100, geomPivot.Y - 40);
                            System.Drawing.PointF wssPivot = new System.Drawing.PointF(geomPivot.X + 34, geomPivot.Y - 50);
                            System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(geomPivot.X + 42, geomPivot.Y - 40);

                            Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                            panel.CreateAttributes();
                            panel.Attributes.Pivot = panelPivot;
                            panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 55, 20);
                            panel.SetUserText(address);

                            Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                            button.CreateAttributes();
                            button.Attributes.Pivot = buttnPivot;

                            RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                            wss.CreateAttributes();
                            wss.Attributes.Pivot = wssPivot;

                            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                            wsSend.CreateAttributes();
                            wsSend.Attributes.Pivot = wsSendPivot;

                            RemoSharp.RemoGeomStreamer RemoGeom = (RemoSharp.RemoGeomStreamer)this.OnPingDocument().FindObject(geomPivot, 3);

                            this.OnPingDocument().ScheduleSolution(1, doc =>
                            {
                                this.OnPingDocument().AddObject(panel, true);
                                this.OnPingDocument().AddObject(button, true);
                                this.OnPingDocument().AddObject(wss, true);
                                this.OnPingDocument().AddObject(wsSend, true);

                                wss.Params.Input[2].AddSource((IGH_Param)button);
                                wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                                wsSend.Params.Input[1].AddSource((IGH_Param)RemoGeom.Params.Output[0]);
                                wss.Params.Input[0].AddSource((IGH_Param)panel);
                            });
                            command += "," + address;
                        }
                        else if (compTypeString.Equals("RemoSharp.RemoGeomParser"))
                        {

                            StreamIPSet gmAddress = new StreamIPSet();
                            gmAddress.ShowDialog();
                            string address = gmAddress.WS_Server_Address;

                            System.Drawing.PointF geomPivot = new System.Drawing.PointF((int)pivot.X, (int)pivot.Y);
                            System.Drawing.PointF panelPivot = new System.Drawing.PointF(geomPivot.X - 375, geomPivot.Y - 121);
                            System.Drawing.PointF buttnPivot = new System.Drawing.PointF(geomPivot.X - 290, geomPivot.Y - 85);
                            System.Drawing.PointF wssPivot = new System.Drawing.PointF(geomPivot.X - 304, geomPivot.Y + 6);
                            System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(geomPivot.X - 159, geomPivot.Y);

                            Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                            panel.CreateAttributes();
                            panel.Attributes.Pivot = panelPivot;
                            panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 300, 20);
                            panel.SetUserText(address);

                            Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                            button.CreateAttributes();
                            button.Attributes.Pivot = buttnPivot;

                            RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                            wss.CreateAttributes();
                            wss.Attributes.Pivot = wssPivot;

                            RemoSharp.WsClientCat.WsClientRecv wsRecv = new WsClientCat.WsClientRecv();
                            wsRecv.CreateAttributes();
                            wsRecv.Attributes.Pivot = wsRecvPivot;


                            RemoSharp.RemoGeomParser remoGeomParser = (RemoSharp.RemoGeomParser)this.OnPingDocument().FindObject(geomPivot, 3);

                            this.OnPingDocument().ScheduleSolution(1, doc =>
                            {
                                this.OnPingDocument().AddObject(panel, true);
                                this.OnPingDocument().AddObject(button, true);
                                this.OnPingDocument().AddObject(wss, true);
                                this.OnPingDocument().AddObject(wsRecv, true);
                                this.OnPingDocument().AddObject(remoGeomParser, true);

                                wss.Params.Input[2].AddSource((IGH_Param)button);
                                wsRecv.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                                remoGeomParser.Params.Input[0].AddSource((IGH_Param)wsRecv.Params.Output[0]);
                                wss.Params.Input[0].AddSource((IGH_Param)panel);
                            });
                            command += "," + address;
                        }
                        else 
                        {
                            downPnt = new Point2d(0, 0);
                            upPnt = new Point2d(0, 0);
                            interaction = null;
                            commandReset = 0;
                        }


                    }
                };
                #endregion

                #region Remove Object Sub
                this.OnPingDocument().ObjectsDeleted += (object sender, GH_DocObjectEventArgs e) =>
                {

                    var objs = e.Objects;
                    foreach (var obj in objs)
                    {
                        string name = obj.Name;

                        var compGuid = obj.InstanceGuid.ToString();
                        var compTypeString = obj.GetType().ToString();
                        var pivot = obj.Attributes.Pivot;
                        command = "Deletion,True" + "," + ((int)pivot.X + 1) + "," + ((int)pivot.Y + 1) + "," + compGuid;
                        downPnt = new Point2d(0, 0);
                        upPnt = new Point2d(0, 0);
                        interaction = null;
                        commandReset = 0;
                    }
                };
                #endregion

            }
            int commandRepeatCount = 5;
            DA.SetData(0,command);
            if (setup > 100) setup = 5;
            if (commandReset > commandRepeatCount) command = "";

            setup++;
            commandReset++;
        }

        private System.Guid GetComponentGuidAnd_Input_Index(
    System.Drawing.Point mouseLocation,
    int offsetMin,
    int offsetMax,
    out int paramIndex,
    out bool isSpecial)
        {
            var component = this.OnPingDocument().FindObject((System.Drawing.PointF)mouseLocation, 2);
            string componentType = component.GetType().ToString();

            if (componentType.Contains("Grasshopper.Kernel.Special") || componentType.Contains("Grasshopper.Kernel.Parameters"))
            {
                System.Guid compGuid = component.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {

                var foundInput = this.OnPingDocument().FindInputParameter(mouseLocation);
                for (int i = offsetMax; i > offsetMin; i -= 3)
                {
                    mouseLocation = System.Drawing.Point.Add(mouseLocation, new System.Drawing.Size(i, 0));
                    foundInput = this.OnPingDocument().FindInputParameter(mouseLocation);
                    if (foundInput != null) break;
                }
                if (foundInput == null)
                {
                    paramIndex = -1;
                    isSpecial = false;
                    return Guid.Empty;
                }
                else
                {
                    var foundComponent = (IGH_Component)foundInput.Attributes.Parent.DocObject;
                    int index = foundComponent.Params.Input.IndexOf(foundInput);

                    paramIndex = index;
                    isSpecial = false;
                    return foundComponent.InstanceGuid;
                }
            }

        }

        private System.Guid GetComponentGuidAnd_Output_Index(
          System.Drawing.Point mouseLocation,
          int offsetMin,
          int offsetMax,
          out int paramIndex,
          out bool isSpecial)
        {
            var component = this.OnPingDocument().FindObject((System.Drawing.PointF)mouseLocation, 2);
            string componentType = component.GetType().ToString();

            if (componentType.Contains("Grasshopper.Kernel.Special") || componentType.Contains("Grasshopper.Kernel.Parameters"))
            {
                System.Guid compGuid = component.InstanceGuid;
                paramIndex = -1;
                isSpecial = true;
                return compGuid;
            }
            else
            {
                var foundOut = this.OnPingDocument().FindOutputParameter(mouseLocation);
                for (int i = offsetMax; i > offsetMin; i -= 3)
                {
                    mouseLocation = System.Drawing.Point.Add(mouseLocation, new System.Drawing.Size(i, 0));
                    foundOut = this.OnPingDocument().FindOutputParameter(mouseLocation);
                    if (foundOut != null) break;
                }
                if (foundOut == null)
                {
                    paramIndex = -1;
                    isSpecial = false;
                    return Guid.Empty;
                }
                else
                {
                    var foundComponent = (IGH_Component)foundOut.Attributes.Parent.DocObject;
                    int index = foundComponent.Params.Output.IndexOf(foundOut);

                    paramIndex = index;
                    isSpecial = false;
                    return foundComponent.InstanceGuid;
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