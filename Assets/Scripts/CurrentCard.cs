using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class CurrentCard : MonoBehaviour
{
    public CardInfo cardData;
    public Sprite portrait;
    void Update()
    {
        Vector2 mousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePos);
        Vector3 fixedPosition = new Vector3(worldPosition.x, worldPosition.y, 0);
        GetComponent<RectTransform>().position = fixedPosition; 
    }

    public void SelectCard(PlayerManager player, int index)
    {   
        if(player.hasAuthority)
        {
            if(portrait != null)
            {
                if(player.hand[index] == null)
                    player.CmdAddCard(cardData, index);
            }
            else
            {
                cardData = player.playerField.transform.GetChild(5).GetChild(index).GetComponentInChildren<HandCard>().cardData;
                portrait = player.playerField.transform.GetChild(5).GetChild(index).GetComponentInChildren<HandCard>().cardData.image;
                GetComponent<Image>().enabled = true;
                GetComponent<Image>().sprite = portrait;
                NetworkServer.Destroy(player.playerField.transform.GetChild(5).GetChild(index).GetChild(0).gameObject);
                player.hand[index] = null;      
            }  
        }
    }
}
