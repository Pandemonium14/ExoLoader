using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.UI;

namespace ExoLoader
{
    [HarmonyPatch]
    public class CorePatches
    {
        public static Dictionary<string, string> settingNames = new()
        {
            { "showErrorOverlay", "Show Errors on Load" }
        };

        public static Dictionary<string, string> settingDescriptions = new()
        {
            { "showErrorOverlay", "Show an overlay with error messages when the game loads. Useful for debugging." }
        };

        [HarmonyPatch(typeof(Groundhogs), "Load")]
        [HarmonyPostfix]
        public static void GroundhogsLoadPostfix()
        {
            ExoLoaderSave.instance.Load();
        }

        [HarmonyPatch(typeof(SettingsMenu), "CreateButton")]
        [HarmonyPostfix]
        public static void SettingsMenuCreateButtonPostfix(SettingsMenu __instance, Selectable __result, string settingName, Selectable aboveButton)
        {
            if (__instance == null || __result == null || settingName != "hardwareMouseCursor")
            {
                return;
            }

            Selectable currentAboveButton = __result;

            NWButton spacerButton = CreateSeparator(__instance);
            if (spacerButton != null)
            {
                ConnectNavigation(currentAboveButton, spacerButton);
                currentAboveButton = spacerButton;
            }

            NWButton headerButton = CreateModHeader(__instance);
            if (headerButton != null)
            {
                ConnectNavigation(currentAboveButton, headerButton);
                currentAboveButton = headerButton;
            }

            foreach (var setting in ExoLoaderSave.instance.settings)
            {
                string key = setting.Key;
                bool value = setting.Value;

                string text = GetButtonText(key);
                NWButton button = __instance.AddButton(text, null);

                string tooltip = GetButtonDescription(key);
                if (tooltip != null)
                {
                    button.tooltipText = tooltip;
                }

                Action listener2 = delegate
                {
                    SetSetting(key, !value, button);
                };
                button.onClick.ReplaceListener(delegate
                {
                    listener2();
                });

                ConnectNavigation(currentAboveButton, button);
                currentAboveButton = button;
            }
        }

        private static NWButton CreateSeparator(SettingsMenu settingsMenu)
        {
            try
            {
                NWButton separatorButton = settingsMenu.AddButton("", null);
                separatorButton.interactable = false;

                var buttonImage = separatorButton.GetComponent<UnityEngine.UI.Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = new UnityEngine.Color(0, 0, 0, 0);
                }

                var textComponent = separatorButton.GetComponent<Text>();
                if (textComponent != null)
                {
                    textComponent.text = "";
                    textComponent.color = new UnityEngine.Color(0, 0, 0, 0);
                }

                var rectTransform = separatorButton.GetComponent<UnityEngine.RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new UnityEngine.Vector2(rectTransform.sizeDelta.x, rectTransform.sizeDelta.y * 1.5f);
                }

                return separatorButton;
            }
            catch (Exception ex)
            {
                ModInstance.log($"Failed to create separator: {ex.Message}");
                return null;
            }
        }

        private static NWButton CreateModHeader(SettingsMenu settingsMenu)
        {
            try
            {
                NWButton headerButton = settingsMenu.AddButton("ExoLoader", null);

                headerButton.interactable = false;

                if (headerButton.GetComponent<Text>() != null)
                {
                    var textComponent = headerButton.GetComponent<Text>();
                    textComponent.fontStyle = UnityEngine.FontStyle.Bold;
                    textComponent.color = new UnityEngine.Color(1f, 0.8f, 0.2f, 1f);
                }

                return headerButton;
            }
            catch (Exception ex)
            {
                ModInstance.log($"Failed to create mod header: {ex.Message}");
                return null;
            }
        }

        private static string GetButtonText(string settingName)
        {
            if (ExoLoaderSave.instance == null)
            {
                return settingName;
            }

            if (settingNames.TryGetValue(settingName, out string name))
            {
                return name + " " + (ExoLoaderSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
            }

            return settingName + " " + (ExoLoaderSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
        }

        private static string GetButtonDescription(string settingName)
        {
            if (ExoLoaderSave.instance == null)
            {
                return null;
            }

            if (settingDescriptions.TryGetValue(settingName, out string description))
            {
                return description;
            }

            return null;
        }

        private static void ConnectNavigation(Selectable top, Selectable bottom)
        {
            if (!(top == null) || !(bottom == null))
            {
                if (bottom is NWSelectable nWSelectable)
                {
                    nWSelectable.selectOverrideOnUp = top;
                }
                else if (bottom is NWButton nWButton)
                {
                    nWButton.selectOverrideOnUp = top;
                }
                else if (bottom is NWDropdown nWDropdown)
                {
                    nWDropdown.selectOverrideOnUp = top;
                }

                if (top is NWSelectable nWSelectable2)
                {
                    nWSelectable2.selectOverrideOnDown = bottom;
                }
                else if (top is NWButton nWButton2)
                {
                    nWButton2.selectOverrideOnDown = bottom;
                }
                else if (top is NWDropdown nWDropdown2)
                {
                    nWDropdown2.selectOverrideOnDown = bottom;
                }
            }
        }

        private static void SetSetting(string settingName, object value, NWButton buttonToUpdate = null)
        {
            if (ExoLoaderSave.instance == null || ExoLoaderSave.instance.settings == null)
            {
                return;
            }

            ExoLoaderSave.UpdateSettings(settingName, (bool)value);

            if (!(buttonToUpdate != null))
            {
                return;
            }

            buttonToUpdate.text = GetButtonText(settingName);

            if (value is bool)
            {
                Action listener = delegate
                {
                    SetSetting(settingName, !(bool)value, buttonToUpdate);
                };
                buttonToUpdate.onClick.ReplaceListener(delegate
                {
                    listener();
                });
            }
        }
    }
}
