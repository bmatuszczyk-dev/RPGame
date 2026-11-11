using System.Collections.Generic;
using System.IO;
using RPGame.Core.Effects;
using RPGame.Progression;
using UnityEditor;
using UnityEngine;

namespace RPGame.Progression.Editor
{
    public sealed class PerkTreeEditorWindow : EditorWindow
    {
        private const float ToolbarHeight = 24f;
        private const float InspectorWidth = 330f;
        private const float NodeMinWidth = 48f;
        private const float NodeMaxWidth = 220f;
        private const float NodeMinHeight = 48f;
        private const float NodePadding = 7f;
        private const float NodeLineSpacing = 2f;
        private const float GridSmall = 20f;
        private const float GridLarge = 100f;

        private readonly List<PerkEditState> perks = new();
        private readonly List<PerkDefinition> jobPerks = new();

        private JobDefinition job;
        private SerializedObject serializedJob;
        private PerkEditState selectedPerk;
        private PerkEditState draggedPerk;
        private PerkEditState pendingConnectionStart;
        private Vector2 canvasPan;
        private Vector2 dragOffset;
        private Vector2 canvasScroll;
        private bool isDirty;

        [MenuItem("Tools/RPGame/Perk Tree Editor")]
        public static void OpenWindow()
        {
            GetWindow<PerkTreeEditorWindow>("Perk Tree Editor");
        }

        public static void Open(JobDefinition jobDefinition)
        {
            PerkTreeEditorWindow window = GetWindow<PerkTreeEditorWindow>("Perk Tree Editor");
            window.SetJob(jobDefinition);
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
        }

