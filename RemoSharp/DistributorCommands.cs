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
    public class DistributorCommands : GH_Component
    {

        GH_Document GrasshopperDocument;
        IGH_Component Component;
        private string currentXMLString = "";
        private int otherCompInx = -1;
        public int deletionIndex = -1;

        /// <summary>
        /// Initializes a new instance of the DistributorCommands class.
        /// </summary>
        public DistributorCommands()
          : base("DistributorCommands", "DistroCmd",
              "Excecution of Commands for Selection, Deletion, PushCopy (copy and paste TO Remote Main GH_Canvas), PullCopy (copy and paste FROM Remote Main GH_Canvas).",
              "RemoSharp", "RemoMakers")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("DistroCommand", "DstCmd", "Selection, Deletion, Push/Pull Commands.", GH_ParamAccess.item,"");
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
            string command = "";
            DA.GetData(0, ref command);

            if (command == null || command == "") return;
            string substring = command.Substring(0, 5);
            if (substring.Equals("<?xml"))
            {
                if (command.Equals(currentXMLString)) return;
                Exception threadEx = null;
                Thread staThread = new Thread(
                  delegate ()
                  {
                      try
                      {
                          System.Windows.Forms.Clipboard.SetText(command);
                      }
                      catch (Exception ex)
                      {
                          threadEx = ex;
                      }
                  });
                staThread.SetApartmentState(ApartmentState.STA);
                staThread.Start();
                staThread.Join();
                InputSimulator sim = new InputSimulator();
                sim.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, new[] { VirtualKeyCode.VK_V });
                currentXMLString = command;
                return;
            }
            else
            {
                string[] cmds = command.Split(',');

                if (cmds[0].Equals("Selection"))
                    {
                    bool selectorAdd = Convert.ToBoolean(cmds[1]);
                    bool selectorRemove = Convert.ToBoolean(cmds[2]);
                    int compX = Convert.ToInt32(cmds[3]);
                    int compY = Convert.ToInt32(cmds[4]);
                    int otherCompInx = SelectionCommandFindComponentOnCanvasByCoordinates(compX, compY);
                    var otherComp = this.OnPingDocument().Objects[otherCompInx];
                    GH_RelevantObjectData grip = new GH_RelevantObjectData(otherComp.Attributes.Pivot);
                    grip.CreateObjectData(otherComp);
                    if (selectorAdd)
                    {
                        this.OnPingDocument().Select(grip, true, false);
                    }
                    else if (selectorRemove)
                    {
                        this.OnPingDocument().Select(grip, false, true);
                    }
                }
                else if (cmds[0].Equals("Deletion"))
                {
                    bool delete = Convert.ToBoolean(cmds[1]);
                    int compX = Convert.ToInt32(cmds[2]);
                    int compY = Convert.ToInt32(cmds[3]);

                    if (delete)
                    {
                        deletionIndex = DeletionCommandFindComponentOnCanvasByCoordinates(compX, compY);

                        this.OnPingDocument().ScheduleSolution(0, DeleteComponent);
                        //Grasshopper.Instances.RedrawCanvas();
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
            get { return new Guid("6b7b7c9e-0e81-4195-93b4-279099080880"); }
        }

        private int SelectionCommandFindComponentOnCanvasByCoordinates(int compX, int compY)
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

        private int DeletionCommandFindComponentOnCanvasByCoordinates(int compX, int compY)
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

        private void DeleteComponent(GH_Document doc)
        {
            try
            {
                var otherComp = this.OnPingDocument().Objects[deletionIndex];
                this.OnPingDocument().RemoveObject(otherComp, true);
            }
            catch (Exception e){
                this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
            }
            

        }
    }
}