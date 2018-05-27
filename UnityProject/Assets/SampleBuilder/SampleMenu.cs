using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZenjectSamples
{
    public class SampleMenu : MonoBehaviour
    {
        public void OnGUI()
        {
            if (GUI.Button(new Rect(100, 100, 100, 50), "Asteroids"))
            {
                SceneManager.LoadScene("Asteroids");
            }
            else if (GUI.Button(new Rect(300, 100, 100, 50), "Space Fighter"))
            {
                SceneManager.LoadScene("SpaceFighter");
            }
        }
    }
}
