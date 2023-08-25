using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Linq;
using System.Drawing;

namespace RemoSharp.Utilities
{
    public class GetObjByTag : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GetObjByTag class.
        /// </summary>
        public GetObjByTag()
          : base("GetObjByTag", "Nickname",
              "Description",
              "RemoSharp", "Utils")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("objs", "objs", "objs", GH_ParamAccess.list);
            pManager.AddTextParameter("tags","tags","tags",GH_ParamAccess.list);
            pManager.AddColourParameter("colors", "colors", "colors", GH_ParamAccess.list);
            pManager.AddTextParameter("tag", "tag", "tag", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGeometryParameter("objs", "objs", "objs", GH_ParamAccess.list);
            pManager.AddTextParameter("tags", "tags", "tags", GH_ParamAccess.list);
            pManager.AddColourParameter("colors", "colors", "colors", GH_ParamAccess.list);
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
            string targetTag = "";

            if (!DA.GetDataList(0, geomList)) return;
            if (!DA.GetDataList(1, tags)) return;
            if (!DA.GetDataList(2, colors)) return;
            if (!DA.GetData(3, ref targetTag)) return;


            var matchingIndices = tags
                .Select((number, index) => new { Number = number, Index = index })
                .Where(pair => pair.Number == targetTag)
                .Select(pair => pair.Index);

            List<GeometryBase> target_geomList = new List<GeometryBase>();
            List<string> target_tags = new List<string>();
            List<Color> target_colors = new List<Color>();

            foreach (int index in matchingIndices)
            {
                target_geomList.Add(geomList[index]);
                target_tags.Add(tags[index]);
                target_colors.Add(colors[index]);
            }

            DA.SetDataList(0, target_geomList);
            DA.SetDataList(1, target_tags);
            DA.SetDataList(2, target_colors);
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
            get { return new Guid("DC680883-C580-4AC1-9C02-3A79A249A7D0"); }
        }
    }
}