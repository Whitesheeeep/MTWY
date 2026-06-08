using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameData.Editor
{
    public sealed class MapGridCheckerWindow : EditorWindow
    {
        private const string WindowTitle = "Map Grid Checker";
        private const string WindowUxmlPath = "Assets/Scripts/GameData/Map/Editor/MapGridChecker/UI/MapGridCheckerWindow.uxml";
        private const string ReportRowUxmlPath = "Assets/Scripts/GameData/Map/Editor/MapGridChecker/UI/MapGridCheckerReportRow.uxml";

        private MapGridCheckerViewModel viewModel;
        private MapGridCheckerView view;

        [MenuItem("Tools/GameData/Map Grid Checker")]
        private static void ShowWindow()
        {
            MapGridCheckerWindow window = GetWindow<MapGridCheckerWindow>();
            window.titleContent = new GUIContent(WindowTitle);
            window.minSize = new Vector2(940f, 560f);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();

            VisualTreeAsset uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(WindowUxmlPath);
            if (uxml == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing UXML: {WindowUxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            VisualTreeAsset reportRowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ReportRowUxmlPath);
            if (reportRowTemplate == null)
            {
                rootVisualElement.Add(new HelpBox($"Missing UXML: {ReportRowUxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            uxml.CloneTree(rootVisualElement);

            viewModel = new MapGridCheckerViewModel();
            viewModel.LoadSelection();

            view = new MapGridCheckerView(rootVisualElement, viewModel, reportRowTemplate);
            view.Bind();
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
            SceneView.duringSceneGui += HandleSceneGUI;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= HandleUndoRedoPerformed;
            SceneView.duringSceneGui -= HandleSceneGUI;
        }

        private void HandleUndoRedoPerformed()
        {
            viewModel?.HandleUndoRedo();
        }

        private void HandleSceneGUI(SceneView sceneView)
        {
            MapGridCheckerSceneOverlay.Draw(sceneView, viewModel);
        }
    }
}
