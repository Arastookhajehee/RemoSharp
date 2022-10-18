using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using GHCustomControls;
using WPFNumericUpDown;

namespace RemoSharp.WsClientCat
{
    abstract public class WsClientComponent : GHCustomComponent
    {
        public WsClientComponent(string name, string nickname, string description)
            : base(name, nickname, description, "RemoSharp", "Com_Tools")
        {
        }
    }//eoc
}//eons