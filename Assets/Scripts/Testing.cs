using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Testing : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            List<Vector2> paths = PathFinding.instance.CaculatePath(new(0, 0), new(50, 50));
        }
    }
}
