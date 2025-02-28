using UnityEngine;

public class DMT_NosePos : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (DMT.StaticStore.bodyPose != null)
        {
            // Debug.Log("StaticStore: " + DMT.StaticStore.bodyPose[0].x + " "+ DMT.StaticStore.bodyPose[0].y);
            Vector2 myHand = new Vector2(DMT.StaticStore.bodyPose[0].x - 1920f/2, -DMT.StaticStore.bodyPose[0].y + 1080f/2);

            this.gameObject.transform.position = new Vector3(myHand.x, myHand.y, 0);
        }

        // Vector2 vector2 = new Vector2(DMT.StaticStore.myData[17]);
        // this.gameObject.transform.position = new Vector3(DMT.StaticStore.myData, 0, 0);
    }
}
