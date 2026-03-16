using System;
using System.Collections;
using System.Collections.Generic; // Added for List
using Fabric;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI; // Added for Graphic

namespace Mix.GagManagement
{
	public class GagContainer : MonoBehaviour
	{
		private const float ANIMATED_FPS = 24f;

		public RuntimeAnimatorController AvatarAnimatorController;

		public GagObject[] GagObjects = new GagObject[0];

		public ParticleObject[] ParticleObjects = new ParticleObject[0];

		private float AvatarOffsetX;

		private float ObjectOffsetX;

		public bool IsAvatarAnim;

		private float ElapsedTime;

		private GameObject flipMesh;

		private Vector2[] unused;

		private bool isReceiverFlipped = true;

		private bool isGagPlaying;

		private bool isGagDone;

		private string eventName = string.Empty;

		private string senderAnimName;

		private string receiverAnimName;

		public GagHead Sender { get; private set; }

		public GagHead Receiver { get; private set; }

		private bool hasLoggedWaitingForAudio;
		private bool hasLoggedGagPlaying;
		private Coroutine shaderUpdateCoroutine; // Track the shader routine

		public void UpdateAvatarOffsetX()
		{
			float t;
			try
			{
				t = Sender.animator.GetFloat("senderOffset");
			}
			catch (Exception)
			{
				t = 0f;
			}
			float x = Mathf.Lerp(Sender.HeadStartPos.x, 0f - AvatarOffsetX, t);
			Sender.Head.transform.position = new Vector3(x, Sender.Head.transform.position.y, Sender.Head.transform.position.z);
			if (receiverAnimName != null)
			{
				try
				{
					t = Receiver.animator.GetFloat("receiverOffset");
				}
				catch (Exception)
				{
					t = 0f;
				}
				x = Mathf.Lerp(Receiver.HeadStartPos.x, AvatarOffsetX, t);
				Receiver.Head.transform.position = new Vector3(x, Receiver.Head.transform.position.y, Receiver.Head.transform.position.z);
			}
		}

		private void Update()
		{
			if (!isGagPlaying && Sender != null && Receiver != null)
			{
				AudioSource componentInChildren = base.gameObject.GetComponentInChildren<AudioSource>();
				if (componentInChildren != null && (double)componentInChildren.time > 0.001)
				{
					Debug.Log("[GagContainer] Audio started. Calling PlayLoadedAnimation().");
					PlayLoadedAnimation();
				}
				else if (!hasLoggedWaitingForAudio)
				{
					hasLoggedWaitingForAudio = true;
					Debug.Log("[GagContainer] Waiting for audio before playing loaded gag animation.");
				}
			}
			else if (isGagDone)
			{
				Debug.Log("[GagContainer] Gag marked done. Destroying gag via GagManager.");
				isGagDone = false;
				if (!MonoSingleton<GagManager>.Instance.IsNullOrDisposed())
				{
					MonoSingleton<GagManager>.Instance.DestroyGag();
				}
			}
		}

