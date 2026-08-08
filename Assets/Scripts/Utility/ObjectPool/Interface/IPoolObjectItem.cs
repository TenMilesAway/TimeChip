using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolObjectItem
{
    void OnGetHandle();

    void OnPutHandle();
}