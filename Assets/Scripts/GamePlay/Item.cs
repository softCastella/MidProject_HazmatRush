using UnityEngine;

public class Item : MonoBehaviour
{
    //아이템 타입 열거형
    public enum ItemType
    {
        Scanner,
        Neutralizer,
        GeneralPad,
        OilPad
    }

    //아이템 디폴트타입은 스캐너이다.
    public ItemType type = ItemType.Scanner;
    //아이템 초당 데미지 변수
    public int itemDps;

    void Awake()
    {
        SetDpsByType();
    }

    void OnValidate()
    {
        SetDpsByType();
    }

    public float GetDps()
    {
        return type switch
        {
            ItemType.Neutralizer => 10f,
            ItemType.GeneralPad => 18f,
            ItemType.OilPad => 20f,
            _ => 0f
        };
    }

    //아이템별 데미지 설정
    void SetDpsByType()
    {
        itemDps = Mathf.RoundToInt(GetDps());
    }
}
