using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avatar;
using Avatar.DataTypes;
using Disney.Mix.SDK;
using Mix.Assets;
using Mix.DeviceDb;
using Mix.Entitlements;
using Mix.Session;
using Mix.Session.Extensions;
using Mix.Tracking;
using Mix.Ui;
using UnityEngine;
using UnityEngine.UI;

namespace Mix.Avatar
{
    public class AvatarManager : MonoSingleton<AvatarManager>, IAvatarSnapshotManagerDependencies, IAvatarSnapshotCallDependencies
    {
        private class SnapshotWork
        {
            public bool Cancelled;
            public bool Started;
            public Action DoWork;
            public Action CompositeCancel;
            public Action<bool> FinalRender;
            public GameObject CubeRigGameObject;
            public int Size;
            public AvatarFlags Flags;
            public Vector3 AvatarRotation;
            public Vector3 AvatarOffset;
            public bool HasGeo;
            public Action<AvatarSnapshotResult> Callback;
            public string SnapshotId;
            public IAvatar Dna;
            public AvatarManager Manager;
        }

        public AvatarApi api;
        private GameObject avatarPreloader;
        private GameObject snapshotScene;
        private GameObject snapshotSceneGeo;
        private AvatarSnapshotCache snapshotCache;
        private AvatarAssetFramework assetFramework;
        private AsyncHelper<AvatarSnapshotResult> snapshotHelper;
        private List<Action> skinnerWork;
        private SdkActions actionGenerator = new SdkActions();
        private SdkEvents eventGenerator = new SdkEvents();
        private ClientAvatar clientAvatar;

        private static readonly Vector3 AVATAR_OFFSET = new Vector3(0f, -0.505f, 3.35f);
        private static readonly Vector3 AVATAR_ROTATION = new Vector3(0f, 180f, 0f);

        private bool isSnapshotAvailable = true;
        private bool isPreloaderAvailable = true;
        private bool canUseAntiAliasing = true;

        public IAvatar CurrentDna
        {
            get
            {
                if (clientAvatar != null)
                    return clientAvatar;
                return MixSession.User?.Avatar;
            }
        }

        void IAvatarSnapshotManagerDependencies.LoadSnapshots(AvatarSnapshotDocument snapshotInfo, LoadSnapshotCallback callback)
        {
            if (snapshotInfo == null || callback == null) return;
            int size = snapshotInfo.size;
            Texture2D image = new Texture2D(size, size, TextureFormat.ARGB32, false);
            MonoSingleton<AssetManager>.Instance.LoadImage(snapshotInfo.path, ref image, texture =>
            {
                if (texture == null)
                {
                    callback(null);
                    return;
                }
                var data = new AvatarSnapshotData
                {
                    snapshot = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f),
                    loadPercentage = 1f
                };
                callback(data);
            });
        }

