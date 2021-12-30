using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class RemoCompTarget : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

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
            pManager.AddTextParameter("SourceCommand", "SrcCmd",
                "Command from RemoCompSource regarding creation, connection, disconnection, and movement of components on the main remote GH_Canvas",
                GH_ParamAccess.item,"");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CompleteCommand", "CompCmd",
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

            

            if (cmds[0] == "RemoCreate")
            {
                DA.SetData(0, cmd);
                return;
            }

            if (cmds[0] == "RemoConnect")
            {
                
                var thisCompPivot = this.Component.Attributes.Pivot;

                cmd += "," + thisCompPivot.X + "," + thisCompPivot.Y;
                DA.SetData(0, cmd);
                return;
            }

            if (cmds[0] == "MoveComp")
            {
                
                int compX = Convert.ToInt32(cmds[1]);
                int compY = Convert.ToInt32(cmds[2]);

                var thisCompPivot = this.Component.Attributes.Pivot;
                int thisCompX = Convert.ToInt32(thisCompPivot.X);
                int thisCompY = Convert.ToInt32(thisCompPivot.Y);

                int translationX = thisCompX - compX;
                int translationY = thisCompY - compY;

                cmd += "," + translationX + "," + translationY;
                DA.SetData(0, cmd);
                return;
            }

            if (cmds[0] == "RemoConnect")
            {
                var thisCompPivot = this.Component.Attributes.Pivot;

                cmd += "," + thisCompPivot.X + "," + thisCompPivot.Y;
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
            get { return new Guid("243dfe88-8c61-451c-996a-2f8f77c9409b"); }
        }

        private int MoveCompFindComponentOnCanvasByCoordinates(int compX, int compY)
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
                    double distance = Math.Sqrt((thisCompLoc.X - pivot.X) * (thisCompLoc.X - pivot.X) + (thisCompLoc.Y - pivot.Y) * (thisCompLoc.Y - pivot.Y));

                    
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
    }
}