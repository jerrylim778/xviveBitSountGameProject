using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

public class GUIAlbumModule : MonoBehaviour
{
    [SerializeField, ReadOnly] private int m_AlbumItemIDX;
    [SerializeField, ReadOnly] RectTransform m_MoveRT;
    private bool m_isFirst = false;

    public void Initlization(int _ItemIDX, Sprite _AlbumSprite, System.Action _EndCallBack = null)
    {
        if (m_isFirst)
        {
            _EndCallBack?.Invoke();
            return;
        }
        m_isFirst = true;
        m_AlbumItemIDX = _ItemIDX;
        m_MoveRT = this.transform as RectTransform;
        m_MoveRT.GetChild(0).GetComponent<Image>().sprite = _AlbumSprite;
        m_MoveRT.anchoredPosition = new Vector2(-300f, m_MoveRT.anchoredPosition.y);
        m_MoveRT.DOAnchorPosX(0f, 0.45f).SetEase(Ease.OutExpo).OnComplete(() =>
        _EndCallBack());
    }

    //보류 여러개 존재시 생성되는 갯수에 따라 스크롤의 가로 형태중 
    //비율을 더해서 원하는 구간까지 가야한다
    private void ReCycleAction(bool _isLeftToRight)
    {

    }
}
