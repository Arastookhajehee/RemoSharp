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

        PushButton pushButton1;
        PushButton pushButton2;
        PushButton pushButton3;
        PushButton pushButton4;
        PushButton pushButton5;
        StackPanel stackPanel;
        StackPanel stackPanel01;

        bool create = false;
        bool hide = false;
        bool unhide = false;
        bool remoParam = false;

        // remoParam public variables
        public string componentType = "";
        public int RemoMakeindex = -1;
        public int DeleteThisComp = -1;
        public int GHbuttonComp = -1;
        public int remoButtonComp = -1;
        public int wsButtonComp = -1;
        public System.Drawing.PointF compPivot;

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
            pushButton1 = new PushButton("Create",
                "Create a new instance of the close component on the main remote GH_Canvas.", "Create");
            pushButton2 = new PushButton("Hide",
                "Hides a component on the main remote GH_Canvas.", "Hide");
            pushButton3 = new PushButton("Unhide",
                            "Unhides a component on the main remote GH_Canvas.", "Unhide");
            pushButton4 = new PushButton("RemoPram",
                            "Creates the necessary remote parameter components", "RemoPram");
            pushButton5 = new PushButton("DelThis",
                "Deletes the component on the top on THIS GH_Canvas", "DelThis");


            pushButton1.OnValueChanged += PushButton1_OnValueChanged;
            pushButton2.OnValueChanged += PushButton2_OnValueChanged;
            pushButton3.OnValueChanged += PushButton3_OnValueChanged;
            pushButton4.OnValueChanged += PushButton4_OnValueChanged;
            pushButton5.OnValueChanged += PushButton5_OnValueChanged;

            stackPanel = new StackPanel("C1", Orientation.Horizontal, true,
                pushButton1, pushButton2, pushButton3
                );
            stackPanel01 = new StackPanel("C2", Orientation.Horizontal, true,
                pushButton4, pushButton5
                );
            AddCustomControl(stackPanel);
            AddCustomControl(stackPanel01);

            pManager.AddTextParameter("SourceCommand", "SrcCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item, "");
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
                unhide = currentValue;
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
            if (currentValue)
            {
                System.Drawing.PointF newPivot;
                FindClosestObjectTypeOnCanvas(out newPivot, out DeleteThisComp);

                this.OnPingDocument().ScheduleSolution(0, DeleteObjectAbove);
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

                    if (type.ToString().Equals("Grasshopper.Kernel.Special.GH_NumberSlider"))
                    {
                        Grasshopper.Kernel.Special.GH_NumberSlider sliderComponent = (Grasshopper.Kernel.Special.GH_NumberSlider)this.OnPingDocument().Objects[currentComponentIndex];
                        decimal minBound = sliderComponent.Slider.Minimum;
                        decimal maxBound = sliderComponent.Slider.Maximum;
                        decimal currentValue = sliderComponent.Slider.Value;
                        int accuracy = sliderComponent.Slider.DecimalPlaces;
                        var sliderType = sliderComponent.Slider.Type;
                        cmd += "," + minBound + "," + maxBound + "," + currentValue + "," + accuracy + "," + sliderType;
                    }

                    DA.SetData(0, cmd);
                    create = false;
                    return;
                }
                catch (Exception e)
                {
                    this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
                }
            }

            if (hide)
            {
                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 1;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd = "RemoHide," + thisCompX + "," + thisCompY;
                DA.SetData(0, cmd);
                hide = false;
                return;
            }

            if (unhide)
            {
                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 1;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd = "RemoUnhide," + thisCompX + "," + thisCompY;
                DA.SetData(0, cmd);
                unhide = false;
                return;
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

            if (cmds[0] == "RemoConnect")
            {

                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 1;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                cmd += "," + thisCompX + "," + thisCompY;
                DA.SetData(0, cmd);
                return;
            }

            if (cmds[0] == "MoveComp")
            {

                int compX = Convert.ToInt32(cmds[1]);
                int compY = Convert.ToInt32(cmds[2]);

                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X) + 1;
                int thisCompY = Convert.ToInt32(thisCompPivot.Y) - 27;

                int translationX = thisCompX - compX;
                int translationY = thisCompY - compY;

                cmd += "," + translationX + "," + translationY;
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

            RecognizeAndMake("RemoSharp.RemoButton", pivotX + 39, pivotY + 37);
            RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 200, pivotY + 48);

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

            RecognizeAndMake("RemoSharp.RemoToggle", pivotX + 39, pivotY + 37);
            RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 200, pivotY + 48);

        }

        private void MakeRemoPanel(GH_Document doc)
        {

            int pivotX = Convert.ToInt32(compPivot.X);
            int pivotY = Convert.ToInt32(compPivot.Y);
            var otherComp = this.Component.OnPingDocument().Objects[RemoMakeindex];

            GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

            grip.CreateObjectData(otherComp);

            this.Component.OnPingDocument().DeselectAll();
            this.Component.OnPingDocument().Select(grip);
            System.Drawing.Size vec = new System.Drawing.Size(-180, 25);
            this.Component.OnPingDocument().TranslateObjects(vec, true);
            this.Component.OnPingDocument().DeselectAll();

            RecognizeAndMake("RemoSharp.RemoPanel", pivotX + 39, pivotY + 37);
            RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 200, pivotY + 48);

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

            RecognizeAndMake("RemoSharp.RemoColorSwatch", pivotX + 39, pivotY + 39);
            RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 200, pivotY + 27);

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

            RecognizeAndMake("RemoSharp.RemoSlider", pivotX + 39, pivotY + 39);
            RecognizeAndMake("Bengesht.WsClientCat.WsClientSend", pivotX + 200, pivotY + 27);

        }

        private void DeleteObjectAbove(GH_Document doc)
        {
            var obj = this.OnPingDocument().Objects[DeleteThisComp];
            this.OnPingDocument().RemoveObject(obj, true);
        }
    }
}