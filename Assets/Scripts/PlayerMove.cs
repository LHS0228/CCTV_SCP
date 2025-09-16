using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("시점 돌리기 속도")]
    public float sensitivity = 20f;
    float rotX = 0f;

    [Header("이동 속도")]
    public float speed = 3;

    Vector2 lookDelta; //시점 코드
    Vector2 moveInput; //이동 코드

    public GameObject headObject;

    void Update()
    {
        //이동
        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
        transform.Translate(move * Time.deltaTime * speed);

        //시점
        float mouseX = lookDelta.x * sensitivity * Time.deltaTime;
        float mouseY = lookDelta.y * sensitivity * Time.deltaTime;

        rotX -= mouseY;
        rotX = Mathf.Clamp(rotX, -80f, 80f);
        headObject.transform.localRotation = Quaternion.Euler(rotX, 0f, 0f);
        headObject.transform.parent.Rotate(Vector3.up * mouseX);
    }

    void OnLook(InputValue value) => lookDelta = value.Get<Vector2>();

    void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
