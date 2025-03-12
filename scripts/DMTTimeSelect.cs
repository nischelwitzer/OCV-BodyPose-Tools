using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/*
 * TimeSelect.cs 
 * Version 2.1
 * 
 * based on code from: Ettl, Brandner, Kabosch & Zhou
 * last changed: 27.02.2025 by DMT Team, FH JOANNEUM, Nischelwitzer
 * 
 * description:
 * EyeToy Menu like Button with fill effect
 * Handles collision events to trigger a timer for the effect and execute events
 * Event 1: Start (inside OnTriggerEnter)
 * Event 2: Canceled (inside OnTriggerExit)
 * Event 3: End (inside TimerCoroutine) Selected
 * 
 */

[RequireComponent(typeof(Image))]
public class DMTTimeSelect : MonoBehaviour
{
    [Tooltip("Image that will be filled over time")]
    public Image workingImage;
    [Tooltip("Duration for the timer in seconds")]
    public float duration = 2.0f;

    [Tooltip("GameObject to activate when the timer starts (optional)")]
    public UnityEvent toExecuteStart;
    [Tooltip("GameObject to activate after the timer is canceld (optional)")]
    public UnityEvent toExecuteCanceled;
    [Tooltip("GameObject to activate after the timer ends (optional)")]
    public UnityEvent toExecuteEnd;

    private Coroutine timerCoroutine;

    private void Start()
    {
        if (workingImage == null)
        {
            workingImage = GetComponent<Image>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"##### DMTTimeSelect: {gameObject.name} collided with {other.name}");

        if (toExecuteStart != null)
        {
            toExecuteStart.Invoke();
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }

        timerCoroutine = StartCoroutine(TimerCoroutine(duration));
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"##### DMTTimeSelect: {gameObject.name} ended collision with {other.name}");

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
            toExecuteCanceled.Invoke();

            if (workingImage != null)
            {
                workingImage.fillAmount = 1.0f; // Reset fill amount
            }
        }
    }

    private IEnumerator TimerCoroutine(float timeout)
    {
        float time = 0;
        while (time < timeout)
        {
            time += Time.deltaTime;
            if (workingImage != null)
            {
                workingImage.fillAmount = 1 - (time / timeout);
            }
            yield return null; // Wait until next frame
        }

        // Code to execute after timeout
        if (toExecuteEnd != null)
        {
            toExecuteEnd.Invoke();
        }
    }

    private void ActivateGameObject()
    {
        toExecuteEnd.Invoke(); // Activate the specified GameObject after timeout
    }
}
