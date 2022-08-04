using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading;

namespace RemoSharp.Utilities
{
    public class testComp : GH_Component
    {
        public string newCompGuid { get; set; }
        public string modified { get; set; }
        /// <summary>
        /// Initializes a new instance of the testComp class.
        /// </summary>
        public testComp()
          : base("testComp", "Nickname",
              "Just a Component for tests by the developer",
              "RemoSharp", "Utils")
        {
            this.newCompGuid = "";
            this.modified = "";
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("a/r", "a/r", "d", GH_ParamAccess.item);
            pManager.AddTextParameter("m", "m", "d", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            foreach (var obj in this.OnPingDocument().Objects)
            {
                if (obj.GetType().ToString().Equals("Grasshopper.Kernel.Special.GH_Timer"))
                {
                    if (obj.NickName.Contains("RemoSharp"))
                    {
                        Grasshopper.Kernel.Special.GH_Timer trigger = (Grasshopper.Kernel.Special.GH_Timer)obj;
                        trigger.AddTarget(this.InstanceGuid);
                    }
                }
            }
            this.OnPingDocument().ObjectsAdded += TestComp_ObjectsAdded;
            this.OnPingDocument().ObjectsDeleted += TestComp_ObjectsDeleted;
            //this.OnPingDocument().ModifiedChanged += TestComp_ModifiedChanged;

            //this.AttributesChanged += TestComp_AttributesChanged;
            //this.ObjectChanged += TestComp_ObjectChanged;
            //this.OnPingDocument().FindObject()

            Grasshopper.Instances.ActiveCanvas.MouseMove += ActiveCanvas_MouseMove;
            DA.SetData(0, this.newCompGuid);
            DA.SetData(1, this.modified);
        }

        private void ActiveCanvas_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void TestComp_ObjectChanged(IGH_DocumentObject sender, GH_ObjectChangedEventArgs e)
        {
            this.modified = e.Type.ToString();
        }

        private void TestComp_AttributesChanged(IGH_DocumentObject sender, GH_AttributesChangedEventArgs e)
        {
            this.modified = e.ToString();
        }

        private void TestComp_ModifiedChanged(object sender, GH_DocModifiedEventArgs e)
        {
            this.modified = e.Modified.ToString();
        }

        private void TestComp_ObjectsDeleted(object sender, GH_DocObjectEventArgs e)
        {
            var objs = e.Objects;
            foreach (var obj in objs)
            {
                string name = obj.Name;

                this.newCompGuid = obj.Name + " Removed from Canvas";
            }
        }

        private void TestComp_ObjectsAdded(object sender, GH_DocObjectEventArgs e)
        {
            var objs = e.Objects;
            foreach (var obj in objs)
            {
                string name = obj.Name;

                this.newCompGuid = obj.Name + " Added to Canvas";
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
            get { return new Guid("B97D3A09-BE9A-46C5-9602-C00079F14983"); }
        }
    }
}