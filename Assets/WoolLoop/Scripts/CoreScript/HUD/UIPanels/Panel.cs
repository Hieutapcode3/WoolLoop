using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;


public class Panel<T> : Panel where T : Panel
{
    public readonly UnityEvent onClosed = new UnityEvent();
    public void Close()
    {
        HUDSystem.Instance.Hide<T>();
    }

    // public override void Show(object data = null, bool duplicated = false)
    // {
    // 	base.Show(data, duplicated);
    // 	UnlockTapToHide();
    // }

    private void OnDisable()
    {
        onClosed.Invoke();
    }
}

/** <summary> Base Panel in UI</summary> */
public class Panel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool preventBgScroll;
    [SerializeField] private float timeToShow = 0.2f;
    [SerializeField] public bool tapToHide = true;
    public bool cacheTapToHide;
    [SerializeField] protected bool fade = true;
    [SerializeField] protected Image imgPanel;
    protected Color colorPanel = new Color32(39, 58, 68, 178);
    [SerializeField] public Transform popup;
    public bool isAllowScrollCamera = false;
    private bool isFirstShow = false;
    private bool _isClosed = true;

    public bool blockTouched = true;

#if UNITY_ANDROID
		[SerializeField] public bool physicBackEnable = true;
#endif

    protected bool duplicated = false;
    protected virtual void Awake()
    {
        if (fade)
        {
            // Logs.Info("change image0");
            imgPanel = GetComponent<Image>();
        }
    }

    public virtual void Show(object data = null, bool duplicated = false)
    {
        this.duplicated = duplicated;
        if (popup != null && (!isFirstShow || !gameObject.activeSelf))
        {
            isFirstShow = true;
            popup.localScale = Vector3.zero;
        }
        gameObject.SetActive(true);
        if (popup != null)
        {
            popup.DOScale(Vector3.one, timeToShow).SetEase(Ease.OutBack);
            //SoundManager.instance.PlayEffect(SoundCommand.SFX_OPEN_POPUP_SCENE);
        }

        if (fade && imgPanel != null)
        {
            // Logs.Info("change image");
            Color colorRoot = imgPanel.color;
            colorRoot.a = 0;
            imgPanel.color = colorRoot;
            imgPanel.DOColor(colorPanel, 1f).SetEase(Ease.Linear).OnComplete(() =>
            {
                _isClosed = false;
            });
        }
        else
            _isClosed = false;

        UnlockTapToHide();
    }

    protected void UnlockTapToHide()
    {
        cacheTapToHide = tapToHide;
    }

    public virtual void Hide(object data = null)
    {
        // isAllowScrollCamera = true;
        if (popup != null)
        {
            //SoundManager.instance.PlayEffect(SoundCommand.SFX_CLOSE_POPUP_SCENE);
        }
        if (gameObject)
            gameObject.SetActive(false);

        HUDSystem.Instance.UpdateActiveScroll();
    }

    public virtual void Back()
    {

#if UNITY_ANDROID
			if (PhysicBackEnable)
				HUDSystem.Instance.Hide<Panel>();
#endif
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (HUDSystem.TapToHideLocked)
            return;

        if (cacheTapToHide && eventData.pointerCurrentRaycast.gameObject == gameObject)
        {
            Invoke(nameof(HideDelay), 0.1f);
        }

        UnlockTapToHide();
    }

    private void HideDelay()
    {
        var p = gameObject.GetComponent<Panel>();
        HUDSystem.Instance.Hide(p.GetType());
    }

    public virtual bool PhysicBackEnable
    {
        get
        {
#if UNITY_ANDROID
				return physicBackEnable;
#else
            return false;
#endif
        }
    }

    private void OnEnable()
    {
        if (preventBgScroll)
            StartCoroutine(WaitForFrames(6));
    }

    IEnumerator WaitForFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
        HUDSystem.Instance.ScrollingLocked = true;
        HUDSystem.Instance.SwipeLocked = true;
        HUDSystem.IsLock = true;
    }

    private void OnDisable()
    {
        if (preventBgScroll)
        {
            HUDSystem.Instance.ScrollingLocked = false;
            HUDSystem.Instance.SwipeLocked = false;
            HUDSystem.IsLock = false;
        }
    }
}
