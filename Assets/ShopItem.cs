using UnityEngine;

[System.Serializable]
public class ShopItem
{
    public string itemName = "Item";
    [TextArea] public string description = "Description...";
    public int price = 10;
    public Sprite icon;

    public enum EffectType { IncreaseMaxHP, SpeedBoost, SetFlag }
    public EffectType effect = EffectType.SetFlag;

    [Header("Параметры")]
    public int amount = 20;
    public string flagName = "";
}