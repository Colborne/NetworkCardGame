using PurrNet;
using UnityEngine;

public class BaseCard : NetworkBehaviour
{
    private readonly SyncVar<CardInfo> syncedCardData = new();
    private readonly SyncVar<int> syncedFrozenTimer = new();

    public CardInfo cardData
    {
        get => syncedCardData.value;
        set
        {
            if (!isSpawned || isServer)
                syncedCardData.value = value;
            else
                ServerSetCardData(value);
        }
    }

    public int frozenTimer
    {
        get => syncedFrozenTimer.value;
        set
        {
            if (!isSpawned || isServer)
                syncedFrozenTimer.value = value;
            else
                ServerSetFrozenTimer(value);
        }
    }

    public string title;
    public int spr;
    public Sprite portrait;
    public bool selected;
    public int cardPosition;
    public Sprite CardBack;

    protected virtual void Awake()
    {
        syncedCardData.onChanged += _ => OnCardDataChanged();
    }

    public virtual void Update()
    {
        GetComponent<RectTransform>().localScale = Vector3.one;
    }

    protected virtual void OnCardDataChanged()
    {
        title = string.Empty;
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetCardData(CardInfo value)
    {
        syncedCardData.value = value;
    }

    [ServerRpc(requireOwnership: false)]
    private void ServerSetFrozenTimer(int value)
    {
        syncedFrozenTimer.value = Mathf.Max(0, value);
    }
}
