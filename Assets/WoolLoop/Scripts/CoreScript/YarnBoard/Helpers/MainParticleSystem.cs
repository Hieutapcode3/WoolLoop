using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VTLTools.Effect
{
    public class MainParticleSystem : MonoBehaviour
    {
        [SerializeField] public Effect effect;
        ParticleSystem ps;
        [ShowInInspector]
        public ParticleSystem ThisParticleSystem
        {
            get
            {
                if (ps == null)
                    ps = GetComponent<ParticleSystem>();
                return ps;
            }
        }
        void Start()
        {
            var _main = ThisParticleSystem.main;
            _main.stopAction = ParticleSystemStopAction.Callback;
        }

        void OnParticleSystemStopped()
        {
            //DPDebug.Log($"<color=green>Ping</color>");
            effect.OnParticleSystemStoppedListener();
        }
    }
}