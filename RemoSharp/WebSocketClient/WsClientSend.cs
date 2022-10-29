﻿using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using RemoSharp.WsClient;



namespace RemoSharp.WsClientCat
{
public class WsClientSend : WsClientComponent
{

    /// <summary>
    /// Initializes a new instance of the AssembleCurves class.
    /// </summary>
        public WsClientSend(): base
    (
        "Websocket Client Sender",
        "WS<<",
        "Send data to websocket server"
    )
    {}//eof
    



 //   /// <summary>
 //   /// Help description
 //   /// </summary>
	//protected override string HelpDescription
	//{
	//	get{return BengeshtInfo.getComponentDescription(
 //           "Send data to websocket server"
 //       );}
	//}//eof




    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
	protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
	{
        pManager.AddGenericParameter("Websocket Objects", "WSC", "websocket objects", GH_ParamAccess.item);
        pManager.AddTextParameter("Message", "Msg", "Message content", GH_ParamAccess.item, "Hello World");
    }//eof




    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
	protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
	{
    }//eof




    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
	protected override void SolveInstance(IGH_DataAccess DA)
	{
        WsObject wscObj = new WsObject();
        string message = "Hello World";

        if (!DA.GetData(0, ref wscObj)) return;
        if (!DA.GetData(1, ref message)) return;

        if (string.IsNullOrEmpty(message)) return;

        wscObj.send(message);
    }//eof




    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
	protected override System.Drawing.Bitmap Icon
	{
		get{return RemoSharp.Properties.Resources.send.ToBitmap(); }
	}//eof




    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
	public override Guid ComponentGuid
	{
        get { return new Guid("48dc6850-2351-40d1-8d01-b58ad698ca93"); }

    }
}//eoc
}//eons