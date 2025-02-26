// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using UnityEngine;

namespace DanielLochner.Assets.CreatureCreator
{
    [RequireComponent(typeof(CreatureCamera))]
    public class CreatureInteractor : Interactor
    {
        #region Properties
        public CreatureCamera CreatureCamera { get; private set; }
        #endregion

        #region Methods
        private void Awake()
        {
            CreatureCamera = GetComponent<CreatureCamera>();
        }

        public override void Setup()
        {
            base.Setup();

            interactionCamera = CreatureCamera.MainCamera;

            InteractionsManager.Instance.OnTarget += delegate (GameObject targeted)
            {
                if (EditorManager.Instance.IsPlaying)
                {
                    CreatureCamera.CameraOrbit.SetFrozen(targeted != null);
                }
            };
        }
        #endregion
    }
}