		private void LateUpdate()
		{
			if (Sender == null || !isGagPlaying)
			{
				return;
			}
			ElapsedTime += Time.deltaTime;
			UpdateAvatarOffsetX();
			int num = 2;
			if (receiverAnimName == null)
			{
				num--;
			}
			int num2 = 0;
			if (Sender.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= Sender.animator.GetCurrentAnimatorStateInfo(0).length)
			{
				num2++;
			}
			if (receiverAnimName != null && Receiver.animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= Receiver.animator.GetCurrentAnimatorStateInfo(0).length)
			{
				num2++;
			}
			Vector3 position = Sender.mainObject.transform.Find("grp_offset").position;
			GagObject[] gagObjects = GagObjects;
			foreach (GagObject gagObject in gagObjects)
			{
				num++;
				if (!gagObject.GagMesh.activeSelf)
				{
					num2++;
					continue;
				}
				float t = gagObject.GagMesh.GetComponent<Animator>().GetFloat("offset");
				if (gagObject.OffsetGroup.Length > 0)
				{
					OffsetData[] offsetGroup = gagObject.OffsetGroup;
					foreach (OffsetData offsetData in offsetGroup)
					{
						float x = Mathf.Lerp(offsetData.startPos.x, offsetData.startPos.x + ObjectOffsetX, t);
						offsetData.OffsetObject.transform.position = new Vector3(x, position.y, offsetData.OffsetObject.transform.position.z);
					}
				}
				else
				{
					Vector3 position2 = gagObject.GagMesh.transform.position;
					gagObject.GagMesh.transform.position = new Vector3(position2.x, position.y, position2.z);
				}
				if (gagObject.GagMesh.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("endState"))
				{
					num2++;
					gagObject.GagMesh.SetActive(false);
				}
			}
			if (eventName != string.Empty)
			{
				num++;
				if (!EventManager.Instance.IsEventActive(eventName, base.gameObject))
				{
					num2++;
				}
			}
			if (num == num2)
			{
				Debug.Log("[GagContainer] All gag animators/effects completed. Marking gag done.");
				isGagDone = true;
				return;
			}
			ParticleObject[] particleObjects = ParticleObjects;
			foreach (ParticleObject particleObject in particleObjects)
			{
				if (!particleObject.isPlaying)
				{
					if (ElapsedTime > (float)particleObject.Start / 24f)
					{
						particleObject.Play(Sender.worldPosition);
						particleObject.Flip(base.transform.localScale.x < 0f);
					}
				}
				else
				{
					float t2 = particleObject.EmitterRig.GetComponent<Animator>().GetFloat("offset");
					float x2 = Mathf.Lerp(particleObject.startPos.x, particleObject.startPos.x + ObjectOffsetX, t2);
					particleObject.EmitterRig.transform.position = new Vector3(x2, position.y, particleObject.EmitterRig.transform.position.z);
				}
			}
		}

		// --- SHADER PATCH LOGIC ---
		private IEnumerator ContinuouslyPatchShaders(GameObject targetInstance)
		{
			// Initial tiny wait
			yield return new WaitForSeconds(0.1f);

			Action scanAndReplace = () =>
			{
				if (targetInstance == null) return;
				
				Shader optimizedUIShader = Shader.Find("Custom/UI/OptimizedUIShader");
				if (optimizedUIShader != null)
				{
					// Patch UI Graphics
					Graphic[] uiGraphics = targetInstance.GetComponentsInChildren<Graphic>(true);
					int graphicCount = 0;
					foreach (Graphic graphic in uiGraphics)
					{
						if (graphic != null && graphic.material != null && graphic.material.shader != optimizedUIShader)
						{
							Material newMat = new Material(graphic.material); 
							newMat.shader = optimizedUIShader;
							graphic.material = newMat;
							graphicCount++;
						}
					}

					// Patch Renderers (Gags are heavily 3D mesh based)
					Renderer[] standardRenderers = targetInstance.GetComponentsInChildren<Renderer>(true);
					int rendererCount = 0;
					foreach (Renderer rend in standardRenderers)
					{
						if (rend != null && rend.sharedMaterials != null)
						{
							bool changed = false;
							Material[] newMaterials = new Material[rend.sharedMaterials.Length];
							for (int i = 0; i < rend.sharedMaterials.Length; i++)
							{
								if (rend.sharedMaterials[i] != null && rend.sharedMaterials[i].shader != optimizedUIShader)
								{
									// Keep existing Standard/complex shaders safe, force UI optimized on the rest
									if (!rend.sharedMaterials[i].name.Contains("Standard"))
									{
										Material repMat = new Material(rend.sharedMaterials[i]);
										repMat.shader = optimizedUIShader;
										newMaterials[i] = repMat;
										changed = true;
									}
									else
									{
										newMaterials[i] = rend.sharedMaterials[i];
									}
								}
								else
								{
									newMaterials[i] = rend.sharedMaterials[i];
								}
							}
							
							if (changed) 
							{
								rend.sharedMaterials = newMaterials;
								rendererCount++;
							}
						}
					}

					if (graphicCount > 0 || rendererCount > 0)
					{
						Debug.Log($"[GagContainer] Patched {graphicCount} UI Graphics, {rendererCount} Renderers on Gag object.");
					}
				}
			};

			// Initial run
			scanAndReplace();
			int lastChildCount = -1;

			while (targetInstance != null)
			{
				int currentChildCount = targetInstance.GetComponentsInChildren<Transform>(true).Length;

				// Repatch if the gag dynamically instantiates new particle effects or mesh updates
				if (currentChildCount != lastChildCount)
				{
					if (lastChildCount != -1) 
					{
						Debug.Log($"[GagContainer] Gag hierarchy changed ({lastChildCount} -> {currentChildCount}). Rerunning Shader Patch.");
					}
					lastChildCount = currentChildCount;
					scanAndReplace();
				}

				yield return new WaitForSeconds(0.5f);
			}
		}
		// --------------------------

