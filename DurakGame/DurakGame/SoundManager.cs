using System;
using System.Media;
using System.IO;
using System.Windows.Forms;

namespace DurakGame
{
    public static class SoundManager
    {
        private static SoundPlayer player = new SoundPlayer();
        private static bool soundsEnabled = true;

        public static void PlayCardPlace()
        {
            PlaySound("card_place");
        }

        public static void PlayWin()
        {
            PlaySound("win_sound");

            // ДОБАВЛЕНО: Альтернатива если файл не найден
            if (!PlaySound("win_sound"))
            {
                SystemSounds.Exclamation.Play(); // Используем системный звук
            }
        }

        public static void PlayLose()
        {
            PlaySound("lose_sound");

            // ДОБАВЛЕНО: Альтернатива если файл не найден
            if (!PlaySound("lose_sound"))
            {
                SystemSounds.Hand.Play(); // Используем системный звук
            }
        }

        public static void PlayButtonClick()
        {
            PlaySound("button_click");
        }

        private static bool PlaySound(string soundName)
        {
            if (!soundsEnabled) return false;

            try
            {
                // Сначала проверяем в папке Resources
                string soundPath = Path.Combine("Resources", soundName + ".wav");
                if (File.Exists(soundPath))
                {
                    player.SoundLocation = soundPath;
                    player.Play();
                    return true;
                }

                // Затем проверяем в корневой директории
                if (File.Exists(soundName + ".wav"))
                {
                    player.SoundLocation = soundName + ".wav";
                    player.Play();
                    return true;
                }

                // Проверяем в папке Sounds
                soundPath = Path.Combine("Sounds", soundName + ".wav");
                if (File.Exists(soundPath))
                {
                    player.SoundLocation = soundPath;
                    player.Play();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка воспроизведения звука {soundName}: {ex.Message}");
                return false;
            }
        }

        public static void SetSoundsEnabled(bool enabled)
        {
            soundsEnabled = enabled;
        }

        // ДОБАВЛЕНО: Метод для проверки доступности звуков
        public static void InitializeSounds()
        {
            // Создаем папку Resources если её нет
            string resourcesDir = "Resources";
            if (!Directory.Exists(resourcesDir))
            {
                Directory.CreateDirectory(resourcesDir);
            }

            // Создаем папку Sounds если её нет
            string soundsDir = "Sounds";
            if (!Directory.Exists(soundsDir))
            {
                Directory.CreateDirectory(soundsDir);
            }
        }
    }
}