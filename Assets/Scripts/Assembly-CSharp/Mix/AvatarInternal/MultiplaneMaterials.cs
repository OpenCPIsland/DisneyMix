using System;
using System.Collections.Generic;
using System.Text;
using Avatar;
using Avatar.DataTypes;
using Avatar.Interfaces;
using Disney.Mix.SDK;
using LitJson;
using Mix.Assets;
using UnityEngine;

namespace Mix.AvatarInternal
{
    public class MultiplaneMaterials
    {
        public static string EmptyCostumePath = string.Empty;
        public static string EmptyGeoPath = string.Empty;

        private MonoBehaviour monoEngine;
        private MultiplaneCompositor faceComp;
        private MultiplaneCompositor eyeComp;
        private MultiplaneCompositor eyebrowComp;
        private MultiplaneCompositor mouthComp;
        private MultiplaneCompositor glassesComp;
        private MultiplaneCompositor hatComp;

        private Material glassesMaterial;
        private Material hatMaterial;
        private Material faceMat;
        private Material leftEyeMat;
        private Material rightEyeMat;
        private Material leftEyebrowMat;
        private Material rightEyebrowMat;
        private Material mouthMat;

        private GameObject leftEyeObj;
        private GameObject rightEyeObj;
        private GameObject leftEyebrowObj;
        private GameObject rightEyebrowObj;
        private GameObject mouthObj;
        private GameObject faceObj;
        private GameObject baseAvatarObject;

        private readonly MultiplaneTextureTracker textureTracker;
        private global::Avatar.Interfaces.IAssetManager assetManager;

        public bool hasErrored;

        public MultiplaneMaterials(MonoBehaviour aMonoEngine, global::Avatar.Interfaces.IAssetManager aAssetManager, GameObject aAvatarObj)
        {
            Log("Constructor called");

            if (aAvatarObj.IsNullOrDisposed())
            {
                LogError("Avatar object is null or disposed in constructor.");
                hasErrored = true;
                return;
            }

            monoEngine = aMonoEngine;
            assetManager = aAssetManager;

            try
            {
                leftEyeObj = aAvatarObj.transform.Find("grp_offset/face_eye_left")?.gameObject;
                rightEyeObj = aAvatarObj.transform.Find("grp_offset/face_eye_right")?.gameObject;
                leftEyebrowObj = aAvatarObj.transform.Find("grp_offset/face_brow_left")?.gameObject;
                rightEyebrowObj = aAvatarObj.transform.Find("grp_offset/face_brow_right")?.gameObject;
                mouthObj = aAvatarObj.transform.Find("grp_offset/face_mouth")?.gameObject;
                faceObj = aAvatarObj.transform.Find("grp_offset/head")?.gameObject;
            }
            catch (Exception ex)
            {
                LogError("Exception in constructor: " + ex.Message);
                hasErrored = true;
                return;
            }

            baseAvatarObject = aAvatarObj;

            faceMat = RetrieveMaterial(faceObj);
            leftEyeMat = RetrieveMaterial(leftEyeObj);
            rightEyeMat = RetrieveMaterial(rightEyeObj);
            leftEyebrowMat = RetrieveMaterial(leftEyebrowObj);
            rightEyebrowMat = RetrieveMaterial(rightEyebrowObj);
            mouthMat = RetrieveMaterial(mouthObj);

            faceComp = new MultiplaneCompositor(monoEngine, 512, TextureFormat.ARGB32, "NewAvatarHair");
            eyebrowComp = new MultiplaneCompositor(monoEngine, 64, TextureFormat.ARGB32, "NewAvatarBrow");
            eyeComp = new MultiplaneCompositor(monoEngine, 256, TextureFormat.ARGB32, "NewAvatarEyes");
            mouthComp = new MultiplaneCompositor(monoEngine, 256, TextureFormat.ARGB32, "NewAvatarMouth");
            glassesComp = new MultiplaneCompositor(monoEngine, 128, TextureFormat.ARGB32, "NewAvatarGeo");
            hatComp = new MultiplaneCompositor(monoEngine, 256, TextureFormat.ARGB32, "NewAvatarGeo");

            Transform texLEye = aAvatarObj.transform.Find("grp_offset/tex_l_eye");
            Transform texREye = aAvatarObj.transform.Find("grp_offset/tex_r_eye");
            Transform texMouth = aAvatarObj.transform.Find("grp_offset/tex_mouth");

            if (texLEye == null || texREye == null || texMouth == null)
            {
                LogError("One or more required texture transforms are missing in constructor.");
                hasErrored = true;
                return;
            }

            AddTextureControllerToObj(leftEyeObj, texLEye.gameObject, 0.25f, 0.25f);
            AddTextureControllerToObj(rightEyeObj, texREye.gameObject, 0.25f, 0.25f);
            AddTextureControllerToObj(mouthObj, texMouth.gameObject, 0.33f, 0.25f);

            textureTracker = aAvatarObj.GetComponent<MultiplaneTextureTracker>() ?? aAvatarObj.AddComponent<MultiplaneTextureTracker>();

            Log("Constructor finished successfully");
        }

