using UnityEngine;
using System;


namespace VTLTools
{
    public sealed class VTLPlayerPrefs
    {
        #region [BASIC]

        #region [VALUE]

        public static void SetObjectValue<T>(string _key, T _value, bool _saveImmediately = false) where T : class
        {
            string _value2 = (_value != null) ? JsonUtility.ToJson(_value) : string.Empty;
            SetString(_key, _value2, _saveImmediately);
        }
        public static T GetObjectValue<T>(string _key) where T : class
        {
            string @string = GetString(_key);
            return (!string.IsNullOrEmpty(@string)) ? JsonUtility.FromJson<T>(@string) : ((T)((object)null));
        }
        #endregion
        //=======================================================================================================================================
        #region [ENUM]

        public static void SetEnumValue<T>(string _key, T value, bool _isSaveImmediately = false) where T : struct, IConvertible
        {
            SetString(_key, value.ToString(), _isSaveImmediately);
        }
        public static T GetEnumValue<T>(string _key, T _defaultValue = default(T)) where T : struct, IConvertible
        {
            string @string = GetString(_key);
            if (!string.IsNullOrEmpty(@string))
                return (T)((object)Enum.Parse(typeof(T), @string));
            return _defaultValue;
        }
        #endregion
        //=======================================================================================================================================
        #region [DATE TIME]

        public static void SetDateTime(string _key, DateTime _value, bool _isSaveImmediately = false)
        {
            PlayerPrefs.SetString(_key, _value.ToBinary().ToString());
            if (_isSaveImmediately)
                Save();
        }
        //-------------------------------------------------------------------------------------------------
        public static DateTime GetDateTime(string _key)
        {
            return GetDateTime(_key, DEFAULT_DATE_TIME);
        }
        public static DateTime GetDateTime(string _key, DateTime _defaultValue)
        {
            string @string = PlayerPrefs.GetString(_key);
            DateTime _result = _defaultValue;
            if (!string.IsNullOrEmpty(@string))
            {
                long _dateData = Convert.ToInt64(@string);
                _result = DateTime.FromBinary(_dateData);
            }
            return _result;
        }
        #endregion
        //=======================================================================================================================================
        #region [BOOL]

        public static void SetBool(string _key, bool _value, bool _isSaveImmediately = false)
        {
            int value2 = (!_value) ? 0 : 1;
            SetInt(_key, value2, _isSaveImmediately);
        }
        //------------------------------------------------------------------------------------------
        public static bool GetBool(string _key)
        {
            return GetInt(_key) == 1;
        }
        public static bool GetBool(string _key, bool _defaultValue)
        {
            int _defaultValue2 = (!_defaultValue) ? 0 : 1;
            return GetInt(_key, _defaultValue2) == 1;
        }
        #endregion
        //=======================================================================================================================================
        #region [FLOAT]

        public static void SetFloat(string _key, float _value, bool _isSaveImmediately = false)
        {
            PlayerPrefs.SetFloat(_key, _value);
            if (_isSaveImmediately)
                Save();
        }
        //------------------------------------------------------------------------------------------
        public static float GetFloat(string _key)
        {
            return GetFloat(_key, 0f);
        }
        public static float GetFloat(string _key, float _defaultValue)
        {
            return PlayerPrefs.GetFloat(_key, _defaultValue);
        }
        #endregion
        //=======================================================================================================================================
        #region [DOUBLE]

        public static void SetDouble(string _key, double _value, bool _isSaveImmediately = false)
        {
            PlayerPrefs.SetString(_key, _value.ToString("G17"));
            if (_isSaveImmediately)
                Save();
        }
        //------------------------------------------------------------------------------------------
        public static double GetDouble(string _key)
        {
            return GetDouble(_key, 0.0);
        }
        public static double GetDouble(string _key, double _defaultValue)
        {
            string @string = PlayerPrefs.GetString(_key);
            double _result = _defaultValue;
            if (!string.IsNullOrEmpty(@string))
            {
                double _num = 0.0;
                if (double.TryParse(@string, out _num))
                {
                    _result = _num;
                }
            }
            return _result;
        }
        #endregion
        //=======================================================================================================================================
        #region [INT]

        public static void SetInt(string _key, int _value, bool _isSaveImmediately = false)
        {
            PlayerPrefs.SetInt(_key, _value);
            if (_isSaveImmediately)
                Save();
        }
        //----------------------------------------------------------------------------------------------------
        public static int GetInt(string _key)
        {
            return GetInt(_key, 0);
        }
        public static int GetInt(string _key, int _defaultValue)
        {
            return PlayerPrefs.GetInt(_key, _defaultValue);
        }
        #endregion
        //=======================================================================================================================================
        #region [STRING]

        public static void SetString(string _key, string _value, bool _isSaveImmediately = false)
        {
            PlayerPrefs.SetString(_key, _value);
            if (_isSaveImmediately)
                Save();
        }
        //----------------------------------------------------------------------------------------------------
        public static string GetString(string _key)
        {
            return GetString(_key, DEFAULT_STRING);
        }
        public static string GetString(string _key, string _defaultValue)
        {
            return PlayerPrefs.GetString(_key, _defaultValue);
        }
        #endregion
        //=======================================================================================================================================
        public static bool HasKey(string _key)
        {
            return PlayerPrefs.HasKey(_key);
        }
        public static void DeleteKey(string _key)
        {
            PlayerPrefs.DeleteKey(_key);
            Save();
        }
        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
            Save();
        }
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        #endregion
        //=======================================================================================================================================
        public static readonly string DEFAULT_STRING = string.Empty;
        public static readonly DateTime DEFAULT_DATE_TIME = DateTime.MinValue;
    }
}
