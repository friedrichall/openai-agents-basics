// Copyright 2019 Patrick Harms
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;

/**
 * 
 */
public class Movement : MonoBehaviour
{
    /** */
    [SerializeField]
    private float panSpeed = 1.5f;

    /** */
    [SerializeField]
    private float rotSpeed = 10f;

    [SerializeField]
    [Range(0.01f, 1f)]
    private float mouseRotationIntensity = 0.15f;

    /** */
    private new Camera camera;

    /** stores the last mouse position */
    private Vector3 lastMouse = new Vector3(255, 255, 255);

    /**
     * 
     */
    void Start()
    {
    	if (this.camera == null)
    	{
    	    this.camera = Camera.main;
    	}
    }

    /**
     * 
     */
    void Update()
    {
        Transform cameraTransform = this.camera.transform;
        Vector3 pos = cameraTransform.position;

        if (Input.GetKey("w"))
        {
            pos += cameraTransform.forward * this.panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("s"))
        {
            pos -= cameraTransform.forward * this.panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("d"))
        {
            pos += cameraTransform.right * this.panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("a"))
        {
            pos -= cameraTransform.right * this.panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("e"))
        {
            pos += cameraTransform.up * this.panSpeed * Time.deltaTime;
        }
        if (Input.GetKey("q"))
        {
            pos -= cameraTransform.up * this.panSpeed * Time.deltaTime;
        }

        this.camera.transform.position = pos;

        Vector3 rotation = this.camera.transform.rotation.eulerAngles;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotation.y -= this.rotSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotation.y += this.rotSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rotation.x += this.rotSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            rotation.x -= this.rotSpeed * Time.deltaTime;
        }

        this.camera.transform.rotation = Quaternion.Euler(rotation);

        if (Input.GetMouseButton(1))
        {
            lastMouse = Input.mousePosition - lastMouse;
            lastMouse = new Vector3(-lastMouse.y * this.mouseRotationIntensity, lastMouse.x * this.mouseRotationIntensity, 0);
            lastMouse = new Vector3(this.camera.transform.eulerAngles.x + lastMouse.x, this.camera.transform.eulerAngles.y + lastMouse.y, 0);
            this.camera.transform.eulerAngles = lastMouse;
        }
        lastMouse = Input.mousePosition;
    }
}
