using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickUpDowp : MonoBehaviour
{

    [SerializeField] private Transform playerCamTransform;
    [SerializeField] private Transform objectGrabPointTransform;
    [SerializeField] private LayerMask pickUpLayerMask;

    private ObjectGrabable objectGrabable;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (objectGrabable == null)
            {
                float pickUpDistance = 2f;
                if (Physics.Raycast(playerCamTransform.position, playerCamTransform.forward, out RaycastHit raycastHit, pickUpDistance))
                {
                    if (raycastHit.transform.TryGetComponent(out objectGrabable))
                    {
                        objectGrabable.Grab(objectGrabPointTransform);
                    }
                }
            }
            else
            {
                objectGrabable.Drop();
                objectGrabable = null;
            }
        }
    }
}