        private void Log(string message) => Debug.Log($"[MultiplaneMaterials] {message}");
        private void LogError(string message) => Debug.LogError($"[MultiplaneMaterials] {message}");

        public Material RetrieveMaterial(GameObject go)
        {
            if (go == null || go.IsNullOrDisposed())
            {
                LogError("GameObject is null or disposed in RetrieveMaterial.");
                hasErrored = true;
                return null;
            }

            Renderer renderer = go.GetComponent<Renderer>();
            if (renderer == null)
            {
                LogError("Renderer component is null in RetrieveMaterial.");
                hasErrored = true;
                return null;
            }

            return renderer.material;
        }

        public void CleanupCompositors()
        {
            Log("CleanupCompositors called");
            if (!baseAvatarObject.IsNullOrDisposed())
            {
                faceComp?.CleanLayers();
                eyebrowComp?.CleanLayers();
                eyeComp?.CleanLayers();
                mouthComp?.CleanLayers();
                glassesComp?.CleanLayers();
                hatComp?.CleanLayers();
            }
        }

        public void ResetAvatarHeadToDefault(string texturePath, Action callback)
        {
            Log($"ResetAvatarHeadToDefault called with texturePath: {texturePath}");

            if (baseAvatarObject.IsNullOrDisposed() || textureTracker.IsNullOrDisposed() || faceMat.IsNullOrDisposed())
            {
                LogError("Cannot reset avatar head to default: required objects are null or disposed.");
                callback?.Invoke();
                return;
            }

            Texture texture = Resources.Load<Texture>(texturePath);
            if (texture != null)
            {
                SetAvatarPlanesVisibility(false);
                textureTracker.CleanAllTextures();
                faceMat.SetTexture("_MainTex", texture);
                callback?.Invoke();
            }
            else
            {
                LogError($"Default texture not found at path: {texturePath}");
                callback?.Invoke();
            }
        }

        private void SetAvatarPlanesVisibility(bool isShown)
        {
            if (baseAvatarObject.IsNullOrDisposed() || leftEyeObj.IsNullOrDisposed() || rightEyeObj.IsNullOrDisposed() ||
                leftEyebrowObj.IsNullOrDisposed() || rightEyebrowObj.IsNullOrDisposed() || mouthObj.IsNullOrDisposed() ||
                textureTracker.IsNullOrDisposed())
                return;

            Log($"SetAvatarPlanesVisibility: {(isShown ? "show" : "hide")} face planes");

            leftEyeObj.SetActive(isShown);
            rightEyeObj.SetActive(isShown);
            leftEyebrowObj.SetActive(isShown);
            rightEyebrowObj.SetActive(isShown);
            mouthObj.SetActive(isShown);

            if (!isShown)
                textureTracker.CleanupGeo();
        }

        private void setBlendValues(GameObject go, float yValue, bool hasXOffset = false, float xValue = 0f)
        {
            if (go.IsNullOrDisposed()) return;

            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null && smr.sharedMesh != null && smr.sharedMesh.blendShapeCount > 0)
            {
                if (hasXOffset)
                {
                    smr.SetBlendShapeWeight(0, xValue + 50f);
                    smr.SetBlendShapeWeight(1, yValue + 50f);
                }
                else
                {
                    smr.SetBlendShapeWeight(0, yValue + 50f);
                }
            }
        }

        private void AddTextureControllerToObj(GameObject go, GameObject jointObject, float xOffsetRatio = 0.333f, float yOffsetRatio = 0.333f)
        {
            if (go.IsNullOrDisposed()) return;

            AnimatedTextureController controller = go.GetComponent<AnimatedTextureController>() ?? go.AddComponent<AnimatedTextureController>();
            controller.jointController = jointObject;
            controller.offsetRatioX = xOffsetRatio;
            controller.offsetRatioY = yOffsetRatio;
        }

