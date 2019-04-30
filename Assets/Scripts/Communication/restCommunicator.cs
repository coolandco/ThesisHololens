using HoloToolkit.Unity;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThesisHololens.States;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace ThesisHololens.Communication
{
    public class restCommunicator : Singleton<restCommunicator>
    {

        //TODO: explanation
        [SerializeField]
        [Tooltip("Pass the URL like this: http://192.168.0.11:8080/rest/")]
        private string restURL = "http://192.168.0.11:8080/rest/";


        // Use this for initialization
        public void initializeStates()
        {
            //we will get a top level array back, so deserialize it to a list
            unifiedJSONGetRequest<List<ItemStateOpenHAB>>(restURL + "items?recursive=false",initializeStatesCallback);

        }

        private void initializeStatesCallback(List<ItemStateOpenHAB> answer)
        {
            if (answer != null)
                ItemStates.Instance.initialize(answer.ToArray());
            else
                ItemStates.Instance.initialize(null);
        }

        //##########################################################################
        //new

        /// <summary>
        /// parses JSON
        /// </summary>
        /// <typeparam name="T">The struktur of the JSON answer</typeparam>
        /// <param name="url"></param>
        /// <param name="callbackWhenFinished">will be called with the answer, when json is back
        /// will be called, when there is an error, with parameter null</param>
        public void unifiedJSONGetRequest<T>(string url, UnityAction<T> callbackWhenFinished)
        {

            StartCoroutine(unifiedJSONGetRequestCoroutine<T>(url, callbackWhenFinished));

        }

        private IEnumerator unifiedJSONGetRequestCoroutine<T>(string url, UnityAction<T> callbackWhenFinished)
        {
            T jsonData = default(T);

            UnityWebRequest uwr = UnityWebRequest.Get(url);

            yield return uwr.SendWebRequest();//now wait for the answer

            Debug.Log("httpError: " + uwr.isHttpError);
            Debug.Log("netError: " + uwr.isNetworkError);
            Debug.Log("error: " + uwr.error);
            Debug.Log("httperror: " + uwr.isHttpError);
            Debug.Log("respCode: " + uwr.responseCode);
            Debug.Log("done: " + uwr.isDone);

            Debug.Log("text: " + uwr.downloadHandler.text);
            Debug.Log("progress: " + uwr.downloadProgress);
            Debug.Log("bytes: " + uwr.downloadedBytes);
            Debug.Log("dh is done: " + uwr.downloadHandler.isDone);


            if (uwr.downloadHandler.text != null)
            {
                try
                {
                    jsonData = JsonConvert.DeserializeObject<T>(uwr.downloadHandler.text);//try to deserialize
                }
                catch (Exception e)
                {
                    Debug.LogError("Error while parsing a JSON string");
                    Debug.LogError(e.Message);
                }
            }

            callbackWhenFinished(jsonData); //answeres the request
        }

        /// <summary>
        /// A Fire and forget send State
        /// </summary>
        /// <param name="toPublish"></param>
        public void publishItemStateopenHAB(ItemStateOpenHAB toPublish)
        {
            StartCoroutine(publishItemStateopenHAB_Async(toPublish));
        }


        private IEnumerator publishItemStateopenHAB_Async(ItemStateOpenHAB toPublish)
        {

            byte[] stateAsByte = Encoding.UTF8.GetBytes(toPublish.state);
            string url = restURL + "items/" + toPublish.name;



            //UnityWebRequest uwr_post = UnityWebRequest.Post(url, toPublish.state);

            UnityWebRequest uwr_post = new UnityWebRequest(url);
            uwr_post.uploadHandler = new UploadHandlerRaw(stateAsByte);
            uwr_post.downloadHandler = new DownloadHandlerBuffer();
            uwr_post.method = UnityWebRequest.kHttpVerbPOST;


            uwr_post.SetRequestHeader("Content-Type", "text/plain");

            yield return uwr_post.SendWebRequest();


        }

    }

}
