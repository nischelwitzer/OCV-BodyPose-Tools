using TMPro;
using UnityEngine;

public class DMT_PosesDataCheck : MonoBehaviour
{
    public TextMeshProUGUI myCounter;

    void Start()
    {
    }

    void Update()
    {
        Debug.Log("##### PosesCounter: " + DMT.StaticStore.posesCounter);
        if (myCounter != null)
        {
            myCounter.text = "" + DMT.StaticStore.posesCounter;
        }
    }
}
