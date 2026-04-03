using System;
using UnityEngine;

namespace World1BossFight
{
    public class BossHeart : Breakable
    {
        public event Action<BossHeart> Damaged;

        public override void Break()
        {
            Damaged?.Invoke(this);
            base.Break();
        }
    }
}
