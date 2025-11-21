using System;
using UnityEngine;

[DisallowMultipleComponent]
public class TeamComponent : MonoBehaviour
{
    public string tankId = Guid.NewGuid().ToString();
    public TeamEnum team = TeamEnum.Neutral;
    public string displayName = "Tank";
}
