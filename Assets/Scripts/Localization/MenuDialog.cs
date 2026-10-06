using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Localized modal used by language selection and returning to the title.</summary>
[DisallowMultipleComponent]
public sealed class MenuDialog : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text description;
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button secondaryButton;
    [SerializeField] private TMP_Text primaryLabel;
    [SerializeField] private TMP_Text secondaryLabel;

    private Action confirmed;
    private GameManager owner;
    private GameObject previousSelection;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private bool isOpen;
    public bool IsOpen => isOpen;

    public static MenuDialog Create()
    {
        GameObject prefab = Resources.Load<GameObject>("Localization/MenuDialog");
        if (prefab == null)
        {
            Debug.LogError("The localized menu dialog prefab is missing.");
            return null;
        }
        return Instantiate(prefab).GetComponent<MenuDialog>();
    }

    private void Awake()
    {
        primaryButton.onClick.AddListener(Confirm);
        secondaryButton.onClick.AddListener(Cancel);
    }

    public void ShowTranslationNotice()
    {
        Configure("dialog.translation.title", "dialog.translation.body", "dialog.confirm", null);
        Open(null);
    }

    public void ShowReturnToTitle(Action onConfirmed)
    {
        Configure("dialog.returnTitle.question", null, "dialog.yes", "dialog.no");
        Open(onConfirmed);
    }

    private void Configure(string titleKey, string bodyKey, string acceptKey, string cancelKey)
    {
        GameLocalization.SetText(heading, titleKey);
        description.gameObject.SetActive(bodyKey != null);
        if (bodyKey != null)
            GameLocalization.SetText(description, bodyKey);
        GameLocalization.SetText(primaryLabel, acceptKey);
        secondaryButton.gameObject.SetActive(cancelKey != null);
        if (cancelKey != null)
            GameLocalization.SetText(secondaryLabel, cancelKey);
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyKey != null ? 470f : 260f);
    }

    private void Open(Action onConfirmed)
    {
        if (isOpen)
            Cancel();
        owner = GameManager.Instance;
        owner?.RegisterMenuDialog(this);
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        confirmed = onConfirmed;
        isOpen = true;
        gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        FitWidth();
        Canvas.ForceUpdateCanvases();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(secondaryButton.gameObject.activeSelf
                ? secondaryButton.gameObject : primaryButton.gameObject);
    }

    private void Update()
    {
        // In gameplay GameManager owns Escape so the same press cannot also close the menu.
        if (isOpen && owner == null && Input.GetKeyDown(KeyCode.Escape))
            Cancel();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (isOpen)
            FitWidth();
    }

    private void FitWidth()
    {
        RectTransform canvasRect = transform as RectTransform;
        panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
            Mathf.Min(880f, Mathf.Max(240f, canvasRect.rect.width - 48f)));
    }

    public void Cancel() => Close();

    private void Confirm()
    {
        Action action = confirmed;
        Close();
        action?.Invoke();
    }

    private void Close()
    {
        ReleaseModal();
        gameObject.SetActive(false);
    }

    private void ReleaseModal()
    {
        if (!isOpen)
            return;
        isOpen = false;
        confirmed = null;
        owner?.UnregisterMenuDialog(this);
        owner = null;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : null);
        previousSelection = null;
    }

    private void OnDisable() => ReleaseModal();

    private void OnDestroy()
    {
        ReleaseModal();
        if (primaryButton != null)
            primaryButton.onClick.RemoveListener(Confirm);
        if (secondaryButton != null)
            secondaryButton.onClick.RemoveListener(Cancel);
    }
}
