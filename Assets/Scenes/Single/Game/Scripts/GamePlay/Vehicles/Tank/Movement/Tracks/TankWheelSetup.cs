using UnityEngine;

public static class TankWheelSetup
{
    public static void ApplyWheelDefaults(WheelCollider wheel, float tankMass)
    {
        if (wheel == null) return;

        float recommended = tankMass * 0.02f; 
        wheel.mass = Mathf.Clamp(recommended, 20f, 80f); 
        
        wheel.radius = 0.3f;
        wheel.wheelDampingRate = 1.5f;
        wheel.suspensionDistance = 0.3f;
        wheel.forceAppPointDistance = 0.35f; 
        wheel.center = new Vector3(0f, 0f, 0f);
        
        float massScale = Mathf.Clamp(tankMass / 1500f, 0.5f, 3f);
        JointSpring spring = wheel.suspensionSpring;
        spring.spring = 35000f * massScale;    
        spring.damper = 4500f * massScale;    
        spring.targetPosition = 0.5f;
        wheel.suspensionSpring = spring;
        
        WheelFrictionCurve fFriction = wheel.forwardFriction;
        fFriction.extremumSlip = 0.35f;
        fFriction.extremumValue = 1.25f;
        fFriction.asymptoteSlip = 0.9f;
        fFriction.asymptoteValue = 0.9f;
        fFriction.stiffness = 1.2f;
        wheel.forwardFriction = fFriction;
        
        WheelFrictionCurve sFriction = wheel.sidewaysFriction;
        sFriction.extremumSlip = 0.25f;
        sFriction.extremumValue = 1.15f;
        sFriction.asymptoteSlip = 0.55f;
        sFriction.asymptoteValue = 0.85f;
        sFriction.stiffness = 1.2f;
        wheel.sidewaysFriction = sFriction;
    }

    public static void ApplyToAllWheels(WheelCollider[] leftWheels, WheelCollider[] rightWheels, float tankMass)
    {
        if (leftWheels != null)
            foreach (var w in leftWheels) ApplyWheelDefaults(w, tankMass);
        if (rightWheels != null)
            foreach (var w in rightWheels) ApplyWheelDefaults(w, tankMass);
    }
}
