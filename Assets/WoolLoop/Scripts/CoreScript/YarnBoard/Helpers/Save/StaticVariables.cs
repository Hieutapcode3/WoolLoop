
using System;
using Sirenix.OdinInspector;

namespace VTLTools
{
    public class StaticVariables
    {
        public static string PREF_USER_DATA = "PREF_USER_DATA";
        #region Public Variables
        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsSoundOn
        {
            get => UserData.isSoundOn;
            set
            {
                UserData.isSoundOn = value;
                SaveData();
            }
        }

        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsMusicOn
        {
            get => UserData.isMusicOn;
            set
            {
                UserData.isMusicOn = value;
                SaveData();
            }
        }

        [ShowInInspector, BoxGroup("Setting")]
        public static bool IsVibrationOn
        {
            get => UserData.isVibrationOn;
            set
            {
                UserData.isVibrationOn = value;
                SaveData();
            }
        }

        [ShowInInspector, BoxGroup("Setting")]
        public static int CurrentLevel
        {
            get => UserData.currentLevel;
            set
            {
                UserData.currentLevel = Math.Max(0, value);
                SaveData();
            }
        }



        #endregion

        #region User Data
        public static UserData UserData { get; private set; }

        public static void SetUserData(UserData _data)
        {
            UserData = _data;
            SaveData();
        }

        static StaticVariables()
        {
            UserData = GetData();
            if (UserData == null)
            {
                UserData = new UserData();
                SaveData();
            }
        }

        static void SaveData()
        {
            VTLPlayerPrefs.SetObjectValue(PREF_USER_DATA, UserData);
        }
        static UserData GetData()
        {
            return VTLPlayerPrefs.GetObjectValue<UserData>(PREF_USER_DATA);
        }
        #endregion
    }

    [Serializable]
    public class UserData
    {
        public bool isSoundOn;
        public bool isMusicOn;
        public bool isVibrationOn;

        public int currentLevel;


        public UserData()
        {
            isSoundOn = true;
            isMusicOn = true;
            isVibrationOn = true;
            currentLevel = 0;
        }

        public override string ToString()
        {
            return $"userData: \n" +
                   $"isSoundOn: {isSoundOn}\n" +
                   $"isMusicOn: {isMusicOn}\n" +
                   $"isVibrationOn: {isVibrationOn}\n";
        }
    }


}