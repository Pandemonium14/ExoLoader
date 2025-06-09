using UnityEngine;

namespace ExoLoader;

public class SpriteAnimationPlayer : MonoBehaviour
{
    public Sprite[] sprites;
    public float frameRate = 12f;
    public bool isPlaying = false;

    private SpriteRenderer spriteRenderer;
    private Material spriteMaterial;
    private float frameTimer;
    private int currentFrameIndex;
    private bool useCustomMaterial = false;

    public int currentFrame
    {
        get { return currentFrameIndex; }
        set
        {
            currentFrameIndex = Mathf.Clamp(value, 0, sprites.Length - 1);
            UpdateCurrentFrame();
        }
    }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteMaterial = spriteRenderer.material;
            // Check if we're using a custom material (not the default sprite material)
            useCustomMaterial = spriteMaterial != null && !spriteMaterial.shader.name.Contains("Sprites/Default");
        }

        if (sprites != null && sprites.Length > 0)
        {
            UpdateCurrentFrame();
        }
    }

    private void UpdateCurrentFrame()
    {
        if (spriteRenderer == null || sprites == null || currentFrameIndex >= sprites.Length)
            return;

        if (useCustomMaterial && spriteMaterial != null)
        {
            spriteMaterial.mainTexture = sprites[currentFrameIndex].texture;
            spriteRenderer.sprite = sprites[currentFrameIndex];
        }
        else
        {
            spriteRenderer.sprite = sprites[currentFrameIndex];
        }

        spriteRenderer.color = Color.white;
    }

    public void PlayAnimation()
    {
        isPlaying = true;
        frameTimer = 0f;
        currentFrameIndex = 0;
        UpdateCurrentFrame();
    }

    public void StopAnimation()
    {
        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying || sprites == null || sprites.Length <= 1)
            return;

        frameTimer += Time.deltaTime;

        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrameIndex = (currentFrameIndex + 1) % sprites.Length;
            UpdateCurrentFrame();
        }
    }

    void OnDestroy()
    {
        if (useCustomMaterial && spriteMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(spriteMaterial);
            else
                DestroyImmediate(spriteMaterial);
        }
    }
}
