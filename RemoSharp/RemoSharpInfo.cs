using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace RemoSharp
{
    public class RemoSharpInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "RemoSharp";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return RemoSharp.Properties.Resources.RemoSharpIcon;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "RemoSharp, a tool for real-time collaborative computational design and digital fabrication";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a1d80423-e6e0-49f5-8514-de158ae1193a");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Arastoo Khajehee";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "a.khajehee@gmail.com";
            }
        }
    }
}
