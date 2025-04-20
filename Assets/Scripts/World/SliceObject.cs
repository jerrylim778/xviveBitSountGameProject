using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HCStore.World
{
    public class SliceObject : MonoBehaviour
    {
        [SerializeField] private int insideMaterialIndex = 0;
        [SerializeField] private Material[] insideMaterials;
        [SerializeField] private MeshRenderer[] partsMeshRenderers;
        [SerializeField] private Rigidbody[] parts;

        private void Start()
        {
            var selectedInsideMaterial = insideMaterials[Random.Range(0, insideMaterials.Length)]; 
            foreach (var meshRenderer in partsMeshRenderers)
            {
                var sharedMaterials = meshRenderer.sharedMaterials;
                sharedMaterials[insideMaterialIndex] = selectedInsideMaterial;
                meshRenderer.sharedMaterials = sharedMaterials;
            }
        }

        public void Slice()
        {
            var centerPoint = Vector3.zero;
            foreach (var part in parts)
            {
                centerPoint += part.position;
            }
            centerPoint /= parts.Length;
            
            Debug.DrawRay(centerPoint,Vector3.up,Color.red,4);
            
            foreach (var part in parts)
            {
                part.isKinematic = false;
                part.AddExplosionForce(100,centerPoint,5,1,ForceMode.Acceleration);
            }
        }
    }
}