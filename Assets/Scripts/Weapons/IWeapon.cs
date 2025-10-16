using UnityEngine;

namespace TD.Weapons
{
    public interface IWeapon
    {
        void Fire(Vector3 origin, Vector3 direction, Transform target, float damage);
        WeaponType WeaponType { get; }
    }
}
