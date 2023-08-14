namespace AuthSystem.IServices
{
    public interface IEmailService
    {
        void SendEmail(string name, string to, string  subject, string plaintext, string htmlcontent);
    }
}