        public void LoadAvatarTextures(IAvatar dna, AvatarFlags flags, Action<bool> callback)
        {
            if (hasErrored)
            {
                callback?.Invoke(false);
                return;
            }

            CompositeCostume(dna, flags, hasCostume =>
            {
                if (hasErrored)
                {
                    CleanupCompositors();
                    callback?.Invoke(false);
                    return;
                }

                SetAvatarPlanesVisibility(!hasCostume);

                if (!hasCostume)
                {
                    var queuer = new DelegateQueuer(() =>
                    {
                        CleanupCompositors();
                        callback?.Invoke(!hasErrored);
                    });

                    CompositeSkinAndHair(dna, flags, queuer.EnqueueAction());
                    CompositeBrow(dna, flags, queuer.EnqueueAction());
                    CompositeEyes(dna, flags, queuer.EnqueueAction());
                    CompositeMouth(dna, flags, queuer.EnqueueAction());
                    CompositeAccessory(dna, flags, queuer.EnqueueAction(), dna.Accessory, "Accessory", glassesComp);

                    if (dna.Hat != null)
                        CompositeAccessory(dna, flags, queuer.EnqueueAction(), dna.Hat, "Hat", hatComp);

                    queuer.FinishedEnqueuing();
                }
                else
                {
                    callback?.Invoke(true);
                }
            });
        }

        // ===================================================================
        // Composite Methods
        // ===================================================================

        private void CompositeCostume(IAvatar dna, AvatarFlags flags, Action<bool> callback)
        {
            if ((flags & AvatarFlags.WithoutCostume) == AvatarFlags.WithoutCostume)
            {
                callback?.Invoke(false);
                return;
            }

            if (dna?.Costume == null || string.IsNullOrEmpty(dna.Costume.SelectionKey) || dna.Costume.SelectionKey == "0")
            {
                callback?.Invoke(false);
                return;
            }

            var propInfos = new[] { new KeyValuePair<IAvatarProperty, string>(dna.Costume, "Costume") };
            string path = GenerateFilenameFromProps(propInfos) + ".png";

            Texture2D cachedTex = MultiplaneMemCache.GetTexture(path);
            Log($"Costume cache lookup for {path}: {(cachedTex != null ? "HIT" : "MISS")}");

            if (cachedTex != null)
            {
                SetTextures("_MainTex", faceMat, string.Empty, () => callback?.Invoke(true))(cachedTex);
                return;
            }

            if (!string.IsNullOrEmpty(EmptyCostumePath) && EmptyCostumePath == path)
            {
                callback?.Invoke(false);
                return;
            }

            assetManager.LoadCachedImage(path, (success, texture2D) =>
            {
                if (!success)
                {
                    AddBundleLoad("Costume", dna.Costume, faceComp, flags, bundleSuccess =>
                    {
                        if (!bundleSuccess)
                        {
                            hasErrored = true;
                            LogError("Failed to load costume bundle for path: " + path);
                            callback?.Invoke(false);
                            return;
                        }
                        bool hasCostume = !faceComp.IsComponentIgnored(dna.Costume.SelectionKey);
                        if (hasCostume)
                        {
                            try
                            {
                                faceComp.Composite(flags, newTexture =>
                                {
                                    if (newTexture == null)
                                    {
                                        hasErrored = true;
                                        LogError("faceComp.Composite returned null texture for path: " + path);
                                        callback?.Invoke(false);
                                        return;
                                    }
                                    MultiplaneMemCache.AddTexture(path, newTexture, () => UnityEngine.Object.Destroy(newTexture));
                                    SetTextures("_MainTex", faceMat, path, () => callback?.Invoke(hasCostume))(newTexture);
                                });
                            }
                            catch (Exception ex)
                            {
                                hasErrored = true;
                                LogError("Exception during faceComp.Composite: " + ex);
                                callback?.Invoke(false);
                            }
                        }
                        else
                        {
                            EmptyCostumePath = path;
                            callback?.Invoke(hasCostume);
                        }
                    });
                }
                else
                {
                    if (texture2D == null)
                    {
                        hasErrored = true;
                        LogError($"Costume cached image load returned success but texture was null for {path}.");
                        callback?.Invoke(false);
                        return;
                    }

                    MultiplaneMemCache.AddTexture(path, texture2D, () => UnityEngine.Object.Destroy(texture2D));
                    SetTextures("_MainTex", faceMat, string.Empty, () => callback?.Invoke(true))(texture2D);
                }
            });
        }

