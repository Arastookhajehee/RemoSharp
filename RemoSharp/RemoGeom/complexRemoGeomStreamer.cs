using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;

namespace RemoSharp
{
    public class complexRemoGeomStreamer : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the complexRemoGeomStreamer class.
        /// </summary>
        public complexRemoGeomStreamer()
          : base("ComplexRemoGeomStreamer", "Cx_StreamGeom",
              "Streams Geometry to the Remote Main GH_Canvas",
              "RemoSharp", "RemoParams")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("RemoGeometry", "StreamGeom", "The Geometry to be Streamed", GH_ParamAccess.list);
            pManager.AddTextParameter("tags", "tags", "tags", GH_ParamAccess.list, "");
            pManager.AddColourParameter("color", "color", "color", GH_ParamAccess.list, System.Drawing.Color.Black);
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
            List<GeometryBase> geomList = new List<GeometryBase>();
            List<string> tags = new List<string>();
            List<Color> colors = new List<Color>();

            if (!DA.GetDataList(0, geomList)) return;
            if (!DA.GetDataList(1, tags)) return;
            if (!DA.GetDataList(2, colors)) return;

            if (this.Params.Input[1].SourceCount == 0)
            {
                tags.Clear();
                for (int i = 0; i < geomList.Count; i++)
                {
                    tags.Add("");
                }
            }
            if (this.Params.Input[2].SourceCount == 0)
            {
                colors.Clear();
                for (int i = 0; i < geomList.Count; i++)
                {
                    colors.Add(Color.Black);
                }
            }


            List<string> jsons = new List<string>();
            foreach (GeometryBase item in geomList)
            {
                string json = item.ToJSON(new Rhino.FileIO.SerializationOptions());
                jsons.Add(json);
            }

            ComplexGeometeySerilization serilizationObj = new ComplexGeometeySerilization(jsons,tags,colors);

            string serialJson = Newtonsoft.Json.JsonConvert.SerializeObject(serilizationObj);

            DA.SetData(0, serialJson);
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
                return RemoSharp.Properties.Resources.ComplexGeomStream.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8F7F3941-1D56-426F-A8ED-99FA59BA490C"); }
        }
    }
}