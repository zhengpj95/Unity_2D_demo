using UnityEngine;

namespace VampireSurvivorsLike {

  public class EnemyChasing : MonoBehaviour, IPoolable
  {
    [SerializeField] private float chaseSpeed = 0.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private DropItemType dropItemType;
    [SerializeField]
    [Tooltip("Probability of dropping an item when killed.")]
    private float dropItemProb = 0.5f;

    public int Damage { get => damage; }
    public DropItemType DropItemType { get => dropItemType; }
    public float DropItemProb { get => dropItemProb; }

    private Transform player;
    private Transform spriteTransform;
    private Rigidbody2D rb;
    private EnemyDirector director;

    private void OnDisable()
    {
      director?.UnregisterEnemy(this);
    }

    private void Awake()
    {
      spriteTransform = transform.Find("Sprite");
      rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Transform target, EnemyDirector owner)
    {
      player = target;
      director = owner;
      director?.RegisterEnemy(this);
    }

    private void FixedUpdate()
    {
      if (player == null || director == null || rb == null) return;

      // 敌人自己判断是否已落后过远，避免由 Director 每帧遍历全部敌人。
      if ((transform.position - player.position).sqrMagnitude > director.DespawnSqrDistance)
      {
        director.RecycleEnemy(gameObject);
        return;
      }

      Vector3 direction = (player.position - transform.position).normalized;
      Vector2 newPosition = transform.position + direction * chaseSpeed * Time.fixedDeltaTime;
      rb.MovePosition(newPosition);

      if (spriteTransform != null && player.position.x > transform.position.x)
      {
        spriteTransform.localScale = new Vector3(-1, 1, 1);
      }
      else if (spriteTransform != null)
      {
        spriteTransform.localScale = new Vector3(1, 1, 1);
      }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
      if (collision.gameObject.CompareTag("Player"))
      {
        var health = collision.gameObject.GetComponent<VSPlayerHealth>();
        if (health != null)
        {
          health.TakeDamage(damage);
        }
        director?.RecycleEnemy(gameObject);
      }
    }

    public void OnAlloc()
    {
      if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnFree()
    {
      if (rb != null) rb.velocity = Vector2.zero;
      director?.UnregisterEnemy(this);
    }
  }

}
