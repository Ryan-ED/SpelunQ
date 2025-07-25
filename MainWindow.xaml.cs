using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SpelunQ.Models;
using SpelunQ.Services;

namespace SpelunQ;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
        private readonly ObservableCollection<RabbitMessage> _messages;
        private readonly RabbitMqService _rabbitMqService;
        private readonly FileService _fileService;
        private bool _isConnected;

        public MainWindow()
        {
            InitializeComponent();
            
            _messages = new ObservableCollection<RabbitMessage>();
            _rabbitMqService = new RabbitMqService(_messages);
            _fileService = new FileService();
            
            MessagesDataGrid.ItemsSource = _messages;
            
            // Set initial placeholder text
            UpdateSendMessagePlaceholder();
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_isConnected)
                {
                    // Disconnect
                    _rabbitMqService.Dispose();
                    _isConnected = false;
                    ConnectButton.Content = "Connect";
                    StartListeningButton.IsEnabled = false;
                    StopListeningButton.IsEnabled = false;
                    SendMessageButton.IsEnabled = false;
                
                    // Enable connection fields
                    HostTextBox.IsEnabled = true;
                    PortTextBox.IsEnabled = true;
                    UsernameTextBox.IsEnabled = true;
                    PasswordBox.IsEnabled = true;
                
                    MessageBox.Show("Disconnected from RabbitMQ", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    // Connect
                    if (!int.TryParse(PortTextBox.Text, out int port))
                    {
                        MessageBox.Show("Invalid port number", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    await _rabbitMqService.Connect(
                        HostTextBox.Text,
                        port,
                        UsernameTextBox.Text,
                        PasswordBox.Password
                    );

                    
                    _isConnected = true;
                    ConnectButton.Content = "Disconnect";
                    StartListeningButton.IsEnabled = true;
                    SendMessageButton.IsEnabled = true;
                    
                    // Disable connection fields
                    HostTextBox.IsEnabled = false;
                    PortTextBox.IsEnabled = false;
                    UsernameTextBox.IsEnabled = false;
                    PasswordBox.IsEnabled = false;
                    
                    MessageBox.Show("Connected to RabbitMQ successfully!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void StartListeningButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ListenQueueTextBox.Text))
                {
                    MessageBox.Show("Please enter a queue name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _rabbitMqService.StartListening(ListenQueueTextBox.Text);
            
                StartListeningButton.IsEnabled = false;
                StopListeningButton.IsEnabled = true;
                ListenQueueTextBox.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to start listening: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void StopListeningButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _rabbitMqService.StopListening();
            
                StartListeningButton.IsEnabled = true;  
                StopListeningButton.IsEnabled = false;
                ListenQueueTextBox.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to stop listening: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void MessagesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MessagesDataGrid.SelectedItem is RabbitMessage selectedMessage)
            {
                MessageContentTextBox.Text = selectedMessage.Content;
                SaveMessageButton.IsEnabled = true;
            }
            else
            {
                MessageContentTextBox.Text = "";
                SaveMessageButton.IsEnabled = false;
            }
        }

        private void SaveMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MessagesDataGrid.SelectedItem is not RabbitMessage selectedMessage)
                {
                    MessageBox.Show("Please select a message to save", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"message_{selectedMessage.ReceivedAt:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() != true) return;
                
                _fileService.SaveMessage(selectedMessage, saveFileDialog.FileName).RunSynchronously();
                MessageBox.Show("Message saved successfully!", "Success", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save message: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void LoadMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() != true) return;
                
                var message = await _fileService.LoadMessage(openFileDialog.FileName);
                if (message == null) return;
                    
                // Show send dialog
                var sendDialog = new SendMessageDialog(message.Content);
                if (sendDialog.ShowDialog() != true) return;
                        
                if (_isConnected && !string.IsNullOrWhiteSpace(sendDialog.QueueName))
                {
                    await _rabbitMqService.SendMessage(sendDialog.QueueName, sendDialog.MessageContent);
                    MessageBox.Show($"Message sent to queue '{sendDialog.QueueName}' successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Please connect to RabbitMQ and specify a queue name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load message: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearMessagesButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all messages?", "Confirm Clear", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            
            _messages.Clear();
            MessageContentTextBox.Text = "";
        }

        private async void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SendQueueTextBox.Text))
                {
                    MessageBox.Show("Please enter a target queue name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var messageText = SendMessageTextBox.Text;
                if (string.IsNullOrWhiteSpace(messageText) || messageText == "Enter message to send...")
                {
                    MessageBox.Show("Please enter a message to send", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _rabbitMqService.SendMessage(SendQueueTextBox.Text, messageText);
                MessageBox.Show($"Message sent to queue '{SendQueueTextBox.Text}' successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                SendMessageTextBox.Text = "";
                UpdateSendMessagePlaceholder();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SendMessageTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SendMessageTextBox.Text != "Enter message to send...") return;
            
            SendMessageTextBox.Text = "";
            SendMessageTextBox.Foreground = Brushes.Black;
        }

        private void SendMessageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SendMessageTextBox.Text))
            {
                UpdateSendMessagePlaceholder();
            }
        }

        private void UpdateSendMessagePlaceholder()
        {
            SendMessageTextBox.Text = "Enter message to send...";
            SendMessageTextBox.Foreground = Brushes.Gray;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _rabbitMqService.Dispose();
            base.OnClosing(e);
        }
    }