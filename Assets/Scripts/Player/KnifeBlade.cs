using System;
using HCStore.World;
using UnityEngine;

namespace HCStore.Player
{
    public class KnifeBlade : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("SliceObject"))
            {
                var sliceObject = other.GetComponentInParent<SliceObject>();
                sliceObject.Slice();
            }
        }
    }
}