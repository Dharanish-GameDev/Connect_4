using System.Collections;
using Photon.Pun;
using UnityEngine;

public class Connect4Piece : MonoBehaviourPun
{
    [Header("Drop Settings")]
    public float speed = 1200f; // UI units/sec (anchoredPosition units)
    [SerializeField] private float dropStartPadding = 120f; // same as Connect5 (+120)

    private RectTransform rectTransform;

    private Vector2 targetPosition;
    private bool hasTarget;

    // InstantiationData
    private float xDest;
    private float yDest;
    private int parentViewID;      // optional, used for safe parenting
    private int columnIndex = -1;  // used for column highlight clearing

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // Read instantiation data first
        ReadInstantiationData();

        // Start parenting + positioning after everything exists (GM + parent view)
        StartCoroutine(SetupRoutine());
    }

    private void ReadInstantiationData()
    {
        object[] data = photonView.InstantiationData;

        // Expected:
        // [0]=xDest, [1]=yDest, [2]=parentViewID, [3]=columnIndex
        if (data == null || data.Length < 2)
        {
            Debug.LogWarning("[Connect4Piece] Missing InstantiationData.");
            hasTarget = false;
            return;
        }

        xDest = ToFloat(data[0]);
        yDest = ToFloat(data[1]);
        targetPosition = new Vector2(xDest, yDest);

        parentViewID = (data.Length >= 3) ? ToInt(data[2]) : 0;
        columnIndex = (data.Length >= 4) ? ToInt(data[3]) : -1;

        hasTarget = true;
    }

    private IEnumerator SetupRoutine()
    {
        // Wait a few frames for GameManager / Canvas / PhotonViews to exist on clients
        for (int i = 0; i < 10; i++)
        {
            if (TryParentToTarget()) break;
            yield return null;
        }

        // Put piece above board (MATCH Connect5 height)
        if (hasTarget && rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(xDest, GetDropStartHeightLikeConnect5());
            transform.SetAsLastSibling();
        }
    }

    private bool TryParentToTarget()
    {
        // 1) Best: parentViewID (most reliable across clients)
        if (parentViewID != 0)
        {
            PhotonView parentPV = PhotonView.Find(parentViewID);
            if (parentPV != null)
            {
                RectTransform parentRT = parentPV.GetComponent<RectTransform>();
                if (parentRT != null)
                {
                    transform.SetParent(parentRT, false);
                    NormalizeLocalUI();
                    return true;
                }
            }
        }

        // 2) Fallback: GameManager.instance.PiecesParent
        GameManager gm = GameManager.instance;
        if (gm != null && gm.PiecesParent != null)
        {
            transform.SetParent(gm.PiecesParent, false);
            NormalizeLocalUI();
            return true;
        }

        return false;
    }

    private void NormalizeLocalUI()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        rectTransform.localScale = Vector3.one;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.anchoredPosition3D = new Vector3(rectTransform.anchoredPosition.x, rectTransform.anchoredPosition.y, 0f);
    }

    // ✅ Same idea as your Connect5: start from half of the CANVAS height + padding
    private float GetDropStartHeightLikeConnect5()
    {
        // Find the Canvas we are under
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            RectTransform canvasRT = canvas.transform as RectTransform;
            if (canvasRT != null)
            {
                return (canvasRT.rect.height * 0.5f) + dropStartPadding;
            }

            // Fallback if canvasRT missing (rare)
            return (canvas.pixelRect.height * 0.5f) + dropStartPadding;
        }

        // Ultimate fallback
        RectTransform parent = rectTransform.parent as RectTransform;
        if (parent != null) return (parent.rect.height * 0.5f) + dropStartPadding;

        return yDest + 600f;
    }

    private void Update()
    {
        if (!hasTarget || rectTransform == null) return;

        rectTransform.anchoredPosition = Vector2.MoveTowards(
            rectTransform.anchoredPosition,
            targetPosition,
            speed * Time.deltaTime
        );

        if ((rectTransform.anchoredPosition - targetPosition).sqrMagnitude <= 0.25f) // ~0.5px
        {
            rectTransform.anchoredPosition = targetPosition;
            hasTarget = false;

            // Clear column highlight when this piece lands (local)
            if (columnIndex >= 0 && GameManager.instance != null)
            {
                GameManager.instance.ClearColumnHighlightLocal(columnIndex);
            }
        }
    }

    private float ToFloat(object obj)
    {
        if (obj is float f) return f;
        if (obj is double d) return (float)d;
        if (obj is int i) return i;
        if (obj is long l) return l;
        return 0f;
    }

    private int ToInt(object obj)
    {
        if (obj is int i) return i;
        if (obj is long l) return (int)l;
        if (obj is float f) return Mathf.RoundToInt(f);
        if (obj is double d) return Mathf.RoundToInt((float)d);
        return -1;
    }
}
