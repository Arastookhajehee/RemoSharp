using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;

namespace RemoSharp
{
    public class TextNumber : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TextNumber class.
        /// </summary>
        public TextNumber()
          : base("TextNumber", "Nickname",
              "Replaces certain text in a script with numerical values",
              "RemoSharp", "Inputs")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Input_Script", "Script", "The script containing varaiable names to be replaced", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Number_Names", "Num_Ns", "List of varaiable names in the inputScript to be replaced.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Number_Values", "Vals", "List of values replacing the number names.", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Output_Script", "Script", "The script text from which the number names are replaced.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string Input_Script = "";
            List<string> NumberNames = new List<string>();
            List<double> Number_Inputs = new List<double>();

            DA.GetData(0, ref Input_Script);
            DA.GetDataList(1, NumberNames);
            DA.GetDataList(2, Number_Inputs);


            if (NumberNames.Count != Number_Inputs.Count)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "The count of variable names and the values do not match!");
            }
            else
            {
                for (int i = 0; i < NumberNames.Count; i++)
                {
                    string numberValue = Convert.ToString(Number_Inputs[i]);
                    Input_Script = Input_Script.Replace(NumberNames[i], numberValue);
                }

                DA.SetData(0, Input_Script);
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
                return RemoSharp.Properties.Resources.Text_Number.ToBitmap();
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("69fdaeda-0144-4e11-9770-fa526a9b5ff3"); }
        }
    }
}