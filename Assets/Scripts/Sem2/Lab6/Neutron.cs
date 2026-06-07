using UnityEngine;

public class Neutron : MonoBehaviour
{
    private NuclearReactor reactor;
    private Vector3 velocity;
    private float lifetime;
    private float spawnTime;
    
    public void Init(NuclearReactor _reactor, Vector3 _velocity, float _lifetime)
    {
        reactor = _reactor;
        velocity = _velocity;
        lifetime = _lifetime;
        spawnTime = Time.time;
        
        // 3D коллайдер
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null) col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 0.2f;
        
        // Физика
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        
        // Визуализация (маленькая сфера)
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        mf.mesh = CreateSphereMesh();
        
        mr.material = new Material(Shader.Find("Standard"));
        mr.material.color = Color.cyan;
        
        transform.localScale = Vector3.one * 0.25f;
    }
    
    Mesh CreateSphereMesh()
    {
        return GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
    }
    
    void Update()
    {
        transform.Translate(velocity * Time.deltaTime);
        
        if (Time.time - spawnTime > lifetime)
        {
            if (reactor != null)
                reactor.RemoveNeutron(gameObject);
        }
        
        // Границы экрана (опционально)
        if (Mathf.Abs(transform.position.x) > 15f || Mathf.Abs(transform.position.z) > 15f)
        {
            reactor.RemoveNeutron(gameObject);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Absorber"))
        {
            if (reactor != null)
                reactor.OnNeutronHitAbsorber(gameObject);
        }
        else if (other.CompareTag("Uranium"))
        {
            if (reactor != null)
                reactor.OnNeutronHitNucleus(gameObject, other.gameObject);
        }
    }
}