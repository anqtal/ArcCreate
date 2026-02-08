using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class I18nText : MonoBehaviour
{
    [SerializeField] private string id;
    [SerializeField] private bool readIdFromContent;

    public TMP_Text Text { get; private set; }

    private void Start()
    {
        LoadComponent();
        I18n.OnLocaleChanged += ApplyLocale;

        if (readIdFromContent) id = Text.text;

        ApplyLocale();
    }

    private void OnDestroy()
    {
        I18n.OnLocaleChanged -= ApplyLocale;
    }

    public virtual void ApplyLocale()
    {
        Text.text = I18n.S(id);
    }

    public void LoadComponent()
    {
        Text = GetComponent<TMP_Text>();
    }
}