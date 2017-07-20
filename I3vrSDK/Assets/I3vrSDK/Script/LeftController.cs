using UnityEngine;
using System.Collections;

public class LeftController : MonoBehaviour {
    public GameObject leftControll, leftCanvas;
    private GameObject leftController, leftControllerPointer, rightControllerPointer;
    // Use this for initialization
    public void AddLeftController()
    {
        rightControllerPointer = GameObject.FindWithTag("RightControllerPointer");
        leftController = Resources.Load<GameObject>("I3vrLeftControllerMain");
        leftControllerPointer = Resources.Load<GameObject>("I3vrLeftControllerPointer");
        Instantiate(leftController);
        Instantiate(leftControllerPointer, rightControllerPointer.transform.position, Quaternion.identity);
    }

    public void FindLeftController()
    {
        if (leftControll && leftCanvas)
        {
            leftControll.SetActive(true);
            leftCanvas.SetActive(true);
        }       
    }
}
