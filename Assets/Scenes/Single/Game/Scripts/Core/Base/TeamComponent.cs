using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TeamComponent : MonoBehaviour
{
    public string tankId = Guid.NewGuid().ToString();
    public TeamEnum team = TeamEnum.Neutral;

    [Tooltip("Ссылка на TankDefinition для этого танка")]
    public TankDefinition tankDefinition;

    public string DisplayName => tankDefinition != null ? tankDefinition.tankName : "Tank";
}
