using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace RemoSharp
{
    public class ComplexGeomParaser : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ComplexGeomParaser class.
        /// </summary>
        public ComplexGeomParaser()
          : base("ComplexRemoGeomParser", "CxRGParser",
              "Regenerates Geometry from a stream of text from the remote client GH_Canvas.",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("RGStream", "RGStream", "Text Representation of Geometry and Tree Structure", GH_ParamAccess.item, "[]");
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("RemoGeometry", "StreamGeom", "Geometry Tree from Remote Client", GH_ParamAccess.list);
            pManager.AddTextParameter("tags", "tags", "tags", GH_ParamAccess.list);
            pManager.AddColourParameter("colors", "colors", "colors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string json = "";
            DA.GetData(0, ref json);

            ComplexGeometeySerilization complexGeom = Newtonsoft.Json.JsonConvert.DeserializeObject<ComplexGeometeySerilization>(json);

            List<GeometryBase> geometries = new List<GeometryBase>();
            foreach (string item in complexGeom.geoms)
            {
                geometries.Add((GeometryBase)GeometryBase.FromJSON(item));
            }

            DA.SetDataList(0, geometries);
            DA.SetDataList(1, complexGeom.tags);
            DA.SetDataList(2, complexGeom.colors);

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
                return RemoSharp.Properties.Resources.ComplexGeomParse.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("3D91767B-F08D-4D02-8375-6DC0C085F8C2"); }
        }
    }
}