        private void CompositeSkinAndHair(IAvatar dna, AvatarFlags flags, Action callback)
        {
            if (hasErrored)
            {
                callback?.Invoke();
                return;
            }

            var propInfos = new[]
            {
                new KeyValuePair<IAvatarProperty, string>(dna.Skin, "Skin"),
                new KeyValuePair<IAvatarProperty, string>(dna.Nose, "Nose"),
                new KeyValuePair<IAvatarProperty, string>(dna.Hair, "Hair")
            };

            string path = GenerateFilenameFromProps(propInfos) + ".png";
            Texture2D texture = MultiplaneMemCache.GetTexture(path);

            Log($"Skin/Hair cache lookup for {path}: {(texture != null ? "HIT" : "MISS")}");

            if (texture != null)
            {
                SetTextures("_MainTex", faceMat, string.Empty, callback)(texture);
                return;
            }

            assetManager.LoadCachedImage(path, (success, texture2D) =>
            {
                Log($"Skin/Hair LoadCachedImage {path} success={success}");

                if (!success)
                {
                    Log("[MultiplaneMaterials] Skin/Hair not cached, compositing from scratch");
                    CompositeSkinAndHairFromScratch(dna, flags, path, callback);
                }
                else
                {
                    MultiplaneMemCache.AddTexture(path, texture2D, () => UnityEngine.Object.Destroy(texture2D));
                    SetTextures("_MainTex", faceMat, string.Empty, callback)(texture2D);
                }
            });
        }

        private void CompositeSkinAndHairFromScratch(IAvatar dna, AvatarFlags flags, string basePath, Action callback)
        {
            Log("CompositeSkinAndHairFromScratch called");

            var queuer = new DelegateQueuer(() =>
            {
                if (hasErrored)
                {
                    callback?.Invoke();
                    return;
                }

                faceComp.Composite(flags, mainTex =>
                {
                    if (textureTracker.IsNullOrDisposed())
                    {
                        hasErrored = true;
                        callback?.Invoke();
                        return;
                    }

                    MultiplaneMemCache.AddTexture(basePath, mainTex, () => UnityEngine.Object.Destroy(mainTex));
                    textureTracker.SetAndTrackTexture(faceMat, "_MainTex", basePath, mainTex);
                    assetManager.SaveCachedImage(mainTex, basePath, (bool savedImage) => callback?.Invoke());
                });
            });

            AddBundleLoad("Skin", dna.Skin, faceComp, flags, HairEnquerHelper(queuer));
            AddBundleLoad("Nose", dna.Nose, faceComp, flags, HairEnquerHelper(queuer));
            AddBundleLoad("Hair", dna.Hair, faceComp, flags, HairEnquerHelper(queuer));

            queuer.FinishedEnqueuing();
        }

        private void CompositeBrow(IAvatar dna, AvatarFlags flags, Action callback)
        {
            if (hasErrored || textureTracker.IsNullOrDisposed())
            {
                callback?.Invoke();
                return;
            }

            var browDupe = new ClientAvatarProperty(dna.Brow.SelectionKey, dna.Hair.TintIndex, dna.Brow.XOffset, dna.Brow.YOffset);

            var propInfos = new[] { new KeyValuePair<IAvatarProperty, string>(browDupe, "Brow") };
            string path = GenerateFilenameFromProps(propInfos, true) + ".png";

            setBlendValues(leftEyebrowObj, (float)browDupe.YOffset);
            setBlendValues(rightEyebrowObj, (float)browDupe.YOffset);

            Texture2D texture = MultiplaneMemCache.GetTexture(path);
            Log($"Brow cache lookup for {path}: {(texture != null ? "HIT" : "MISS")}");

            if (texture != null)
            {
                if (textureTracker.IsNullOrDisposed())
                {
                    hasErrored = true;
                    callback?.Invoke();
                    return;
                }

                textureTracker.SetAndTrackTexture(leftEyebrowMat, "_MainTex", path, texture);
                textureTracker.SetAndTrackTexture(rightEyebrowMat, "_MainTex", path, texture);
                callback?.Invoke();
                return;
            }

            assetManager.LoadCachedImage(path, (success, texture2D) =>
            {
                Log($"Brow LoadCachedImage {path} success={success}");

                if (success && texture2D == null)
                {
                    LogError($"Brow cached image load returned success but texture was null for {path}. Falling back to bundle load.");
                    success = false;
                }

                if (!success)
                {
                    AddBundleLoad("Brow", browDupe, eyebrowComp, flags, bundleSuccess =>
                    {
                        if (!bundleSuccess)
                        {
                            hasErrored = true;
                            callback?.Invoke();
                            return;
                        }

                        eyebrowComp.Composite(flags, mainTex =>
                        {
                            if (mainTex == null)
                            {
                                hasErrored = true;
                                LogError("eyebrowComp.Composite returned null texture for path: " + path);
                                callback?.Invoke();
                                return;
                            }

                            if (textureTracker.IsNullOrDisposed())
                            {
                                hasErrored = true;
                                callback?.Invoke();
                                return;
                            }

                            MultiplaneMemCache.AddTexture(path, mainTex, () => UnityEngine.Object.Destroy(mainTex));
                            textureTracker.SetAndTrackTexture(leftEyebrowMat, "_MainTex", path, mainTex);
                            textureTracker.SetAndTrackTexture(rightEyebrowMat, "_MainTex", path, mainTex);
                            assetManager.SaveCachedImage(mainTex, path, (bool imageSaved) => callback?.Invoke());
                        });
                    });
                }
                else
                {
                    MultiplaneMemCache.AddTexture(path, texture2D, () => UnityEngine.Object.Destroy(texture2D));
                    textureTracker.SetAndTrackTexture(leftEyebrowMat, "_MainTex", path, texture2D);
                    textureTracker.SetAndTrackTexture(rightEyebrowMat, "_MainTex", path, texture2D);
                    callback?.Invoke();
                }
            });
        }

