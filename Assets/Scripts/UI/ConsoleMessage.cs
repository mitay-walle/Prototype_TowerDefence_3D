namespace TD
{
    public class ConsoleMessage
    {
        public string text { get; private set; }
        public float timeRemaining { get; set; }

        public ConsoleMessage(string text, float duration)
        {
            this.text = text;
            this.timeRemaining = duration;
        }
    }
}
