namespace ChatBot.Models;

public record Document(
    string Id,
    string Title,
    string TitleEn,
    string Content,
    string PageUrl
);
