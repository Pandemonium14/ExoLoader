using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.UI;

namespace ExoLoader
{
    [HarmonyPatch]
    public class CorePatches
    {
        // Load exoloader save data when loading the Groundhogs
        [HarmonyPatch(typeof(Groundhogs), "Load")]
        [HarmonyPostfix]
        public static void GroundhogsLoadPostfix()
        {
            ExoLoaderSave.instance.Load();
        }

        // Inject our own setting button
        [HarmonyPatch(typeof(SettingsMenu), "CreateButton")]
        [HarmonyPostfix]
        public static void SettingsMenuCreateButtonPostfix(SettingsMenu __instance, Selectable __result, string settingName, Selectable aboveButton)
        {
            if (__instance == null || __result == null || settingName != "hardwareMouseCursor")
            {
                return;
            }

            Selectable currentAboveButton = __result;

            foreach (var setting in ExoLoaderSave.instance.settings)
            {
                string key = setting.Key;
                bool value = setting.Value;

                string text = GetButtonText(key);
                NWButton button = __instance.AddButton(text, null);
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
        
        public static Dictionary<string, string> settingNames = new()
        {
            { "showErrorOverlay", "Show Errors on Load" }
        };

        private static string GetButtonText(string settingName)
        {
            if (ExoLoaderSave.instance == null)
            {
                return settingName;
            }

            if (settingNames.TryGetValue(settingName, out string name))
            {
                return "[ExoLoader] " + name + " " + (ExoLoaderSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
            }

            return settingName + " " + (ExoLoaderSave.GetSetting(settingName) ? TextLocalized.Localize("button_on") : TextLocalized.Localize("button_off"));
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
