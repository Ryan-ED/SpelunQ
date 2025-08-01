﻿using System.Windows;

namespace SpelunQ;

public partial class SendMessageDialog
{
    public string QueueName => QueueNameTextBox.Text;
    public string MessageContent => MessageContentTextBox.Text;

    public SendMessageDialog(string messageContent = "", string queueName = "")
    {
        InitializeComponent();
        MessageContentTextBox.Text = messageContent;
        QueueNameTextBox.Text = queueName;
        MessageContentTextBox.Focus();
    }

    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(QueueNameTextBox.Text))
        {
            MessageBox.Show("Please enter a queue name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            QueueNameTextBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageContentTextBox.Text))
        {
            MessageBox.Show("Please enter message content", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            MessageContentTextBox.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}