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
    [SerializeField] private Transform RR0A;
    [SerializeField] private Transform RR1A;
    [SerializeField] private Transform CR0A;
    [SerializeField] private Transform CR1A;
    [SerializeField] private MeshRenderer backgroundMeshRenderer;
    [SerializeField] private Material winMaterial;
    [SerializeField] private Material loseMaterial;
    private bool rotatable = false;
    private float[] rotatableRange = {0f, 0f};
    private float[] correctRange = {0f, 0f};

    public override void OnEpisodeBegin()
    {
        rotatable = false;
        transform.rotation = Quaternion.Euler(new Vector3(0, 180, 0));
        lockTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        shivTransform.rotation = Quaternion.Euler(new Vector3(0, 0, 180));
        lockPickerDurability = 100f;
        initRange();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.eulerAngles.y);//观察（observe）
        sensor.AddObservation(lockTransform.eulerAngles.y);  //观察（observe）锁芯（lock）旋转了多少
        sensor.AddObservation(lockPickerDurability);  //观察（observe）开锁器的耐久度
        sensor.AddObservation(rotatable);  //观察（observe）锁心（lock）是否可以旋转
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float rotateLockPicker = actions.ContinuousActions[0];
        int pickLockFlag = actions.DiscreteActions[0];

        rotate(rotateLockPicker); //开锁器可旋转度数是 continuous action
        pickLock(pickLockFlag);  //是否执行开锁动作是 discrete action
        if(lockPickerDurability <= 0f)
        {
            AddReward(-40f);  //开锁器耐久度归零
            backgroundMeshRenderer.material = loseMaterial;  //更换背景颜色  失败：红色
            EndEpisode();  //开始新的episode
        } 
        if(Mathf.Abs(lockTransform.eulerAngles.y) >= 90f)
        {
            AddReward(+200f);  //成功开锁
            backgroundMeshRenderer.material = winMaterial;   //更换背景颜色  成功：绿色 
            EndEpisode();  //开始新的episode
        }
        AddReward(-0.5f);  // agent只要存在就会扣除reward，以保证它不会原地不动
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
        if(rotateSpeed < 0 && transform.eulerAngles.y > 90)
            transform.Rotate(0, rotateSpeed * Time.deltaTime * defualtSpeed, 0);
        else if(rotateSpeed > 0 && transform.eulerAngles.y < 270)
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
                AddReward(-0.5f);
            }
            else
            {
                if(Mathf.Abs(lockTransform.eulerAngles.y) <= degree)
                {
                    rotatable = true;
                    lockTransform.Rotate(0, -Time.deltaTime * defualtPickingSpeed, 0);
                    shivTransform.Rotate(0, -Time.deltaTime * defualtPickingSpeed, 0);
                    AddReward(+5f);
                }
                else
                {
                    rotatable = false;
                    lockPickerDurability -= lockPickerDurabilityLoss;
                    AddReward(-0.2f);
                }
            }
        }
    }

    private void initRange()
    {
        rotatableRange[0] = Random.Range(90f, 270f - rangeSize);
        rotatableRange[1] = rotatableRange[0] + rangeSize;
        correctRange[0] = rotatableRange[0] + rangeOffset;
        correctRange[1] = rotatableRange[1] - rangeOffset;
        RR0A.rotation = Quaternion.Euler(new Vector3(0, rotatableRange[0] + 180f, 0));
        RR1A.rotation = Quaternion.Euler(new Vector3(0, rotatableRange[1] + 180f, 0));
        CR0A.rotation = Quaternion.Euler(new Vector3(0, correctRange[0] + 180f, 0));
        CR1A.rotation = Quaternion.Euler(new Vector3(0, correctRange[1] + 180f, 0));
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
                    return 90f * ((y - rotatableRange[0]) / rangeOffset);
                }
            }
        }
        else
        {
            return 0f;
        }
    }
}
