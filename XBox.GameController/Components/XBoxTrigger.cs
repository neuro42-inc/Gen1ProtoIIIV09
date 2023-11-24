using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using n42_Robot_PROTO_III.Helpers;

namespace n42_Robot_PROTO_III
{
    public class XBoxTrigger : XBoxComponent<float>
    {
        public float DeadZone { get; set; } = 0.0f;

        public override float Value
        {
            get => base.Value;
            internal set => base.Value = value.DeadZoneCorrected(DeadZone);
        }

        public XBoxTrigger(float deadZone = 0.0f, float initialValue = 0.0f) : base(initialValue)
        {
            DeadZone = deadZone;
        }
    }
}
