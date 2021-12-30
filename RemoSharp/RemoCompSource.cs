using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class RemoCompSource : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

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
            pManager.AddBooleanParameter("Create", "Create",
                "Create a new instance of the close component on the main remote GH_Canvas",
                GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Move", "Mve",
                "Move the closest component in the same direction of the target RemoConnector Component.",
                GH_ParamAccess.item,false);
            pManager.AddIntegerParameter("SourceComp Output", "SrcOut", "The output index of the source component",
                GH_ParamAccess.item, 0); 
            pManager.AddIntegerParameter("TargetComp Input", "TrgtIn", "The input index of the target component",
                 GH_ParamAccess.item, 0);
            pManager.AddBooleanParameter("Connect", "Cnct",
                "Connects components remotely.",
                GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Disconnect", "DsCnct",
                "Disconnects components remotely.",
                GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Cmd",
                "Command to be executed to make, connect, and disconnect components on the main remote GH_Canvas.",
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

            // defining all the trigger variables
            bool create = false;
            bool move = false;
            int sourceOutput = 0;
            int targetInput = 0;
            bool connect = false;
            bool disconnect = false;

            DA.GetData(0, ref create);
            DA.GetData(1, ref move);
            DA.GetData(2, ref sourceOutput);
            DA.GetData(3, ref targetInput);
            DA.GetData(4, ref connect);
            DA.GetData(5, ref disconnect);

            //setting the output command string
            string cmd = "";
            if (!create && !move && !connect && !disconnect) return;

            if (create) { 
                try
                {
                    // getting the GH Document
                    var thisDoc = this.GrasshopperDocument;

                    // getting the type of the closest component on the canvas in string format
                    // getting its location too
                    System.Drawing.PointF newPivot;
                    string typeName = FindClosestObjectTypeOnCanvas(out newPivot);

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

                    cmd = "RemoCreate," + type + "," + newPivot.X + "," + newPivot.Y;
                    DA.SetData(0, cmd);
                    return;
                }
                catch (Exception e)
                {
                    this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error,e.Message);
                }
            }

            if (move) {

                var pivot = this.Component.Attributes.Pivot;
                int otherCompX = Convert.ToInt32(pivot.X);
                int otherCompY = Convert.ToInt32(pivot.Y);

                cmd = "MoveComp," + otherCompX + "," + otherCompY;
                DA.SetData(0, cmd);
                return;
            }

            if (connect || disconnect) 
            { 
                var thisPivot = this.Component.Attributes.Pivot;
                cmd = "RemoConnect," + connect + "," + disconnect + "," + thisPivot.X + "," + thisPivot.Y + "," + sourceOutput + "," + targetInput;
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
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("9a3a9712-9b99-409d-9c02-f6b338305f5b"); }
        }

        private int MoveFromFindComponentOnCanvasByCoordinates(float compX, float compY)
        {

            int compCoordX = Convert.ToInt32(compX);
            int compCoordY = Convert.ToInt32(compY);
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

        private string FindClosestObjectTypeOnCanvas(out System.Drawing.PointF compPivot)
        {

            // getting the active instances of the GH document and current component
            // also we need the list of all of the objects on the canvas
            var ghDoc = this.GrasshopperDocument;
            var ghObjects = ghDoc.Objects;
            var thisCompLoc = this.Component.Attributes.Pivot;

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
                        }
                    }
                }
            }
            catch { }
            compPivot = newPivot;
            return componentType;
        }

    }
}