using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
namespace RemoSharp
{
    public class RemoCompExecutor : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        public int srcComp = -1;
        public int tgtComp = -1;
        public int srcCompOutputIndex = -1;
        public int tgtCompInputIndex = -1;

        /// <summary>
        /// Initializes a new instance of the RemoCompExecutor class.
        /// </summary>
        public RemoCompExecutor()
          : base("RemoCompExecutor", "RemoCompEx",
              "Excecutes the Creation, connection, disconnection, and move commands on its active GH_Canvas",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("CompleteCommand", "CompCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item, "");
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
            Component = this;
            GrasshopperDocument = this.OnPingDocument();

            string cmd = "";
            if (cmd == null) return;
            // parsing the incoming command
            DA.GetData(0, ref cmd);

            string[] cmds = cmd.Split(',');


            if (cmds[0] == "MoveComp")
            {
                int compX = Convert.ToInt32(cmds[1]);
                int compY = Convert.ToInt32(cmds[2]);
                int trsX = Convert.ToInt32(cmds[3]);
                int trsY = Convert.ToInt32(cmds[4]);

                int otherCompInx = MoveCompFindComponentOnCanvasByCoordinates(compX, compY);
                var otherComp = this.GrasshopperDocument.Objects[otherCompInx];

                GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);

                grip.CreateObjectData(otherComp);
                this.GrasshopperDocument.Select(grip);

                Size vec = new Size(trsX, trsY);

                this.GrasshopperDocument.TranslateObjects(vec, true);
                this.GrasshopperDocument.DeselectAll();
                return;
            }

            if (cmds[0] == "RemoCreate")
            {

                string typeName = cmds[1];
                int pivotX = Convert.ToInt32(cmds[2]);
                int pivotY = Convert.ToInt32(cmds[3]);

                try
                {
                    RecognizeAndMake(typeName, pivotX, pivotY);
                }
                catch (Exception e)
                {
                    this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
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

                var ghDocument = this.OnPingDocument();
                var ghObjectsList = ghDocument.Objects;

                srcComp = RemoConnectFindComponentOnCanvasByCoordinates(srcPivotX, srcPivotY);
                tgtComp = RemoConnectFindComponentOnCanvasByCoordinates(tgtPivotX, tgtPivotY);
                var srcObject = ghObjectsList[srcComp];
                var tgtObject = ghObjectsList[tgtComp];

                string srcType = CategoryString(srcComp);
                string tgtType = CategoryString(tgtComp);

                bool srcIsSpecialType = CheckforSpecialCase(srcType);
                bool tgtIsSpecialType = CheckforSpecialCase(tgtType);
                string[] tgtComptype = tgtObject.GetType().ToString().Split('.');
                bool tgtGradientComponent = tgtComptype[tgtComptype.Length - 1].Equals("GH_GradientControl");
                if (tgtGradientComponent) { tgtIsSpecialType = false; }


                if (srcIsSpecialType)
                {
                    if (connect)
                    {
                        if (tgtIsSpecialType)
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, SpecialToSpecial);
                        }
                        else
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, SpecialToComp);
                        }
                    }
                    else if (disconnect)
                    {
                        if (tgtIsSpecialType)
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, DisSpecialFromSpecial);
                        }
                        else
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, DisSpecialFromComp);
                        }

                    }
                }
                else
                {
                    if (connect)
                    {
                        if (tgtIsSpecialType)
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, CompToSpecial);
                        }
                        else
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, CompToComp);
                        }
                    }
                    else if (disconnect)
                    {
                        if (tgtIsSpecialType)
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, DisCompFromSpecial);
                        }
                        else
                        {
                            this.GrasshopperDocument.ScheduleSolution(0, DisCompFromComp);
                        }
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a9e03f06-3649-41c5-b96c-864f64360687"); }
        }

        int MoveCompFindComponentOnCanvasByCoordinates(int compX, int compY)
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
            string tgtCategory = this.GrasshopperDocument.Objects[compIndex].Category;
            string tgtSubcategory = this.GrasshopperDocument.Objects[compIndex].SubCategory;
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

            var sourceComponent = (GH_Component)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (GH_Component)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.Params.Input[tgtCompInputIndex].AddSource((IGH_Param)sourceComponent.Params.Output[srcCompOutputIndex]);

        }

        // 2 CompToSpecial
        public void CompToSpecial(GH_Document doc)
        {

            var sourceComponent = (GH_Component)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (IGH_Param)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.AddSource((IGH_Param)sourceComponent.Params.Output[srcCompOutputIndex]);

        }

        // 3 SpecialToComp
        public void SpecialToComp(GH_Document doc)
        {

            var sourceComponent = (IGH_Param)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (GH_Component)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.Params.Input[tgtCompInputIndex].AddSource(sourceComponent);

        }

        // 4 SpecialToSpecial
        public void SpecialToSpecial(GH_Document doc)
        {

            var sourceComponent = (IGH_Param)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (IGH_Param)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.AddSource((IGH_Param)sourceComponent);

        }

        // 5 CompFromComp
        public void DisCompFromComp(GH_Document doc)
        {

            var sourceComponent = (GH_Component)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (GH_Component)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.Params.Input[tgtCompInputIndex].RemoveSource((IGH_Param)sourceComponent.Params.Output[srcCompOutputIndex]);
        }

        // 6 CompFromSpecial
        public void DisCompFromSpecial(GH_Document doc)
        {

            var sourceComponent = (GH_Component)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (IGH_Param)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.RemoveSource((IGH_Param)sourceComponent.Params.Output[srcCompOutputIndex]);

        }

        // 7 SpecialFromComp
        public void DisSpecialFromComp(GH_Document doc)
        {
            var sourceComponent = (IGH_Param)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (GH_Component)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.Params.Input[tgtCompInputIndex].RemoveSource(sourceComponent);
        }

        // 8 SpecialFromSpecial
        public void DisSpecialFromSpecial(GH_Document doc)
        {
            var sourceComponent = (IGH_Param)this.GrasshopperDocument.Objects[srcComp];
            var closeComponent = (IGH_Param)this.GrasshopperDocument.Objects[tgtComp];

            closeComponent.RemoveSource(sourceComponent);
        }

    }
}