using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

public class Gamemanager_World : MonoBehaviour
{
    public GameObject itemPrefab;
    public Dictionary<int, GameObject> ItemIcons = new();
    public Sprite[] allItemSprites;

    public DisplayStatTop[] displays;
    public GameObject InventoryPanel;
    public Button endOfTurn;
    public GameObject bar;
    public Image[] icons;
    private double[] topSizes;

    [Range(0.4f, 1.3f)]
    public float scale = 0.8f;
    public string Mode = "None";

    private int currentI = 0, currentJ = 0;

    private void Start()
    {
        topSizes = new double[icons.Length];
        for (int i = 0; i < icons.Length; i++) topSizes[i] = icons[i].rectTransform.sizeDelta.y;


        if (bar.gameObject.activeSelf) UpdateIconsText();

        // Create empty spots based on physical strength
        ExpandInventoryDisplay();

        StaticHolder.InventoryManagement.AddItem("Bambo Sword", "Weapon", GetNextAvailableSlot());
    }

    public void SpawnItem(Item item)
    {
        var freeSlot = GetNextAvailableSlot();
        var slot = ItemIcons[freeSlot].GetComponent<SlotContainer>();

        slot.CurrentItem = item;
        slot.SlotIndex = freeSlot;
        slot.transform.GetChild(1).GetComponent<Image>().sprite = FindItemPicture(item.Name);
        slot.transform.GetChild(1).gameObject.SetActive(true);
        slot.name = item.Name;
    }

    public void UpdateIconsText()
    {
        var stats = StaticHolder.PlayerStats;
        displays[0].UpdateText(stats.CurrentHealth + " / " + stats.GetStatValue("Health") + " HP"); // Health
        displays[1].UpdateText(stats.CurrentMana + " / " + stats.GetStatValue("Mana") + " MP"); // Mana
        displays[2].UpdateText(stats.CurrentStamina + " / " + stats.GetStatValue("Stamina") + " SP"); // Stamina
    }

    public void EndTurn()
    {
        var battle = GetComponent<BattleSimulator>();

        // Button should not be active but just in case
        if (!battle.IsPlayerTurn()) return;

        endOfTurn.interactable = false;
        battle.NextTurn();
    }

    public void TurnButtonOn()
    {
        endOfTurn.interactable = true;
        StaticHolder.PlayerStats.StartOfTurn();

        var playerStats = StaticHolder.PlayerStats;

        displays[1].UpdateText(playerStats.CurrentMana + " / " + playerStats.GetStatValue("Mana") + " MP");
        displays[2].UpdateText(playerStats.CurrentStamina + " / " + playerStats.GetStatValue("Stamina") + " SP");
    }

    public void UpdateIcon()
    {
        int i = 0;
        foreach (var icon in icons)
        {
            double current = 0, max = 0;
            var goodIcon = icons[i];
            var topSize = topSizes[i];
            var stats = StaticHolder.PlayerStats;

            switch (i)
            {
                case 0:
                    current = stats.CurrentHealth;
                    max = stats.GetStatValue("Health");
                    break;
                case 1:
                    current = stats.CurrentMana;
                    max = stats.GetStatValue("Mana");
                    break;
                case 2:
                    current = stats.CurrentStamina;
                    max = stats.GetStatValue("Stamina");
                    break;
                default: throw new System.Exception("Exceeded the number icons -- internal error!");
            }

            if (current <= 0) goodIcon.rectTransform.sizeDelta = new Vector2(goodIcon.rectTransform.sizeDelta.x, 0);
            if (current > max) goodIcon.rectTransform.sizeDelta = new Vector2(goodIcon.rectTransform.sizeDelta.x, (float)topSize);

            var slope = topSize / max;
            goodIcon.rectTransform.sizeDelta = new Vector2(goodIcon.rectTransform.sizeDelta.x, Mathf.Clamp((float)(slope * current), 0, (float)topSize));

            i++;
        }

    }

    private int GetNextAvailableSlot()
    {
        foreach (var icon in ItemIcons)
        {
            var slot = icon.Value.GetComponent<SlotContainer>();
            if (slot.CurrentItem != null) continue;

            
            return icon.Key;
        }

        return -1;
    }

    private Sprite FindItemPicture(string name)
    {
        foreach (var sprite in allItemSprites)
        {
            //print(sprite.name);
            if (sprite.name == name) return sprite;
        }

        throw new System.Exception("Unable to location the item picture for " + name);
    }

    private void ExpandInventoryDisplay()
    {
        var iv = StaticHolder.InventoryManagement;
        var newSlots = iv.ExpandSlots();

        // Spawn new slots
        for (int i = 0; i < newSlots;  i++)
        {
            var l = Instantiate(itemPrefab, Vector2.zero, Quaternion.identity);
            l.name = "Empty Slot";
            l.transform.SetParent(InventoryPanel.transform);
            l.GetComponent<RectTransform>().anchoredPosition = StaticHolder.InventoryManagement.FindSpawningPosition(currentI, currentJ);
            ItemIcons.Add(i, l);

            var nextSpot = (currentI + 1) % 11;
            currentI = nextSpot;

            if (nextSpot == 0) currentJ++;
        }
    }
}
