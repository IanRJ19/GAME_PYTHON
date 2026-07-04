using System.Collections.Generic;
using UnityEngine;

// Single-shot overlap query fired on demand via PerformAttack() — mirrors the Godot
// Area2D "get_overlapping_areas() once per swing" pattern rather than continuous
// OnTriggerEnter2D, so combo steps can call it repeatedly with fresh hit-dedupe state.
public class MeleeHitbox : MonoBehaviour
{
    [SerializeField] private Vector2 size = new Vector2(0.5f, 0.45f);
    [SerializeField] private LayerMask targetMask;

    public int damage = 20;
    public float knockbackForce = 260f;

    private GameObject _owner;
    private IAttacker _attacker;
    private readonly HashSet<int> _alreadyHit = new HashSet<int>();

    public void Setup(GameObject owner)
    {
        _owner = owner;
        _attacker = owner.GetComponent<IAttacker>();
    }

    public void ResetHits()
    {
        _alreadyHit.Clear();
    }

    public void PerformAttack()
    {
        int outgoingDamage = damage;
        bool isCrit = false;
        if (_attacker != null)
        {
            AttackPayload payload = _attacker.GetAttackPayload();
            outgoingDamage = payload.damage;
            isCrit = payload.isCrit;
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, size, transform.eulerAngles.z, targetMask);
        foreach (Collider2D col in hits)
        {
            Hurtbox hurtbox = col.GetComponent<Hurtbox>();
            if (hurtbox == null) continue;

            int id = col.GetInstanceID();
            if (_alreadyHit.Contains(id)) continue;
            _alreadyHit.Add(id);

            Vector2 direction = _owner != null
                ? ((Vector2)col.transform.position - (Vector2)_owner.transform.position).normalized
                : Vector2.right;
            if (direction == Vector2.zero) direction = Vector2.right;

            hurtbox.ReceiveHit(outgoingDamage, _owner, direction * knockbackForce, isCrit);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }
}
