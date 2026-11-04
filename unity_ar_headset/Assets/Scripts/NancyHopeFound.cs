using UnityEngine;

public class NancyHopeFound : MonoBehaviour
{
    public Vector3 BoxTargetFound = new Vector3(100,150,0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(BoxTargetFound * Time.deltaTime);
    }
}
