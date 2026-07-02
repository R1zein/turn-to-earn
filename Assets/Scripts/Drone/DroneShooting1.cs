using UnityEngine;

public class DroneShooting : MonoBehaviour
{
    public Transform spr;
    public Transform spl;
    public float interval;
    public float damage;
    public HovlLaser laserLeft;
    public HovlLaser laserRight;

    public void Shoot()
    {
        if (Input.GetMouseButtonDown(0))
        {
            laserLeft.EnablePrepare();
            laserRight.EnablePrepare();
        }

        if (Input.GetMouseButtonUp(0))
        {
            laserLeft.DisablePrepare();
            laserRight.DisablePrepare();
        }
    }
}