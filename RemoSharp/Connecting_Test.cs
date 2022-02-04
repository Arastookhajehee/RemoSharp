using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

using Grasshopper.GUI;
namespace RemoSharp
{
    public class Connecting_Test : GH_Component
    {

        public IGH_Param inputParam;
        public IGH_Param outputParam;
       

        /// <summary>
        /// Initializes a new instance of the Connecting_Test class.
        /// </summary>
        public Connecting_Test()
          : base("Connecting_Test", "Nickname",
              "Description",
              "Category", "Subcategory")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("x", "x", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("y", "y", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("z", "z", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("u", "u", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run","", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("c_d", "c_d", "", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("a", "a", "a", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            try
            {
                double x = -1;
                double y = -1;
                double z = -1;
                double u = -1;
                bool RUN = false;
                int c_d = 0;
                DA.GetData(0, ref x);
                DA.GetData(1, ref y);
                DA.GetData(2, ref z);
                DA.GetData(3, ref u);
                DA.GetData(4, ref RUN);
                DA.GetData(5, ref c_d);

                int mouseIntX = Convert.ToInt32(x) - 30;
                int mouseIntY = Convert.ToInt32(y);

                var foundOut = this.OnPingDocument().FindOutputParameter(new System.Drawing.Point(mouseIntX, mouseIntY));
                if (foundOut == null || foundOut.ToString().Equals(""))
                {
                    Single mouseIntXs = Convert.ToSingle(x) - 30;
                    Single mouseIntYs = Convert.ToSingle(y);
                    try
                    {
                        foundOut = (IGH_Param)this.OnPingDocument().FindObject(new System.Drawing.PointF(mouseIntXs, mouseIntYs), 5);
                        return;
                    }
                    catch { }
                }

                int mouseIntXin = Convert.ToInt32(z) + 30;
                int mouseIntYin = Convert.ToInt32(u);

                var foundIn = this.OnPingDocument().FindInputParameter(new System.Drawing.Point(mouseIntXin, mouseIntYin));
                if (foundIn == null || foundIn.ToString().Equals(""))
                {
                    Single mouseIntXins = Convert.ToSingle(x) + 30;
                    Single mouseIntYins = Convert.ToSingle(y);
                    try
                    {
                        foundIn = (IGH_Param)this.OnPingDocument().FindObject(new System.Drawing.PointF(mouseIntXins, mouseIntYins), 5);
                        return;
                    }
                    catch { }
                }




                outputParam = (IGH_Param)foundOut;
                inputParam = (IGH_Param)foundIn;
                if (RUN && c_d == 1)
                {
                    this.OnPingDocument().ScheduleSolution(0, ConnectWire);
                }
                else if (RUN && c_d == -1)
                {
                    this.OnPingDocument().ScheduleSolution(0, DiconnectWire);
                }
            }
            catch (Exception e)
            {
                DA.SetData(0, e.Message);
            }

        }

        void ConnectWire(GH_Document doc)
        {
            inputParam.AddSource(outputParam);
        }
        void DiconnectWire(GH_Document doc)
        {
            inputParam.RemoveSource(outputParam);
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
            get { return new Guid("2dc920ea-1bd3-4063-99e2-58b773cf41dc"); }
        }
    }
}