using UnityEngine;
using UnityEngine.UIElements;

public class objectMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public float speed;
    private float rotateSpeed = 100f;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float xTranslation = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        float yTranslation = Input.GetAxis("yTranslation") * speed * Time.deltaTime;
        float zTranslation = Input.GetAxis("Vertical") * speed * Time.deltaTime;

        float xRotation = Input.GetAxis("xRotation") * rotateSpeed * Time.deltaTime;
        float yRotation = Input.GetAxis("yRotation") * rotateSpeed * Time.deltaTime;
        float zRotation = Input.GetAxis("zRotation") * rotateSpeed * Time.deltaTime;

        transform.position += new Vector3(xTranslation, yTranslation, zTranslation);
        transform.Rotate(xRotation, yRotation, zRotation);
    }
}
