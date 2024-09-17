using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class GatlingGun : MonoBehaviour
{
    private NPCNavigation target;

    public Transform go_baseRotation;
    public Transform go_GunBody;
    public Transform go_barrel;

    public float barrelRotationSpeed;
    float currentRotationSpeed;

    public float firingRange;
    public ParticleSystem muzzelFlash;

    bool canFire = false;
    private float timer;

    public float fireRate = 0.1f;
    public float damage;

    private float damageTimer;

    
    void Start()
    {
        this.GetComponent<SphereCollider>().radius = firingRange;
    }

    void Update()
    {
        timer += Time.deltaTime;
        damageTimer += Time.deltaTime;
        AimAndFire();
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

            Vector3 baseTargetPostition = new Vector3(target.transform.position.x, this.transform.position.y, target.transform.position.z);
            Vector3 gunBodyTargetPostition = new Vector3(target.transform.position.x, target.transform.position.y, target.transform.position.z);

            go_baseRotation.transform.LookAt(baseTargetPostition);
            go_GunBody.transform.LookAt(gunBodyTargetPostition);

            if (!muzzelFlash.isPlaying)
            {
                muzzelFlash.Play();
            }
            if (damageTimer > fireRate)
            {
                target.GetComponent<StatsHandler>().TakeDamage(damage);
                damageTimer = 0;
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
        NPCNavigation currentTarget = null;
        foreach (Collider collider in colliders)
        {
            if (collider.TryGetComponent<NPCNavigation>(out var enemy))
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    currentTarget = enemy;
                }
            }
        }
        if (currentTarget != null && currentTarget != target)
        {
            currentTarget.GetComponent<StatsHandler>().OnDeath += CheckTarget;
            if(target != null)
            {
                target.GetComponent<StatsHandler>().OnDeath -= CheckTarget;
            }
            target = currentTarget;
        }
    }
    private void CheckTarget()
    {
        target.GetComponent<StatsHandler>().OnDeath -= CheckTarget;
        target = null;
        canFire = false;
    }
    private void OnDestroy()
    {

    }

    public void BuildTower()
    {
        GameObject buildPoint = GameObject.FindGameObjectWithTag("BuildingPoint");
        Instantiate(gameObject,buildPoint.transform.position,Quaternion.identity);
    }
}