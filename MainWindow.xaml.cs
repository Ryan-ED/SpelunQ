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
        private readonly List<QueueInfo> _queues;
        private bool _isConnected;
        private bool _isListening;

        public MainWindow()
        {
            InitializeComponent();
            
            _messages = new ObservableCollection<RabbitMessage>();
            _rabbitMqService = new RabbitMqService(_messages);
            _fileService = new FileService();
            _queues = new List<QueueInfo>();
            
            MessagesDataGrid.ItemsSource = _messages;
            QueuesComboBox.ItemsSource = _queues;
            
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
                    RefreshQueuesButton.IsEnabled = false;
                    
                    // Clear queues
                    _queues.Clear();
                    QueuesComboBox.ItemsSource = null;
                    QueuesComboBox.ItemsSource = _queues;
                    SendQueueTextBox.Text = "";
                
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
                    StartListeningButton.IsEnabled = false; // Will enable after queue selection
                    RefreshQueuesButton.IsEnabled = true;
                    SendMessageButton.IsEnabled = true;
                    
                    // Disable connection fields
                    HostTextBox.IsEnabled = false;
                    PortTextBox.IsEnabled = false;
                    UsernameTextBox.IsEnabled = false;
                    PasswordBox.IsEnabled = false;
                    
                    // Load queues automatically
                    await RefreshQueues();
                    
                    MessageBox.Show("Connected to RabbitMQ successfully!", "Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        private async Task RefreshQueues()
        {
            try
            {
                var managementUrl = $"http://{HostTextBox.Text}:15672";
                var queues = await _rabbitMqService.GetQueuesAsync(managementUrl, UsernameTextBox.Text, PasswordBox.Password);
                
                _queues.Clear();
                foreach (var queue in queues)
                {
                    _queues.Add(queue);
                }
                
                // Refresh the ComboBox
                QueuesComboBox.ItemsSource = null;
                QueuesComboBox.ItemsSource = _queues;
                
                if (_queues.Count == 0)
                {
                    MessageBox.Show("No queues found. Make sure RabbitMQ Management plugin is enabled.", 
                        "Queue Discovery", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to refresh queues: {ex.Message}\n\nNote: This requires RabbitMQ Management plugin to be enabled.", 
                    "Queue Refresh Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        private async void RefreshQueuesButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshQueues();
        }
        
        private void QueuesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StartListeningButton.IsEnabled = QueuesComboBox.SelectedItem != null && _isConnected && !_isListening;
            
            // Update send queue when selection changes
            if (QueuesComboBox.SelectedItem is QueueInfo selectedQueue)
            {
                SendQueueTextBox.Text = selectedQueue.Name;
                SendMessageButton.IsEnabled = _isConnected;
            }
            else
            {
                SendQueueTextBox.Text = "";
                SendMessageButton.IsEnabled = false;
            }
        }

        private async void StartListeningButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(QueuesComboBox.Text))
                {
                    MessageBox.Show("Please enter a queue name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                await _rabbitMqService.StartListening(QueuesComboBox.Text);
            
                StartListeningButton.IsEnabled = false;
                StopListeningButton.IsEnabled = true;
                QueuesComboBox.IsEnabled = false;
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
                QueuesComboBox.IsEnabled = true;
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

        private async void SaveMessageButton_Click(object sender, RoutedEventArgs e)
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
                
                await _fileService.SaveMessage(selectedMessage, saveFileDialog.FileName);
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
                
                // Refresh queue info to show updated message count
                _ = RefreshQueues();
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