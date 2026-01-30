using UnityEditor;
using UnityEngine;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Editor
{
    [InitializeOnLoad]
    public class WelcomeWindowInitializer
    {
        private static int applicationHash => Application.dataPath.GetHashCode();
        private const string HasShownWindowKey = "ParallelCascades.HasShownWelcomeWindow";

        private static bool hasShownWindow
        {
            get => EditorPrefs.GetBool(HasShownWindowKey + applicationHash, false);
            set => EditorPrefs.SetBool(HasShownWindowKey + applicationHash, value);
        }

        static WelcomeWindowInitializer()
        {
            if (hasShownWindow)
            {
                return;
            }

            EditorApplication.update += OnEditorApplicationUpdate;
        }

        private static void OnEditorApplicationUpdate()
        {
            EditorApplication.update -= OnEditorApplicationUpdate;

            // Open the welcome window
            WelcomeWindow.ShowWindow();

            // Set the flag to indicate that the window has been shown
            hasShownWindow = true;
        }
    }
}