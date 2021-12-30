using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using WindowsInput.Native;
using WindowsInput;
using System.Windows.Forms;
using System.Threading;

namespace RemoSharp
{
    public class Distributor : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        /// <summary>
        /// Initializes a new instance of the Distributor class.
        /// </summary>
        public Distributor()
          : base("Distributor", "Distro",
              "A tool for Selection, Deletion, PushCopy (copy and paste TO Remote Main GH_Canvas), PullCopy (copy and paste FROM Remote Main GH_Canvas).",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("SelectAdd", "SelAdd", "Add to selection", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("SelectRemove", "SelRmv", "Remove from selection", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("PullCopy", "PullCp", "Copy and paste selection FROM Remote Main GH_Canvas", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("PushCopy", "PushCp", "Copy and paste selection TO Remote Main GH_Canvas", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Delete", "Del", "Delete from REMOTE MAIN GH_Canvas", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "Cmd", "The command to be executed on the destination GH_Canvas.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // adding the this keywords
            Component = this;
            GrasshopperDocument = this.OnPingDocument();
            // checks and boolean buttons
            bool selectorAdd = false;
            bool selectorRemove = false;
            bool pullCopy = false;
            bool pushCopy = false;
            bool delete = false;

            DA.GetData(0, ref selectorAdd);
            DA.GetData(1, ref selectorRemove);
            DA.GetData(2, ref pullCopy);
            DA.GetData(3, ref pushCopy);
            DA.GetData(4, ref delete);

            if (!selectorAdd && !selectorRemove && !pullCopy && !pushCopy && !delete) return;

            // Setting the output string variable
            string cmd = "";

            if (selectorAdd || selectorRemove)
            {
                var pivot = this.Component.Attributes.Pivot;
                int otherCompInx = SelectionFindComponentOnCanvasByCoordinates(pivot.X, pivot.Y);
                var otherComp = this.GrasshopperDocument.Objects[otherCompInx];
                int otherCompX = Convert.ToInt32(otherComp.Attributes.Pivot.X);
                int otherCompY = Convert.ToInt32(otherComp.Attributes.Pivot.Y);
                cmd = "Selection," + selectorAdd + "," + selectorRemove + "," + otherCompX + "," + otherCompY;
                DA.SetData(0, cmd);
                return;
            }

            if (pushCopy || pullCopy)
            {
                if (pullCopy)
                {
                    cmd = "pullCopyRequest";
                    DA.SetData(0, cmd);
                    return;
                }

                var pivot = this.Component.Attributes.Pivot;
                GH_RelevantObjectData grip = new GH_RelevantObjectData(pivot);
                grip.CreateObjectData(this.Component);
                this.GrasshopperDocument.Select(grip, false, true);

                InputSimulator sim = new InputSimulator();

                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, new[] { VirtualKeyCode.VK_C });

                IDataObject idat = null;
                Exception threadEx = null;
                Thread staThread = new Thread(
                  delegate ()
                  {
                      try
                      {
                          cmd = System.Windows.Forms.Clipboard.GetText();
                          idat = Clipboard.GetDataObject();
                      }
                      catch (Exception ex)
                      {
                          threadEx = ex;
                      }
                  });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();

                DA.SetData(0, cmd);
                return;
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
            get { return new Guid("4fc61913-a994-4d51-ad2a-5b6a9f402586"); }
        }

        private int SelectionFindComponentOnCanvasByCoordinates(float compX, float compY)
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
    }
}