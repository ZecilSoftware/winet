using Azure.AI.OpenAI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Net;

namespace Dalle3_CSharp_Advent
{
    public sealed partial class MainWindow : Window
    {
        private const string OPENAI_KEY = "";
        private const string SAVE_FOLDER = "Holiday DALLE";
        private Uri _currentImage;
        private string _currentPrompt;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            WorkingState();
            var folder = await GetPicturesFolder();
            var destination = Path.Combine(folder, SAVE_FOLDER, $"{HumanPrompt.Text}.png");
            var destination2 = Path.Combine(folder, SAVE_FOLDER, $"{HumanPrompt.Text}.txt");

            using (var client = new WebClient())
            {
                client.DownloadFile(_currentImage, destination);
            }

            using (StreamWriter outputFile = new StreamWriter(destination2, false))
            {
                outputFile.WriteLine(_currentPrompt);
            }

            SaveNotification.Subtitle = destination;
            SaveNotification.IsOpen = true;
            FinishedState();
        }

        private async void GenerateImage_Click(object sender, RoutedEventArgs e)
        {
            GeneratedImage.Source = null;
            WorkingState();

            try
            {
                string selectedHoliday = GetSelectedHoliday();
                _currentPrompt = await GeneratePrompt(HumanPrompt.Text, selectedHoliday);

                ShowPrompt(_currentPrompt);
                var image = await GenerateImage(_currentPrompt);
                HidePrompt();

                GeneratedImage.Source = image;
                Save.IsEnabled = true;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("OpenAI API key"))
            {
                // Show user-friendly error message
                SaveNotification.Title = "Configuration Error";
                SaveNotification.Subtitle = "Please configure your OpenAI API key in the code before using this feature.";
                SaveNotification.IsOpen = true;
                HidePrompt();
            }
            catch (Exception ex)
            {
                // Handle other potential errors gracefully
                SaveNotification.Title = "Error";
                SaveNotification.Subtitle = $"An error occurred: {ex.Message}";
                SaveNotification.IsOpen = true;
                HidePrompt();
            }

            FinishedState();
        }

        private static async Task<string> GeneratePrompt(string userPrompt, string holiday)
        {
            if (string.IsNullOrWhiteSpace(OPENAI_KEY))
            {
                throw new InvalidOperationException("OpenAI API key is not configured. Please add your API key to the OPENAI_KEY constant.");
            }

            OpenAIClient client = new(OPENAI_KEY);

            string systemMessage = GetHolidaySystemMessage(holiday);

            var responseCompletion = await client.GetChatCompletionsAsync(
                new ChatCompletionsOptions()
                {
                    ChoiceCount = 1,
                    Temperature = 1,
                    MaxTokens = 256,                    
                    DeploymentName = "gpt-4",
                    Messages = {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage(userPrompt),
                    },
                });

            return responseCompletion.Value.Choices[0].Message.Content;
        }

        private async Task<BitmapImage> GenerateImage(String prompt)
        {
            if (string.IsNullOrWhiteSpace(OPENAI_KEY))
            {
                throw new InvalidOperationException("OpenAI API key is not configured. Please add your API key to the OPENAI_KEY constant.");
            }

            OpenAIClient client = new(OPENAI_KEY);

            var responseImages = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    ImageCount = 1,
                    Prompt = prompt,
                    Size = ImageSize.Size1792x1024,
                    DeploymentName = "dall-e-3"
                });

            _currentImage = responseImages.Value.Data[0].Url;
            return new BitmapImage(_currentImage);
        }

        private void ShowPrompt(string prompt)
        {
            GeneratedPrompt.Text = prompt;
            GeneratedPrompt.Visibility = Visibility.Visible;
        }

        private void HidePrompt()
        {
            GeneratedPrompt.Visibility = Visibility.Collapsed;
        }

        private void WorkingState()
        {
            Save.IsEnabled = false;
            Generate.IsEnabled = false;
            ProgressIndicator.IsActive = true;
        }

        private void FinishedState()
        {
            ProgressIndicator.IsActive = false;
            Generate.IsEnabled = true;
        }

        private string GetSelectedHoliday()
        {
            if (HolidaySelector.SelectedItem is ComboBoxItem selectedItem)
            {
                return selectedItem.Content.ToString();
            }
            return "Christmas"; // Default fallback
        }

        private static string GetHolidaySystemMessage(string holiday)
        {
            return holiday switch
            {
                "Christmas" => "Create a prompt for Dall-e that will generate a beautiful Christmas scene using the following text for inspiration:",
                "Easter" => "Create a prompt for Dall-e that will generate a beautiful Easter scene with spring elements, Easter eggs, bunnies, and pastel colors using the following text for inspiration:",
                "Valentine's Day" => "Create a prompt for Dall-e that will generate a beautiful Valentine's Day scene with romantic elements, hearts, roses, and warm colors using the following text for inspiration:",
                "Halloween" => "Create a prompt for Dall-e that will generate a beautiful Halloween scene with spooky elements, pumpkins, autumn colors, and festive decorations using the following text for inspiration:",
                "Fourth of July" => "Create a prompt for Dall-e that will generate a beautiful Fourth of July scene with patriotic elements, fireworks, red white and blue colors, and American symbols using the following text for inspiration:",
                _ => "Create a prompt for Dall-e that will generate a beautiful holiday scene using the following text for inspiration:"
            };
        }

        private static async Task<string> GetPicturesFolder()
        {
            var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            Directory.CreateDirectory(Path.Combine(myPictures.SaveFolder.Path, SAVE_FOLDER));
            return myPictures.SaveFolder.Path;
        }
    }
}