		public void Play(GameObject aSenderHead, GameObject aReceiverHead, string aSenderAnimName, string aReceiverAnimName, bool aFlipReceiver)
		{
			Debug.Log("[GagContainer] Play called. senderAnim=" + aSenderAnimName + ", receiverAnim=" + aReceiverAnimName + ", flipReceiver=" + aFlipReceiver);

			if (MonoSingleton<GagManager>.Instance.IsNullOrDisposed() || MonoSingleton<NavigationManager>.Instance.IsNullOrDisposed() || Singleton<SoundManager>.Instance == null)
			{
				Debug.LogWarning("[GagContainer] Dependencies unavailable. Marking gag as done.");
				isGagPlaying = true;
				isGagDone = true;
				return;
			}

			// Start monitoring the shader for this gag
			if (shaderUpdateCoroutine != null) StopCoroutine(shaderUpdateCoroutine);
			shaderUpdateCoroutine = StartCoroutine(ContinuouslyPatchShaders(gameObject));

			hasLoggedWaitingForAudio = false;
			hasLoggedGagPlaying = false;

			AvatarOffsetX = MonoSingleton<GagManager>.Instance.GetAvatarOffset();
			ObjectOffsetX = MonoSingleton<GagManager>.Instance.GetObjectOffset();
			Sender = new GagHead(aSenderHead, AvatarAnimatorController);
			if (aReceiverAnimName != null)
			{
				Receiver = new GagHead(aReceiverHead, AvatarAnimatorController);
			}
			senderAnimName = aSenderAnimName;
			receiverAnimName = aReceiverAnimName;
			isReceiverFlipped = aFlipReceiver;
			EventTrigger component = base.gameObject.GetComponent<EventTrigger>();
			if (component != null)
			{
				ChatController lastProcessedRequestController = MonoSingleton<NavigationManager>.Instance.GetLastProcessedRequestController<ChatController>();
				if (lastProcessedRequestController != null)
				{
					Singleton<SoundManager>.Instance.StopAllAudioSourcesFromRoot(lastProcessedRequestController.transform, base.gameObject);
				}
				eventName = component._eventName;
				Debug.Log("[GagContainer] EventTrigger found. Waiting for event/audio. eventName=" + eventName);

				GagObject[] gagObjects = GagObjects;
				foreach (GagObject gagObject in gagObjects)
				{
					gagObject.GagMesh.SetActive(false);
				}
			}
			else
			{
				Debug.Log("[GagContainer] No EventTrigger. Playing loaded animation immediately.");
				PlayLoadedAnimation();
			}
		}

