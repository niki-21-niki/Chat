using DurakGame;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // Глобальная обработка исключений
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (s, e) => HandleException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            HandleException(e.ExceptionObject as Exception);

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Инициализация менеджера звуков
        SoundManager.SetSoundsEnabled(true);
        SoundManager.InitializeSounds();

        // Предзагрузка изображений карт
        CardImageManager.PreloadCardImages();

        Application.Run(new RegistrationForm());
    }

    private static void HandleException(Exception ex)
    {
        if (ex != null)
        {
            System.Diagnostics.Debug.WriteLine($"?? КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}\n{ex.StackTrace}");

            MessageBox.Show(
                $"Произошла критическая ошибка:\n{ex.Message}\n\n" +
                "Приложение будет закрыто. Пожалуйста, перезапустите игру.",
                "Критическая ошибка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }

        Environment.Exit(1);
    }
}