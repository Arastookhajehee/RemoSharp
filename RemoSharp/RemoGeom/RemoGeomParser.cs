using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace RemoSharp
{
    public class RemoGeomParser : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the RemoGeomParser class.
        /// </summary>
        public RemoGeomParser()
          : base("RemoGeomParser", "RGParser",
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
            pManager.AddGenericParameter("RemoGeometry", "StreamGeom", "Geometry Tree from Remote Client", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            string jsonString = "";
            DA.GetData(0, ref jsonString);

            List<string> stringList = JsonConvert.DeserializeObject<List<string>>(jsonString);

            List<int[]> dataTreeInfo = new List<int[]>();

            DataTree<GeometryBase> crvTree = new DataTree<GeometryBase>();

            List<RemoGeomJsonStructure> geomObjs = new List<RemoGeomJsonStructure>();
            if (stringList.Count == 0) return;
            for (int i = 0; i < stringList.Count; i++)
            {
                RemoGeomJsonStructure geomObj = JsonConvert.DeserializeObject<RemoGeomJsonStructure>(stringList[i]);
                string treePath = geomObj.TreePath;
                string geomData = geomObj.Data;

                int[] treePathIndecies = RemoGeomJsonStructure.TreePathIntArray(treePath);
                GeometryBase crv = (GeometryBase) GeometryBase.FromJSON(geomData);
                crvTree.Add(crv, new GH_Path(treePathIndecies));
            }

            DA.SetDataTree(0, crvTree);
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
                return RemoSharp.Properties.Resources.RemoGeomParser.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0faf57c2-d4f0-4066-99a4-2a9826109592"); }
        }
    }
}