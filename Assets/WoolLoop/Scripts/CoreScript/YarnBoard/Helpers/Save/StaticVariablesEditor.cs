using Sirenix.OdinInspector;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif
using UnityEditor;


namespace VTLTools
{
#if UNITY_EDITOR

    public class StaticVariablesEditor : OdinEditorWindow
    {
        [MenuItem("Tools/User Data Editor")]
        private static void OpenWindow()
        {
            GetWindow<StaticVariablesEditor>().Show();
            userData = StaticVariables.UserData;
        }

        [ShowInInspector] public static UserData userData;


        private void OnValidate()
        {
            if (userData != null)
                StaticVariables.SetUserData(userData);
        }

        [Button(ButtonSizes.Gigantic)]
        void Refresh()
        {
            userData = StaticVariables.UserData;
        }
    }
#endif
}