using UnityEngine;
using System.Collections.Generic;

namespace ExoLoader;
public class LoadingErrorOverlay : MonoBehaviour
{
    private bool showOverlay = true;
    private string errorMessage = "";
    private Vector2 scrollPosition;

    void Start()
    {
        List<string> errors = ModLoadingStatus.GetErrors();
        bool showErrorOverlay = ExoLoaderSave.GetSetting("showErrorOverlay", true);
        if (showErrorOverlay && errors != null && errors.Count > 0)
        {
            errorMessage = "Mod loading encountered errors:\n\n";
            foreach (string error in errors)
            {
                errorMessage += $"- {error}\n";
            }
        }
        else
        {
            showOverlay = false;
        }
    }

    void OnGUI()
    {
        if (!showOverlay)
            return;

        float width = Screen.width / 4f;
        float height = Screen.height / 4f;
        float x = Screen.width - width;
        float y = 0;

        GUI.Box(new Rect(x, y, width, height), "ExoLoader Mod Loading Errors");

        GUILayout.BeginArea(new Rect(x + 10, y + 25, width - 20, height - 50));

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUILayout.Label(errorMessage);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("Copy to Clipboard"))
        {
            GUIUtility.systemCopyBuffer = errorMessage;
        }
        if (GUILayout.Button("Do not show again"))
        {
            ExoLoaderSave.UpdateSettings("showErrorOverlay", false);
            showOverlay = false;
        }
        if (GUILayout.Button("Close"))
        {
            showOverlay = false;
        }

        GUILayout.EndArea();
    }
}
