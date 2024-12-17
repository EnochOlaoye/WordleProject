namespace Wordle
{
    public partial class MainPage : ContentPage
    {
        int count = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        // Event handler that triggers when the user clicks the Submit button
        private void OnSubmitGuessClicked(object sender, EventArgs e)
        {
            // Increment attempt counter
            count++;

            // Retrieve text from each Entry control in the first row
            // The '?' operator safely handles null values in the chain of operations
            // Trim() removes any whitespace before and after the text
            string letter1 = (Row1Letter1.Content as Entry)?.Text?.Trim();
            string letter2 = (Row1Letter2.Content as Entry)?.Text?.Trim();
            string letter3 = (Row1Letter3.Content as Entry)?.Text?.Trim();
            string letter4 = (Row1Letter4.Content as Entry)?.Text?.Trim();
            string letter5 = (Row1Letter5.Content as Entry)?.Text?.Trim();

            // Combine all letters into a single word
            string guess = $"{letter1}{letter2}{letter3}{letter4}{letter5}";

            // Validate that all Entry controls have a value
            // IsNullOrWhiteSpace checks for null, empty, or whitespace-only strings
            if (string.IsNullOrWhiteSpace(letter1) ||
                string.IsNullOrWhiteSpace(letter2) ||
                string.IsNullOrWhiteSpace(letter3) ||
                string.IsNullOrWhiteSpace(letter4) ||
                string.IsNullOrWhiteSpace(letter5))
            {
                // Display error message if any letter is missing
                ResultLabel.Text = "Please fill in all letters.";
            }
            else
            {
                // If all letters are present, create the guess word and display it
                string Currentguess = $"{letter1}{letter2}{letter3}{letter4}{letter5}";
                ResultLabel.Text = $"Attempt {count}: You entered '{guess}'";
            }

            // Announce the result for screen readers (accessibility feature)
            SemanticScreenReader.Announce(ResultLabel.Text);
        }

        // Event handler that triggers whenever text changes in an Entry control
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Check if the sender is an Entry control
            if (sender is Entry entry)
            {
                // Convert input to uppercase, handling null values
                // If NewTextValue is null or empty, use empty string; otherwise convert to upper case
                string input = string.IsNullOrEmpty(e.NewTextValue) ? "" : e.NewTextValue.ToUpper();

                // Validate that the first character (if any) is a letter
                // If not, clear the Entry control
                if (input.Length > 0 && !char.IsLetter(input[0]))
                {
                    entry.Text = "";
                    return;
                }
            }
        }
    }

}
