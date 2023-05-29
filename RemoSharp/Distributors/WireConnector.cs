using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using RemoSharp.RemoCommandTypes;
using Rhino.Geometry;

namespace RemoSharp.Distributors
{
    public class WireConnector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the WireConnector class.
        /// </summary>
        public WireConnector()
          : base("wires", "wires",
              "Excecution of wiring Commands for all manipulations from the client side remotely.",
              "RemoSharp", "RemoSetup")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("errors", "errors", "command execution errors", GH_ParamAccess.list);
            pManager.AddGenericParameter("wires", "wires", "Wiring commands from executor component", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("errors", "errors", "command execution errors", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> erros = new List<string>();
            List<RemoConnect> connectionList = new List<RemoConnect>();
            DA.GetDataList(0, erros);
            DA.GetDataList(1, connectionList);

            foreach (RemoConnect item in connectionList)
            {
                try
                {
                    ConnectWires(item);
                }
                catch (Exception e)
                {
                    erros.Add(e.Message);
                }
            }

            DA.SetDataList(0, erros);   
        }

        private void ConnectWires(RemoConnect wireCommand)
        {

            bool connect = wireCommand.RemoConnectType == RemoConnectType.Add || wireCommand.RemoConnectType == RemoConnectType.Replace;
            bool disconnect = wireCommand.RemoConnectType == RemoConnectType.Remove || wireCommand.RemoConnectType == RemoConnectType.Replace;
            System.Guid sourceGuid = wireCommand.sourceObjectGuid;
            int outIndex = wireCommand.sourceOutput;
            bool sourceIsSpecial = wireCommand.isSourceSpecial;
            System.Guid targetGuid = wireCommand.targetObjectGuid;
            int inIndex = wireCommand.targetInput;
            bool targetIsSpecial = wireCommand.isTargetSpecial;
            if (connect)
            {
                if (sourceIsSpecial)
                {
                    if (targetIsSpecial)
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.RemoveAllSources();
                            target.AddSource(source);
                        });
                    }
                    else
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.Params.Input[inIndex].RemoveAllSources();
                            target.Params.Input[inIndex].AddSource(source);
                        });
                    }
                }
                else
                {
                    if (targetIsSpecial)
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.RemoveAllSources();
                            target.AddSource(source.Params.Output[outIndex]);
                        });
                    }
                    else
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            if (disconnect) target.Params.Input[inIndex].RemoveAllSources();
                            target.Params.Input[inIndex].AddSource(source.Params.Output[outIndex]);
                        });
                    }
                }

            }
            else
            {
                if (sourceIsSpecial)
                {
                    if (targetIsSpecial)
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.RemoveSource(source);
                        });
                    }
                    else
                    {
                        var source = (IGH_Param)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.Params.Input[inIndex].RemoveSource(source);
                        });
                    }
                }
                else
                {
                    if (targetIsSpecial)
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (IGH_Param)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.RemoveSource(source.Params.Output[outIndex]);
                        });
                    }
                    else
                    {
                        var source = (GH_Component)this.OnPingDocument().FindObject(sourceGuid, false);
                        var target = (GH_Component)this.OnPingDocument().FindObject(targetGuid, false);
                        this.OnPingDocument().ScheduleSolution(1, doc =>
                        {
                            target.Params.Input[inIndex].RemoveSource(source.Params.Output[outIndex]);
                        });
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
            get { return new Guid("F3F50036-3F5C-4029-B69B-42F71D802528"); }
        }
    }
}