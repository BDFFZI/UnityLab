using System.Collections;
using System.Linq;
using UnityEngine;

public class HolographicBuildTest : MonoBehaviour
{
    static readonly int DissolvePosition = Shader.PropertyToID("_DissolvePosition");
    static readonly int DissolveOffset = Shader.PropertyToID("_DissolveOffset");
    
    new Camera camera;
    Material[] materials;
    void Start()
    {
        camera = Camera.main;
        materials = GetComponentsInChildren<MeshRenderer>().Select(meshRenderer => meshRenderer.material).ToArray();

        IEnumerator BuildAnimation()
        {
            foreach (Material material in materials)
                material.SetVector(DissolvePosition, camera.transform.position);

            float time = 0;
            while (time < 5)
            {
                yield return null;
                time += Time.deltaTime;

                foreach (Material material in materials)
                    material.SetFloat(DissolveOffset, time / 10 * 150);
            }

            StartCoroutine(BuildAnimation());
        }

        StartCoroutine(BuildAnimation());
    }
}
