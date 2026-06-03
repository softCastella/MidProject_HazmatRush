using UnityEngine;
using UnityEngine.UI;

public class TapeScroll : MonoBehaviour
{
    [SerializeField] private RawImage rawImage;
    [Tooltip("1=테입1, 2=테입2. 각 오브젝트에 맞게 설정")]
    [SerializeField] private int tapeIndex = 1;
    [SerializeField] private float tape1Speed = 0.15f;
    [SerializeField] private float tape2Speed = 0.35f;

    void Awake()
    {
        if (rawImage == null)
            rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (rawImage == null)
            return;

        float speed = tapeIndex == 2 ? tape2Speed : tape1Speed;

        Rect uv = rawImage.uvRect;
        if (tapeIndex == 1)
            uv.x += speed * Time.deltaTime;
        else  
            uv.x -= speed * Time.deltaTime;
        rawImage.uvRect = uv;
    }
}
