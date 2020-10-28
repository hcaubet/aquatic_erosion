using UnityEngine;

public class PlanePilot : MonoBehaviour
{
    float speed = 90.0f;

    public Camera cam1;
    public Camera cam2;
    
    void Start()
    {
        cam1.enabled = true;
        cam2.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (cam1.enabled)
        {
            Vector3 moveCamTo = transform.position - transform.forward * 40.0f + Vector3.up * 25.0f;
            float bias = 0.96f;
            Camera.main.transform.position = Camera.main.transform.position * bias + moveCamTo * (1.0f - bias);
            Camera.main.transform.LookAt(transform.position + transform.forward * 30.0f);
        }

        transform.position += transform.forward * Time.deltaTime * speed;

        speed -= transform.forward.y*Time.deltaTime * 50.0f;

        if (speed < 35.0f) speed = 35.0f;

        float terrainHeight = Terrain.activeTerrain.SampleHeight(transform.position);

        if (Input.GetKeyDown(KeyCode.Space)) speed = 200.0f;

        if (Input.GetKeyDown(KeyCode.C))
        {
            cam1.enabled = !cam1.enabled;
            cam2.enabled = !cam2.enabled; ;
        }


        transform.Rotate(Input.GetAxis("Vertical"),0.0f,-Input.GetAxis("Horizontal")) ;
        transform.Rotate(0.0f,Input.GetAxis("Side"),0.0f);
    }
}
