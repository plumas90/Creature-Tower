using System.Collections;
using UnityEngine;

public class PeaPodExplosionEffect : MonoBehaviour
{
    private SpriteRenderer sr;
    private Material mat;

    private static Sprite whiteSprite;
    private static Texture2D whiteTexture;

    private static Sprite GetOrCreateWhiteSprite()
    {
        if (whiteSprite == null)
        {
            whiteTexture = new Texture2D(1, 1);
            whiteTexture.SetPixel(0, 0, Color.white);
            whiteTexture.Apply();
            whiteSprite = Sprite.Create(whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
        return whiteSprite;
    }

    public void Play(float explosionRadius, string sortingLayer, int sortingOrder)
    {
        sr = gameObject.AddComponent<SpriteRenderer>();

        // Use cached 1x1 white sprite to prevent native memory leaks
        sr.sprite = GetOrCreateWhiteSprite();

        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = sortingOrder;

        // Set localScale based on explosionRadius.
        // Quad diameter needs to be 2 * explosionRadius to match the radius mathematically
        float size = explosionRadius * 2f;
        transform.localScale = new Vector3(size, size, 1f);

        // Find and assign custom shader
        Shader shader = Shader.Find("Custom/ExplosionShockwave");
        if (shader == null)
        {
            Debug.LogWarning("[PeaPodExplosionEffect] Custom/ExplosionShockwave shader not found! Destroying immediately.");
            Destroy(gameObject);
            return;
        }

        mat = new Material(shader);
        sr.material = mat;

        StartCoroutine(CoAnimate());
    }

    private IEnumerator CoAnimate()
    {
        float elapsed = 0f;
        float duration = 0.4f; // 0.4s expansion time

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            if (mat != null)
            {
                mat.SetFloat("_Radius", t);
            }

            yield return null;
        }

        if (mat != null)
        {
            Destroy(mat);
        }
        Destroy(gameObject);
    }
}