        private void CompositeEyes(IAvatar dna, AvatarFlags flags, Action callback)
        {
            if (hasErrored || textureTracker.IsNullOrDisposed())
            {
                callback?.Invoke();
                return;
            }

            var propInfos = new[] { new KeyValuePair<IAvatarProperty, string>(dna.Eyes, "Eyes") };
            string path = GenerateFilenameFromProps(propInfos, true) + ".png";

            setBlendValues(leftEyeObj, (float)dna.Eyes.YOffset);
            setBlendValues(rightEyeObj, (float)dna.Eyes.YOffset);

            Texture2D texture = MultiplaneMemCache.GetTexture(path);
            Log($"Eyes cache lookup for {path}: {(texture != null ? "HIT" : "MISS")}");

            if (texture != null)
            {
                if (textureTracker.IsNullOrDisposed())
                {
                    hasErrored = true;
                    callback?.Invoke();
                    return;
                }

                textureTracker.SetAndTrackTexture(leftEyeMat, "_MainTex", path, texture);
                textureTracker.SetAndTrackTexture(rightEyeMat, "_MainTex", path, texture);
                callback?.Invoke();
                return;
            }

            assetManager.LoadCachedImage(path, (success, texture2D) =>
            {
                Log($"Eyes LoadCachedImage {path} success={success}");

                if (success && texture2D == null)
                {
                    LogError($"Eyes cached image load returned success but texture was null for {path}. Falling back to bundle load.");
                    success = false;
                }

                if (!success)
                {
                    AddBundleLoad("Eyes", dna.Eyes, eyeComp, flags, bundleSuccess =>
                    {
                        if (!bundleSuccess)
                        {
                            hasErrored = true;
                            callback?.Invoke();
                            return;
                        }

                        eyeComp.Composite(flags, mainTex =>
                        {
                            if (mainTex == null)
                            {
                                hasErrored = true;
                                LogError("eyeComp.Composite returned null texture for path: " + path);
                                callback?.Invoke();
                                return;
                            }

                            if (textureTracker.IsNullOrDisposed())
                            {
                                hasErrored = true;
                                callback?.Invoke();
                                return;
                            }

                            MultiplaneMemCache.AddTexture(path, mainTex, () => UnityEngine.Object.Destroy(mainTex));
                            textureTracker.SetAndTrackTexture(leftEyeMat, "_MainTex", path, mainTex);
                            textureTracker.SetAndTrackTexture(rightEyeMat, "_MainTex", path, mainTex);
                            assetManager.SaveCachedImage(mainTex, path, (bool savedImage) => callback?.Invoke());
                        });
                    });
                }
                else
                {
                    MultiplaneMemCache.AddTexture(path, texture2D, () => UnityEngine.Object.Destroy(texture2D));
                    textureTracker.SetAndTrackTexture(leftEyeMat, "_MainTex", path, texture2D);
                    textureTracker.SetAndTrackTexture(rightEyeMat, "_MainTex", path, texture2D);
                    callback?.Invoke();
                }
            });
        }

