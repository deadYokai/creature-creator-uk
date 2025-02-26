using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.CreatureCreator
{
    public class MobileControlsUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private PlatformSpecificScaler[] scalers;

        [SerializeField] private Joystick fixedJoystick;
        [SerializeField] private Joystick floatJoystick;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button networkButton;
        [SerializeField] private Button talkButton;
        #endregion

        #region Properties
        public Joystick FixedJoystick => fixedJoystick;
        public Joystick FloatJoystick => floatJoystick;
        public Button SettingsButton => settingsButton;
        public Button NetworkButton => networkButton;
        public Button TalkButton => talkButton;
        public PlatformSpecificScaler[] Scalers => scalers;
        #endregion

        #region Methods
        private void Start()
        {
            settingsButton.onClick.AddListener(delegate
            {
                PauseMenu.Instance.Open();
            });

            networkButton.gameObject.SetActive(WorldManager.Instance.IsMultiplayer);
            networkButton.onClick.AddListener(delegate
            {
                NetworkPlayersMenu.Instance.Toggle();
            });

            talkButton.gameObject.SetActive(WorldManager.Instance.IsMultiplayer);
            talkButton.onClick.AddListener(delegate
            {
                Player.Instance.Messenger.Open();
            });
        }
        #endregion
    }
}