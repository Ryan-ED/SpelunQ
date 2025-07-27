using System.IO;
using System.Text.Json;
using System.Windows;
using SpelunQ.Models;

namespace SpelunQ.Services;

public class FileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task SaveMessage(RabbitMessage message, string filePath)
    {
        try
        { 
            var json = JsonSerializer.Serialize(message, Options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            // TODO: log
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
            var message = JsonSerializer.Deserialize<RabbitMessage>(json, Options);
            return message;
        }
        catch (Exception ex)
        {
            // TODO: log
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}