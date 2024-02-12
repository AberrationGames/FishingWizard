using UnityEngine;

namespace FishingWizard
{
    public class HelperFunctions
    {
        public static float ClampAngle(float a_angle, float a_min, float a_max)
        {
            if (a_angle < 90 || a_angle > 270)
            {
                if (a_angle > 180) 
                    a_angle -= 360;
                    
                if (a_max > 180) 
                    a_max -= 360;
                    
                if (a_min > 180) 
                    a_min -= 360;
            }
            a_angle = Mathf.Clamp(a_angle, a_min, a_max);
            if (a_angle < 0) 
                a_angle += 360;
            return a_angle;
        }
    }
}