        private void OnGUI()
        {
            DrawToolbar();

            Rect contentRect = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
            Rect inspectorRect = new Rect(position.width - InspectorWidth, ToolbarHeight, InspectorWidth, contentRect.height);
            Rect canvasRect = new Rect(0f, ToolbarHeight, position.width - InspectorWidth, contentRect.height);

            DrawCanvas(canvasRect);
            DrawInspector(inspectorRect);
            HandleCanvasEvents(canvasRect);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(ToolbarHeight)))
            {
                EditorGUI.BeginChangeCheck();
                JobDefinition selectedJob = (JobDefinition)EditorGUILayout.ObjectField(job, typeof(JobDefinition), false, GUILayout.Width(260f));
                if (EditorGUI.EndChangeCheck())
                {
                    SetJob(selectedJob);
                }

                using (new EditorGUI.DisabledScope(job == null))
                {
                    if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    {
                        LoadJob();
                    }

                    if (GUILayout.Button("Add Perk", EditorStyles.toolbarButton, GUILayout.Width(80f)))
                    {
                        AddPerk();
                    }

                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                    {
                        Save();
                    }
                }

                GUILayout.FlexibleSpace();

                if (isDirty)
                {
                    GUILayout.Label("Unsaved changes", EditorStyles.miniLabel);
                }
            }
        }

        private void DrawCanvas(Rect canvasRect)
        {
            GUI.Box(canvasRect, GUIContent.none);
            DrawGrid(canvasRect, GridSmall, new Color(0f, 0f, 0f, 0.16f));
            DrawGrid(canvasRect, GridLarge, new Color(0f, 0f, 0f, 0.28f));

            if (job == null)
            {
                GUI.Label(canvasRect, "Select a JobDefinition to edit its perk tree.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            GUI.BeginGroup(canvasRect);
            Rect localCanvasRect = new Rect(Vector2.zero, canvasRect.size);
            DrawConnections(localCanvasRect);
            DrawNodes(localCanvasRect);
            GUI.EndGroup();
        }

        private void DrawGrid(Rect canvasRect, float spacing, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            Vector2 offset = new Vector2(canvasPan.x % spacing, canvasPan.y % spacing);
            for (float x = canvasRect.x + offset.x; x < canvasRect.xMax; x += spacing)
            {
                Handles.DrawLine(new Vector3(x, canvasRect.y), new Vector3(x, canvasRect.yMax));
            }

            for (float y = canvasRect.y + offset.y; y < canvasRect.yMax; y += spacing)
            {
                Handles.DrawLine(new Vector3(canvasRect.x, y), new Vector3(canvasRect.xMax, y));
            }

            Handles.color = Color.white;
            Handles.EndGUI();
        }

        private void DrawConnections(Rect canvasRect)
        {
            Handles.BeginGUI();
            HashSet<string> drawnConnectionKeys = new();

            for (int i = 0; i < perks.Count; i++)
            {
                PerkEditState from = perks[i];
                for (int j = 0; j < from.ConnectedPerkIds.Count; j++)
                {
                    PerkEditState to = FindPerk(from.ConnectedPerkIds[j]);
                    if (to == null || !drawnConnectionKeys.Add(GetConnectionKey(from.PerkId, to.PerkId)))
                    {
                        continue;
                    }

                    DrawConnectionLine(GetNodeCenter(from), GetNodeCenter(to), new Color(0.78f, 0.78f, 0.78f, 1f));
                }
            }

            if (pendingConnectionStart != null)
            {
                Vector2 mousePosition = Event.current.mousePosition;
                DrawConnectionLine(GetNodeCenter(pendingConnectionStart), mousePosition, new Color(0.95f, 0.78f, 0.25f, 1f));
            }

            Handles.EndGUI();
        }

        private void DrawConnectionLine(Vector2 from, Vector2 to, Color color)
        {
            Handles.color = color;
            Handles.DrawAAPolyLine(4f, from, to);
            Handles.color = Color.white;
        }

        private void DrawNodes(Rect canvasRect)
        {
            for (int i = 0; i < perks.Count; i++)
            {
                PerkEditState perk = perks[i];
                Rect nodeRect = GetNodeRect(perk);
                bool isSelected = perk == selectedPerk;
                Color previousColor = GUI.color;
                GUI.color = isSelected ? new Color(0.95f, 0.82f, 0.35f, 1f) : Color.white;
                GUI.Box(nodeRect, GUIContent.none, EditorStyles.helpBox);
                GUI.color = previousColor;

                string titleText = GetNodeTitle(perk);
                string statusText = perk.IsStartingPerk ? "Starting perk" : $"Cost: {perk.Cost}";
                float contentWidth = nodeRect.width - NodePadding * 2f;
                float titleHeight = EditorStyles.boldLabel.CalcHeight(new GUIContent(titleText), contentWidth);
                float idHeight = EditorStyles.miniLabel.CalcHeight(new GUIContent(perk.PerkId), contentWidth);
                float statusHeight = EditorStyles.miniLabel.CalcHeight(new GUIContent(statusText), contentWidth);
                float currentY = nodeRect.y + NodePadding;

                Rect titleRect = new Rect(nodeRect.x + NodePadding, currentY, contentWidth, titleHeight);
                GUI.Label(titleRect, titleText, EditorStyles.boldLabel);
                currentY += titleHeight + NodeLineSpacing;

                Rect idRect = new Rect(nodeRect.x + NodePadding, currentY, contentWidth, idHeight);
                GUI.Label(idRect, perk.PerkId, EditorStyles.miniLabel);
                currentY += idHeight + NodeLineSpacing;

                Rect startRect = new Rect(nodeRect.x + NodePadding, currentY, contentWidth, statusHeight);
                GUI.Label(startRect, statusText, EditorStyles.miniLabel);
            }
        }

        private void DrawInspector(Rect inspectorRect)
        {
            GUILayout.BeginArea(inspectorRect, EditorStyles.helpBox);

            if (selectedPerk == null)
            {
                GUILayout.Label("Perk", EditorStyles.boldLabel);
                GUILayout.Label("Select a perk node to edit its fields.", EditorStyles.wordWrappedMiniLabel);
                GUILayout.EndArea();
                return;
            }

            canvasScroll = EditorGUILayout.BeginScrollView(canvasScroll);
            GUILayout.Label("Perk", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();

            selectedPerk.PerkId = EditorGUILayout.TextField("Perk Id", selectedPerk.PerkId);
            selectedPerk.DisplayName = EditorGUILayout.TextField("Display Name", selectedPerk.DisplayName);
            EditorGUILayout.LabelField("Description");
            selectedPerk.Description = EditorGUILayout.TextArea(selectedPerk.Description, GUILayout.MinHeight(58f));
            selectedPerk.Cost = Mathf.Max(1, EditorGUILayout.IntField("Cost", selectedPerk.Cost));
            selectedPerk.IsStartingPerk = EditorGUILayout.Toggle("Starting Perk", selectedPerk.IsStartingPerk);
            selectedPerk.UIPosition = EditorGUILayout.Vector2Field("UI Position", selectedPerk.UIPosition);

            EditorGUILayout.Space(8f);
            DrawConnectionEditor(selectedPerk);

            EditorGUILayout.Space(8f);
            DrawEffectsEditor(selectedPerk);

            if (EditorGUI.EndChangeCheck())
            {
                MarkDirty();
            }

            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawConnectionEditor(PerkEditState perk)
        {
            GUILayout.Label("Next Perks", EditorStyles.boldLabel);

            for (int i = perk.ConnectedPerkIds.Count - 1; i >= 0; i--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    PerkEditState connectedPerk = FindPerk(perk.ConnectedPerkIds[i]);
                    string label = connectedPerk != null ? connectedPerk.PerkId : perk.ConnectedPerkIds[i];
                    GUILayout.Label(label);

                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        RemoveConnection(perk, perk.ConnectedPerkIds[i]);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                pendingConnectionStart = GUILayout.Toggle(pendingConnectionStart == perk, "Link From This", "Button")
                    ? perk
                    : pendingConnectionStart == perk ? null : pendingConnectionStart;

                if (GUILayout.Button("Clear", GUILayout.Width(70f)))
                {
                    ClearConnections(perk);
                }
            }
        }

        private void DrawEffectsEditor(PerkEditState perk)
        {
            GUILayout.Label("Effects", EditorStyles.boldLabel);

            for (int i = 0; i < perk.Effects.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    perk.Effects[i] = (EffectDefinition)EditorGUILayout.ObjectField(perk.Effects[i], typeof(EffectDefinition), false);
                    if (GUILayout.Button("-", GUILayout.Width(24f)))
                    {
                        perk.Effects.RemoveAt(i);
                        MarkDirty();
                    }
                }
            }

            if (GUILayout.Button("Add Effect"))
            {
                perk.Effects.Add(null);
                MarkDirty();
            }
        }

        private void HandleCanvasEvents(Rect canvasRect)
        {
            Event current = Event.current;
            if (!canvasRect.Contains(current.mousePosition))
            {
                return;
            }

            Vector2 localMousePosition = current.mousePosition - canvasRect.position;
            switch (current.type)
            {
                case EventType.MouseDown:
                    if (current.button == 0)
                    {
                        HandleLeftMouseDown(localMousePosition);
                        current.Use();
                    }
                    else if (current.button == 2)
                    {
                        dragOffset = localMousePosition - canvasPan;
                        current.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (current.button == 0 && draggedPerk != null)
                    {
                        draggedPerk.UIPosition = ScreenToTreePosition(localMousePosition - dragOffset);
                        MarkDirty();
                        Repaint();
                        current.Use();
                    }
                    else if (current.button == 2)
                    {
                        canvasPan = localMousePosition - dragOffset;
                        Repaint();
                        current.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (current.button == 0)
                    {
                        draggedPerk = null;
                        current.Use();
                    }
                    break;
            }
        }

        private void HandleLeftMouseDown(Vector2 localMousePosition)
        {
            PerkEditState clickedPerk = GetPerkAt(localMousePosition);
            if (clickedPerk == null)
            {
                selectedPerk = null;
                pendingConnectionStart = null;
                Repaint();
                return;
            }

            if (pendingConnectionStart != null && pendingConnectionStart != clickedPerk)
            {
                ToggleConnection(pendingConnectionStart, clickedPerk);
                pendingConnectionStart = null;
                selectedPerk = clickedPerk;
                return;
            }

            selectedPerk = clickedPerk;
            draggedPerk = clickedPerk;
            dragOffset = localMousePosition - TreeToScreenPosition(clickedPerk.UIPosition);
            Repaint();
        }

        private void SetJob(JobDefinition jobDefinition)
        {
            if (isDirty && !EditorUtility.DisplayDialog("Unsaved perk changes", "Discard unsaved perk editor changes?", "Discard", "Cancel"))
            {
                return;
            }

            job = jobDefinition;
            serializedJob = job != null ? new SerializedObject(job) : null;
            LoadJob();
        }

        private void LoadJob()
        {
            perks.Clear();
            jobPerks.Clear();
            selectedPerk = null;
            draggedPerk = null;
            pendingConnectionStart = null;
            isDirty = false;

            if (job == null)
            {
                Repaint();
                return;
            }

            serializedJob ??= new SerializedObject(job);
            serializedJob.Update();
            SerializedProperty jobPerksProperty = serializedJob.FindProperty("jobPerks");

            for (int i = 0; i < jobPerksProperty.arraySize; i++)
            {
                PerkDefinition perk = jobPerksProperty.GetArrayElementAtIndex(i).objectReferenceValue as PerkDefinition;
                if (perk == null || jobPerks.Contains(perk))
                {
                    continue;
                }

                jobPerks.Add(perk);
                perks.Add(new PerkEditState(perk));
            }

            Repaint();
        }

        private void AddPerk()
        {
            if (job == null)
            {
                return;
            }

            string perkId = GetUniquePerkId("NewPerk");
            PerkEditState state = PerkEditState.CreateNew(perkId);
            state.UIPosition = ScreenToTreePosition(new Vector2(120f + perks.Count * 24f, 120f + perks.Count * 24f));

            perks.Add(state);
            selectedPerk = state;
            MarkDirty();
        }

        private void Save()
        {
            if (job == null)
            {
                return;
            }

            NormalizePerkIds();
            NormalizeConnections();

            for (int i = 0; i < perks.Count; i++)
            {
                EnsurePerkAsset(perks[i]);
                perks[i].Save();
                RenameAssetToPerkId(perks[i]);
            }

            serializedJob ??= new SerializedObject(job);
            serializedJob.Update();
            SerializedProperty jobPerksProperty = serializedJob.FindProperty("jobPerks");
            jobPerksProperty.arraySize = perks.Count;
            for (int i = 0; i < perks.Count; i++)
            {
                jobPerksProperty.GetArrayElementAtIndex(i).objectReferenceValue = perks[i].Asset;
            }

            serializedJob.ApplyModifiedProperties();
            EditorUtility.SetDirty(job);
            AssetDatabase.SaveAssets();
            isDirty = false;
            Repaint();
        }

        private void EnsurePerkAsset(PerkEditState perk)
        {
            if (perk.Asset != null)
            {
                return;
            }

            string folderPath = GetPerksFolderPath(job);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string assetName = SanitizeAssetName(perk.PerkId);
            string perkPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{assetName}.asset");
            PerkDefinition asset = CreateInstance<PerkDefinition>();
            AssetDatabase.CreateAsset(asset, perkPath);
            perk.AssignAsset(asset);
        }

        private void NormalizePerkIds()
        {
            Dictionary<string, string> changedPerkIds = new();
            HashSet<string> usedPerkIds = new();

            for (int i = 0; i < perks.Count; i++)
            {
                PerkEditState perk = perks[i];
                string oldPerkId = perk.OriginalPerkId;
                string normalizedPerkId = string.IsNullOrWhiteSpace(perk.PerkId)
                    ? (perk.Asset != null ? perk.Asset.name : "NewPerk")
                    : perk.PerkId.Trim();
                string uniquePerkId = normalizedPerkId;
                int duplicateIndex = 1;

                while (!usedPerkIds.Add(uniquePerkId))
                {
                    uniquePerkId = $"{normalizedPerkId}{duplicateIndex}";
                    duplicateIndex++;
                }

                perk.PerkId = uniquePerkId;

                if (!string.IsNullOrWhiteSpace(oldPerkId) && oldPerkId != uniquePerkId)
                {
                    changedPerkIds[oldPerkId] = uniquePerkId;
                }
            }

            if (changedPerkIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < perks.Count; i++)
            {
                List<string> connectedPerkIds = perks[i].ConnectedPerkIds;
                for (int j = 0; j < connectedPerkIds.Count; j++)
                {
                    if (changedPerkIds.TryGetValue(connectedPerkIds[j], out string newPerkId))
                    {
                        connectedPerkIds[j] = newPerkId;
                    }
                }
            }
        }

        private void RenameAssetToPerkId(PerkEditState perk)
        {
            string assetPath = AssetDatabase.GetAssetPath(perk.Asset);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return;
            }

            string assetName = SanitizeAssetName(perk.PerkId);
            if (string.IsNullOrWhiteSpace(assetName) || Path.GetFileNameWithoutExtension(assetPath) == assetName)
            {
                return;
            }

            string error = AssetDatabase.RenameAsset(assetPath, assetName);
            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.LogWarning($"Could not rename perk asset '{assetPath}' to '{assetName}': {error}", perk.Asset);
            }
        }

        private string GetUniquePerkId(string basePerkId)
        {
            string sanitizedBaseId = string.IsNullOrWhiteSpace(basePerkId) ? "NewPerk" : basePerkId.Trim();
            string candidate = sanitizedBaseId;
            int index = 1;

            while (FindPerk(candidate) != null)
            {
                candidate = $"{sanitizedBaseId}{index}";
                index++;
            }

            return candidate;
        }

        private static string SanitizeAssetName(string value)
        {
            string fallbackValue = string.IsNullOrWhiteSpace(value) ? "Perk" : value.Trim();
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                fallbackValue = fallbackValue.Replace(invalidChar, '_');
            }

            return fallbackValue;
        }

        private void NormalizeConnections()
        {
            for (int i = 0; i < perks.Count; i++)
            {
                PerkEditState perk = perks[i];
                for (int j = perk.ConnectedPerkIds.Count - 1; j >= 0; j--)
                {
                    if (FindPerk(perk.ConnectedPerkIds[j]) == null || perk.ConnectedPerkIds[j] == perk.PerkId)
                    {
                        perk.ConnectedPerkIds.RemoveAt(j);
                    }
                }
            }
        }

        private string GetPerksFolderPath(JobDefinition jobDefinition)
        {
            string jobPath = AssetDatabase.GetAssetPath(jobDefinition);
            string jobFolder = Path.GetDirectoryName(jobPath)?.Replace("\\", "/");
            return string.IsNullOrWhiteSpace(jobFolder) ? "Assets/Perks" : $"{jobFolder}/Perks";
        }

        private PerkEditState GetPerkAt(Vector2 localPosition)
        {
            for (int i = perks.Count - 1; i >= 0; i--)
            {
                if (GetNodeRect(perks[i]).Contains(localPosition))
                {
                    return perks[i];
                }
            }

            return null;
        }

        private PerkEditState FindPerk(string perkId)
        {
            for (int i = 0; i < perks.Count; i++)
            {
                if (perks[i].PerkId == perkId)
                {
                    return perks[i];
                }
            }

            return null;
        }

        private Rect GetNodeRect(PerkEditState perk)
        {
            Vector2 position = TreeToScreenPosition(perk.UIPosition);
            Vector2 size = GetNodeSize(perk);
            return new Rect(position.x - size.x * 0.5f, position.y - size.y * 0.5f, size.x, size.y);
        }

        private Vector2 GetNodeCenter(PerkEditState perk)
        {
            return TreeToScreenPosition(perk.UIPosition);
        }

        private Vector2 TreeToScreenPosition(Vector2 treePosition)
        {
            return new Vector2(treePosition.x, -treePosition.y) + canvasPan;
        }

        private Vector2 ScreenToTreePosition(Vector2 screenPosition)
        {
            Vector2 localPosition = screenPosition - canvasPan;
            return new Vector2(localPosition.x, -localPosition.y);
        }

        private Vector2 GetNodeSize(PerkEditState perk)
        {
            string titleText = GetNodeTitle(perk);
            string statusText = perk.IsStartingPerk ? "Starting perk" : $"Cost: {perk.Cost}";
            float measuredWidth = Mathf.Max(
                EditorStyles.boldLabel.CalcSize(new GUIContent(titleText)).x,
                EditorStyles.miniLabel.CalcSize(new GUIContent(perk.PerkId)).x,
                EditorStyles.miniLabel.CalcSize(new GUIContent(statusText)).x);
            float width = Mathf.Clamp(measuredWidth + NodePadding * 2f, NodeMinWidth, NodeMaxWidth);
            float contentWidth = width - NodePadding * 2f;
            float height = NodePadding * 2f
                + EditorStyles.boldLabel.CalcHeight(new GUIContent(titleText), contentWidth)
                + EditorStyles.miniLabel.CalcHeight(new GUIContent(perk.PerkId), contentWidth)
                + EditorStyles.miniLabel.CalcHeight(new GUIContent(statusText), contentWidth)
                + NodeLineSpacing * 2f;

            return new Vector2(width, Mathf.Max(NodeMinHeight, height));
        }

        private string GetNodeTitle(PerkEditState perk)
        {
            return string.IsNullOrWhiteSpace(perk.DisplayName) ? perk.PerkId : perk.DisplayName;
        }

        private void ToggleConnection(PerkEditState first, PerkEditState second)
        {
            if (first.ConnectedPerkIds.Contains(second.PerkId))
            {
                RemoveConnection(first, second.PerkId);
                return;
            }

            AddUnique(first.ConnectedPerkIds, second.PerkId);
            MarkDirty();
        }

        private void RemoveConnection(PerkEditState perk, string connectedPerkId)
        {
            perk.ConnectedPerkIds.Remove(connectedPerkId);
            MarkDirty();
        }

        private void ClearConnections(PerkEditState perk)
        {
            for (int i = perk.ConnectedPerkIds.Count - 1; i >= 0; i--)
            {
                RemoveConnection(perk, perk.ConnectedPerkIds[i]);
            }
        }

        private static string GetConnectionKey(string firstPerkId, string secondPerkId)
        {
            return string.CompareOrdinal(firstPerkId, secondPerkId) <= 0
                ? $"{firstPerkId}:{secondPerkId}"
                : $"{secondPerkId}:{firstPerkId}";
        }

        private void MarkDirty()
        {
            isDirty = true;
        }

        private static void AddUnique(List<string> values, string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && !values.Contains(value))
            {
                values.Add(value);
            }
        }

        private sealed class PerkEditState
        {
            public PerkDefinition Asset { get; private set; }
            public string OriginalPerkId;
            public string PerkId;
            public string DisplayName;
            public string Description;
            public readonly List<string> ConnectedPerkIds = new();
            public int Cost;
            public bool IsStartingPerk;
            public Vector2 UIPosition;
            public readonly List<EffectDefinition> Effects = new();

            public static PerkEditState CreateNew(string perkId)
            {
                return new PerkEditState
                {
                    OriginalPerkId = perkId,
                    PerkId = perkId,
                    DisplayName = "New Perk",
                    Description = string.Empty,
                    Cost = 1,
                    IsStartingPerk = false,
                    UIPosition = Vector2.zero
                };
            }

            private PerkEditState()
            {
            }

            public PerkEditState(PerkDefinition asset)
            {
                Asset = asset;
                SerializedObject serializedPerk = new SerializedObject(asset);
                serializedPerk.Update();

                PerkId = serializedPerk.FindProperty("perkId").stringValue;
                OriginalPerkId = PerkId;
                DisplayName = serializedPerk.FindProperty("displayName").stringValue;
                Description = serializedPerk.FindProperty("description").stringValue;
                Cost = serializedPerk.FindProperty("cost").intValue;
                IsStartingPerk = serializedPerk.FindProperty("isStartingPerk").boolValue;
                UIPosition = serializedPerk.FindProperty("uiPosition").vector2Value;

                SerializedProperty connectedPerks = serializedPerk.FindProperty("connectedPerkIds");
                for (int i = 0; i < connectedPerks.arraySize; i++)
                {
                    AddUnique(ConnectedPerkIds, connectedPerks.GetArrayElementAtIndex(i).stringValue);
                }

                SerializedProperty effects = serializedPerk.FindProperty("effects");
                for (int i = 0; i < effects.arraySize; i++)
                {
                    Effects.Add(effects.GetArrayElementAtIndex(i).objectReferenceValue as EffectDefinition);
                }
            }

            public void AssignAsset(PerkDefinition asset)
            {
                Asset = asset;
            }

            public void Save()
            {
                if (Asset == null)
                {
                    Debug.LogWarning($"Cannot save perk '{PerkId}' because it does not have an asset.");
                    return;
                }

                SerializedObject serializedPerk = new SerializedObject(Asset);
                serializedPerk.Update();
                serializedPerk.FindProperty("perkId").stringValue = string.IsNullOrWhiteSpace(PerkId) ? Asset.name : PerkId.Trim();
                serializedPerk.FindProperty("displayName").stringValue = string.IsNullOrWhiteSpace(DisplayName) ? PerkId : DisplayName.Trim();
                serializedPerk.FindProperty("description").stringValue = Description;
                serializedPerk.FindProperty("cost").intValue = Mathf.Max(1, Cost);
                serializedPerk.FindProperty("isStartingPerk").boolValue = IsStartingPerk;
                serializedPerk.FindProperty("uiPosition").vector2Value = UIPosition;

                SerializedProperty connectedPerks = serializedPerk.FindProperty("connectedPerkIds");
                connectedPerks.arraySize = ConnectedPerkIds.Count;
                for (int i = 0; i < ConnectedPerkIds.Count; i++)
                {
                    connectedPerks.GetArrayElementAtIndex(i).stringValue = ConnectedPerkIds[i];
                }

                SerializedProperty effects = serializedPerk.FindProperty("effects");
                effects.arraySize = Effects.Count;
                for (int i = 0; i < Effects.Count; i++)
                {
                    effects.GetArrayElementAtIndex(i).objectReferenceValue = Effects[i];
                }

                serializedPerk.ApplyModifiedProperties();
                EditorUtility.SetDirty(Asset);
                OriginalPerkId = PerkId;
            }
        }
    }
}
