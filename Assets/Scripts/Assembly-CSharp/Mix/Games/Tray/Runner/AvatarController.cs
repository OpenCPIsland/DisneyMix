using System.Collections;
using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	public class AvatarController : MonoBehaviour
	{
		protected PlayerGameplay playerGameplay;

		private float runSpeed = 1f;

		private float targetSpeed = 15f;

		protected Rigidbody2D rBody;

		public PolygonCollider2D playerCollider;

		public BoxCollider2D groundCollider;

		public BoxCollider2D deathCollider;

		protected int groundCollisions;

		public GameObject avatarMesh;

		public GameObject avatarShadow;

		protected Animator animatorController;

		protected string animMode = "run";

		protected GameObject gamePrefab;

		protected GameObject main;

		protected MainRunnerGame mainController;

		public GameObject cubeMesh;

		private string swid;

		protected float damageTimer;

		public bool isDamaged;

		public GameObject deathParticle;

		public GameObject deathDropParticle;

		protected bool isCollisionFree;

		public float minYForDropParticles = -1.5f;

		[Range(0f, 10f)]
		public float deathParticlesCameraOffset;

		public float elapsedPlayTime;

		public int elapsedTicks;

		protected float respawnTimer;

		protected bool isPausingFromSpawn;

		public GameObject jumpParticle;

		public ParticleSystem runParticle;

		public GameObject landParticle;

		public GameObject gravestone;

		private Vector2 velocity;

		protected virtual void Awake()
		{
			rBody = GetComponent<Rigidbody2D>();
			animatorController = avatarMesh.GetComponent<Animator>();
			if (rBody == null)
			{
				Debug.LogWarning("No rigidbody attached to player");
				return;
			}
			if (animatorController == null)
			{
				Debug.LogWarning("No animator attached to player");
				return;
			}
			main = GameObject.Find("main");
			if (main != null)
			{
				mainController = main.GetComponent<MainRunnerGame>();
				playerGameplay = main.GetComponent<PlayerGameplay>();
				if (mainController != null)
				{
					gamePrefab = mainController.gamePrefab;
					rBody.gravityScale = playerGameplay.gravity;
					targetSpeed = playerGameplay.maxSpeed;
					runSpeed = 0f;
				}
				else
				{
					Debug.LogWarning("Could not find MainRunnerGame on main");
				}
			}
			else
			{
				Debug.LogWarning("Could not find main GameObject from RunnerController");
			}
		}

		private void Start()
		{
			ApplySkin();
		}

		public virtual void Jump()
		{
			rBody.linearVelocity = new Vector2(0f, playerGameplay.jumpForce);
			if (mainController.mode == GameMode.PLAY)
			{
				animatorController.SetTrigger(GetRandomJumpTransition());
				animMode = "jump";
			}
			GameObject gameObject = (GameObject)Object.Instantiate(jumpParticle, base.transform.position, Quaternion.identity);
			gameObject.transform.parent = gamePrefab.transform;
		}

		public void Move()
		{
			rBody.linearVelocity = new Vector2(runSpeed, rBody.linearVelocity.y);
		}

		public void Launch(float force)
		{
			rBody.linearVelocity = new Vector2(0f, force);
			animMode = "jump";
		}

		public virtual void End()
		{
			SetTargetSpeed(0f);
			animatorController.Play("Dance");
		}

		public virtual void Damage()
		{
			if (!isDamaged)
			{
				damageTimer = playerGameplay.GetTotalDamageWait();
				isDamaged = true;
				SpawnDeathParticle();
				animatorController.SetTrigger("roll");
				animMode = "damage";
				SpawnGravestone();
			}
		}

		protected void SpawnDeathParticle()
		{
			GameObject gameObject = ((!(base.transform.position.y > minYForDropParticles)) ? Object.Instantiate(deathDropParticle) : Object.Instantiate(deathParticle));
			gameObject.transform.parent = gamePrefab.transform;
			Vector3 vector = (mainController.GameController.MixGameCamera.transform.position - base.transform.position).normalized * deathParticlesCameraOffset;
			gameObject.transform.position = base.transform.position + vector;
			ParticleSystem componentInChildren = gameObject.GetComponentInChildren<ParticleSystem>();
			StartCoroutine(RemoveParticleSystemWhenFinished(componentInChildren));
		}

		public void SpawnGravestone()
		{
		}

		public virtual void Respawn()
		{
			rBody.linearVelocity = Vector2.zero;
			isPausingFromSpawn = true;
			respawnTimer = playerGameplay.respawnWait;
			SetAllColliders(true);
		}

		public virtual void PausePlayer()
		{
			if (rBody != null)
			{
				rBody.bodyType = RigidbodyType2D.Kinematic;
			}
			if (animatorController != null)
			{
				animatorController.speed = 0f;
			}
			velocity = rBody.linearVelocity;
		}

		public virtual void UnPausePlayer()
		{
			rBody.bodyType = RigidbodyType2D.Dynamic;
			animatorController.speed = 1f;
			rBody.linearVelocity = velocity;
		}

		public void PlayRun()
		{
			ResetTicks();
			animatorController.Play("Run");
		}

		protected string GetRandomJumpTransition()
		{
			int num = Random.Range(0, playerGameplay.jumpTransitions.Length);
			return playerGameplay.jumpTransitions[num];
		}

		protected void DecrementDamageTimer()
		{
			if (damageTimer >= 0f)
			{
				damageTimer -= Time.fixedDeltaTime;
			}
			else if (isDamaged)
			{
				isDamaged = false;
				animMode = "run";
			}
		}

		protected void DecrementSpawnPauseTimer()
		{
			if (isPausingFromSpawn)
			{
				respawnTimer -= Time.fixedDeltaTime;
				if (respawnTimer < 0f)
				{
					isPausingFromSpawn = false;
					SetTargetSpeed(playerGameplay.maxSpeed);
					SetCurrentSpeed(0f);
				}
			}
		}

		protected void CheckCollisionFreeFall()
		{
			if (isCollisionFree && base.transform.localPosition.y < playerGameplay.collisionResetHeight)
			{
				isCollisionFree = false;
				SetAllColliders(true);
			}
		}

		protected void ManageAcceleration()
		{
			if (runSpeed != targetSpeed)
			{
				if (Mathf.Abs(runSpeed - targetSpeed) < playerGameplay.ACCEL_THRESHOLD)
				{
					runSpeed = targetSpeed;
				}
				else if (runSpeed < targetSpeed)
				{
					runSpeed += playerGameplay.acceleration * Time.fixedDeltaTime;
				}
				else
				{
					runSpeed -= playerGameplay.acceleration * Time.fixedDeltaTime;
				}
			}
		}

		public void SetRunParticle(bool val)
		{
			if (val)
			{
				runParticle.Play(true);
			}
			else
			{
				runParticle.Stop(true);
			}
		}

		public void ToggleColidersOnHeight()
		{
			SetAllColliders(false);
			groundCollisions = 0;
			isCollisionFree = true;
		}

		public virtual void HitObstacle()
		{
			Launch(playerGameplay.damageLaunchForce);
			ToggleColidersOnHeight();
			Damage();
		}

		public void SetTargetSpeed(float aTargetSpeed)
		{
			targetSpeed = aTargetSpeed;
		}

		public void SetCurrentSpeed(float aCurrentSpeed)
		{
			runSpeed = aCurrentSpeed;
		}

		protected void SetAllColliders(bool state)
		{
			playerCollider.enabled = state;
			groundCollider.enabled = state;
			deathCollider.enabled = state;
		}

		public void ApplySkin()
		{
			mainController.GameController.LoadFriend(cubeMesh, swid);
		}

		public void SetSwid(string aSwid)
		{
			swid = aSwid;
		}

		public string GetSwid()
		{
			return swid;
		}

		protected IEnumerator RemoveParticleSystemWhenFinished(ParticleSystem particles)
		{
			if (particles != null)
			{
				while (particles.IsAlive())
				{
					yield return null;
				}
				Object.Destroy(particles.gameObject);
			}
		}

		public void ResetTicks()
		{
			elapsedTicks = 0;
		}

		public void SetShadow(bool displayShadow)
		{
			avatarShadow.SetActive(displayShadow);
		}
	}
}
