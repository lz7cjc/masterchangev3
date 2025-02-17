using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateCameraold4huds : MonoBehaviour
{
    public Rigidbody player;
    public float rotationSpeed = 100f;
    Vector3 m_EulerAngleVelocity;
    public bool loop = false;
    public bool mouseHover1 = false;
    public float counter1 = 0;
    public float Delay;
    public float DelayStop;
    private float x;


    private hudCountdown hudCountdown;
    
    public Sprite spriteLeft;
    public Sprite spriteHoverLeft;
    public Sprite spriteSelectedLeft;
    public Sprite spriteRight;
    public Sprite spriteSelectedRight;
    public Sprite spriteHoverRight;

    public SpriteRenderer leftspriterenderer;
    public SpriteRenderer leftspriterenderer1;
    public SpriteRenderer leftspriterenderer2;
    public SpriteRenderer leftspriterenderer3;

    public SpriteRenderer rightspriterenderer;
    public SpriteRenderer rightspriterenderer1;
    public SpriteRenderer rightspriterenderer2;
    public SpriteRenderer rightspriterenderer3;

    // Start is called before the first frame update
    void FixedUpdate()
    {
        if (mouseHover1)
        {
            counter1 += Time.deltaTime;
            hudCountdown = hudCountdown.FindFirstObjectByType<hudCountdown>();
            hudCountdown.SetCountdown(Delay, counter1);

            // debug.log("counter value = " + counter1);
            //waiting to hit threshold to trigger walking
            if (counter1 < Delay)
            {
                // debug.log("h1h1 counting");
                counter1 += Time.deltaTime;
                // Get the input axis for horizontal movement (e.g., A/D or left arrow/right arrow)
                if (x <0)
                { 
                leftspriterenderer.sprite = spriteHoverLeft;
                leftspriterenderer1.sprite = spriteHoverLeft;
                leftspriterenderer2.sprite = spriteHoverLeft;
                leftspriterenderer3.sprite = spriteHoverLeft;
                }
                else if (x > 0)
                { 
                rightspriterenderer.sprite = spriteHoverRight;
                rightspriterenderer1.sprite = spriteHoverRight;
                rightspriterenderer2.sprite = spriteHoverRight;
                rightspriterenderer3.sprite = spriteHoverRight;
                }
            }
            else if (counter1 >= Delay)
            {
                m_EulerAngleVelocity = new Vector3(0, x, 0);
                Quaternion deltaRotation = Quaternion.Euler(m_EulerAngleVelocity * Time.fixedDeltaTime);
                player.MoveRotation(player.rotation * deltaRotation);
                if (x < 0)
                {
                    leftspriterenderer.sprite = spriteSelectedLeft;
                    leftspriterenderer1.sprite = spriteSelectedLeft;
                    leftspriterenderer2.sprite = spriteSelectedLeft;
                    leftspriterenderer3.sprite = spriteSelectedLeft;
                }
                else if (x > 0)
                {
                    rightspriterenderer.sprite = spriteSelectedRight;
                    rightspriterenderer1.sprite = spriteSelectedRight;
                    rightspriterenderer2.sprite = spriteSelectedRight;
                    rightspriterenderer3.sprite = spriteSelectedRight;
                }
            }

        }





    }

 
    public void OnMouseHoverEnter(float moveby)
    {
        
        mouseHover1 = true;
        // debug.log("hhh0 mouseover on");
        x = moveby;
    }

    public void OnMouseHoverExit()
    {
        // debug.log("hhh4 mouseover off");
        mouseHover1 = false;
        hudCountdown.resetCountdown();
        counter1 = 0;
        rightspriterenderer.sprite = spriteRight;
        rightspriterenderer1.sprite = spriteRight;
        rightspriterenderer2.sprite = spriteRight;
        rightspriterenderer3.sprite = spriteRight;

        leftspriterenderer.sprite = spriteLeft;
        leftspriterenderer1.sprite = spriteLeft;
        leftspriterenderer2.sprite = spriteLeft;
        leftspriterenderer3.sprite = spriteLeft;
    }
   
    
}