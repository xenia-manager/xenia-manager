using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace Xenia_Manager.Windows
{
    public class Change
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("release_date")]
        public DateTime ReleaseDate { get; set; }

        [JsonProperty("changes")]
        public List<string> Changes { get; set; }
    }

    /// <summary>
    /// Interaction logic for ChangelogWindow.xaml
    /// </summary>
    public partial class ChangelogWindow : Window
    {
        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        public ChangelogWindow()
        {
            InitializeComponent();
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Grabs the Changelog JSON file from the repository
        /// </summary>
        /// <param name="url">URL to hte Changelog JSON file</param>
        /// <returns>Deserialized JSON file as a List</returns>
        public static async Task<List<Change>> GetChangelogAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                var response = await client.GetStringAsync(url);
                return JsonConvert.DeserializeObject<List<Change>>(response);
            }
        }

        /// <summary>
        /// Ready the Deserialized Changelog JSON and display's it in the RichTextBox
        /// </summary>
        private async Task DisplayChangelog()
        {
            List<Change> changelog = await GetChangelogAsync("https://raw.githubusercontent.com/xenia-manager/xenia-manager/changelog/changelog.json");

            // Create a FlowDocument to hold the entire content
            FlowDocument document = new FlowDocument();

            foreach (Change entry in changelog)
            {
                // Create and format the version text
                Paragraph versionText = new Paragraph(new Run(entry.Version))
                {
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 5, 0, 15),
                };

                // Create and format the release date text
                versionText.Inlines.Add(new LineBreak());
                versionText.Inlines.Add(new Run($"Release Date: {entry.ReleaseDate.ToUniversalTime()}")
                {
                    FontSize = 12,
                    FontStyle = FontStyles.Italic
                });

                // Add the version and release date paragraphs to the document
                document.Blocks.Add(versionText);
                // Create and format the changes list
                if (entry.Changes.Count > 0)
                {
                    foreach (string change in entry.Changes)
                    {
                        Paragraph changeParagraph = new Paragraph(new Run($"   • {change}")
                        {
                            FontSize = 16
                        })
                        {
                            Margin = new Thickness(0, 0, 0, 5)
                        };
                        document.Blocks.Add(changeParagraph);
                    }
                }
                else
                {
                    Paragraph changeParagraph = new Paragraph(new Run($"   Initial Release")
                    {
                        FontSize = 16
                    })
                    {
                        Margin = new Thickness(0, 0, 0, 5)
                    };
                    document.Blocks.Add(changeParagraph);
                }

                document.Blocks.Add(new Paragraph(new LineBreak())
                {
                    Margin = new Thickness(0, 0, 0, 10)
                });
            }

            // Set the FlowDocument as the document of the RichTextBox
            ChangesRTB.Document = document;
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                });
                await DisplayChangelog();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectGame window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        // Buttons
        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }
    }
}
