using TMPro;
using UnityEngine;

public class VersionText : MonoBehaviour
{
    public TMP_Text versionText;

    void Awake()
    {
        if (versionText == null)
            versionText = GetComponent<TMP_Text>();
        if (versionText == null)
            return;

        versionText.text = "v" + Application.version;
    }
}
