using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ParallelCascades.ProceduralCelestialBodyMaterials.Editor
{
    public class WelcomeWindow : EditorWindow
    {
        [SerializeField] private VisualTreeAsset m_UXMLDocument;
        
        [MenuItem("Window/Parallel Cascades/Procedural Celestial Body Materials Welcome Window", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow(typeof(WelcomeWindow), true, "Procedural Celestial Body Materials Welcome", true);
            window.minSize = new Vector2(550, 350);
            window.maxSize = new Vector2(550, 350);
            window.Show();
        }

        private void CreateGUI()
        {
            m_UXMLDocument.CloneTree(rootVisualElement);
        }
    }
}