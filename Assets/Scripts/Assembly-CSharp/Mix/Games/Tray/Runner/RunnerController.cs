using DG.Tweening;
using UnityEngine;

namespace Mix.Games.Tray.Runner
{
	public class RunnerController : AvatarController
	{
		public GameObject magnetCollider;

		public float jumpRecharge = 0.1f;

		private float jumpTimer;

		private bool isTapping;

		private GameObject lastChunkCheckpoint;

		private ChunkController lastChunkCheckpointController;

		public RunnerRecorder playerRecording = new RunnerRecorder();

		private float mInAirTime;

		private float mMinAirTimeForLandParticles = 0.2f;

		private void OnEnable()
		{
			ResetGroundCollisions();
		}

		protected override void Awake()
		{
			base.Awake();
			jumpTimer = jumpRecharge;
			SetRunParticle(false);
		}

		private void FixedUpdate()
		{
			if (mainController.mode == GameMode.PLAY || mainController.mode == GameMode.DAMAGE || mainController.mode == GameMode.END)
			{
				if (!isPausingFromSpawn)
				{
					Jump();
					Move();
					DecrementJumpTimer();
					DecrementDamageTimer();
					CheckCollisionFreeFall();
					ManageAcceleration();
				}
				else
				{
					DecrementSpawnPauseTimer();
				}
				isTapping = UnityEngine.InputSystem.Pointer.current != null && UnityEngine.InputSystem.Pointer.current.press.isPressed && mainController.IsInputInViewport();
				elapsedTicks++;
				elapsedPlayTime += Time.fixedDeltaTime;
			}
			if (groundCollisions == 0)
			{
				mInAirTime += Time.fixedDeltaTime;
			}
		}

		public void SwapNextCheckpoint()
		{
			if (lastChunkCheckpointController != null && lastChunkCheckpointController.nextChunk != null)
			{
				lastChunkCheckpoint = lastChunkCheckpointController.nextChunk;
			}
		}

		private void DecrementJumpTimer()
		{
			if (jumpTimer >= 0f)
			{
				jumpTimer -= Time.fixedDeltaTime;
			}
		}

		public Vector3 GetPosLastCheckpoint()
		{
			return lastChunkCheckpoint.transform.localPosition + lastChunkCheckpointController.RESPAWN_POSITION;
		}

		public GameObject GetCurrentChunk()
		{
			return lastChunkCheckpoint;
		}

		public void AddGroundedCount()
		{
			groundCollisions++;
			if (jumpTimer <= 0f && animMode == "jump" && mainController.mode == GameMode.PLAY)
			{
				SetShadow(true);
				animMode = "run";
				animatorController.SetTrigger("run");
				SetRunParticle(true);
				MainRunnerGame.PlaySound("PlayerLand", mainController.SOUND_PREFIX);
			}
			if (groundCollisions == 1 && mInAirTime > mMinAirTimeForLandParticles)
			{
				GameObject gameObject = (GameObject)Object.Instantiate(landParticle, base.transform.position, Quaternion.identity);
				gameObject.transform.parent = gamePrefab.transform;
			}
			mInAirTime = 0f;
		}

		public void SubtractGroundedCount()
		{
			groundCollisions--;
		}

		public void SetJump(bool onOrOff)
		{
			isTapping = onOrOff;
		}

		public override void Jump()
		{
			if (groundCollisions > 0 && jumpTimer < 0f && isTapping && mainController.mode == GameMode.PLAY)
			{
				SetShadow(false);
				rBody.linearVelocity = new Vector2(0f, playerGameplay.jumpForce);
				jumpTimer = jumpRecharge;
				animatorController.SetTrigger(GetRandomJumpTransition());
				animMode = "jump";
				RecordKey(GhostDataType.JUMP);
				mainController.HasThePlayerJumpedAtLeastOnce = true;
				GameObject gameObject = (GameObject)Object.Instantiate(jumpParticle, base.transform.position, Quaternion.identity);
				gameObject.transform.parent = gamePrefab.transform;
				gameObject.transform.position = base.transform.position;
				SetRunParticle(false);
				MainRunnerGame.PlaySound("PlayerJump", mainController.SOUND_PREFIX);
			}
		}

		public override void Damage()
		{
			if (!isDamaged)
			{
				damageTimer = playerGameplay.damageWait;
				isDamaged = true;
				SpawnDeathParticle();
				mainController.RemoveScore(10);
				mainController.SetDamagePause();
				animatorController.SetTrigger("roll");
				animMode = "damage";
				SetJump(false);
				mainController.cameraController.Shake();
				SpawnGravestone();
			}
		}

		public override void PausePlayer()
		{
			base.PausePlayer();
		}

		public override void UnPausePlayer()
		{
			base.UnPausePlayer();
		}

		private void ResetGroundCollisions()
		{
			groundCollisions = 0;
		}

		public override void Respawn()
		{
			base.Respawn();
			if (lastChunkCheckpoint != null)
			{
				base.transform.localPosition = lastChunkCheckpoint.transform.localPosition + lastChunkCheckpointController.RESPAWN_POSITION;
				RecordKey(GhostDataType.RESPAWN);
			}
		}

		public override void End()
		{
			base.End();
			SetJump(false);
			RecordKey(GhostDataType.END);
			DOVirtual.DelayedCall(1.5f, delegate
			{
				MainRunnerGame.PlaySound("PlayerFinishFanfare", mainController.SOUND_PREFIX);
			});
		}

		public override void HitObstacle()
		{
			base.HitObstacle();
			mainController.HitObstacle();
			RecordKey(GhostDataType.DIE);
		}

		public void SetMagnetCollider(bool state)
		{
			magnetCollider.SetActive(state);
		}

		public void SetCheckpoint(GameObject checkpoint)
		{
			if (!isDamaged)
			{
				lastChunkCheckpoint = checkpoint;
				lastChunkCheckpointController = checkpoint.GetComponent<ChunkController>();
				mainController.ResetChunkAttempts();
			}
		}

		public void RecordKey(GhostDataType type)
		{
			RunnerGhostData aData = new RunnerGhostData(type, elapsedTicks, base.transform.localPosition);
			playerRecording.AddData(aData);
		}
	}
}
