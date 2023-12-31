using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player_Movement : MonoBehaviour
{
    public string playerName;

    public float speed = 10;
    public GameObject walls;

    private PlayerControls controls;
    private InputAction move;
    private Vector2 moveDirection;

    private Rigidbody2D rb;
    private float width, height;
    private bool canMove; 

    public PlayerControls GetControls() { return controls; }

    public void EnableMovement(bool enable = true) { canMove = enable; }

    private void Awake()
    {
        controls = new PlayerControls();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        width = transform.localScale.x / 2;
        height = transform.localScale.y / 2;

        if (!GameObject.FindGameObjectWithTag("GameManager").GetComponent<Gamemanager_World>().IsTutorialOn())
            canMove = true;
    }

    private void OnEnable()
    {
        move = controls.Player.Move;
        move.Enable();
    }

    private void OnDisable()
    {
        move.Disable();
    }

    private void Update()
    {
        if(canMove)
            moveDirection = move.ReadValue<Vector2>();

        ClampPosition();
    }

    private void ClampPosition()
    {
        var pos = transform.position;

        pos.x = Mathf.Clamp(transform.position.x, walls.transform.GetChild(2).position.x + width, walls.transform.GetChild(3).position.x - width);
        pos.y = Mathf.Clamp(transform.position.y, walls.transform.GetChild(0).position.y + height, walls.transform.GetChild(1).position.y - height);

        transform.position = pos;
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveDirection.x * speed, moveDirection.y * speed);
    }
}
