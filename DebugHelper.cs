using System.IO;

namespace NKhinsider
{
    public static class DebugHelper
    {
        public static void SaveHtmlForDebugging(string html, string filename)
        {
#if DEBUG
            try
            {
                var debugPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KhinsiderDebug");
                Directory.CreateDirectory(debugPath);
                var filePath = Path.Combine(debugPath, $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.html");
                File.WriteAllText(filePath, html);
            }
            catch
            {
                // Ignore debug save errors
            }
#endif
        }

        public static void LogElementsFound(HtmlAgilityPack.HtmlDocument doc, string selector, string elementType)
        {
#if DEBUG
            try
            {
                var nodes = doc.DocumentNode.SelectNodes(selector);
                var count = nodes?.Count ?? 0;
                System.Diagnostics.Debug.WriteLine($"Found {count} {elementType} elements with selector: {selector}");
                
                if (nodes != null && count > 0 && count <= 20) // Show more for debugging
                {
                    foreach (var node in nodes.Take(10)) // Show first 10
                    {
                        var text = node.InnerText?.Trim();
                        var href = node.GetAttributeValue("href", "");
                        
                        if (!string.IsNullOrEmpty(text))
                        {
                            if (text.Length > 80)
                                text = text.Substring(0, 80) + "...";
                            
                            var logText = $"  - {text}";
                            if (!string.IsNullOrEmpty(href))
                                logText += $" (href: {href})";
                                
                            System.Diagnostics.Debug.WriteLine(logText);
                        }
                    }
                }
            }
            catch
            {
                // Ignore debug logging errors
            }
#endif
        }

        public static void LogPageStructure(HtmlAgilityPack.HtmlDocument doc)
        {
#if DEBUG
            try
            {
                System.Diagnostics.Debug.WriteLine("=== PAGE STRUCTURE ANALYSIS ===");
                
                // Look for tables
                var tables = doc.DocumentNode.SelectNodes("//table");
                System.Diagnostics.Debug.WriteLine($"Total tables found: {tables?.Count ?? 0}");
                
                if (tables != null)
                {
                    for (int i = 0; i < Math.Min(tables.Count, 3); i++)
                    {
                        var table = tables[i];
                        var id = table.GetAttributeValue("id", "");
                        var className = table.GetAttributeValue("class", "");
                        var rowCount = table.SelectNodes(".//tr")?.Count ?? 0;
                        
                        System.Diagnostics.Debug.WriteLine($"Table {i + 1}: id='{id}', class='{className}', rows={rowCount}");
                    }
                }
                
                // Look for song-related links
                var songLinks = doc.DocumentNode.SelectNodes("//a[contains(@href, 'song')]");
                System.Diagnostics.Debug.WriteLine($"Song-related links: {songLinks?.Count ?? 0}");
                
                // Look for common track indicators
                var trackIndicators = new[] { "track", "song", "music", "audio", "mp3" };
                foreach (var indicator in trackIndicators)
                {
                    var elements = doc.DocumentNode.SelectNodes($"//*[contains(text(), '{indicator}')]");
                    if (elements != null && elements.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Elements containing '{indicator}': {elements.Count}");
                    }
                }
                
                System.Diagnostics.Debug.WriteLine("================================");
            }
            catch
            {
                // Ignore debug logging errors
            }
#endif
        }
    }
} 