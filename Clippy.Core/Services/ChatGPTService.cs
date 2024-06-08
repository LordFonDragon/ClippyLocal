// Replace this part with your actual namespace and imports
using Clippy.Core.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Clippy.Core.Services
{
    public class ChatGPTService : IChatService
    {
        // Existing constants
        private const string ClippyStart = "Hi! I'm Clippy, your Windows assistant...";
        private const string Instruction = "You are in an app that revives Microsoft Clippy in Windows. Speak in a Clippy style and try to stay as concise/brief as possible and be friendly!";
        public ObservableCollection<IMessage> Messages { get; } = new ObservableCollection<IMessage>();

        private ISettingsService Settings;
        private IKeyService KeyService;
        private HttpClient httpClient;

        public ChatGPTService(ISettingsService settings, IKeyService keys)
        {
            Settings = settings;
            KeyService = keys;
            httpClient = new HttpClient();
            Add(new ClippyMessage(ClippyStart, true));
        }

        public void Refresh()
        {
            Messages.Clear();
            Add(new ClippyMessage(ClippyStart, true));
        }

        public async Task SendAsync(IMessage message) /// Send a message
        {
            Add(message); // Send user message to UI
            List<dynamic> GPTMessages = new List<dynamic>
            {
                new { role = "system", content = Instruction }
            };
            foreach (IMessage m in Messages) // Remove any editable message
            {
                if (m is ClippyMessage)
                    GPTMessages.Add(new { role = "assistant", content = m.Message });
                else if (m is UserMessage)
                    GPTMessages.Add(new { role = "user", content = m.Message });
            }
            await Task.Delay(300);
            ClippyMessage Response = new ClippyMessage(true);
            Add(Response); // Send empty message and update text later to show preview UI

            GPTMessages.Add(new { role = "user", content = message.Message });

            var json = JsonConvert.SerializeObject(new { prompt = GPTMessages.Last().content });
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var result = await httpClient.PostAsync("http://localhost:5000/generate", content);
            var responseString = await result.Content.ReadAsStringAsync();
            dynamic response = JsonConvert.DeserializeObject(responseString);

            if (result.IsSuccessStatusCode)
            {
                Response.Message = response.response;
            }
            else
            {
                Response.Message = $"Unfortunately an error occured `{response.error}`";
                Response.IsLatest = false;
            }
        }

        private void Add(IMessage Message) /// Add a message
        {
            foreach(IMessage message in Messages) /// Remove any editable message
            {
                if (message is ClippyMessage)
                    ((ClippyMessage)message).IsLatest = false;
            }
            Messages.Add(Message);
        }
    }
}
