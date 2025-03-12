
using UnityEngine;
using UnityEngine.Events;


/*
* triggers a event
* usage: put on GO with a collider with trigger
* 
* example usage: can be used to trigger events on enter or exit
* 
* Author: FH JOANNEUM, IMA,´DMT, Nischelwitzer, 2025
* www.fh-joanneum.at & exhibits.fh-joanneum.at
*/

namespace DMT
{
    public class DMTTriggerEvent : MonoBehaviour
    {

        [Tooltip("GameObject to activate after EnterEvent")]
        public UnityEvent toExecuteEnter;

        [Tooltip("GameObject to activate after ExitEvent")]
        public UnityEvent toExecuteExit;

        private void Start()
        {
            Debug.Log("##### DMTTriggerEvent Start: " + this.gameObject.name + " " + this.gameObject.GetType().ToString());
        }

        private void OnTriggerEnter(Collider other)
        {
            if (toExecuteEnter != null)
            {
                toExecuteEnter.Invoke();
            }

            Debug.Log("##### DMTTriggerEvent toExecuteEnter: " + this.gameObject.name + " <> " + other.name);
        }

        private void OnTriggerExit(Collider other)
        {
            if (toExecuteExit != null)
            {
                toExecuteExit.Invoke();
            }
            Debug.Log("##### DMTTriggerEvent toExecuteExit: " + this.gameObject.name + " <> " + other.name);
        }

    }

}