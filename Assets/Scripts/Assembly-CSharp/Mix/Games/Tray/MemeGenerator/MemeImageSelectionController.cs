using Mix.Games.Session;
using UnityEngine;
using System.Collections.Generic;

namespace Mix.Games.Tray.MemeGenerator
{
    public class MemeImageSelectionController : MonoBehaviour, IMemeImageOptionSelected
    {
        public RectTransform MemeHolder;

        public GameObject MemeImageContainer;

        protected List<MemeImageOption> mImageOptions;

        protected IMemeGeneration mMemeGeneration;

        Object IMemeImageOptionSelected.GetImage(string aPath)
        {
            Debug.Log("[MemeImageSelectionController] GetImage called with path: " + aPath);
            return mMemeGeneration.GetMemeGeneratorController().GetBundleInstance(aPath);
        }

        void IMemeImageOptionSelected.LoadImage(IGameAsset aSessionAsset, string aPath)
        {
            Debug.Log("[MemeImageSelectionController] LoadImage called with path: " + aPath);
            mMemeGeneration.GetMemeGeneratorController().LoadAsset(aSessionAsset, aPath);
        }

        void IMemeImageOptionSelected.DestroyImageOption(string aPath, Object aObject)
        {
            Debug.Log("[MemeImageSelectionController] DestroyImageOption called with path: " + aPath + ", object: " + aObject);
            mMemeGeneration.GetMemeGeneratorController().CancelBundles(aPath);
            mMemeGeneration.GetMemeGeneratorController().DestroyBundleInstance(aPath, aObject);
        }

        void IMemeImageOptionSelected.OnImageSelected(MemeImageOption aOption)
        {
            Debug.Log("[MemeImageSelectionController] OnImageSelected called with option: " + (aOption != null ? aOption.name : "null"));
            BaseGameController.Instance.Session.SessionSounds.PlaySoundEvent("MemeGenerator/SFX/SelectMemeImage");
            mMemeGeneration.SetMemeImage(aOption.ImageUrl);
        }

        public void Initialize(IMemeGeneration aMemeGeneration, List<Meme_Generator_Data> aData, string aBasePath)
        {
            Debug.Log("[MemeImageSelectionController] Initialize called with meme count: " + (aData != null ? aData.Count : 0) + ", basePath: " + aBasePath);
            mImageOptions = new List<MemeImageOption>();
            mMemeGeneration = aMemeGeneration;
            MemeImageContainer = base.transform.Find("MemeScroller/MemeHolder/meme_item").gameObject;
            aData.Sort(new MemeGeneratorDataSorter());
            for (int i = 0; i < aData.Count; i++)
            {
                Debug.Log("[MemeImageSelectionController] Instantiating meme item " + i);
                GameObject gameObject = Object.Instantiate(MemeImageContainer);
                gameObject.transform.SetParent(MemeHolder, false);
                gameObject.SetActive(true);
                string aImageUrl = aBasePath + aData[i].url;
                string aThumbUrl = aBasePath + aData[i].thumb;
                Debug.Log("[MemeImageSelectionController] Meme item " + i + " imageUrl: " + aImageUrl + ", thumbUrl: " + aThumbUrl);
                MemeImageOption component = gameObject.GetComponent<MemeImageOption>();
                component.Initialize(this, aImageUrl, aThumbUrl, aData[i]);
                mImageOptions.Add(component);
            }
            Debug.Log("[MemeImageSelectionController] Initialize complete. Total options: " + mImageOptions.Count);
        }
    }
}
