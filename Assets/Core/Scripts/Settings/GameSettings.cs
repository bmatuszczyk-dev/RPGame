using System;
using UnityEngine;

namespace RPGame.Core.Settings
{
    public readonly struct GameSettingsValues
    {
        public GameSettingsValues(float masterVolume, float musicVolume, float soundVolume, float mouseSensitivity, float drawingSensitivity)
        {
            MasterVolume = masterVolume;
            MusicVolume = musicVolume;
            SoundVolume = soundVolume;
            MouseSensitivity = mouseSensitivity;
            DrawingSensitivity = drawingSensitivity;
        }

        public float MasterVolume { get; }
        public float MusicVolume { get; }
        public float SoundVolume { get; }
        public float MouseSensitivity { get; }
        public float DrawingSensitivity { get; }
    }

    public static class GameSettings
    {
        private const string MasterVolumeKey = "Settings.MasterVolume";
        private const string MusicVolumeKey = "Settings.MusicVolume";
        private const string SoundVolumeKey = "Settings.SoundVolume";
        private const string MouseSensitivityKey = "Settings.MouseSensitivity";
        private const string DrawingSensitivityKey = "Settings.DrawingSensitivity";

        public const float DefaultMasterVolume = 100f;
        public const float DefaultMusicVolume = 100f;
        public const float DefaultSoundVolume = 100f;
        public const float DefaultMouseSensitivity = 0.12f;
        public const float DefaultDrawingSensitivity = 0.35f;

        private const float MinimumVolume = 0f;
        private const float MaximumVolume = 100f;
        private const float MinimumMouseSensitivity = 0.01f;
        private const float MaximumMouseSensitivity = 1f;
        private const float MinimumDrawingSensitivity = 0f;
        private const float MaximumDrawingSensitivity = 1f;

        public static event Action<float> MouseSensitivityChanged;
        public static event Action<float> DrawingSensitivityChanged;

        public static GameSettingsValues Load()
        {
            return new GameSettingsValues(
                ReadValue(MasterVolumeKey, DefaultMasterVolume, MinimumVolume, MaximumVolume),
                ReadValue(MusicVolumeKey, DefaultMusicVolume, MinimumVolume, MaximumVolume),
                ReadValue(SoundVolumeKey, DefaultSoundVolume, MinimumVolume, MaximumVolume),
                ReadValue(MouseSensitivityKey, DefaultMouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity),
                ReadValue(DrawingSensitivityKey, DefaultDrawingSensitivity, MinimumDrawingSensitivity, MaximumDrawingSensitivity));
        }

        public static void ApplySavedSettings()
        {
            Apply(Load());
        }

        public static void SaveAndApply(GameSettingsValues values)
        {
            GameSettingsValues validatedValues = Validate(values);
            PlayerPrefs.SetFloat(MasterVolumeKey, validatedValues.MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, validatedValues.MusicVolume);
            PlayerPrefs.SetFloat(SoundVolumeKey, validatedValues.SoundVolume);
            PlayerPrefs.SetFloat(MouseSensitivityKey, validatedValues.MouseSensitivity);
            PlayerPrefs.SetFloat(DrawingSensitivityKey, validatedValues.DrawingSensitivity);
            PlayerPrefs.Save();
            Apply(validatedValues);
        }

        public static float GetMouseSensitivity()
        {
            return ReadValue(MouseSensitivityKey, DefaultMouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity);
        }

        public static float GetDrawingSensitivity()
        {
            return ReadValue(DrawingSensitivityKey, DefaultDrawingSensitivity, MinimumDrawingSensitivity, MaximumDrawingSensitivity);
        }

        private static void Apply(GameSettingsValues values)
        {
            AudioListener.volume = Mathf.Clamp01(values.MasterVolume / 100f);
            MouseSensitivityChanged?.Invoke(values.MouseSensitivity);
            DrawingSensitivityChanged?.Invoke(values.DrawingSensitivity);
        }

        private static GameSettingsValues Validate(GameSettingsValues values)
        {
            return new GameSettingsValues(
                ValidateValue(values.MasterVolume, DefaultMasterVolume, MinimumVolume, MaximumVolume),
                ValidateValue(values.MusicVolume, DefaultMusicVolume, MinimumVolume, MaximumVolume),
                ValidateValue(values.SoundVolume, DefaultSoundVolume, MinimumVolume, MaximumVolume),
                ValidateValue(values.MouseSensitivity, DefaultMouseSensitivity, MinimumMouseSensitivity, MaximumMouseSensitivity),
                ValidateValue(values.DrawingSensitivity, DefaultDrawingSensitivity, MinimumDrawingSensitivity, MaximumDrawingSensitivity));
        }

        private static float ReadValue(string key, float defaultValue, float minimum, float maximum)
        {
            return ValidateValue(PlayerPrefs.GetFloat(key, defaultValue), defaultValue, minimum, maximum);
        }

        private static float ValidateValue(float value, float defaultValue, float minimum, float maximum)
        {
            return float.IsNaN(value) || float.IsInfinity(value) ? defaultValue : Mathf.Clamp(value, minimum, maximum);
        }
    }
}