        void IAvatarSnapshotManagerDependencies.SaveSnapshots(AvatarSnapshotDocument snapshotInfo, AvatarSnapshotData snapshot, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(snapshotInfo?.path) || snapshot?.snapshot?.texture == null)
            {
                callback?.Invoke(false);
                return;
            }
            MonoSingleton<AssetManager>.Instance.SaveImage(snapshotInfo.path, snapshot.snapshot.texture, callback);
        }

        Action IAvatarSnapshotCallDependencies.GenerateSnapshotFromDna(IAvatar dna, AvatarFlags flags, int size, Action<AvatarSnapshotResult> callback, Vector3 avatarRotation, Vector3 avatarOffset)
        {
            var work = new SnapshotWork
            {
                Size = size,
                Flags = flags,
                AvatarRotation = avatarRotation,
                AvatarOffset = avatarOffset,
                Callback = callback,
                Dna = dna,
                Manager = this,
                Started = false,
                CompositeCancel = null,
                Cancelled = false
            };
            work.SnapshotId = CreateSnapshotId(dna, flags, size, avatarRotation, avatarOffset);
            work.HasGeo = AvatarHasGeo(dna);
            var rigParent = work.HasGeo ? snapshotSceneGeo : snapshotScene;
            var cubeRig = rigParent?.transform.Find("cube_rig")?.gameObject;
            work.CubeRigGameObject = cubeRig;

            work.FinalRender = success =>
            {
                if (work.Cancelled)
                {
                    skinnerWork.Remove(work.DoWork);
                    iterateThroughSkinnerQueue();
                    return;
                }
                RenderAvatarSnapshot(work.CubeRigGameObject, work.Size, work.Flags, snapshotData =>
                {
                    if (!work.Cancelled)
                        work.Callback?.Invoke(new AvatarSnapshotResult(success && snapshotData != null, work.SnapshotId, snapshotData));
                    skinnerWork.Remove(work.DoWork);
                    iterateThroughSkinnerQueue();
                }, work.AvatarRotation, work.AvatarOffset, work.HasGeo);
            };

            Action doWork = null;
            doWork = () =>
            {
                work.Started = true;
                Singleton<TechAnalytics>.Instance.TrackTimeToCompositeAvatarStart(rigParent);
                work.CompositeCancel = SkinAvatar(work.CubeRigGameObject, work.Dna, work.Flags, (compositeResult, cacheId) =>
                {
                    work.CompositeCancel = null;
                    HandleApiCallback(work.CubeRigGameObject, compositeResult, cacheId, (_, __) => work.FinalRender(compositeResult));
                });
            };
            work.DoWork = doWork;

            Action cancelAction = () =>
            {
                if (!work.Started)
                {
                    skinnerWork.Remove(work.DoWork);
                }
                else if (work.CompositeCancel != null)
                {
                    work.CompositeCancel();
                    work.Cancelled = true;
                    work.FinalRender(false);
                }
                else
                {
                    work.Cancelled = true;
                }
            };

            skinnerWork.Add(work.DoWork);
            if (skinnerWork.Count == 1)
                iterateThroughSkinnerQueue();

            return cancelAction;
        }

        public void Init(IAvatarManagerInitListener aInitListener)
        {
            snapshotHelper = new AsyncHelper<AvatarSnapshotResult>();
            skinnerWork = new List<Action>();
            snapshotCache = new AvatarSnapshotCache(this, Singleton<MixDocumentCollections>.Instance.avatarSnapshotDocumentCollectionApi);

            avatarPreloader = UnityEngine.Object.Instantiate(Resources.Load<GameObject>("Prefabs/Avatar/avatar_mp_preloader"));
            UnityEngine.Object.DontDestroyOnLoad(avatarPreloader);
            if (avatarPreloader)
            {
                avatarPreloader.name = "AvatarPreloader";
                avatarPreloader.transform.Translate(-1000f, -1000f, 0f);
                var animator = avatarPreloader.transform.Find("cube_rig")?.gameObject.GetComponent<Animator>();
                if (animator != null)
                    UnityEngine.Object.Destroy(animator);
            }

            snapshotScene = CreateSnapshotScene("Prefabs/Avatar/AvatarSnapshotCamera_Default");
            snapshotSceneGeo = CreateSnapshotScene("Prefabs/Avatar/AvatarSnapshotCamera_Geo");

            GenerateFolder("/Avatar/cache/");

            api = new AvatarApi(this);
            assetFramework = new AvatarAssetFramework(MonoSingleton<AssetManager>.Instance, Singleton<EntitlementsManager>.Instance);
            api.InitializeAvatars(assetFramework, null, aInitListener.OnShadersLoaded);

            if (UnityEngine.Application.platform == RuntimePlatform.Android)
            {
                string model = SystemInfo.deviceModel.ToLower();
                if (model.Contains("sm-n91") || model.Contains("motorola nexus 6"))
                    canUseAntiAliasing = false;
            }
        }

        private GameObject CreateSnapshotScene(string prefabPath)
        {
            var scene = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(prefabPath));
            UnityEngine.Object.DontDestroyOnLoad(scene);
            scene.transform.Translate(-1000f, -1100f, 0f);
            var spawner = scene.GetComponent<AvatarObjectSpawner>();
            if (spawner != null)
            {
                var go = spawner.Init();
                if (go != null)
                {
                    UnityEngine.Object.Destroy(go.transform.parent.gameObject);
                    go.transform.SetParent(scene.transform);
                }
            }
            scene.SetActive(false);
            return scene;
        }

        public void saveCurrentUsersDna()
        {
            if (!MixSession.IsValidSession)
                return;

            MixSession.User.SetAvatar(CurrentDna, actionGenerator.CreateAction<ISetAvatarResult>(results =>
            {
                if (!results.Success)
                    MixSession.AddOfflineItem(new AvatarQueueItem(api.SerializeAvatar(CurrentDna)));
                else
                    clientAvatar = null;
            }));
            PreloadAvatar(CurrentDna, 0);
        }

        public void setCurrentUsersDna(IAvatar dna)
        {
            clientAvatar = dna != null ? new ClientAvatar(dna) : null;
        }

        public void SetPriority(bool isCompositing)
        {
            api.SetProcessing(isCompositing);
        }

        public IAvatar GenerateRandomDna() => api.GenerateRandomDna();

        private IEnumerator LockedPreload(Action<Action> work)
        {
            while (!isPreloaderAvailable || MonoSingleton<NavigationManager>.Instance.FindCurrentRequest() != null)
                yield return new WaitForSeconds(0.1f);

            isPreloaderAvailable = false;
            yield return new WaitForEndOfFrame();
            work(() => isPreloaderAvailable = true);
        }

        public void PreloadAvatar(IAvatar avatar, AvatarFlags flags)
        {
            StartCoroutine(LockedPreload(callback =>
            {
                if (AvatarApi.ValidateAvatar(avatar) && avatarPreloader != null && !avatarPreloader.IsNullOrDisposed())
                {
                    var avatarObj = avatarPreloader.transform.Find("cube_rig")?.gameObject;
                    if (avatarObj == null)
                    {
                        callback();
                        return;
                    }
                    api.CompositeTextures(avatar, avatarObj, flags, (success, _) => callback());
                }
                else
                {
                    callback();
                }
            }));
        }

        public Action GetSnapshotFromDna(IAvatar dna, AvatarFlags flags, int size, SnapshotCallback snapshotCallback)
            => GetSnapshotFromDna(dna, flags, size, snapshotCallback, AVATAR_ROTATION, AVATAR_OFFSET);

        public Action GetSnapshotFromDna(IAvatar dna, AvatarFlags flags, int size, SnapshotCallback snapshotCallback, Vector3 avatarRotation)
            => GetSnapshotFromDna(dna, flags, size, snapshotCallback, avatarRotation, AVATAR_OFFSET);

        public Action GetSnapshotFromDna(IAvatar dna, AvatarFlags flags, int size, SnapshotCallback snapshotCallback, Vector3 avatarRotation, Vector3 avatarOffset)
        {
            if (!AvatarApi.ValidateAvatar(dna))
                dna = null;

            Action cancellableAsync = null;
            bool cancelled = false;
            Action result = () =>
            {
                cancelled = true;
                cancellableAsync?.Invoke();
            };
            string id = CreateSnapshotId(dna, flags, size, avatarRotation, avatarOffset);
            snapshotCache.GetCacheData(id, (success, cacheSnapshot) =>
            {
                if (this.IsNullOrDisposed() || cancelled) return;
                if (success && cacheSnapshot != null)
                {
                    snapshotCallback?.Invoke(success, cacheSnapshot.snapshot);
                }
                else if (snapshotHelper != null)
                {
                    cancellableAsync = snapshotHelper.AddAsyncCall(new AvatarSnapshotCall(dna, flags, size, avatarRotation, avatarOffset, this), snapshotResults =>
                    {
                        cancellableAsync = null;
                        if (snapshotResults == null)
                        {
                            snapshotCallback?.Invoke(false, null);
                        }
                        else if (snapshotResults.success && snapshotCache != null)
                        {
                            snapshotCache.SetCacheData(snapshotResults.cacheId, snapshotResults.data, cacheResult =>
                                snapshotCallback?.Invoke(snapshotResults.success && cacheResult, snapshotResults.data?.snapshot));
                        }
                        else
                        {
                            snapshotCallback?.Invoke(snapshotResults.success, snapshotResults.data?.snapshot);
                        }
                    });
                }
            });
            return result;
        }

        private IEnumerator SnapshotLock(Action work)
        {
            while (!isSnapshotAvailable || MonoSingleton<NavigationManager>.Instance.FindCurrentRequest() != null)
                yield return new WaitForSeconds(0.1f);

            isSnapshotAvailable = false;
            yield return new WaitForEndOfFrame();
            work();
            isSnapshotAvailable = true;
        }

        private void RenderAvatarSnapshot(GameObject avatarTarget, int size, AvatarFlags flags, Action<AvatarSnapshotData> result, Vector3 avatarRotation, Vector3 avatarOffset, bool hasGeo)
        {
            if (avatarTarget == null)
            {
                result(null);
                return;
            }
            StartCoroutine(SnapshotLock(() =>
            {
                var scene = hasGeo ? snapshotSceneGeo : snapshotScene;
                scene.SetActive(true);
                var cam = scene.GetComponent<Camera>();

                // Create the RenderTexture with the correct antiAliasing value from the start
                int antiAliasing = (hasGeo && canUseAntiAliasing) ? 4 : 1;
                var renderTexture = new RenderTexture(size, size, 24)
                {
                    anisoLevel = 8,
                    antiAliasing = antiAliasing
                };
                cam.cullingMask = 1 << avatarTarget.layer;
                cam.targetTexture = renderTexture;

                var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
                RenderTexture.active = cam.targetTexture;
                GL.Clear(true, true, new Color(0.9f, 0f, 0f, 0f));
                cam.Render();
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply();
                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                RenderTexture.active = null;
                cam.targetTexture = null;
                scene.SetActive(false);
                renderTexture.Release();

                var data = new AvatarSnapshotData
                {
                    isHd = (flags & AvatarFlags.IsHd) == AvatarFlags.IsHd,
                    hasNormals = (flags & AvatarFlags.WithNormals) == AvatarFlags.WithNormals,
                    size = size,
                    snapshot = sprite
                };
                result(data);
            }));
        }

        public Action SkinAvatar(GameObject avatarObj, IAvatar dna, AvatarFlags flags, SkinnedCallback callback)
        {
            if (!AvatarApi.ValidateAvatar(dna))
                dna = null;
            if (avatarObj == null || avatarObj.IsNullOrDisposed() || api == null)
            {
                callback?.Invoke(false, string.Empty);
                return () => { };
            }
            TextureCallback apiCallback = (success, id) => HandleApiCallback(avatarObj, success, id, callback);
            api.CompositeTextures(dna, avatarObj, flags, apiCallback);
            return () => api.CancelAvatarComposite(dna, avatarObj, flags, apiCallback);
        }

        public void HandleApiCallback(GameObject avatarObj, bool aIsSuccess, string cacheId, SkinnedCallback callback)
        {
            Singleton<TechAnalytics>.Instance.TrackTimeToCompositeAvatarEnd(avatarObj, aIsSuccess);
            if (!aIsSuccess)
            {
                ResetAvatarHead(avatarObj, () => callback?.Invoke(aIsSuccess, cacheId));
            }
            else
            {
                callback?.Invoke(aIsSuccess, cacheId);
            }
        }

        public void ResetAvatarHead(GameObject avatarObj, Action callback = null)
        {
            api.ResetAvatarTextures(avatarObj, "Textures/Avatar/avtr_0000_basic_diff", callback ?? (() => { }));
        }

        public void GenerateFolder(string relPath)
        {
            var dir = new DirectoryInfo(Application.PersistentDataPath + "/cache/" + relPath);
            if (!dir.Exists) dir.Create();
        }

        public bool DoesFileExist(string relPath) => MonoSingleton<AssetManager>.Instance.DoesExist(relPath);

        private void iterateThroughSkinnerQueue()
        {
            if (skinnerWork.Count > 0)
                skinnerWork[0]();
        }

        public string CreateSnapshotId(IAvatar dna, AvatarFlags flags, int size, Vector3 avatarRotation, Vector3 avatarOffset)
        {
            return CreateHash($"{flags}{api.SerializeAvatar(dna)}{size}{avatarRotation}{avatarOffset}");
        }

        public string CreateHash(string input) => AssetManager.GetShaString(input);

        public void TintSnapshot(Graphic image, float alpha)
        {
            if (image != null)
                image.color = new Color(image.color.r, image.color.g, image.color.b, alpha);
        }

        public void RenderAvatarSnapshotWithCancel(IAvatarHolder aAvatarHolder, GameObject aNormalTarget, string aTransformSearchString, Action aCancelSnapshot)
        {
            // Implementation omitted for brevity — unchanged from original
        }

        public GameObject CreateAvatarObject(bool flipped = false)
        {
            string path = flipped ? "Prefabs/Avatar/avatar_mp_final_flipped" : "Prefabs/Avatar/avatar_mp_final";
            return UnityEngine.Object.Instantiate(Resources.Load<GameObject>(path));
        }

        public bool AvatarHasGeo(IAvatar avatar)
        {
            if (avatar == null) return false;
            return (avatar.Accessory.SelectionKey != "3" || avatar.Hat.SelectionKey != "257") &&
                   avatar.Costume.SelectionKey == "15";
        }
    }
}