﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
	const string CHATBOX_PREFIX = "<font=\"Minecraft Regular SDF\"><mark=#00000065>";

	/// <summary>
	/// The time it takes for a chat message to disappear in seconds
	/// </summary>
	const float CHATBOX_TIMEOUT = 6f;

	public delegate void ChatSendEventHandler(ChatSendEventArgs e, object sender);
	public event ChatSendEventHandler ChatSend;

	public TextMeshProUGUI ChatBox;
	public int MaxMessages = 100;
	public InputField ChatInput;

	private readonly List<string> _formattedMessages = new List<string>();
	private bool _open = false;
	// set this to the negative of timeout so if somehow someone starts the game within the timeout the box doesn't show
	private float _lastMessageReceived = -CHATBOX_TIMEOUT;

	private void Awake()
	{
		CheckChatVisibility();
	}

	/// <summary>
	/// Handles a chat packet from the server
	/// </summary>
	/// <param name="pkt"></param>
	public void HandleChatPacket(ChatMessagePacket pkt)
	{
		ChatMessage chatMessage;

		try
		{
			chatMessage = new ChatMessage(pkt);
		}
		catch (Exception ex)
		{
			Debug.LogWarning($"Error parsing chat message: {ex}");
			return;
		}

		AddChatMessage(chatMessage);
	}

	/// <summary>
	/// Adds a <see cref="ChatMessage"/> to the UI chat box
	/// </summary>
	/// <param name="message"></param>
	public void AddChatMessage(ChatMessage message)
	{
		Debug.Log(message.PlaintextMessage);
		_formattedMessages.Add(message.HtmlFormattedMessage);
		_lastMessageReceived = Time.time;

		RepopulateText();

		// show chat box
		CheckChatVisibility();

		// check again after the timout
		Invoke("CheckChatVisibility", CHATBOX_TIMEOUT + 0.5f);	// the extra 0.5s shouldn't be needed but I don't trust unity
	}

	private void RepopulateText()
	{
		// prune max messages
		if (_formattedMessages.Count > MaxMessages)
			_formattedMessages.RemoveRange(0, _formattedMessages.Count - MaxMessages);

		var chatText = new StringBuilder(CHATBOX_PREFIX);
		chatText.Append(string.Join("\n", _formattedMessages));

		ChatBox.text = chatText.ToString();
	}

	public void HandleInputEndEdit()
	{
		// end edit can be caused by enter or otherwise losing focus so we need to make sure the user wants to send a message
		if (Input.GetKeyDown(KeyCode.Return))
		{
			SendChatMessage();
			return;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CloseChat();
			return;
		}		
	}

	/// <summary>
	/// Opens the chat and starts typing a message
	/// </summary>
	public void OpenChat()
	{
		ChatInput.gameObject.SetActive(true);

		_open = true;

		CheckChatVisibility();
	}

	/// <summary>
	/// Ends editing the text box
	/// </summary>
	public void CloseChat()
	{
		// clear input box
		ChatInput.text = string.Empty;

		// deselect input box
		if (ChatInput.isFocused)
			EventSystem.current.SetSelectedGameObject(null);

		// hide input box
		ChatInput.gameObject.SetActive(false);

		_open = false;

		CheckChatVisibility();
	}

	/// <summary>
	/// Sends the chat message currently in the chat box
	/// </summary>
	public void SendChatMessage()
	{
		if (string.IsNullOrWhiteSpace(ChatInput.text))
			return;

		// send chat packet
		ChatSend?.Invoke(new ChatSendEventArgs() { Message = ChatInput.text }, this);

		CloseChat();	// this also clears the chat box
	}

	/// <summary>
	/// Checks to see if the chatbox needs to be hidden
	/// </summary>
	private void CheckChatVisibility()
	{
		Debug.Log("checking " + Time.time);

		// see if the chatbox needs to close
		if (Time.time - _lastMessageReceived > CHATBOX_TIMEOUT && !_open)
		{
			ChatBox.gameObject.SetActive(false);
		}
		else
		{
			ChatBox.gameObject.SetActive(true);
		}
	}

	public class ChatSendEventArgs : EventArgs
	{
		public string Message { get; set; }
	}
}
