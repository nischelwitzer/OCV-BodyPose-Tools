using UnityEngine;
using DG.Tweening;

/*
* DOTween Helper
* for easy use of DOTween from https://dotween.demigiant.com/
* usage: use with DMTTriggerEvent
* 
* example usage: can be used to animate on events
* 
* Author: FH JOANNEUM, IMA,´DMT, Nischelwitzer, 2025
* www.fh-joanneum.at & exhibits.fh-joanneum.at
*/

public class DoTweenHelpers : MonoBehaviour
{
    public void DTAwayRenderer()
    {
        Renderer objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
            objectRenderer.material.DOColor(new Color(0, 0, 0, 0), 2f);
    }

    public void DTAwaySize(float time) 
    {
        // transform.DOScale(Vector3.one * 0.1f, time);
        transform.DOScale(Vector3.zero, time).OnComplete(() => gameObject.SetActive(false));
    }

    public void DTSizeYoyo(float time)
    {
        transform.DOScale(Vector3.one * 0.5f, 1f).SetLoops(-1, LoopType.Yoyo);
    }
    
}