        private void CompositeMouth(IAvatar dna, AvatarFlags flags, Action callback)
        {
            if (hasErrored)
            {
                callback?.Invoke();
                return;
            }

            var mouthPropDupe = new ClientAvatarProperty(dna.Mouth.SelectionKey, dna.Skin.TintIndex, dna.Mouth.XOffset, dna.Mouth.YOffset);

            var propInfos = new[] { new KeyValuePair<IAvatarProperty, string>(mouthPropDupe, "Mouth") };
            string path = GenerateFilenameFromProps(propInfos, true) + ".png";

            setBlendValues(mouthObj, (float)dna.Mouth.YOffset, true, (float)dna.Mouth.XOffset);

            Texture2D texture = MultiplaneMemCache.GetTexture(path);
            Log($"Mouth cache lookup for {path}: {(texture != null ? "HIT" : "MISS")}");

            if (texture != null)
            {
                SetTextures("_MainTex", mouthMat, string.Empty, callback)(texture);
                return;
            }

            assetManager.LoadCachedImage(path, (success, texture2D) =>
            {
                Log($"Mouth LoadCachedImage {path} success={success}");

                if (!success)
                {
                    AddBundleLoad("Mouth", mouthPropDupe, mouthComp, flags, bundleSuccess =>
                    {
                        if (!bundleSuccess)
                        {
                            hasErrored = true;
                            callback?.Invoke();
                            return;
                        }

                        mouthComp.Composite(flags, newTexture =>
                        {
                            MultiplaneMemCache.AddTexture(path, newTexture, () => UnityEngine.Object.Destroy(newTexture));
                            SetTextures("_MainTex", mouthMat, path, callback)(newTexture);
                        });
                    });
                }
                else
                {
                    MultiplaneMemCache.AddTexture(path, texture2D, () => UnityEngine.Object.Destroy(texture2D));
                    SetTextures("_MainTex", mouthMat, string.Empty, callback)(texture2D);
                }
            });
        }

        private void CompositeAccessory(IAvatar dna, AvatarFlags flags, Action callback, IAvatarProperty accessoryProperty, string propertyName, MultiplaneCompositor compositor)
        {
            Log($"CompositeAccessory called for {propertyName}");

            if (dna == null || textureTracker.IsNullOrDisposed())
            {
                hasErrored = true;
                callback?.Invoke();
                return;
            }

            if ((flags & AvatarFlags.WithoutGeo) == AvatarFlags.WithoutGeo)
            {
                textureTracker.DisableActiveGeoJoint(propertyName);
                callback?.Invoke();
                return;
            }

            offsetAccessory((float)accessoryProperty.YOffset, propertyName);

            var propInfos = new[] { new KeyValuePair<IAvatarProperty, string>(accessoryProperty, propertyName) };
            string id = GenerateFilenameFromProps(propInfos, true);
            string path = id + ".png";

            if (textureTracker.currentGeoIds.TryGetValue(propertyName, out string current) && current == id)
            {
                callback?.Invoke();
                return;
            }

            LoadAccessoryGeometry(accessoryProperty, propertyName, geoSuccess =>
            {
                if (geoSuccess)
                {
                    assetManager.LoadCachedImage(path, (cacheSuccess, texture) =>
                    {
                        if (!cacheSuccess)
                        {
                            LoadTestGeoAccessory(accessoryProperty, compositor, propertyName, textureSuccess =>
                            {
                                if (textureSuccess && !getGeoMaterial(propertyName).IsNullOrDisposed())
                                {
                                    compositor.Composite(flags, mainTex =>
                                    {
                                        if (textureTracker.IsNullOrDisposed()) return;

                                        textureTracker.SetAndTrackTexture(getGeoMaterial(propertyName), "_MainTex", path, mainTex);
                                        textureTracker.EnableActiveGeoJoint(propertyName);
                                        offsetAccessory((float)accessoryProperty.YOffset, propertyName);
                                        textureTracker.currentGeoIds[propertyName] = id;

                                        assetManager.SaveCachedImage(mainTex, path, (bool savedImage) => callback?.Invoke());
                                    });
                                }
                                else
                                {
                                    textureTracker.DisableActiveGeoJoint(propertyName);
                                    textureTracker.currentGeoIds[propertyName] = null;
                                    callback?.Invoke();
                                }
                            });
                        }
                        else
                        {
                            if (!textureTracker.IsNullOrDisposed())
                            {
                                MultiplaneMemCache.AddTexture(path, texture, () => UnityEngine.Object.Destroy(texture));
                                textureTracker.SetAndTrackTexture(getGeoMaterial(propertyName), "_MainTex", path, texture);
                                textureTracker.EnableActiveGeoJoint(propertyName);
                                offsetAccessory((float)accessoryProperty.YOffset, propertyName);
                                textureTracker.currentGeoIds[propertyName] = id;
                            }
                            callback?.Invoke();
                        }
                    });
                }
                else
                {
                    textureTracker.DisableActiveGeoJoint(propertyName);
                    textureTracker.currentGeoIds[propertyName] = null;
                    callback?.Invoke();
                }
            });
        }

