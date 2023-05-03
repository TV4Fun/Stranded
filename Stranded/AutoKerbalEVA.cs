using UnityEngine;

namespace Stranded
{
    public class AutoKerbalEVA : KerbalEVA
    {
        public new Vector3 tgtRpos
        {
            get { return base.tgtRpos; }
            set { base.tgtRpos = value; }
        }
    }
}