using System;
using System.Collections;
using System.IO;
using Disney.MobileNetwork; // Ensure this assembly is in your project
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

namespace Mix.Video
{
    public class FullScreenVideo
    {
        public delegate void VideoCompleteEvent(FullScreenVideo aFullScreenVideo);
        public event VideoCompleteEvent OnVideoComplete;

        public static bool IsVideoPlaying;

        protected string InternalVideoPath;
        protected float VideoPlayTime;

        public FullScreenVideo(string a169FileName, string a43FileName, string aCacheFileName, float aVideoPlayTime)
        {
            VideoPlayTime = aVideoPlayTime;

            float screenRatio = (float)Screen.height / (float)Screen.width;
            float ratioThreshold = 1.6666666f;

            // Select the sub-path based on aspect ratio logic from original file
            if (screenRatio > ratioThreshold)
            {
                InternalVideoPath = "video/" + a169FileName;
            }
            else
            {
                InternalVideoPath = "video/" + a43FileName;
            }
        }

        public void PlayVideo(MonoBehaviour aMonoBehaviour)
        {
            aMonoBehaviour.StartCoroutine(ExecuteVideoSequence());
        }

        private IEnumerator ExecuteVideoSequence()
        {
            // Logic ported from original FullScreenVideo.cs
            EnvironmentManager.ShowStatusBar(false);
            IsVideoPlaying = true;

            float startTime = Time.realtimeSinceStartup;

            // Use the internal runner to handle the Unity VideoPlayer
            VideoPlaybackRunner runner = VideoPlaybackRunner.Ensure();
            yield return runner.Play(InternalVideoPath);

            float endTime = Time.realtimeSinceStartup;

            // Check if the video was skipped (played for less than its duration)
            if (endTime - startTime < VideoPlayTime)
            {
                // Analytics.LogVideoSkip(); // Uncomment if your Analytics class is available
            }

            IsVideoPlaying = false;
            EnvironmentManager.ShowStatusBar(true);

            OnVideoComplete?.Invoke(this);
        }
    }

    /// <summary>
    /// Internal component that manages the Unity VideoPlayer and UI overlay.
    /// </summary>
    internal class VideoPlaybackRunner : MonoBehaviour
    {
        private static VideoPlaybackRunner instance;

        public static VideoPlaybackRunner Ensure()
        {
            if (instance != null) return instance;
            GameObject runnerObject = new GameObject("VideoPlaybackRunner");
            DontDestroyOnLoad(runnerObject);
            instance = runnerObject.AddComponent<VideoPlaybackRunner>();
            return instance;
        }

        public IEnumerator Play(string videoPath)
        {
            // FIX: Use Application.streamingAssetsPath (lowercase 's')
            string url = Path.Combine(Application.StreamingAssetsPath, videoPath).Replace("\\", "/");

            // Create Overlay UI
            GameObject root = new GameObject("VideoPlaybackRoot");
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            root.AddComponent<GraphicRaycaster>();

            // Dark background
            GameObject bg = new GameObject("VideoBG");
            bg.transform.SetParent(root.transform, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = Color.black;

            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Video Display
            GameObject rawImageObj = new GameObject("VideoImage");
            rawImageObj.transform.SetParent(root.transform, false);
            RawImage rawImage = rawImageObj.AddComponent<RawImage>();
            AspectRatioFitter fitter = rawImageObj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

            // Video Player Setup
            VideoPlayer player = root.AddComponent<VideoPlayer>();
            player.source = VideoSource.Url;
            player.url = url;
            player.playOnAwake = false;
            player.renderMode = VideoRenderMode.RenderTexture;

            bool finished = false;
            player.loopPointReached += (v) => finished = true;

            player.Prepare();
            while (!player.isPrepared) yield return null;

            RenderTexture rt = new RenderTexture((int)player.width, (int)player.height, 0);
            player.targetTexture = rt;
            rawImage.texture = rt;
            fitter.aspectRatio = (float)player.width / (float)player.height;

            player.Play();

            while (!finished)
            {
                // FIX: Replaced New Input System (Keyboard/Mouse) with Legacy Input for compatibility
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
                {
                    break;
                }
                yield return null;
            }

            player.Stop();
            rt.Release();
            Destroy(root);
        }
    }
}