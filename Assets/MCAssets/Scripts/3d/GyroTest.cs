using UnityEngine;

public class GyroTest : MonoBehaviour
{
    private bool gyroEnabled;
    private Gyroscope gyro;

    void Start()
    {
        gyroEnabled = EnableGyro();
    }

    private bool EnableGyro()
    {
        if (SystemInfo.supportsGyroscope)
        {
            gyro = Input.gyro;
            gyro.enabled = true;
            return true;
        }
        return false;
    }

    void Update()
    {
        if (gyroEnabled)
        {
            // Display gyro data
            Debug.Log("Gyro Attitude: " + gyro.attitude);
            Debug.Log("Gyro Rotation Rate: " + gyro.rotationRate);
        }
        else
        {
            Debug.LogWarning("Gyroscope not supported on this device.");
        }
    }
}
