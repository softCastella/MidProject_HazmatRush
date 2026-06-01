using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class CsvToJsonWindow : EditorWindow
{
    private string _csvPath = "";
    private string _message = "";
    private MessageType _msgType = MessageType.None;

    [MenuItem("Tools/CSV to JSON")]
    public static void Open() => GetWindow<CsvToJsonWindow>("CSV to JSON");

    private void OnGUI()
    {
        GUILayout.Label("CSV to JSON Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space(8);

        // Drag & Drop 영역
        var dropRect = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        GUI.Box(dropRect, string.IsNullOrEmpty(_csvPath) ? "Drag & Drop CSV here" : Path.GetFileName(_csvPath));

        HandleDragDrop(dropRect);

        EditorGUILayout.Space(8);

        // 수동 입력
        _csvPath = EditorGUILayout.TextField("CSV Path", _csvPath);

        EditorGUILayout.Space(8);

        // 변환 버튼
        GUI.enabled = !string.IsNullOrEmpty(_csvPath) && File.Exists(_csvPath);
        if (GUILayout.Button("Convert to JSON", GUILayout.Height(36)))
            Convert();
        GUI.enabled = true;

        // 결과 메시지
        if (!string.IsNullOrEmpty(_message))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(_message, _msgType);
        }
    }

    private void HandleDragDrop(Rect rect)
    {
        var evt = Event.current;
        if (!rect.Contains(evt.mousePosition)) return;

        if (evt.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            evt.Use();
        }
        else if (evt.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (var path in DragAndDrop.paths)
            {
                if (path.EndsWith(".csv"))
                {
                    _csvPath = path;
                    _message = "";
                    break;
                }
            }
            evt.Use();
        }
    }

    private void Convert()
    {
        var lines = File.ReadAllLines(_csvPath, Encoding.UTF8);
        if (lines.Length < 2)
        {
            SetMessage("CSV has no data rows.", MessageType.Error);
            return;
        }

        var headers = lines[0].Split(',');

        var typeKeywords = new HashSet<string> { "int", "string", "float", "double", "bool", "long" };

        var rows = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            // 타입 정의 행 스킵
            var cols = line.Split(',');
            bool isTypeRow = true;
            foreach (var col in cols)
                if (!typeKeywords.Contains(col.Trim().ToLower())) { isTypeRow = false; break; }
            if (isTypeRow) continue;

            var values = line.Split(',');
            var fields = new List<string>();
            for (int j = 0; j < headers.Length; j++)
            {
                var key = headers[j].Trim();
                var val = j < values.Length ? values[j].Trim() : "";

                fields.Add(int.TryParse(val, out var n)
                    ? $"        \"{key}\": {n}"
                    : $"        \"{key}\": \"{val}\"");
            }
            rows.Add("    {\n" + string.Join(",\n", fields) + "\n    }");
        }

        var json = "[\n" + string.Join(",\n", rows) + "\n]";
        var outputPath = Path.ChangeExtension(_csvPath, ".json");
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        AssetDatabase.Refresh();

        SetMessage($"Saved: {outputPath}", MessageType.Info);
    }

    private void SetMessage(string msg, MessageType type)
    {
        _message = msg;
        _msgType = type;
        Repaint();
    }
}
