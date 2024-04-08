using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GatlingGun : MonoBehaviour
{
    Transform target;

    public Transform go_baseRotation;
    public Transform go_GunBody;
    public Transform go_barrel;

    public float barrelRotationSpeed;
    float currentRotationSpeed;

    public float firingRange;

    // Particle system for the muzzel flash
    public ParticleSystem muzzelFlash;

    bool canFire = false;
    private float timer;

    
    void Start()
    {
        this.GetComponent<SphereCollider>().radius = firingRange;
    }

    void Update()
    {
        AimAndFire();
        timer += Time.deltaTime;
        if (timer > 1f)
        {
            timer = 0f;
            ScanForTarget();
            if (target != null)
            {
                canFire = true;
            }
            else
            {
                canFire = false;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, firingRange);
    }


    void AimAndFire()
    {
        go_barrel.transform.Rotate(0, 0, currentRotationSpeed * Time.deltaTime);

        if (canFire)
        {
            currentRotationSpeed = barrelRotationSpeed;

            Vector3 baseTargetPostition = new Vector3(target.position.x, this.transform.position.y, target.position.z);
            Vector3 gunBodyTargetPostition = new Vector3(target.position.x, target.position.y, target.position.z);

            go_baseRotation.transform.LookAt(baseTargetPostition);
            go_GunBody.transform.LookAt(gunBodyTargetPostition);

            if (!muzzelFlash.isPlaying)
            {
                muzzelFlash.Play();
            }
        }
        else
        {
            currentRotationSpeed = Mathf.Lerp(currentRotationSpeed, 0, 10 * Time.deltaTime);

            if (muzzelFlash.isPlaying)
            {
                muzzelFlash.Stop();
            }
        }
    }
    private void ScanForTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, firingRange);
        float shortestDistance = Mathf.Infinity;
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<EnemyNavigation>(out var bot))
            {
                float distance = Vector3.Distance(transform.position, bot.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    target = bot.transform;
                }
            }
        }
    }
}