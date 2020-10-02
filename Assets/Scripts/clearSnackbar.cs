using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class clearSnackbar : MonoBehaviour
{
    private static float timerDuration = 3;
    private float timeRemaining = timerDuration;
    private bool timerRunning = false;
    private Text textField;
    private Image parentImage;
    private Animator animator;
    private Color32 green;
    private Color32 red;
    // Start is called before the first frame update
    void Start()
    {
        textField = this.GetComponent<Text>();
        parentImage = this.transform.parent.gameObject.GetComponent<Image>();
        animator = this.transform.parent.gameObject.GetComponent<Animator>();
        green = new Color32(9, 196, 31, 255);
        red = new Color32(126, 1, 1, 255);
    }

    // Update is called once per frame
    void Update()
    {
        if (timerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;
            }
            else
            {
                //time ran out
                textField.text = "";
                setTransparent();
                timeRemaining = timerDuration;
                timerRunning = false;
            }
        }
        
    }

    public void setText(string text, bool error=true)
    {
        textField.text = text;
        timeRemaining = timerDuration;
        timerRunning = true;
        if (error)
        {
            parentImage.color = red;
        } else
        {
            parentImage.color = green;
        }
        animator.Play("WiggleSnackbar");

    }

    private void setTransparent(bool transparency = true)
    {
        if (transparency)
        {
            var tempColor = parentImage.color;
            tempColor.a = 0f;
            parentImage.color = tempColor;
        }
        else
        {
            var tempColor = parentImage.color;
            tempColor.a = 1f;
            parentImage.color = tempColor;
        }
    }
}
