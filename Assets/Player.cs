using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{

    private Rigidbody2D rb;

    [SerializeField] private KeyCode up = KeyCode.W;
    [SerializeField] private KeyCode down = KeyCode.S;
    [SerializeField] private KeyCode right = KeyCode.D;
    [SerializeField] private KeyCode left = KeyCode.A;
    [SerializeField] private KeyCode jump = KeyCode.Space;

    [SerializeField] private int speed = 1;
    [SerializeField] private int jumpforce = 3;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(up))
        {

        }
        if (Input.GetKey(down))
        {

        }
        if (Input.GetKey(right))
        {
            rb.AddForce(Vector3.right * speed / 10, ForceMode2D.Impulse);
        }
        if (Input.GetKey(left))
        {
            rb.AddForce(Vector3.left * speed / 10, ForceMode2D.Impulse);
        }
        if (Input.GetKeyDown(jump))
        {
            rb.AddForce(Vector3.up * jumpforce * 10, ForceMode2D.Impulse);
        }
    }
}
