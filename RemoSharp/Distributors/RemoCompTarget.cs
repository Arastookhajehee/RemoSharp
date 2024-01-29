using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;

using RemoSharp.RemoCommandTypes;
using Grasshopper.Kernel.Types;
using WebSocketSharp;
using GH_IO.Serialization;

namespace RemoSharp
{
    public class RemoCompTarget : GHCustomComponent
    {
        //GH_Document GrasshopperDocument;
        //IGH_Component Component;

        PushButton selectButton;
        PushButton hideButton;
        PushButton lockButton;
        PushButton syncComponents;
        StackPanel stackPanel;


        int commandRepeat = 5;

        WebSocket client;
        string username = "";
        // Move Mode variables
        //int setup = 0;

        /// <summary>
        /// Initializes a new instance of the RemoCompTarget class.
        /// </summary>
        public RemoCompTarget()
          : base("RemoCompTarget", "RemoCompT",
              "Creates, connects, disconnects, and moves components remotely on the main remote GH_Canvas",
              "RemoSharp", "RemoSetup")
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
            syncComponents = new PushButton("SyncComp",
                            "Syncs selected components' attributes.", "SyncComp");


            selectButton.OnValueChanged += SelectButton_OnValueChanged;
            hideButton.OnValueChanged += HideButton_OnValueChanged;
            lockButton.OnValueChanged += LockButton_OnValueChanged;
            syncComponents.OnValueChanged += SyncComponents_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Horizontal, true,
                selectButton, hideButton, lockButton, syncComponents
                );

            AddCustomControl(stackPanel);