		public void PlayLoadedAnimation()
		{
			if (!hasLoggedGagPlaying)
			{
				hasLoggedGagPlaying = true;
				Debug.Log("[GagContainer] PlayLoadedAnimation started.");
			}

			isGagPlaying = true;
			Sender.animator.Play(senderAnimName);
			if (receiverAnimName != null)
			{
				Receiver.animator.Play(receiverAnimName);
			}
			base.transform.localScale *= MonoSingleton<GagManager>.Instance.GetAvatarScale();
			base.transform.position = Sender.worldPosition;
			base.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
			if (receiverAnimName != null)
			{
				flipMesh = Receiver.Head.transform.Find("cube_rig").gameObject;
				SwapUVs();
			}
			if (MonoSingleton<GagManager>.Instance.ActiveSenderPosition == GagManager.SenderPosition.RIGHT)
			{
				base.transform.localScale = new Vector3(0f - base.transform.localScale.x, base.transform.localScale.y, base.transform.localScale.z);
			}
			Vector3 position = Sender.mainObject.transform.Find("grp_offset").position;
			GagObject[] gagObjects = GagObjects;
			foreach (GagObject gagObject in gagObjects)
			{
				gagObject.GagMesh.SetActive(true);
				gagObject.play(Sender.worldPosition);
				OffsetData[] offsetGroup = gagObject.OffsetGroup;
				foreach (OffsetData offsetData in offsetGroup)
				{
					offsetData.SetStartPosition(position);
				}
			}
		}

		private void ResetReceiverUVs()
		{
			if (flipMesh != null)
			{
				SwapUVs();
				flipMesh = null;
			}
		}

		private void SwapUVs()
		{
			Debug.Log("[GagContainer] SwapUVs called. isReceiverFlipped=" + isReceiverFlipped);
			if (!isReceiverFlipped)
			{
				return;
			}
			flipMesh = Receiver.Head.transform.Find("cube_rig").gameObject;
			FlipObjectAtTransform("grp_offset/head");
			FlipObjectAtTransform("grp_offset/face_mouth");
			if (this.IsNullOrDisposed() || flipMesh.IsNullOrDisposed() || !(flipMesh != null))
			{
				return;
			}
			GameObject gameObject = flipMesh.gameObject;
			Transform transform = gameObject.transform.Find("grp_offset/def_front");
			if (transform != null && transform.childCount > 0)
			{
				Transform child = transform.GetChild(0);
				if (child != null)
				{
					child.localScale = new Vector3(child.localScale.x * -1f, 1f, 1f);
				}
			}
			Transform transform2 = gameObject.transform.Find("grp_offset/def_hatBase");
			if (transform2 != null && transform2.childCount > 0)
			{
				Transform child2 = transform2.GetChild(0);
				if (child2 != null)
				{
					child2.localScale = new Vector3(child2.localScale.x * -1f, 1f, 1f);
				}
			}
			Transform transform3 = gameObject.transform.Find("grp_offset/face_mouth");
			if (!(transform3 != null))
			{
				return;
			}
			GameObject gameObject2 = transform3.gameObject;
			if (!gameObject2.IsNullOrDisposed())
			{
				SkinnedMeshRenderer component = gameObject2.GetComponent<SkinnedMeshRenderer>();
				if (!component.IsNullOrDisposed())
				{
					float blendShapeWeight = component.GetBlendShapeWeight(0);
					component.SetBlendShapeWeight(0, 100f - blendShapeWeight);
				}
			}
		}

		private void FlipObjectAtTransform(string transformPath)
		{
			GameObject gameObject = flipMesh.transform.Find(transformPath).gameObject;
			Mesh sharedMesh = gameObject.GetComponent<SkinnedMeshRenderer>().sharedMesh;
			unused = sharedMesh.uv;
			sharedMesh.uv = sharedMesh.uv2;
			sharedMesh.uv2 = unused;
		}

		public void Destroy()
		{
			Debug.Log("[GagContainer] Destroy called. Cleaning gag resources.");
			
			if (shaderUpdateCoroutine != null) 
			{
				StopCoroutine(shaderUpdateCoroutine);
				shaderUpdateCoroutine = null;
			}

			AvatarAnimatorController = null;
			GagObject[] gagObjects = GagObjects;
			foreach (GagObject gagObject in gagObjects)
			{
				gagObject.Destroy();
			}
			GagObjects = null;
			ParticleObject[] particleObjects = ParticleObjects;
			foreach (ParticleObject particleObject in particleObjects)
			{
				particleObject.Destroy();
			}
			ParticleObjects = null;
			if (Sender != null)
			{
				Sender.Destroy();
				Sender = null;
			}
			if (receiverAnimName != null)
			{
				ResetReceiverUVs();
				Receiver.Destroy();
				Receiver = null;
			}
		}
	}
}
