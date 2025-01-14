using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator.Abilities
{
    [CreateAssetMenu(menuName = "Creature Creator/Ability/Dance/StickBug")]
    public class StickBug : Dance
    {
        public override bool CanPerform => base.CanPerform && Player.Instance.Constructor.Legs.Count > 0 && !Player.Instance.Underwater.IsUnderwater && Player.Instance.Grounded.IsGrounded;
    }
}