using System;
using UnityEngine;

namespace HCStore.Player
{
    public class KnifeController : MonoBehaviour
    {
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform forcePoint;
        [SerializeField] private Vector2 jumpForce = new Vector2(5,10);

        private void Awake()
        {
            rb.centerOfMass = Vector3.zero;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rb.worldCenterOfMass,.2f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                //rb.AddForceAtPosition(jumpForce,transform.position + Vector3.left + Vector3.down,ForceMode.Impulse);
                rb.linearVelocity = jumpForce;
                rb.AddTorque(Vector3.forward * -100);
                

            }
        }
    }
}
