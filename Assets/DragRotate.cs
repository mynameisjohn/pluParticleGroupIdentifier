using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ParticleDataVisualizer))]
public class DragRotate : MonoBehaviour
{
    private float _sensitivity;
    private Vector3 _mouseReference;
    private Vector3 _mouseOffset;
    private Vector3 _rotation;
    private bool _isRotating;

    void Start()
    {
        _sensitivity = 0.4f;
        _rotation = Vector3.zero;
    }

    void Update()
    {
        if (!_isRotating && Input.GetMouseButton(0))
        {
            _isRotating = true;
            _mouseReference = Input.mousePosition;
        }
        else if (_isRotating && !Input.GetMouseButton(0))
        {
            _isRotating = false;
        }
        if (_isRotating)
        {
            Vector3 center = GetComponent<ParticleDataVisualizer>().centroid;

            // offset
            _mouseOffset = _sensitivity * (Input.mousePosition - _mouseReference);
            transform.RotateAround(center, new Vector3(0, 1, 0), -_mouseOffset.x);
            transform.RotateAround(center, new Vector3(1, 0, 0), _mouseOffset.y);

            // store mouse
            _mouseReference = Input.mousePosition;
        }
    }

    void OnMouseDown()
    {
        // rotating flag
        _isRotating = true;

        // store mouse
        _mouseReference = Input.mousePosition;
    }

    void OnMouseUp()
    {
        // rotating flag
        _isRotating = false;
    }
}