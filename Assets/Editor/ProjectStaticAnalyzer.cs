using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectStaticAnalyzer
{
    private const string RulesPath = "Assets/Data/static_rules.json";
    private const string SpecHtmlPath = "Assets/Docs/project-static-analysis.html";
    private const string ReportHtmlPath = "Assets/Docs/static-analysis-report.html";

    [MenuItem("MidProject/Static Analysis/Run All")]
    public static void RunAll()
    {
        List<CheckResult> results = RunChecks();
        WriteReportHtml(results);
        int pass = 0;
        int fail = 0;
        int skip = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].status == "Pass") pass++;
            else if (results[i].status == "Skip") skip++;
            else fail++;
        }

        string summary = $"정적 분석 완료 — Pass {pass}, Fail {fail}, Skip {skip}\n리포트: {ReportHtmlPath}";
        if (fail > 0)
            Debug.LogWarning(summary);
        else
            Debug.Log(summary);

        EditorUtility.RevealInFinder(Path.GetFullPath(ReportHtmlPath));
    }

    [MenuItem("MidProject/Static Analysis/Open Spec (HTML)")]
    public static void OpenSpec()
    {
        if (!File.Exists(SpecHtmlPath))
        {
            Debug.LogWarning($"스펙 없음: {SpecHtmlPath}");
            return;
        }
        Application.OpenURL("file:///" + Path.GetFullPath(SpecHtmlPath).Replace("\\", "/"));
    }

    [MenuItem("MidProject/Static Analysis/Open Latest Report")]
    public static void OpenReport()
    {
        if (!File.Exists(ReportHtmlPath))
        {
            Debug.LogWarning("리포트 없음. MidProject > Static Analysis > Run All 실행");
            return;
        }
        Application.OpenURL("file:///" + Path.GetFullPath(ReportHtmlPath).Replace("\\", "/"));
    }

    private static List<CheckResult> RunChecks()
    {
        var results = new List<CheckResult>();
        string projectRoot = Path.GetDirectoryName(Application.dataPath);

        if (!File.Exists(RulesPath))
        {
            results.Add(Fail("SYS", "규칙 파일", "", "static_rules.json 없음"));
            return results;
        }

        string jsonText = File.ReadAllText(RulesPath, Encoding.UTF8);
        JObject root = JObject.Parse(jsonText);
        JArray rules = root["rules"] as JArray;
        if (rules == null)
        {
            results.Add(Fail("SYS", "규칙 파싱", "", "rules 배열 없음"));
            return results;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            JObject rule = rules[i] as JObject;
            if (rule == null)
                continue;

            string id = rule.Value<string>("id") ?? "?";
            string feature = rule.Value<string>("feature") ?? "";
            string intent = rule.Value<string>("intent") ?? "";
            string kind = rule.Value<string>("kind") ?? "";

            switch (kind)
            {
                case "build_scenes":
                    results.Add(CheckBuildScenes(id, feature, intent, rule));
                    break;
                case "file_exists":
                    results.Add(CheckFileExists(id, feature, intent, rule, projectRoot));
                    break;
                case "files_exist":
                    results.Add(CheckFilesExist(id, feature, intent, rule, projectRoot));
                    break;
                case "file_contains":
                    results.Add(CheckFileContains(id, feature, intent, rule, projectRoot));
                    break;
                case "forbidden_in_scripts":
                    results.Add(CheckForbiddenInScripts(id, feature, intent, rule, projectRoot));
                    break;
                case "scene_refs":
                    results.Add(CheckSceneRefs(id, feature, intent, rule));
                    break;
                case "scene_values":
                    results.Add(CheckSceneValues(id, feature, intent, rule));
                    break;
                default:
                    results.Add(Fail(id, feature, intent, "알 수 없는 kind: " + kind));
                    break;
            }
        }

        return results;
    }

    private static CheckResult CheckBuildScenes(string id, string feature, string intent, JObject rule)
    {
        JArray expected = rule["scenes"] as JArray;
        if (expected == null)
            return Fail(id, feature, intent, "scenes 배열 없음");

        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length != expected.Count)
            return Fail(id, feature, intent, $"Build Settings 씬 수 {scenes.Length} (기대 {expected.Count})");

        for (int i = 0; i < expected.Count; i++)
        {
            string want = expected[i].ToString();
            if (i >= scenes.Length || scenes[i].path != want)
                return Fail(id, feature, intent, $"인덱스 {i}: 기대 {want}, 실제 {(i < scenes.Length ? scenes[i].path : "없음")}");
        }

        return Pass(id, feature, intent, "Build Settings 5씬 순서 일치");
    }

    private static CheckResult CheckFileExists(string id, string feature, string intent, JObject rule, string projectRoot)
    {
        string path = rule.Value<string>("path");
        if (string.IsNullOrEmpty(path))
            return Fail(id, feature, intent, "path 없음");

        string full = Path.Combine(projectRoot, path);
        if (!File.Exists(full))
            return Fail(id, feature, intent, "파일 없음: " + path);

        return Pass(id, feature, intent, path);
    }

    private static CheckResult CheckFilesExist(string id, string feature, string intent, JObject rule, string projectRoot)
    {
        JArray paths = rule["paths"] as JArray;
        if (paths == null)
            return Fail(id, feature, intent, "paths 없음");

        for (int i = 0; i < paths.Count; i++)
        {
            string path = paths[i].ToString();
            string full = Path.Combine(projectRoot, path);
            if (!File.Exists(full))
                return Fail(id, feature, intent, "파일 없음: " + path);
        }

        return Pass(id, feature, intent, paths.Count + "개 파일 존재");
    }

    private static CheckResult CheckFileContains(string id, string feature, string intent, JObject rule, string projectRoot)
    {
        string path = rule.Value<string>("path");
        if (string.IsNullOrEmpty(path))
            return Fail(id, feature, intent, "path 없음");

        string full = Path.Combine(projectRoot, path);
        if (!File.Exists(full))
            return Fail(id, feature, intent, "파일 없음: " + path);

        string text = File.ReadAllText(full, Encoding.UTF8);
        JArray must = rule["must_contain"] as JArray;
        if (must != null)
        {
            for (int i = 0; i < must.Count; i++)
            {
                string needle = must[i].ToString();
                if (text.IndexOf(needle, StringComparison.Ordinal) < 0)
                    return Fail(id, feature, intent, "미포함: " + needle);
            }
        }

        JArray mustNot = rule["must_not_contain"] as JArray;
        if (mustNot != null)
        {
            for (int i = 0; i < mustNot.Count; i++)
            {
                string needle = mustNot[i].ToString();
                if (text.IndexOf(needle, StringComparison.Ordinal) >= 0)
                    return Fail(id, feature, intent, "금지 문자열 발견: " + needle);
            }
        }

        string pattern = rule.Value<string>("pattern");
        if (!string.IsNullOrEmpty(pattern) && rule["max_count"] != null)
        {
            int maxCount = rule.Value<int>("max_count");
            int count = 0;
            int idx = 0;
            while (true)
            {
                idx = text.IndexOf(pattern, idx, StringComparison.Ordinal);
                if (idx < 0)
                    break;
                count++;
                idx += pattern.Length;
            }
            if (count > maxCount)
                return Fail(id, feature, intent, pattern + " 출현 " + count + "회 (허용 " + maxCount + " — 호출 재도입 의심)");
        }

        return Pass(id, feature, intent, path + " 패턴 OK");
    }

    private static CheckResult CheckForbiddenInScripts(string id, string feature, string intent, JObject rule, string projectRoot)
    {
        string pattern = rule.Value<string>("pattern");
        string searchRoot = rule.Value<string>("search_root") ?? "Assets/Scripts";
        if (string.IsNullOrEmpty(pattern))
            return Fail(id, feature, intent, "pattern 없음");

        string dir = Path.Combine(projectRoot, searchRoot);
        if (!Directory.Exists(dir))
            return Fail(id, feature, intent, "폴더 없음: " + searchRoot);

        int minIndent = rule.Value<int?>("min_indent") ?? 0;

        string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string text = File.ReadAllText(files[i], Encoding.UTF8);
            string rel = files[i].Replace(projectRoot + Path.DirectorySeparatorChar, "").Replace("\\", "/");
            string hit = FindForbiddenPatternLine(text, pattern, minIndent);
            if (hit != null)
                return Fail(id, feature, intent, "발견: " + pattern + " @ " + rel + " — " + hit);
        }

        return Pass(id, feature, intent, "Scripts 내 " + pattern + " 없음");
    }

    private static string FindForbiddenPatternLine(string text, string pattern, int minIndent)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.IndexOf(pattern, StringComparison.Ordinal) < 0)
                continue;

            if (minIndent > 0)
            {
                int indent = line.Length - line.TrimStart().Length;
                if (indent >= minIndent)
                    continue;
            }

            return "줄 " + (i + 1) + ": " + line.Trim();
        }

        return null;
    }

    private static bool IsSceneLoaded(string scenePath)
    {
        Scene s = EditorSceneManager.GetSceneByPath(scenePath);
        return s.IsValid() && s.isLoaded;
    }

    private static CheckResult CheckSceneRefs(string id, string feature, string intent, JObject rule)
    {
        string scenePath = rule.Value<string>("scene");
        if (!IsSceneLoaded(scenePath))
            return Skip(id, feature, intent, scenePath + " 미로드 — GameScene 연 후 재실행");

        JArray checks = rule["checks"] as JArray;
        if (checks == null)
            return Fail(id, feature, intent, "checks 없음");

        for (int i = 0; i < checks.Count; i++)
        {
            JObject c = checks[i] as JObject;
            if (c == null)
                continue;

            string typeName = c.Value<string>("type");
            if (typeName == "Player")
            {
                Player p = UnityEngine.Object.FindAnyObjectByType<Player>();
                if (p == null)
                    return Fail(id, feature, intent, "Player 없음");
                if (p.GetComponent<Rigidbody2D>() == null)
                    return Fail(id, feature, intent, "Player Rigidbody2D 없음");
                continue;
            }

            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            MonoBehaviour target = null;
            for (int j = 0; j < all.Length; j++)
            {
                if (all[j] != null && all[j].GetType().Name == typeName)
                {
                    target = all[j];
                    break;
                }
            }

            if (target == null)
                return Fail(id, feature, intent, typeName + " 없음");

            JArray fields = c["fields"] as JArray;
            if (fields == null)
                continue;

            SerializedObject so = new SerializedObject(target);
            for (int f = 0; f < fields.Count; f++)
            {
                string fieldName = fields[f].ToString();
                SerializedProperty prop = so.FindProperty(fieldName);
                if (prop == null)
                    return Fail(id, feature, intent, typeName + "." + fieldName + " 프로퍼티 없음");
                if (prop.propertyType == SerializedPropertyType.ObjectReference && prop.objectReferenceValue == null)
                    return Fail(id, feature, intent, typeName + "." + fieldName + " null");
            }
        }

        return Pass(id, feature, intent, "GameScene 참조 OK");
    }

    private static CheckResult CheckSceneValues(string id, string feature, string intent, JObject rule)
    {
        string scenePath = rule.Value<string>("scene");
        if (!IsSceneLoaded(scenePath))
            return Skip(id, feature, intent, scenePath + " 미로드 — GameScene 연 후 재실행");

        JArray values = rule["values"] as JArray;
        if (values == null)
            return Fail(id, feature, intent, "values 없음");

        for (int i = 0; i < values.Count; i++)
        {
            JObject v = values[i] as JObject;
            if (v == null)
                continue;

            string typeName = v.Value<string>("type");
            string fieldName = v.Value<string>("field");
            float expected = v.Value<float>("expected");

            MonoBehaviour[] all = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            MonoBehaviour target = null;
            for (int j = 0; j < all.Length; j++)
            {
                if (all[j] != null && all[j].GetType().Name == typeName)
                {
                    target = all[j];
                    break;
                }
            }

            if (target == null)
                return Fail(id, feature, intent, typeName + " 없음");

            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
                return Fail(id, feature, intent, fieldName + " 없음");

            float actual = prop.floatValue;
            if (Mathf.Abs(actual - expected) > 0.001f)
                return Fail(id, feature, intent, typeName + "." + fieldName + $" = {actual} (기대 {expected})");
        }

        return Pass(id, feature, intent, "씬 기대값 일치");
    }

    private static void WriteReportHtml(List<CheckResult> results)
    {
        int pass = 0;
        int fail = 0;
        int skip = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].status == "Pass") pass++;
            else if (results[i].status == "Skip") skip++;
            else fail++;
        }

        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html lang=\"ko\"><head><meta charset=\"UTF-8\"/>");
        sb.AppendLine("<title>Static Analysis Report — MidProject</title>");
        sb.AppendLine("<style>body{font-family:'Malgun Gothic',sans-serif;max-width:1050px;margin:1rem auto;padding:0 1rem;font-size:0.88rem;}");
        sb.AppendLine("table{width:100%;border-collapse:collapse;}th,td{border:1px solid #dde3ea;padding:0.45rem;vertical-align:top;}");
        sb.AppendLine("th{background:#e8f4f1;}.Pass{color:#1a7f4e;font-weight:600;}.Fail{color:#b33;font-weight:600;}.Skip{color:#886500;}");
        sb.AppendLine(".meta{color:#5a6578;}</style></head><body>");
        sb.AppendLine("<h1>정적 분석 리포트 — MidProject</h1>");
        sb.AppendLine($"<p class=\"meta\">생성: {time} · Pass {pass} / Fail {fail} / Skip {skip} · 규칙: {RulesPath}</p>");
        sb.AppendLine("<table><thead><tr><th>ID</th><th>기능</th><th>의도</th><th>결과</th><th>메시지</th></tr></thead><tbody>");

        for (int i = 0; i < results.Count; i++)
        {
            CheckResult r = results[i];
            sb.AppendLine($"<tr><td>{r.id}</td><td>{Escape(r.feature)}</td><td>{Escape(r.intent)}</td>");
            sb.AppendLine($"<td class=\"{r.status}\">{r.status}</td><td>{Escape(r.message)}</td></tr>");
        }

        sb.AppendLine("</tbody></table>");
        sb.AppendLine($"<p class=\"meta\">스펙: <a href=\"project-static-analysis.html\">project-static-analysis.html</a></p>");
        sb.AppendLine("</body></html>");

        File.WriteAllText(ReportHtmlPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
    }

    private static string Escape(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";
        return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }

    private static CheckResult Pass(string id, string feature, string intent, string msg)
    {
        return new CheckResult { id = id, feature = feature, intent = intent, status = "Pass", message = msg };
    }

    private static CheckResult Fail(string id, string feature, string intent, string msg)
    {
        return new CheckResult { id = id, feature = feature, intent = intent, status = "Fail", message = msg };
    }

    private static CheckResult Skip(string id, string feature, string intent, string msg)
    {
        return new CheckResult { id = id, feature = feature, intent = intent, status = "Skip", message = msg };
    }

    private struct CheckResult
    {
        public string id;
        public string feature;
        public string intent;
        public string status;
        public string message;
    }
}
