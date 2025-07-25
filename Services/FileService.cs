using System.IO;
using System.Text.Json;
using System.Windows;
using SpelunQ_wpf.Models;

namespace SpelunQ_wpf.Services;

public class FileService
{
    public async Task SaveMessage(RabbitMessage message, string filePath)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
                
            var json = JsonSerializer.Serialize(message, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }

    public async Task<RabbitMessage?> LoadMessage(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                MessageBox.Show("File does not exist", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath);
            var message = JsonSerializer.Deserialize<RabbitMessage>(json);
            return message;
        }
        catch (Exception ex)
        {
            // log
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}