using ChatBot.Models;
using Microsoft.Data.Sqlite;

namespace ChatBot.Services;

/// <summary>
/// Stores complete Wikipedia articles (use for the first iteration of the search engine)
/// </summary>
public class DocumentStore
{
    private const string DbFile = "DocumentStore.db";

    static DocumentStore()
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
    CREATE TABLE IF NOT EXISTS Documents(
      Id TEXT PRIMARY KEY,
      Title TEXT,
      TitleEn TEXT,
      Content TEXT,
      PageUrl TEXT
    );
;";
        cmd.ExecuteNonQuery();
    }


    public List<Document> GetDocuments(IEnumerable<string> ids)
    {
        var idList = ids?.Distinct().ToList() ?? [];
        if (idList.Count == 0) return [];

        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();

        var paramNames = new List<string>(idList.Count);
        for (int i = 0; i < idList.Count; i++)
        {
            var p = "$p" + i;
            paramNames.Add(p);
            cmd.Parameters.AddWithValue(p, idList[i]);
        }

        // Preserve the caller's order of ids
        var orderByCase =
            "CASE Id " +
            string.Join(" ", idList.Select((id, i) => $"WHEN $p{i} THEN {i}")) +
            " END";

        cmd.CommandText = $@"
SELECT Id, Title, TitleEn, Content, PageUrl
FROM Documents
WHERE Id IN ({string.Join(", ", paramNames)})
ORDER BY {orderByCase};";

        var results = new List<Document>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new Document(
                Id: reader.GetString(0),
                Title: reader.GetString(1),
                TitleEn: reader.GetString(2),
                Content: reader.GetString(3),
                PageUrl: reader.GetString(4)
            ));
        }

        return results;
    }

    public void SaveDocument(Document document)
    {
        using var conn = new SqliteConnection($"Data Source={DbFile}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT OR REPLACE INTO Documents
(Id, Title, TitleEn, Content, PageUrl)
VALUES ($id, $title, $titleEn, $content, $pageUrl);";
        cmd.Parameters.AddWithValue("$id", document.Id);
        cmd.Parameters.AddWithValue("$title", document.Title);
        cmd.Parameters.AddWithValue("$titleEn", document.TitleEn);
        cmd.Parameters.AddWithValue("$content", document.Content);
        cmd.Parameters.AddWithValue("$pageUrl", document.PageUrl);
        cmd.ExecuteNonQuery();
    }

}