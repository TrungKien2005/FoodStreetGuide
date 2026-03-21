using Microsoft.Maui.Media;

namespace doanCSharp.Services
{
    public class AudioService
    {
        public async Task SpeakAsync(string text)
        {
            await TextToSpeech.Default.SpeakAsync(text);
        }
    }
}