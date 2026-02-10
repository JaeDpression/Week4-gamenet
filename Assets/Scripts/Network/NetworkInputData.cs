using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 InputVector;
    public NetworkButtons Buttons;
    public Quaternion Rotation;
}
