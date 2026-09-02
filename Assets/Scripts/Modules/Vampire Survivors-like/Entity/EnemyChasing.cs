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

    private void OnEnable()
    {
      if (EnemySpawnManager.Instance != null)
      {
        EnemySpawnManager.Instance.RegisterEnemy(this);
      }
    }

    private void OnDisable()
    {
      if (EnemySpawnManager.Instance != null)
      {
        EnemySpawnManager.Instance.UnregisterEnemy(this);
      }
    }

    private void Awake()
    {
      spriteTransform = transform.Find("Sprite");
      rb = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
      ResolvePlayer();
    }

    private void FixedUpdate()
    {
      if (!ResolvePlayer() || rb == null) return;

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
        EnemySpawnManager.Instance.RecycleEnemy(gameObject);
      }
    }

    public void OnAlloc()
    {
      ResolvePlayer();
      if (rb != null) rb.velocity = Vector2.zero;
    }

    public void OnFree()
    {
      if (rb != null) rb.velocity = Vector2.zero;
    }

    private bool ResolvePlayer()
    {
      if (player != null) return true;

      GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
      if (playerObject != null) player = playerObject.transform;
      return player != null;
    }
  }

}
