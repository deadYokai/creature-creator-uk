// Creature Creator - https://github.com/daniellochner/Creature-Creator
// Copyright (c) Daniel Lochner

using System;
using System.Text;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

namespace DanielLochner.Assets.CreatureCreator
{
    public class SingleplayerUI : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Menu singleplayerMenu;
        [SerializeField] private OptionSelector mapOS;
        [SerializeField] private OptionSelector modeOS;
        [SerializeField] private Toggle npcToggle;
        [SerializeField] private Toggle pveToggle;
        [SerializeField] private CanvasGroup pveCG;
        [SerializeField] private Toggle unlimitedToggle;
        [SerializeField] private CanvasGroup unlimitedCG;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private BlinkingText statusBT;
        [SerializeField] private MapUI mapUI;
        [SerializeField] private bool showComingSoon;

        private Coroutine updateStatusCoroutine;
        #endregion

        #region Methods
        private void Start()
        {
            Setup();
        }

        private void OnEnable()
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnFailed;
        }
        private void OnDisable()
        {
            if (NetworkManager.Singleton)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnFailed;
            }
            statusText.text = "";
        }

        public void Setup()
        {
            mapOS.SetupUsingEnum<Map>();
            if (!showComingSoon)
            {
                mapOS.Options.RemoveAt(mapOS.Options.Count - 1);
            }
            mapOS.Select(Map.Island, false);
            singleplayerMenu.OnOpen += UpdateMap;

            modeOS.SetupUsingEnum<Mode>();
            modeOS.OnSelected.AddListener(delegate (int option)
            {
                bool show = option == 1;
                unlimitedCG.interactable = show;
                unlimitedCG.alpha = show ? 1f : 0.25f;
            });
            modeOS.Select(Mode.Adventure, false);

            npcToggle.onValueChanged.AddListener(delegate (bool isOn)
            {
                pveCG.interactable = isOn;
                pveCG.alpha = isOn ? 1f : 0.25f;
            });
        }

        public void Play()
        {
            try
            {
                // Setup World
                string mapName = ((Map)mapOS.Selected).ToString();
                bool creativeMode = ((Mode)modeOS.Selected) == Mode.Creative;
                bool spawnNPC = npcToggle.isOn;
                bool enablePVE = pveToggle.isOn;
                bool unlimited = unlimitedToggle.isOn && creativeMode;
                WorldManager.Instance.World = new WorldSP(mapName, creativeMode, spawnNPC, enablePVE, unlimited);

                // Check Premium
                if (unlimited && !PremiumManager.Data.IsPremium)
                {
                    throw new Exception(LocalizationUtility.Localize("mainmenu_premium_unlimited"));
                }

                // Check Map
                Map map = Enum.Parse<Map>(mapName);
                if (map == Map.ComingSoon)
                {
                    throw new Exception(LocalizationUtility.Localize("mainmenu_map-coming-soon"));
                }
                if (!creativeMode && !ProgressManager.Instance.IsMapUnlocked(map))
                {
                    throw new Exception(LocalizationUtility.Localize("mainmenu_map-locked", LocalizationUtility.Localize($"option_map_{mapName}".ToLower())));
                }

                // Set Connection Data
                NetworkManager.Singleton.NetworkConfig.NetworkTransport = NetworkTransportPicker.Instance.GetTransport<UnityTransport>("localhost");
                NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new ConnectionData("", "", "")));

                // Start Host
                NetworkManager.Singleton.StartHost();
            }
            catch (Exception e)
            {
                UpdateStatus(e.Message, Color.red);
            }
        }

        private void OnFailed(ulong clientId)
        {
            UpdateStatus("Failed to create world...", Color.red);
        }

        private void UpdateMap()
        {
            mapUI.UpdatePadlock(mapOS, modeOS);
        }
        private void UpdateStatus(string status, Color color, float duration = 5)
        {
            if (updateStatusCoroutine != null)
            {
                StopCoroutine(updateStatusCoroutine);
            }

            statusText.CrossFadeAlpha(0f, 0f, true);
            statusText.CrossFadeAlpha(1f, 0.25f, true);
            statusText.text = status;
            statusText.color = color;
            statusBT.IsBlinking = false;

            if (duration == -1)
            {
                statusBT.IsBlinking = true;
            }
            else
            {
                updateStatusCoroutine = this.Invoke(HideStatus, duration);
            }
        }
        private void HideStatus()
        {
            statusText.CrossFadeAlpha(0, 0.25f, true);
        }
        #endregion
    }
}