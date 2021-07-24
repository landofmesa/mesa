using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public ProjectileData data;
    public float delta;
    public Rigidbody2D rb;
    public Vector2 iniPos;
    

    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        iniPos = transform.position;
        
    }
    

    public void Shoot(Vector3 target)
    {
        Vector2 moveDirection = (target - transform.position).normalized*data.speed;
        rb.velocity = new Vector2(moveDirection.x, moveDirection.y);


        


    }
    public void Update()
    {
        if (Vector2.Distance(transform.position, iniPos) > data.maxDistance)
        {
            Destroy(gameObject);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
    }
}