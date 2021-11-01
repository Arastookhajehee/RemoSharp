using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using System.Linq;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using ScriptComponents;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace RemoSharp
{
    public class RemoSharpComponent : GH_Component
    {
        GH_Document GrasshopperDocument;
        IGH_Component Component;

        public List<GH_ActiveObject> components = new List<GH_ActiveObject>();
        public List<GH_ActiveObject> reserveComponents = new List<GH_ActiveObject>();

        float initialComponentLocX = 200;
        float initialComponentLocY = 200;

        public System.Drawing.PointF componentLocation = new System.Drawing.PointF(200, 200);
        public bool isHidden = false;
        public bool isLocked = false;
        public List<Grasshopper.Kernel.GH_ParamAccess> accessTypes = new List<Grasshopper.Kernel.GH_ParamAccess>();
        public List<string> componentInputTypes = new List<string>();
        public List<string> componentInputNames = new List<string>();
        public List<string> componentOutputNames = new List<string>();

        public List<IGH_Param> connectedComponentIDs = new List<IGH_Param>();

        float compLocationShiftX = 75;
        float compLocationShiftY = 0;

        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public RemoSharpComponent()
          : base("RemoSharp", "Remos",
              "This tool converts text to executable programs within grasshopper.",
              "RemoSharp", "RemoSharpScript")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Update", "Update", "Updates the created component with every input data change.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Catch_Component", "Catch_Comp", "Find the closest C# component and set it as the generating C# component", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Default_Comp", "Def_Comp", "Releases the current component and creats a new component from scratch.", GH_ParamAccess.item, false);
            pManager.AddTextParameter("UsingCode", "Using", "Generates the library reference 'using' code section of the C# component.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Script", "Script", "Generates the main function script body of the C# component.", GH_ParamAccess.item, "");
            pManager.AddTextParameter("AdditionalCode", "Additional", "Generates the custom additional code section of the C# component.", GH_ParamAccess.item, "");
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Component = this;
            GrasshopperDocument = this.OnPingDocument();            

            bool update = false;
            bool Trap_Component = false;
            bool Default_Comp = false;
            string usingCode = "";
            string script = "";
            string additionalCode = "";

            if (!DA.GetData(0, ref update))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Component Not Running.");
            };
            DA.GetData(1, ref Trap_Component);
            DA.GetData(2, ref Default_Comp);
            DA.GetData(3, ref usingCode);
            DA.GetData(4, ref script);
            DA.GetData(5, ref additionalCode);


            if (Default_Comp)
            {
                RemoveAllComponents(GrasshopperDocument);
                components.Clear();
                reserveComponents.Clear();
                accessTypes.Clear();
                componentInputTypes.Clear();
                componentInputNames.Clear();

                var anchor = this.Component.Attributes.Pivot;
                anchor.X += this.compLocationShiftX; ;
                anchor.Y += this.compLocationShiftY; ;
                componentLocation = anchor;
            }

            if (components.Count > 0)
            {
                componentInputTypes.Clear();
                connectedComponentIDs.Clear();
                accessTypes.Clear();
                componentInputNames.Clear();
                componentOutputNames.Clear();

                var component = (ScriptComponents.Component_CSNET_Script)components[0];
                componentLocation = component.Attributes.Pivot;
                isHidden = component.Hidden;
                isLocked = component.Locked;

                for (int i = 0; i < component.Params.Output.Count; i++)
                {
                    componentOutputNames.Add(component.Params.Output[i].NickName);
                }

                for (int i = 0; i < component.Params.Input.Count; i++)
                {
                    var inputType = component.Params.Input[i];
                    accessTypes.Add(inputType.Access);
                    componentInputNames.Add(inputType.NickName);

                    if (inputType.Sources.Count > 0)
                    {
                        var source = inputType.Sources[inputType.Sources.Count - 1];
                        var sourceID = (IGH_Param)source;
                        connectedComponentIDs.Add(sourceID);
                    }
                    else
                    {
                        connectedComponentIDs.Add(null);
                    }
                }

                foreach (var ghParam in component.Params.Input.OfType<Param_ScriptVariable>())
                {
                    var typeHint = ghParam.TypeHint ?? new GH_NullHint();
                    string[] typeHintStrArr = typeHint.ToString().Split('.');
                    string typeHintStr = typeHintStrArr[typeHintStrArr.Length - 1];
                    componentInputTypes.Add(typeHintStr);
                }

            }
            //else
            //{

            //    componentLocation = this.Component.Attributes.Pivot;
            //    componentLocation.X += this.compLocationShiftX;
            //    componentLocation.Y += this.compLocationShiftY;

            //}

            RemoveAllComponents(GrasshopperDocument);

            if (update)
            {

                var scriptComponent = new ScriptComponents.Component_CSNET_Script();


                int extraInputCount = componentInputNames.Count - scriptComponent.Params.Input.Count;

                for (int i = 0; i < extraInputCount; i++)
                {
                    Grasshopper.Kernel.Parameters.Param_ScriptVariable newInputParam = new Grasshopper.Kernel.Parameters.Param_ScriptVariable();
                    newInputParam.NickName = "def_input" + i;
                    newInputParam.AllowTreeAccess = true;
                    newInputParam.Optional = true;
                    scriptComponent.Params.RegisterInputParam(newInputParam, scriptComponent.Params.Input.Count);
                }


                scriptComponent.CreateAttributes();
                if (componentLocation.X == this.initialComponentLocX && componentLocation.Y == this.initialComponentLocY)
                {
                    componentLocation = this.Component.Attributes.Pivot;
                    componentLocation.X += this.compLocationShiftX;
                    componentLocation.Y += this.compLocationShiftY;
                }
                scriptComponent.Attributes.Pivot = componentLocation;

                scriptComponent.SourceCodeChanged(new Grasshopper.GUI.Script.GH_ScriptEditor(Grasshopper.GUI.Script.GH_ScriptLanguage.CS));

                
                
                scriptComponent.ScriptSource.UsingCode = usingCode ?? ""; ;
                scriptComponent.ScriptSource.ScriptCode = script ?? ""; ;
                scriptComponent.ScriptSource.AdditionalCode = additionalCode ?? "";

                if (accessTypes.Count > 0)
                {
                    for (int i = 0; i < accessTypes.Count; i++)
                    {
                        scriptComponent.Params.Input[i].Access = accessTypes[i];
                        scriptComponent.Params.Input[i].NickName = componentInputNames[i];
                        if (connectedComponentIDs.Count > 0 && connectedComponentIDs[i] != null)
                        {
                            scriptComponent.Params.Input[i].AddSource((IGH_Param)connectedComponentIDs[i]);
                        }
                    }

                    int extraOutputCount = componentOutputNames.Count - scriptComponent.Params.Output.Count;

                    for (int i = 0; i < extraOutputCount; i++)
                    {
                        Grasshopper.Kernel.Parameters.Param_ScriptVariable newOutputParam = new Grasshopper.Kernel.Parameters.Param_ScriptVariable();
                        scriptComponent.Params.RegisterOutputParam(newOutputParam);
                    }
                    for (int i = 0; i < componentOutputNames.Count; i++)
                    {
                        scriptComponent.Params.Output[i].NickName = componentOutputNames[i];
                    }

                    int inputIndex = 0;
                    foreach (var ghParam in scriptComponent.Params.Input.OfType<Param_ScriptVariable>())
                    {
                        InputTypeDeterminer(ghParam, componentInputTypes[inputIndex]);
                        inputIndex++;
                    }

                }
                scriptComponent.Hidden = isHidden;
                scriptComponent.Locked = isLocked;

                components.Add(scriptComponent);
                GrasshopperDocument.AddObject(scriptComponent, false);


            }

            if (Trap_Component)
            {

                double minDistance = double.MaxValue;
                int index = -1;
                for (int i = 0; i < GrasshopperDocument.ObjectCount; i++)
                {
                    try
                    {
                        ScriptComponents.Component_CSNET_Script component = (Component_CSNET_Script)GrasshopperDocument.Objects[i];

                        var pivot = component.Attributes.Pivot;
                        double distanceToThisComponent = DistanceToCsharpComponent(component);
                        if (distanceToThisComponent < minDistance && distanceToThisComponent != 0)
                        {
                            minDistance = distanceToThisComponent;
                            index = i;
                        }
                    }
                    catch
                    {
                    }
                }
                if (index == -1)
                {
                    string message = "Create a C# component on the canvas and bring this component close to it";
                    this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, message);
                }
                else
                {
                    Component_CSNET_Script scriptComponent = (Component_CSNET_Script)GrasshopperDocument.Objects[index];
                    components.Clear();
                    components.Add(scriptComponent);
                }

            }

        }

        public double DistanceToCsharpComponent(Component_CSNET_Script component)
        {
            double thisX = this.Component.Attributes.Pivot.X;
            double thisY = this.Component.Attributes.Pivot.Y;
            double otherX = component.Attributes.Pivot.X;
            double otherY = component.Attributes.Pivot.Y;

            double distance = Math.Sqrt((thisX - otherX) * (thisX - otherX) + (thisY - otherY) * (thisY - otherY));
            return distance;
        }

        public void InputTypeDeterminer(Grasshopper.Kernel.Parameters.Param_ScriptVariable inputParam, string inputType)
        {
            var typeHintObject = inputParam.TypeHint ?? new GH_NullHint();
            string[] typeHintLong = typeHintObject.ToString().Split('.');
            string determinedType = typeHintLong[typeHintLong.Length - 1];

            if (inputType == "GH_NullHint")
            {
            }
            else if (inputType == "GH_BooleanHint_CS")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_BooleanHint_CS();
            }
            else if (inputType == "GH_IntegerHint_CS")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_IntegerHint_CS();
            }
            else if (inputType == "GH_DoubleHint_CS")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_DoubleHint_CS();
            }
            else if (inputType == "GH_ComplexHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_ComplexHint();
            }
            else if (inputType == "GH_StringHint_CS")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_StringHint_CS();
            }
            else if (inputType == "GH_DateTimeHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_DateTimeHint();
            }
            else if (inputType == "GH_ColorHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_ColorHint();
            }
            else if (inputType == "GH_GuidHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_GuidHint();
            }
            else if (inputType == "GH_Point3dHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_Point3dHint();
            }
            else if (inputType == "GH_Vector3dHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_Vector3dHint();
            }
            else if (inputType == "GH_PlaneHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_PlaneHint();
            }
            else if (inputType == "GH_IntervalHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_IntervalHint();
            }
            else if (inputType == "GH_UVIntervalHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_UVIntervalHint();
            }
            else if (inputType == "GH_Rectangle3dHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_Rectangle3dHint();
            }
            else if (inputType == "GH_BoxHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_BoxHint();
            }
            else if (inputType == "GH_TransformHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_TransformHint();
            }
            else if (inputType == "GH_LineHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_LineHint();
            }
            else if (inputType == "GH_CircleHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_CircleHint();
            }
            else if (inputType == "GH_ArcHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_ArcHint();
            }
            else if (inputType == "GH_PolylineHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_PolylineHint();
            }
            else if (inputType == "GH_CurveHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_CurveHint();
            }
            else if (inputType == "GH_SurfaceHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_SurfaceHint();
            }
            else if (inputType == "GH_BrepHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_BrepHint();
            }
            else if (inputType == "GH_MeshHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_MeshHint();
            }
            else if (inputType == "GH_GeometryBaseHint")
            {
                inputParam.TypeHint = new Grasshopper.Kernel.Parameters.Hints.GH_GeometryBaseHint();
            }
        }
        public void RemoveAllComponents(GH_Document doc)
        {

            reserveComponents.Clear();
            foreach (var component in components)
            {
                reserveComponents.Add(component);
            }

            doc.ScheduleSolution(0, (val) => {

                foreach (var component in reserveComponents)
                {
                    doc.RemoveObject(component.Attributes, false);
                    components.Remove(component);
                }
                reserveComponents.Clear();
            });

        }
        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                
                return RemoSharp.Properties.Resources.RemoSharp.ToBitmap();
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0802d15a-354c-432f-8f96-69a948d23d95"); }
        }
    }
}
