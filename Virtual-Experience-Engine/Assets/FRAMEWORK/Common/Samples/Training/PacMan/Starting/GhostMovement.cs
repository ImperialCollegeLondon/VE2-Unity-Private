using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public bool moving = false;
    public float speed = .55f;
    public float animSpeed = 6f;

    private Vector3 startPos;

    private const float MOVE_SPEED = 2f;

    private int mode = 0;  //0=normal, 1=PP, 2=dead
    Color originalColor;
    private void Start()
    {
        originalColor = hideablePartsWhenEaten[0].GetComponent<Renderer>().material.color;
        startPos = transform.position;
    }

    //TODO - should trigger when the ghost and player rig collide. Maybe use a V_PressurePlateActivatable?
    public void HandlePlayerCollision()
    {
        if (mode == 0)
        {
            GameHandler.instance.LoseLife();
        }
        else if (mode == 1)
        {
            Scorer.instance.AddScore(1000);
            Eaten();
            GetComponent<AudioSource>().Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        //TODO: Tie these into VE2 interactables 
        float amountX = 0; 
        float amountZ = 0;

        Vector3 xVector = Vector3.right * amountX * Time.deltaTime * MOVE_SPEED;
        Vector3 zVector = Vector3.forward * amountZ * Time.deltaTime * MOVE_SPEED;

        if (CheckMove(zVector))
            transform.position += zVector;

        if (CheckMove(xVector))
            transform.position += xVector;

        float animPhase = Mathf.Sin((transform.position.x + transform.position.z) * animSpeed);
        Animate(animPhase);

        DoColour();
    }

    private void DoColour()
    {
        if (mode == 1)
        {
            float animPhase = Mathf.Sin((transform.position.x + transform.position.z) * 10);
            animPhase += 1f;
            animPhase /= 2f;
            foreach (GameObject g in hideablePartsWhenEaten)
            {
                g.GetComponent<Renderer>().material.color = Color.Lerp(Color.white, Color.cyan, animPhase); ;
            }
        }
            
    }

    public GameObject eye1, eye2;
    public GameObject skirt1, skirt2, skirt3, skirt4;

    private void Animate(float animPhase)
    {
        eye1.transform.localEulerAngles = new Vector3(0f, animPhase * 30f +15f, 0f); 
        eye2.transform.localEulerAngles = new Vector3(0f, animPhase * 30f + 15f, 0f);
        skirt1.transform.localScale = new Vector3(0.4f, 0.4f, animPhase * .2f + .6f);
        skirt3.transform.localScale = new Vector3(0.4f, 0.4f, animPhase * .2f + .6f);
        skirt2.transform.localScale = new Vector3(0.4f, 0.4f, -animPhase * .2f + .6f);
        skirt4.transform.localScale = new Vector3(0.4f, 0.4f, -animPhase * .2f + .6f);
    }

    private bool CheckMove(Vector3 direction)
    {
        //ghosts cant teleport
        if ((direction + transform.position).x < -18) return false;
        if ((direction + transform.position).x > 26) return false;

        int layerMask = LayerMask.GetMask("V_Layer5");
        if (Physics.SphereCast(transform.position, 0.7f, direction, out RaycastHit hitInfo, direction.magnitude, layerMask))
        {
            return false;
        }
        else return true;
    }

    public void SetModeToPowerPill()
    {
        if (mode == 0) mode = 1;
        DOVirtual.DelayedCall(8f, () => PowerPillExpires());
    }

    private void PowerPillExpires()
    {
        if (mode == 1) mode = 0;
        OriginalColours();
    }

    private void OriginalColours()
    {
        foreach (GameObject g in hideablePartsWhenEaten)
        {
            g.GetComponent<Renderer>().material.color = originalColor;
        }
    }

    public GameObject[] hideablePartsWhenEaten;
    public void Eaten()
    {
        mode = 2;
        foreach (GameObject g in hideablePartsWhenEaten) g.SetActive(false);
        transform.DOMove(startPos, 6f).OnComplete(() => Rejuvenate());
    }

    public void Rejuvenate()
    {
        mode = 0;
        OriginalColours();
        foreach (GameObject g in hideablePartsWhenEaten) g.SetActive(true);
    }
}