        private Material getGeoMaterial(string propertyName)
        {
            return propertyName == "Hat" ? hatMaterial : glassesMaterial;
        }

        private void offsetAccessory(float aYOffset, string aPropertyName)
        {
            GameObject activeGeoJoint = textureTracker.GetActiveGeoJoint(aPropertyName);
            if (activeGeoJoint.IsNullOrDisposed()) return;

            Transform child = activeGeoJoint.transform.GetChild(0);
            if (child.IsNullOrDisposed()) return;

            float num = 22.81f;
            float num2 = (aYOffset + 50f) / 100f;
            float y = num * num2 - 9.7f;

            child.localPosition = new Vector3(0f, y, 0f);
        }

        private Action<bool> HairEnquerHelper(DelegateQueuer queuer)
        {
            Action callback = queuer.EnqueueAction();
            return success =>
            {
                hasErrored = !success;
                callback?.Invoke();
            };
        }

        private Action<Texture2D> SetTextures(string prop, Material mat, string path, Action callback)
        {
            return mainTex =>
            {
                if (!string.IsNullOrEmpty(path))
                {
                    assetManager.SaveCachedImage(mainTex, path, imageSaved =>
                    {
                        hasErrored = !imageSaved;
                        if (!hasErrored && !textureTracker.IsNullOrDisposed())
                            textureTracker.SetAndTrackTexture(mat, prop, path, mainTex);
                        callback?.Invoke();
                    });
                }
                else
                {
                    if (!textureTracker.IsNullOrDisposed())
                        textureTracker.SetAndTrackTexture(mat, prop, path, mainTex);
                    callback?.Invoke();
                }
            };
        }

        private string GenerateFilenameFromProps(KeyValuePair<IAvatarProperty, string>[] propInfos, bool ignoreOffsets = false)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.WriteArrayStart();

            foreach (var kvp in propInfos)
            {
                SerializeProperty(writer, kvp.Key, kvp.Value, ignoreOffsets);
            }