            pManager.AddTextParameter("Username", "user", "This client's username", GH_ParamAccess.item, "");
            pManager.AddGenericParameter("WSClient", "wsc", "Command Websocket Client", GH_ParamAccess.item);
        }

        private void SyncComponents_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (!currentValue) return;


            List<string> types = new List<string>();
            List<Guid> guids = new List<Guid>();
            List<string> xmls = new List<string>();

            var selection = this.OnPingDocument().SelectedObjects();
            foreach (var item in selection)
            {
                string type = item.GetType().ToString();
                Guid itemGuid = item.InstanceGuid;

                GH_LooseChunk chunk = new GH_LooseChunk(null);
                item.Write(chunk);

                string xml = chunk.Serialize_Xml();

                types.Add(type);
                guids.Add(itemGuid);
                xmls.Add(xml);

            }

            RemoCompSync remoCompSync = new RemoCompSync(this.username,types,guids,xmls);
            string cmdJson = RemoCommand.SerializeToJson(remoCompSync);

            for (int i = 0; i < commandRepeat; i++)
            {
                client.Send(cmdJson);
            }

        }

        private void HideButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                List<bool> states = new List<bool>();
                List<Guid> guids = new List<Guid>();
                bool notFound = false;
                var selectionObjs = this.OnPingDocument().SelectedObjects();
                foreach (var selection in selectionObjs)
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
                                Grasshopper.Kernel.Parameters.Param_Circle paramComponentParam_Circle = (Grasshopper.Kernel.Parameters.Param_Circle)selection;
                                states.Add(!paramComponentParam_Circle.Hidden);
                                paramComponentParam_Circle.Hidden = !paramComponentParam_Circle.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Arc"):
                                Grasshopper.Kernel.Parameters.Param_Arc paramComponentParam_Arc = (Grasshopper.Kernel.Parameters.Param_Arc)selection;
                                states.Add(!paramComponentParam_Arc.Hidden);
                                paramComponentParam_Arc.Hidden = !paramComponentParam_Arc.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Curve"):
                                Grasshopper.Kernel.Parameters.Param_Curve paramComponentParam_Curve = (Grasshopper.Kernel.Parameters.Param_Curve)selection;
                                states.Add(!paramComponentParam_Curve.Hidden);
                                paramComponentParam_Curve.Hidden = !paramComponentParam_Curve.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Line"):
                                Grasshopper.Kernel.Parameters.Param_Line paramComponentParam_Line = (Grasshopper.Kernel.Parameters.Param_Line)selection;
                                states.Add(!paramComponentParam_Line.Hidden);
                                paramComponentParam_Line.Hidden = !paramComponentParam_Line.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Plane"):
                                Grasshopper.Kernel.Parameters.Param_Plane paramComponentParam_Plane = (Grasshopper.Kernel.Parameters.Param_Plane)selection;
                                states.Add(!paramComponentParam_Plane.Hidden);
                                paramComponentParam_Plane.Hidden = !paramComponentParam_Plane.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Rectangle"):
                                Grasshopper.Kernel.Parameters.Param_Rectangle paramComponentParam_Rectangle = (Grasshopper.Kernel.Parameters.Param_Rectangle)selection;
                                states.Add(!paramComponentParam_Rectangle.Hidden);
                                paramComponentParam_Rectangle.Hidden = !paramComponentParam_Rectangle.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Box"):
                                Grasshopper.Kernel.Parameters.Param_Box paramComponentParam_Box = (Grasshopper.Kernel.Parameters.Param_Box)selection;
                                states.Add(!paramComponentParam_Box.Hidden);
                                paramComponentParam_Box.Hidden = !paramComponentParam_Box.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Surface"):
                                Grasshopper.Kernel.Parameters.Param_Surface paramComponentParam_Surface = (Grasshopper.Kernel.Parameters.Param_Surface)selection;
                                states.Add(!paramComponentParam_Surface.Hidden);
                                paramComponentParam_Surface.Hidden = !paramComponentParam_Surface.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Brep"):
                                Grasshopper.Kernel.Parameters.Param_Brep paramComponentParam_Brep = (Grasshopper.Kernel.Parameters.Param_Brep)selection;
                                states.Add(!paramComponentParam_Brep.Hidden);
                                paramComponentParam_Brep.Hidden = !paramComponentParam_Brep.Hidden;
                                break;
                            case ("Grasshopper.Kernel.Parameters.Param_Mesh"):
                                Grasshopper.Kernel.Parameters.Param_Mesh paramComponentParam_Mesh = (Grasshopper.Kernel.Parameters.Param_Mesh)selection;
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
                    RemoHide cmd = new RemoHide(this.username, guids, states, DateTime.Now.Second);
                    string cmdJson = RemoCommand.SerializeToJson(cmd);

                    for (int i = 0; i < commandRepeat; i++)
                    {
                        client.Send(cmdJson);
                    }
                }
            }
        }


        private void SelectButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                var selection = this.OnPingDocument().SelectedObjects();

                List<Guid> slectionGuids = new List<Guid>();
                foreach (var item in selection)
                {
                    slectionGuids.Add(item.InstanceGuid);
                }
                RemoSelect cmd = new RemoSelect(this.username, slectionGuids, DateTime.Now.Second);
                string cmdJson = RemoCommand.SerializeToJson(cmd);

                for (int i = 0; i < commandRepeat; i++)
                {
                    client.Send(cmdJson);
                }
            }
        }

        private void LockButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                List<bool> states = new List<bool>();
                List<Guid> guids = new List<Guid>();

                var selectionObjs = this.OnPingDocument().SelectedObjects();

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    foreach (IGH_DocumentObject selection in selectionObjs)
                    {
                        guids.Add(selection.InstanceGuid);
                        if (selection is GH_Component)
                        {
                            GH_Component LockComponent = (GH_Component)selection;
                            states.Add(!LockComponent.Locked);
                            LockComponent.Locked = !LockComponent.Locked;
                        }
                        else if (selection is IGH_Param)
                        {
                            IGH_Param LockComponent = (IGH_Param)selection;
                            states.Add(!LockComponent.Locked);
                            LockComponent.Locked = !LockComponent.Locked;
                        }

                    }

                    RemoLock cmd = new RemoLock(this.username, guids, states, DateTime.Now.Second);
                    string cmdJson = RemoCommand.SerializeToJson(cmd);
                    
                    for (int i = 0; i < commandRepeat; i++)
                    {
                        client.Send(cmdJson);
                    }

                });
            }
        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddTextParameter(">⚫<       Command", ">⚫<       Command",
            //    "Complete command from RemoCompSource and RemoCompTarget regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
            //    GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            DA.GetData(0, ref username);
            DA.GetData(1, ref client);

            

        }

        //private void ActiveCanvas_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        //{
        //    if (e.KeyCode == System.Windows.Forms.Keys.Tab)
        //    {
        //        this.movingModeSwitch.CurrentValue = false;
        //    }
        //}

        //private void ActiveCanvas_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        //{
        //    if (e.KeyCode == System.Windows.Forms.Keys.Tab)
        //    {
        //        this.movingModeSwitch.CurrentValue = true;
        //    }
        //}

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