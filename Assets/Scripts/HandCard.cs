using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class HandCard : BaseCard
{
    private readonly SyncVar<bool> syncedSeen = new();

    public bool seen => syncedSeen.value;

    internal void SetSeenAuthoritative(bool value)
    {
        if (!isSpawned || isServer)
            syncedSeen.value = value;
    }

    public override void Update()
    {
        if (string.IsNullOrEmpty(title) || title != cardData.title)
        {
            title = cardData.title;
            spr = cardData.spr;
            portrait = cardData.image;
            GetComponent<Image>().sprite = portrait;
        }
    }
}
