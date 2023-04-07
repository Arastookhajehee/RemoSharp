using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Grasshopper;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;

namespace RemoSharp
{
    public class RemoGeomStreamer : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RemoGeomStreamer class.
        /// </summary>
        public RemoGeomStreamer()
          : base("RemoGeomStreamer", "StreamGeom",
              "Streams Geometry to the Remote Main GH_Canvas",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("RemoGeometry", "StreamGeom", "The Geometry to be Streamed", GH_ParamAccess.tree);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("RGStream", "RGStream", "Text representing the geometry and the Tree structure.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            GH_Structure<IGH_Goo> dataTree;
            if(!DA.GetDataTree(0, out dataTree)) return;

            var paths = dataTree.Paths;

            var jsonStringList = new List<string>();
            for (int i = 0; i < paths.Count; i++)
            {
                var path = paths[i];
                string pathString = path.ToString();
                for (int j = 0; j < dataTree.get_Branch(path).Count; j++)
                {
                    Rhino.Geometry.GeometryBase crv = GH_Convert.ToGeometryBase(dataTree.get_Branch(path)[j]);
                    string crvJson = crv.ToJSON(new Rhino.FileIO.SerializationOptions());
                    RemoGeomJsonStructure geomJsonObject = new RemoGeomJsonStructure(pathString, crvJson);
                    string jsonGeomString = JsonConvert.SerializeObject(geomJsonObject);
                    jsonStringList.Add(jsonGeomString);
                }
            }

            string singleJsonString = JsonConvert.SerializeObject(jsonStringList);

            DA.SetData(0, singleJsonString);
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
                return RemoSharp.Properties.Resources.RemoGeomStream.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("5fff0a24-0de9-4229-9619-0714123435cf"); }
        }
    }
}