using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class drawCard : MonoBehaviour
{
    private Camera firstPersonCamera;

    private GameController gameController;
    public GameObject cardPrefab;

    
    // Start is called before the first frame update
    void Start()
    {
        firstPersonCamera = Camera.main;
        gameController = GameObject.FindObjectOfType<GameController>();
        Vector3 offset;
        GameObject card;
        Vector3 scale = Vector3.one;
        scale.z = 0.3f;
        Quaternion rotation = Quaternion.identity;
        
        //////////////////////////////////////////////////////////////////////
        int numOfCards = GameObject.FindObjectOfType<GameController>().numOfCardsLeftInDeck;
        for (int i=0; i<numOfCards; i++)
        {
            card = Instantiate(cardPrefab, this.transform, false);
            offset = Vector3.zero;
            offset.z = -i * 0.1f;
            offset.x = Random.Range(-1, 1);
            offset.y = Random.Range(-0.5f, 0.5f);
            card.transform.localPosition = offset;
            card.transform.localScale = scale;
            offset = Vector3.zero;
            offset.z = Random.Range(-2, 2);
            rotation.eulerAngles = offset;
            card.transform.localRotation = rotation;
            if (1!=numOfCards-1)  card.GetComponent<BoxCollider>().enabled = false;
            card.tag = "Deck";
            card.layer = 0;
        }
        //////////////////////////////////////////////////////////////////////
    }

    // Update is called once per frame
    void Update()
    {
        // If the player has not touched the screen, we are done with this update.
        Touch touch;
        if (Input.touchCount < 1 || (touch = Input.GetTouch(0)).phase != TouchPhase.Began)
        {
            return;
        }

        // Should not handle input if the player is pointing on UI.
        //if (IsPointerOverUIObject(touch))
        //{
        //    return;
       // }

        RaycastHit hitobject;
        Ray ray = firstPersonCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out hitobject))
            {
                // Check if what is hit is the desired object
                if (hitobject.transform.tag == "Deck")
                {
                gameController.drawCard();
                }
            }
    }

    private bool IsPointerOverUIObject(Touch t)
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(t.position.x, t.position.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    public void removeCardFromDeck(int num = 1)
    {
        if (num > 0 && num<=this.transform.childCount)
        {
            if (this.transform.childCount>=num+1) this.transform.GetChild(this.transform.childCount - num - 1).transform.GetComponent<BoxCollider>().enabled = true;
            for (int i=0; i<num; i++)
            {
                Destroy(this.transform.GetChild(this.transform.childCount - i - 1).gameObject);
            }

        }
       
    }

    


}
