using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp
{
    public class RemoCompTarget : GHCustomComponent
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        PushButton createButton;
        PushButton selectButton;
        PushButton hideButton;
        PushButton lockButton;
        PushButton remoParamButton;
        ToggleSwitch deleteToggle;
        PushButton wsClientButton;
        PushButton deleteButton;
        StackPanel stackPanel;
        StackPanel stackPanel01;



        bool create = false;
        bool select = false;
        bool delete = false;
        bool hide = false;
        bool lockThis = false;
        bool remoParam = false;

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
        int con_DisConCounter = 0;

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
            createButton = new PushButton("Create",
                "Create a new instance of the close component on the main remote GH_Canvas.", "Create");
            selectButton = new PushButton("Select",
                "Select a component on the main remote GH_Canvas.", "Sel");
            hideButton = new PushButton("Hide",
                "Hides a component on the main remote GH_Canvas.", "Hide");
            lockButton = new PushButton("Lock",
                            "Unhides a component on the main remote GH_Canvas.", "Lock");
            remoParamButton = new PushButton("RmoPrm",
                            "Creates the necessary remote parameter components", "RmoPrm");
            deleteToggle = new ToggleSwitch("DelAfterCreation","Delete Component Upon Remote Creation",false);
            wsClientButton = new PushButton("WSC",
                "Creates The Required WS Client Components To Broadcast Canvas Screen.", "WSC");
            deleteButton = new PushButton("Del",
                "Deletes a component on the main remote GH_Canvas.", "Del");

            createButton.OnValueChanged += PushButton1_OnValueChanged;
            selectButton.OnValueChanged += SelectButton_OnValueChanged;
            hideButton.OnValueChanged += PushButton2_OnValueChanged;
            lockButton.OnValueChanged += PushButton3_OnValueChanged;
            remoParamButton.OnValueChanged += PushButton4_OnValueChanged;
            deleteToggle.OnValueChanged += PushButton5_OnValueChanged;
            wsClientButton.OnValueChanged += PushButton6_OnValueChanged;
            deleteButton.OnValueChanged += PushButton7_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Horizontal, true,
                createButton, selectButton, hideButton, lockButton
                );
            stackPanel01 = new StackPanel("C2", Orientation.Horizontal, true,
                deleteButton, remoParamButton,  wsClientButton
                );
            AddCustomControl(stackPanel);
            AddCustomControl(stackPanel01);
            AddCustomControl(deleteToggle);

            pManager.AddTextParameter("SourceCommand", "SrcCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item, "");
        }

        private void SelectButton_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                select = currentValue;
                this.ExpireSolution(true);
            }
        }

        private void PushButton7_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                delete = currentValue;
                this.ExpireSolution(true);
            }
        }

        private void PushButton1_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                create = currentValue;
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

        private void PushButton4_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                remoParam = currentValue;
                this.ExpireSolution(true);
            }
        }

        private void PushButton5_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            DeleteThisCompBool = currentValue;
            this.ExpireSolution(true);
        }

        private void PushButton6_OnValueChanged(object sender, ValueChangeEventArgumnet e)
        {
            bool currentValue = Convert.ToBoolean(e.Value);
            if (currentValue)
            {
                int shiftX = 192;
                int shiftY = -40;
                System.Drawing.PointF pivot = this.Attributes.Pivot;

                foreach (var obj in this.OnPingDocument().Objects)
                {
                    try
                    {
                        if (obj.GetType().ToString().Equals("RemoSharp.ClientCanvasBounds"))
                        {
                            pivot = obj.Attributes.Pivot;
                            shiftX = -29;
                            shiftY = +240;
                        }
                    }
                    catch { }
                }

                System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 255 + shiftX, pivot.Y - 66 + shiftY);
                System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 255 + shiftX, pivot.Y - 18 + shiftY);
                System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 57 + shiftX, pivot.Y - 27 + shiftY);
                System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 60 + shiftX, pivot.Y - 17 + shiftY);


                //StreamIPSet commandAddress = new StreamIPSet();
                //commandAddress.DialougeTitle.Text = "Please Set Your Command Server Address";
                //commandAddress.ShowDialog();
                string commandSrvAddress = "";
                Grasshopper.Kernel.Special.GH_Panel panel = new Grasshopper.Kernel.Special.GH_Panel();
                panel.CreateAttributes();
                panel.Attributes.Pivot = panelPivot;
                panel.Attributes.Bounds = new System.Drawing.RectangleF(panelPivot.X, panelPivot.Y, 100, 20);
                panel.SetUserText(commandSrvAddress);
                panel.NickName = "RemoSharp Command Server";


                Grasshopper.Kernel.Special.GH_ButtonObject button = new Grasshopper.Kernel.Special.GH_ButtonObject();
                button.CreateAttributes();
                button.Attributes.Pivot = buttnPivot;
                button.NickName = "RemoSharp";

                RemoSharp.WsClientCat.WsClientStart wss = new WsClientCat.WsClientStart();
                wss.CreateAttributes();
                wss.Attributes.Pivot = wssPivot;
                wss.Params.RepairParamAssociations();

                RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
                wsSend.CreateAttributes();
                wsSend.Attributes.Pivot = wsSendPivot;
                wsSend.Params.RepairParamAssociations();

                this.OnPingDocument().ScheduleSolution(1, doc =>
                {
                    this.OnPingDocument().AddObject(panel, true);
                    this.OnPingDocument().AddObject(button, true);
                    this.OnPingDocument().AddObject(wss, true);
                    this.OnPingDocument().AddObject(wsSend, true);

                    if (!commandSrvAddress.Equals("")) wss.Params.Input[0].AddSource((IGH_Param)panel);
                    wss.Params.Input[2].AddSource((IGH_Param)button);
                    wsSend.Params.Input[0].AddSource((IGH_Param)wss.Params.Output[0]);
                    wsSend.Params.Input[1].AddSource((IGH_Param)this.Params.Output[0]);
                });

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
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            string cmd = "";
            if (cmd == null) return;
            // parsing the incoming command
            DA.GetData(0, ref cmd);

            string[] cmds = cmd.Split(',');



            if (create)
            {
                try
                {
                    // getting the GH Document
                    var thisDoc = this.GrasshopperDocument;

                    // getting the type of the closest component on the canvas in string format
                    // getting its location too
                    System.Drawing.PointF newPivot;
                    int currentComponentIndex = -1;
                    string typeName = FindClosestObjectTypeOnCanvas(out newPivot, out currentComponentIndex);
                    int otherCompX = Convert.ToInt32(newPivot.X);
                    int otherCompY = Convert.ToInt32(newPivot.Y);

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

                    cmd = "RemoCreate," + type + "," + otherCompX + "," + otherCompY;

                    if (
                        type.ToString().Equals("Grasshopper.Kernel.Special.GH_NumberSlider") ||
                        type.ToString().Equals("Grasshopper.Kernel.Special.GH_ButtonObject") ||
                        type.ToString().Equals("Grasshopper.Kernel.Special.GH_BooleanToggle") ||
                        type.ToString().Equals("Grasshopper.Kernel.Special.GH_ColourSwatch") ||
                        type.ToString().Equals("Grasshopper.Kernel.Special.GH_Panel") ||
                        type.ToString().Equals("RemoSharp.RemoGeomStreamer") ||
                        type.ToString().Equals("RemoSharp.RemoGeomParser") 
                        )
                    {
                        DeleteThisCompBool = false;
                    }

                    if(type.ToString().Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                    {
                        Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider)this.OnPingDocument().Objects[currentComponentIndex];
                        decimal minBound = sliderComponent.Slider.Minimum;
                        decimal maxBound = sliderComponent.Slider.Maximum;
                        decimal currentValue = sliderComponent.Slider.Value;
                        int accuracy = sliderComponent.Slider.DecimalPlaces;
                        var sliderType = sliderComponent.Slider.Type;
                        cmd += "," + minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType;
                    }

                    else if(type.ToString().Equals("Grasshopper.Kernel.Special.GH_Panel"))
                    {
                        Grasshopper.Kernel.Special.GH_Panel panelComponent = (Grasshopper.Kernel.Special.GH_Panel) this.OnPingDocument().Objects[currentComponentIndex];
                        bool multiLine = panelComponent.Properties.Multiline;
                        bool drawIndicies = panelComponent.Properties.DrawIndices;
                        bool drawPaths = panelComponent.Properties.DrawPaths;
                        bool wrap = panelComponent.Properties.Wrap;
                        Grasshopper.Kernel.Special.GH_Panel.Alignment alignment = panelComponent.Properties.Alignment;
                        float panelSizeX = panelComponent.Attributes.Bounds.Width;
                        float panelSizeY = panelComponent.Attributes.Bounds.Height;

                        string content = panelComponent.UserText;
                        
                        cmd += "," + multiLine + "," + drawIndicies + "," + drawPaths + "," + wrap + "," + alignment.ToString() + "," + panelSizeX + "," + panelSizeY + "," + content;
                    }

                    else if (type.ToString().Equals("RemoSharp.RemoGeomStreamer"))
                    {
                        StreamIPSet gmAddress = new StreamIPSet();
                        gmAddress.ShowDialog();
                        string address = gmAddress.WS_Server_Address;



                        System.Drawing.PointF pivot = new System.Drawing.PointF(otherCompX, otherCompY);
                        System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 75, pivot.Y - 80);
                        System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 100, pivot.Y - 40);
                        System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X + 34, pivot.Y - 50);
                        System.Drawing.PointF wsSendPivot = new System.Drawing.PointF(pivot.X + 42, pivot.Y - 40);

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

                        RemoSharp.RemoGeomStreamer RemoGeom = (RemoSharp.RemoGeomStreamer) this.OnPingDocument().FindObject(pivot, 3);

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
                        cmd += "," + address;
                    }

                    else if (typeName.Equals("RemoSharp.RemoGeomParser"))
                    {
                        StreamIPSet gmAddress = new StreamIPSet();
                        gmAddress.ShowDialog();
                        string address = gmAddress.WS_Server_Address;

                        System.Drawing.PointF pivot = new System.Drawing.PointF(otherCompX, otherCompY);
                        System.Drawing.PointF panelPivot = new System.Drawing.PointF(pivot.X - 375, pivot.Y - 121);
                        System.Drawing.PointF buttnPivot = new System.Drawing.PointF(pivot.X - 290, pivot.Y - 85);
                        System.Drawing.PointF wssPivot = new System.Drawing.PointF(pivot.X - 304, pivot.Y + 6);
                        System.Drawing.PointF wsRecvPivot = new System.Drawing.PointF(pivot.X - 159, pivot.Y);

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

                        
                        RemoSharp.RemoGeomParser remoGeomParser = (RemoSharp.RemoGeomParser)this.OnPingDocument().FindObject(pivot, 3);

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
                        cmd += "," + address;
                    }

                    DA.SetData(0, cmd);
                    create = false;
                }


                catch (Exception e)
                {
                    this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                }
            }
           
            if (delete)
            {
                var pivot = this.Component.Attributes.Pivot;
                int otherCompInx = DeletionFindComponentOnCanvasByCoordinates(pivot.X, pivot.Y);
                var otherComp = this.GrasshopperDocument.Objects[otherCompInx];
                int coordX = Convert.ToInt32(otherComp.Attributes.Pivot.X);
                int coordY = Convert.ToInt32(otherComp.Attributes.Pivot.Y);

                string otherCompCoords = coordX + "," + coordY;
                cmd = "Deletion," + delete + "," + otherCompCoords;
                DA.SetData(0, cmd);
                delete = false;
            }

            if (hide)
            {
                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 15;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd = "RemoHide," + thisCompX + "," + thisCompY + "," + DateTime.Now.Second;
                DA.SetData(0, cmd);
                hide = false;
            }

            if (select)
            {
                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 15;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd = "Selection," + thisCompX + "," + thisCompY + "," + DateTime.Now.Second;
                DA.SetData(0, cmd);
                select = false;
            }

            if (lockThis)
            {
                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 15;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd = "RemoLock," + thisCompX + "," + thisCompY + "," + DateTime.Now.Second;
                DA.SetData(0, cmd);
                lockThis = false;
            }

            if (remoParam)
            {

                componentType = FindClosestObjectTypeOnCanvas(out compPivot, out RemoMakeindex);
                if (componentType.Equals("Grasshopper.Kernel.Special.GH_ButtonObject"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoButton);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_BooleanToggle"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoToggle);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_Panel"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoPanel);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_ColourSwatch"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoColorSwatch);
                }
                else if (componentType.Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                {
                    this.Component.OnPingDocument().ScheduleSolution(0, MakeRemoSlider);
                }
                remoParam = false;
                return;
            }


            string[] outGoingCommand = cmd.Split(',');
            int commandRepeatCount = 7;
            if (   outGoingCommand[0] == "RemoConnect" 
                || outGoingCommand[0] == "MoveComp"
                || outGoingCommand[0] == "RemoCreate"
                || outGoingCommand[0] == "RemoHide"
                || outGoingCommand[0] == "RemoLock"
                || outGoingCommand[0] == "Deletion")
            {
                con_DisConCounter = 0;
                currentConnectString = cmd ;
                if (outGoingCommand[0] == "RemoCreate" && DeleteThisCompBool)
                {
                    System.Drawing.PointF deletionPivot;
                    FindClosestObjectTypeOnCanvas(out deletionPivot, out DeleteThisComp);

                    this.OnPingDocument().ScheduleSolution(0, DeleteObjectAbove);
                }
            }
            if (con_DisConCounter < commandRepeatCount)
            {
                // a trigger is already connected to the component. We don't need extra repeated runs
                //this.Component.OnPingDocument().ScheduleSolution(15, doc =>
                //{
                //    this.Component.ExpireSolution(false);
                //});
                DA.SetData(0, currentConnectString);
            }
            else currentConnectString = "";
            con_DisConCounter++;

            if (cmds[0] == "MoveComp")
            {
                DA.SetData(0, cmd);
                return;
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




        private string FindClosestObjectTypeOnCanvas(out System.Drawing.PointF compPivot, out int compIndex)
        {
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.GrasshopperDocument;
            var ghObjects = ghDoc.Objects;
            var thisCompLoc = this.Component.Attributes.Pivot;
            int cmpIndex = -1;
            // finding the closest component
            string componentType = "";
            double minDistance = double.MaxValue;
            System.Drawing.PointF newPivot = new System.Drawing.PointF(0, 0);
            try
            {
                for (int i = 0; i < ghObjects.Count; i++)
                {

                    var component = ghObjects[i];
                    var pivot = component.Attributes.Pivot;
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));

                    if (distance < minDistance)
                    {
                        if (distance > 0)
                        {
                            // getting the type of the component via the ToString() method
                            // later the ToString() method is better to be changed to something more reliable
                            minDistance = distance;
                            componentType = component.ToString();
                            newPivot = component.Attributes.Pivot;
                            cmpIndex = i;
                        }
                    }
                }
            }
            catch { }
            compPivot = newPivot;
            compIndex = cmpIndex;
            return componentType;
        }

        private int DeletionFindComponentOnCanvasByCoordinates(float compX, float compY)
        {
            int compCoordX = Convert.ToInt32(compX);
            int compCoordY = Convert.ToInt32(compY);
            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.GrasshopperDocument;
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
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X)
                                              * (thisCompLoc.X - pivot.X)
                                              + (thisCompLoc.Y - pivot.Y)
                                              * (thisCompLoc.Y - pivot.Y));
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

        private void RecognizeAndMake(string typeName, int pivotX, int pivotY)
        {
            var thisDoc = this.Component.OnPingDocument();
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


        private IGH_Param FindWebSocketClientComponentOutput()
        {
            var recepients = this.Params.Output[0].Recipients;
            IGH_Param thisCompOutput = recepients[0];
            IGH_Component WSClientWSC = new WsClientCat.WsClientStart();
            bool wssfound = false;
            foreach (IGH_Param item in this.Params.Output[0].Recipients)
            {
                try
                {
                    var wsSendComponentGuid = item.Attributes.Parent.InstanceGuid;
                    WsClientCat.WsClientSend wsSendComponent = (WsClientCat.WsClientSend)this.OnPingDocument().FindObject(wsSendComponentGuid, false);
                    var wsSendInputSource = wsSendComponent.Params.Input[0].Sources[0].Attributes.Parent.InstanceGuid;
                    WSClientWSC = (WsClientCat.WsClientStart)this.OnPingDocument().FindObject(wsSendInputSource, false);
                    wssfound = true;
                }
                catch 
                {
                }

            }

            if (wssfound) return WSClientWSC.Params.Output[0];
            else return null;
        }

        private void MakeRemoButton(GH_Document doc)
        {
            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            System.Drawing.Size vec = new System.Drawing.Size(-160, 25);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_ButtonObject button = (Grasshopper.Kernel.Special.GH_ButtonObject)otherComp;

            RemoSharp.RemoButton remoButton = new RemoButton();
            remoButton.CreateAttributes();
            remoButton.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39, pivotY + 37);

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24);

            remoButton.Params.Input[0].AddSource( (IGH_Param) button);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoButton.Params.Output[0]);
            try { wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput()); }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoButton, true);
            this.OnPingDocument().AddObject(wsSend, true);
        }

        private void MakeRemoToggle(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            System.Drawing.Size vec = new System.Drawing.Size(-160, 25);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_BooleanToggle toggle = (Grasshopper.Kernel.Special.GH_BooleanToggle)otherComp;

            RemoSharp.RemoToggle remoToggle = new RemoToggle();
            remoToggle.CreateAttributes();
            remoToggle.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39, pivotY + 37);

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24);

            remoToggle.Params.Input[0].AddSource((IGH_Param)toggle);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoToggle.Params.Output[0]);
            try { wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput()); }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoToggle, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoPanel(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            Grasshopper.Kernel.Special.GH_Panel panel = (Grasshopper.Kernel.Special.GH_Panel)otherComp;

            float panelSizeX = panel.Attributes.Bounds.Width;
            float panelSizeY = panel.Attributes.Bounds.Height;
            System.Drawing.Size vec = new System.Drawing.Size(-180, 25);
            if (panelSizeX > 280 && panelSizeY < 50) vec = new System.Drawing.Size(-180, -25);
            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            
            RemoSharp.RemoPanel remoPanel = new RemoPanel();
            remoPanel.CreateAttributes();
            remoPanel.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39, pivotY + 37);

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24);

            remoPanel.Params.Input[0].AddSource((IGH_Param)panel);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoPanel.Params.Output[0]);
            try { wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput()); }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoPanel, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoColorSwatch(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            System.Drawing.Size vec = new System.Drawing.Size(-160, 35);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_ColourSwatch color = (Grasshopper.Kernel.Special.GH_ColourSwatch)otherComp;

            RemoSharp.RemoColorSwatch remoColor = new RemoColorSwatch();
            remoColor.CreateAttributes();
            remoColor.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39, pivotY + 37);

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24);

            remoColor.Params.Input[1].AddSource((IGH_Param)color);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoColor.Params.Output[0]);
            try { wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput()); }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoColor, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void MakeRemoSlider(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            System.Drawing.Size vec = new System.Drawing.Size(-220, 35);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            Grasshopper.Kernel.Special.GH_NumberSlider slider = (Grasshopper.Kernel.Special.GH_NumberSlider)otherComp;

            RemoSharp.RemoSlider remoSlider = new RemoSlider();
            remoSlider.CreateAttributes();
            remoSlider.Attributes.Pivot = new System.Drawing.PointF(pivotX + 39, pivotY + 37);

            RemoSharp.WsClientCat.WsClientSend wsSend = new WsClientCat.WsClientSend();
            wsSend.CreateAttributes();
            wsSend.Attributes.Pivot = new System.Drawing.PointF(pivotX + 200, pivotY + 24);

            remoSlider.Params.Input[1].AddSource((IGH_Param)slider);
            wsSend.Params.Input[1].AddSource((IGH_Param)remoSlider.Params.Output[0]);
            try { wsSend.Params.Input[0].AddSource(FindWebSocketClientComponentOutput()); }
            catch { }
            wsSend.Params.Input[0].WireDisplay = GH_ParamWireDisplay.hidden;

            this.OnPingDocument().AddObject(remoSlider, true);
            this.OnPingDocument().AddObject(wsSend, true);

        }

        private void DeleteObjectAbove(GH_Document doc)
        {
            var obj = this.OnPingDocument().Objects[DeleteThisComp];
            this.OnPingDocument().RemoveObject(obj, true);
        }
    }
}