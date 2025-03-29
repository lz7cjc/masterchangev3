using UnityEngine;

public class ResetCameraPosition : MonoBehaviour
{
  public Rigidbody player;  
 
    public void MovePlayerToTarget(GameObject target)
    {
        // Move player to the target position and reset transform
        player.transform.position = target.transform.position;
        player.transform.rotation = Quaternion.identity;
    }
}
