using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Assets.Others.Scenes.Gameplay.Scripts.Point
{
#if UNITY_EDITOR
    [CustomEditor(typeof(DiffPoint))]
    [CanEditMultipleObjects]
    class DiffPointEditor : Editor
    {
    }
#endif
}
