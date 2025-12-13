using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TeamComponent : MonoBehaviour
{
    public TeamEnum team = TeamEnum.Neutral;
    public TankDefinition tankDefinition;
    public string DisplayName => tankDefinition != null ? tankDefinition.tankName : "Tank";
    public int GetInstanceId() => GetInstanceID();
}