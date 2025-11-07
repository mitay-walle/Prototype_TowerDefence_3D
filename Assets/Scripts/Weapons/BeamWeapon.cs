using TD.Monsters;
using UnityEngine;

namespace TD.Weapons
{
    public class BeamWeapon : MonoBehaviour, IWeapon
    {
        private const string TOOLTIP_BEAM_DURATION = "How long the beam stays active";
        private const string TOOLTIP_DAMAGE_INTERVAL = "Time between damage ticks while beam is active";
        private const string TOOLTIP_PARTICLE_SYSTEM = "Particle system for beam visual (use particles.collision for hits)";

        [Tooltip(TOOLTIP_BEAM_DURATION)]
        [SerializeField] private float beamDuration = 1f;
        [Tooltip(TOOLTIP_DAMAGE_INTERVAL)]
        [SerializeField] private float damageInterval = 0.1f;
        [Tooltip(TOOLTIP_PARTICLE_SYSTEM)]
        [SerializeField] private ParticleSystem beamParticles;

        [SerializeField] private bool Logs = false;

        private float beamEndTime;
        private float nextDamageTime;
        private float damagePerTick;
        private bool isBeamActive;

        public WeaponType WeaponType => WeaponType.Beam;

        private void Awake()
        {
            if (beamParticles != null)
            {
                var collision = beamParticles.collision;
                collision.enabled = true;
                collision.type = ParticleSystemCollisionType.World;
                collision.mode = ParticleSystemCollisionMode.Collision3D;
                collision.sendCollisionMessages = true;
            }
        }

        private void Update()
        {
            if (isBeamActive && Time.time >= beamEndTime)
            {
                StopBeam();
            }
        }

        public void Fire(Vector3 origin, Vector3 direction, Transform target, float damage)
        {
            damagePerTick = damage * damageInterval;
            beamEndTime = Time.time + beamDuration;
            nextDamageTime = Time.time;
            isBeamActive = true;

            if (beamParticles != null)
            {
                beamParticles.transform.rotation = Quaternion.LookRotation(direction);
                beamParticles.Play();
            }

            if (Logs) Debug.Log($"[BeamWeapon] Started beam for {beamDuration}s, {damagePerTick:F1} damage per tick");
        }

        private void OnParticleCollision(GameObject other)
        {
            if (!isBeamActive) return;

            if (Time.time < nextDamageTime) return;

            var health = other.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.TakeDamage(damagePerTick);
                nextDamageTime = Time.time + damageInterval;

                if (Logs) Debug.Log($"[BeamWeapon] Beam hit {other.name} for {damagePerTick:F1} damage");
            }
        }

        private void StopBeam()
        {
            isBeamActive = false;

            if (beamParticles != null)
            {
                beamParticles.Stop();
            }

            if (Logs) Debug.Log("[BeamWeapon] Beam stopped");
        }

        private void OnDisable()
        {
            StopBeam();
        }
    }
}
