using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Mouse_Handler : MonoBehaviour
{
    public PlayerInput input;
    public GameObject[] temp;

    private PlayerControls controls;

    private void Start()
    {
        controls = GetComponent<Player_Movement>().GetControls();
        input.onActionTriggered += OnClick;
    }

    private void OnClick(InputAction.CallbackContext context)
    {
        if (context.action.name.Equals("Left Click") && context.performed) 
        {
            var pos = Mouse.current.position.ReadValue();
            var worldPos = Camera.main.ScreenToWorldPoint(pos);

            Collider2D detectedCollider = Physics2D.OverlapPoint(worldPos, 1<<6);

            if (detectedCollider != null)
            {
                //Debug.Log(detectedCollider.name);

                switch (detectedCollider.tag)
                {
                    case "NPC":
                        var handler = detectedCollider.GetComponent<Chatbox_Handler>();
                        if(!handler.playing)
                            handler.StartChat();
                        break;
                    case "Chatbox":
                        if (detectedCollider.GetComponentInParent<Chatbox_Handler>().canContinue)
                            detectedCollider.transform.GetComponentInParent<Chatbox_Handler>().ContinueChat();
                        break;
                    case "Enemy":
                        var enemy = detectedCollider.GetComponent<EnemyInfo>();
                        enemy.StartBattle(temp);
                        break;
                }


            }

        }
    }

}