            writer.WriteArrayEnd();
            return assetManager.GetShaString(sb.ToString());
        }

        private void SerializeProperty(JsonWriter writer, IAvatarProperty prop, string propName, bool ignoreOffsets)
        {
            if (prop == null)
                prop = new ClientAvatarProperty(string.Empty, 0, 0.0, 0.0);

            writer.WriteObjectStart();
            writer.WritePropertyName(propName);
            writer.WriteObjectStart();
            writer.WritePropertyName("SelectionKey");
            writer.Write(prop.SelectionKey);
            writer.WritePropertyName("TintIndex");
            writer.Write(prop.TintIndex);

            if (!ignoreOffsets)
            {
                writer.WritePropertyName("XOffset");
                writer.Write(prop.XOffset);
                writer.WritePropertyName("YOffset");
                writer.Write(prop.YOffset);
            }

            if (assetManager != null)
            {
                writer.WritePropertyName("VersionInfo");
                writer.Write(assetManager.GetAvatarElementVersionInfo(assetManager.GetAvatarData(prop.SelectionKey)));
            }

            writer.WriteObjectEnd();
            writer.WriteObjectEnd();
        }

        // ===================================================================
        // Remaining helper methods (kept mostly as original, with minor safety)
        // ===================================================================

        private void LoadAccessoryGeometry(IAvatarProperty geoProperty, string propertyName, Action<bool> callback)
        {
            if (geoProperty == null)
            {
                callback?.Invoke(false);
                return;
            }

            AvatarElement avatarData = assetManager.GetAvatarData(geoProperty.SelectionKey);
            if (avatarData == null || string.IsNullOrEmpty(avatarData.Hd))
            {
                callback?.Invoke(false);
                return;
            }

            string bundlePath = avatarData.Hd.Replace("_hd", "_geo_hd");
            if (!string.IsNullOrEmpty(EmptyGeoPath) && bundlePath == EmptyGeoPath)
            {
                callback?.Invoke(false);
                return;
            }

            assetManager.LoadABundle((systemObject, userdata) =>
            {
                GeoLoadedCallback(systemObject, bundlePath, propertyName, callback);
            }, bundlePath, null, string.Empty, false, false, true);
        }

        private void LoadTestGeoAccessory(IAvatarProperty geoProperty, MultiplaneCompositor comper, string propertyName, Action<bool> callback)
        {
            if (geoProperty == null || comper == null)
            {
                callback?.Invoke(false);
                return;
            }

            AvatarElement avatarData = assetManager.GetAvatarData(geoProperty.SelectionKey);
            if (avatarData == null || string.IsNullOrEmpty(avatarData.Hd))
            {
                callback?.Invoke(false);
                return;
            }

            comper.AddLayerInfo("Accessory", geoProperty, avatarData);
            string bundlePath = avatarData.Hd;

            assetManager.LoadABundle((systemObject, userdata) =>
            {
                BundleLoadedCallback(systemObject, comper, bundlePath, geoProperty.SelectionKey, callback);
            }, bundlePath, null, string.Empty, false, false, true);
        }

        private void GeoLoadedCallback(UnityEngine.Object aGameObject, string bundlePath, string propertyName, Action<bool> callback)
        {
            GameObject gameObject = null;
            if (aGameObject != null)
            {
                GameObject go2 = aGameObject as GameObject;
                AvatarGeoComponent component = go2?.GetComponent<AvatarGeoComponent>();
                if (component != null && !baseAvatarObject.IsNullOrDisposed())
                {
                    Transform t = baseAvatarObject.transform.Find(component.JointName);
                    if (t != null)
                        gameObject = t.gameObject;
                }
            }

            if (MonoSingleton<AssetManager>.Instance != null && !gameObject.IsNullOrDisposed() && !textureTracker.IsNullOrDisposed())
            {
                bool result;
                if (propertyName == "Hat")
                {
                    result = textureTracker.AttachAndTrackGeoAccessory(bundlePath, gameObject, propertyName, ref hatMaterial);
                    if (hatMaterial != null) textureTracker.DisableActiveGeoJoint(propertyName);
                }
                else
                {
                    result = textureTracker.AttachAndTrackGeoAccessory(bundlePath, gameObject, propertyName, ref glassesMaterial);
                    if (glassesMaterial != null) textureTracker.DisableActiveGeoJoint(propertyName);
                }
                callback?.Invoke(result);
            }
            else
            {
                hasErrored = true;
                callback?.Invoke(false);
            }
        }

        private void BundleLoadedCallback(UnityEngine.Object aGameObject, MultiplaneCompositor comper, string bundlePath, string key, Action<bool> callback)
        {
            if (MonoSingleton<AssetManager>.Instance == null)
            {
                callback?.Invoke(false);
                return;
            }

            GameObject instance = (GameObject)MonoSingleton<AssetManager>.Instance.GetBundleInstance(bundlePath);
            if (instance == null)
            {
                callback?.Invoke(false);
                return;
            }

            Action cleanup = AddTextureLayersFromComponent(instance, comper, bundlePath, key);
            comper.AddBundleCleanupCall(cleanup);
            callback?.Invoke(true);
        }

        private Action AddTextureLayersFromComponent(GameObject bundleInstance, MultiplaneCompositor comper, string bundlePath, string key)
        {
            AvatarComponent component = bundleInstance?.GetComponent<AvatarComponent>();
            if (component == null) return () => { };

            AvatarComponentLayer[] layers = component.Layers;
            foreach (var layer in layers)
            {
                if (layer != null)
                {
                    AvatarTextureTracker.AddTextureReference(layer.Texture);
                    AvatarTextureTracker.AddTextureReference(layer.NormalMap);
                }
            }

            comper.AddComponent(key, component);

            return () =>
            {
                if (component != null && component.Layers != null)
                {
                    for (int j = 0; j < component.Layers.Length; j++)
                    {
                        if (component.Layers[j].Texture != null)
                            AvatarTextureTracker.DecrementTextureReference(component.Layers[j].Texture);
                        if (component.Layers[j].NormalMap != null)
                            AvatarTextureTracker.DecrementTextureReference(component.Layers[j].NormalMap);
                    }
                }

                if (!string.IsNullOrEmpty(bundlePath) && bundleInstance != null)
                {
                    MonoSingleton<AssetManager>.Instance.DestroyBundleInstance(bundlePath, bundleInstance);
                }
            };
        }

        private void AddBundleLoad(string name, IAvatarProperty property, MultiplaneCompositor comp, AvatarFlags flags, Action<bool> callback)
        {
            AvatarElement avatarData = assetManager.GetAvatarData(property.SelectionKey);
            if (avatarData == null)
            {
                callback?.Invoke(false);
                return;
            }

            comp.AddLayerInfo(name, property, avatarData);
            string bundlePath = avatarData.Hd;

            assetManager.LoadABundle((systemObject, userdata) =>
            {
                BundleLoadedCallback(systemObject, comp, bundlePath, property.SelectionKey, callback);
            }, bundlePath, null, string.Empty, false, false, true);
        }

        private void CompositeHairAndNormals(IAvatar dna, AvatarFlags flags, Action callback)
        {
            // Placeholder - implement if needed
            callback?.Invoke();
        }
    }
}