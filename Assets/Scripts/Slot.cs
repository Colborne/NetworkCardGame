using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Slot : NetworkBehaviour
{
    public int slotNumber;
    [SyncVar(hook = nameof(UpdateRot))] public bool rot;

    public void UpdateRot(bool oldRot, bool newRot)
    {
        rot = newRot;
    }
}
