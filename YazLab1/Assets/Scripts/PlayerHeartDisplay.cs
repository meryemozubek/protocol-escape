using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth), typeof(SpriteRenderer))]
public sealed class PlayerHeartDisplay : MonoBehaviour
{
    [SerializeField] private Sprite[] hearts;
    private PlayerHealth health;
    private SpriteRenderer body;
    private RectTransform display;
    private Image image;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        body = GetComponent<SpriteRenderer>();
        var root = new GameObject("PlayerHearts", typeof(RectTransform), typeof(Canvas));
        display = root.GetComponent<RectTransform>();
        display.SetParent(transform, false);
        display.sizeDelta = new Vector2(150, 50);
        display.localScale = Vector3.one * 0.01f;
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        var graphic = new GameObject("Hearts", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        graphic.transform.SetParent(display, false);
        image = graphic.GetComponent<Image>();
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;
        image.raycastTarget = false;
        image.preserveAspect = true;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += Refresh;
        Refresh(health.CurrentHealth, health.MaxHealth);
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= Refresh;
    }

    private void Refresh(int current, int maximum)
    {
        image.enabled = current > 0 && hearts != null && hearts.Length >= 3;
        if (image.enabled) image.sprite = hearts[Mathf.Clamp(current, 1, 3) - 1];
    }

    private void LateUpdate()
    {
        display.position = new Vector3(body.bounds.center.x, body.bounds.max.y + 0.3f, transform.position.z);
        display.rotation = Quaternion.identity;
    }
}
