using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class LockPickerAgent : Agent
{
    [SerializeField] private float defualtSpeed = 50f;
    [SerializeField] private float defualtPickingSpeed = 10f;
    [SerializeField] private Transform lockTransform;
    [SerializeField] private Transform shivTransform;
    [SerializeField] private float rangeOffset = 8f;
    [SerializeField] private float rangeSize = 40f;
    [SerializeField] private float lockPickerDurability = 100f;
    [SerializeField] private float lockPickerDurabilityLoss = 1f;
    private float[] rotatableRange = {0f, 0f};
    private float[] correctRange = {0f, 0f};

    public override void OnEpisodeBegin()
    {
        transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
        lockTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        shivTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        initRange();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.eulerAngles.y);
        sensor.AddObservation(lockTransform.eulerAngles.y);
        sensor.AddObservation(lockPickerDurability);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float rotateLockPicker = actions.ContinuousActions[0];
        int pickLockFlag = actions.DiscreteActions[0];

        rotate(rotateLockPicker);
        pickLock(pickLockFlag);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        continousActions[0] = Input.GetAxisRaw("Horizontal");
        if (Input.GetKey(KeyCode.E))
        {
            discreteActions[0] = 1;
        }
        else
        {
            discreteActions[0] = 0;
        }
    }

    private void rotate(float rotateSpeed)
    {
        transform.Rotate(0, rotateSpeed * Time.deltaTime * defualtSpeed, 0);
    }

    private void pickLock(int flag)
    {
        if(flag == 1)
        {
            float degree = rotatableDegree();
            if (degree == 0f)
            {
                lockPickerDurability -= lockPickerDurabilityLoss;
            }
            else
            {
                if(Mathf.Abs(lockTransform.eulerAngles.y) <= degree)
                {
                    lockTransform.Rotate(0, -Time.deltaTime * defualtPickingSpeed, 0);
                    shivTransform.Rotate(0, -Time.deltaTime * defualtPickingSpeed, 0);
                }
                else
                {
                    lockPickerDurability -= lockPickerDurabilityLoss;
                }
            }
        }
    }

    private void initRange()
    {
        rotatableRange[0] = Random.Range(0f, 360f - rangeSize);
        rotatableRange[1] = rotatableRange[0] + rangeSize;
        correctRange[0] = rotatableRange[0] + rangeOffset;
        correctRange[1] = rotatableRange[1] - rangeOffset;
        Debug.Log(rotatableRange[0]);
        Debug.Log(rotatableRange[1]);
    }

    private float rotatableDegree()
    {
        float y = transform.eulerAngles.y;
        if(y > rotatableRange[0] && y < rotatableRange[1])
        {
            if(y >= correctRange[0] && y <= correctRange[1])
            {
                return 90f;
            }
            else
            {
                if(y > correctRange[0])
                {
                    //Debug.Log("Y: " + y + " rotatableRange[1]: " + rotatableRange[1]);
                    return 90f * ((rotatableRange[1] - y) / rangeOffset);
                }
                else
                {
                   //Debug.Log("Y: " + y + " rotatableRange[0]: " + rotatableRange[0]);
                    return 90f * ((rotatableRange[0] - y) / rangeOffset);
                }
            }
        }
        else
        {
            return 0f;
        }
    }
}
