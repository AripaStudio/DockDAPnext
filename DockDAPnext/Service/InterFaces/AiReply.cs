namespace DockDAPnext.Service.InterFaces
{
    // Raw outcome of one AI request: either reply text or an error message.
    public class AiReply
    {
        public bool Success { get; set; }

        public string Text { get; set; } = string.Empty;

        public string Error { get; set; } = string.Empty;
    }
}
