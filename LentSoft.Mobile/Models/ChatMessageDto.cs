namespace LentSoft.Mobile.Models;

public class ChatMessageDto
{
    public string Sender { get; set; } = "Bot"; // "User" or "Bot"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public bool IsUser => Sender.Equals("User", StringComparison.OrdinalIgnoreCase);
    public bool IsBot => !IsUser;

    public string SenderDisplay => IsUser ? "Tú" : "Morgana AI 🤖";
    public string BackgroundColorHex => IsUser ? "#7E22CE" : "#F3E8FF";
    public string TextColorHex => IsUser ? "#FFFFFF" : "#3B0764";
    public string Alignment => IsUser ? "End" : "Start";
}
