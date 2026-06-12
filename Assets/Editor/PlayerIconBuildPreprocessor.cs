#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class PlayerIconBuildPreprocessor : IPreprocessBuildWithReport
{
    const string IconPath = "Assets/UI/Icon/HazmatRush_Ico.png";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.StandaloneWindows64
            && report.summary.platform != BuildTarget.StandaloneWindows)
            return;

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        if (icon == null)
        {
            Debug.LogWarning($"[PlayerIcon] 아이콘 없음: {IconPath}");
            return;
        }

        NamedBuildTarget target = NamedBuildTarget.Standalone;
        int[] sizes = PlayerSettings.GetIconSizes(target, IconKind.Application);
        if (sizes == null || sizes.Length == 0)
            return;

        Texture2D[] icons = new Texture2D[sizes.Length];
        for (int i = 0; i < icons.Length; i++)
            icons[i] = icon;

        PlayerSettings.SetIcons(target, icons, IconKind.Application);
        Debug.Log("[PlayerIcon] Windows 빌드 아이콘 적용");
    }
}
#endif
